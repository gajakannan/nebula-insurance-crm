using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using FluentValidation;
using Serilog;
using Serilog.Events;
using Nebula.Infrastructure;
using Nebula.Infrastructure.Persistence;
using Nebula.Application.Common;
using Nebula.Application.Services;
using Nebula.Application.Validators;
using Nebula.Api.Endpoints;
using Nebula.Api.Logging;
using Nebula.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var configuredDocumentRoot = builder.Configuration["Documents:RootPath"];
if (!string.IsNullOrWhiteSpace(configuredDocumentRoot))
{
    var resolvedDocumentRoot = Path.IsPathRooted(configuredDocumentRoot)
        ? configuredDocumentRoot
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, configuredDocumentRoot));
    Environment.SetEnvironmentVariable("NEBULA_DOCUMENT_ROOT", resolvedDocumentRoot);
}

// Serilog — structured logging baseline (F0033-S0001)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Authentication — authentik OIDC JWT (F0005)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Dev mode: accept any JWT without contacting authentik.
            // dev-auth.ts crafts a local token with iss/sub matching the seeded UserProfiles.
            // Must use JwtSecurityTokenHandler (not the .NET 8+ default JsonWebTokenHandler)
            // because only JwtSecurityTokenHandler respects the SignatureValidator delegate.
            var devHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            options.TokenHandlers.Clear();
            options.TokenHandlers.Add(devHandler);
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                SignatureValidator = (token, _) => devHandler.ReadJwtToken(token),
            };
        }
        else
        {
            var audience = builder.Configuration["Authentication:Audience"]
                           ?? throw new InvalidOperationException(
                               "Authentication:Audience configuration value is required.");

            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = audience;
            options.RequireHttpsMetadata = true;

            // F-003 (F0009 Implementation Contract §5): explicitly enforce aud claim validation.
            // The JWT middleware must reject tokens where aud != Authentication:Audience before
            // any endpoint handler runs. Setting ValidateAudience here makes the intent
            // unambiguous and guards against any future framework default change.
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudiences = [audience],
            };
        }
    });
builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"] ?? "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Rate Limiting
var authRateLimit = builder.Configuration.GetValue("RateLimiting:AuthenticatedPermitLimit", 100);
var anonRateLimit = builder.Configuration.GetValue("RateLimiting:AnonymousPermitLimit", 20);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("authenticated", opt =>
    {
        opt.PermitLimit = authRateLimit;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("anonymous", opt =>
    {
        opt.PermitLimit = anonRateLimit;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// OpenAPI
builder.Services.AddOpenApi();

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection") ?? "");

// HttpClient for external service calls (authentik revocation, etc.)
builder.Services.AddHttpClient("AuthentikRevocation");

// Infrastructure (repositories, authorization, caching)
builder.Services.AddInfrastructure();

// Application services
builder.Services.AddScoped<BrokerService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<AccountContactService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<SubmissionService>();
builder.Services.AddScoped<PolicyService>();
builder.Services.AddScoped<RenewalService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TimelineService>();
builder.Services.AddScoped<ReferenceDataService>();
builder.Services.AddScoped<BrokerScopeResolver>();

// Current user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpCurrentUserService>();

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<BrokerCreateValidator>();

var app = builder.Build();

// Apply pending migrations and seed dev data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (app.Environment.IsDevelopment())
        await DevSeedData.SeedDevDataAsync(db);
}

// Global exception handler — RFC 7807 ProblemDetails
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        context.Response.ContentType = "application/problem+json";
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        // F0009 §6.1: BrokerUser scope unresolvable → 403 with discriminator code.
        // This is NOT a session teardown trigger — the JWT is valid.
        if (exception is BrokerScopeUnresolvableException)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "https://nebula.local/problems/broker-scope-unresolvable",
                Title = "Broker scope could not be resolved.",
                Status = StatusCodes.Status403Forbidden,
                Extensions =
                {
                    ["code"] = "broker_scope_unresolvable",
                    ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier,
                }
            });
            return;
        }

        var statusCode = StatusCodes.Status500InternalServerError;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = "An unexpected error occurred",
            Type = "https://docs.nebula.com/errors/internal-error",
            Extensions =
            {
                ["code"] = "internal_error",
                ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier,
            }
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    });
});

app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    if (response.ContentType is null or "")
    {
        response.ContentType = "application/problem+json";
        var (title, code) = response.StatusCode switch
        {
            401 => ("Unauthorized", "unauthorized"),
            403 => ("Forbidden", "forbidden"),
            404 => ("Not Found", "not_found"),
            429 => ("Too Many Requests", "rate_limited"),
            _ => ("Error", "error")
        };
        var problem = new ProblemDetails
        {
            Status = response.StatusCode,
            Title = title,
            Extensions =
            {
                ["code"] = code,
                ["traceId"] = System.Diagnostics.Activity.Current?.Id ?? statusCodeContext.HttpContext.TraceIdentifier,
            }
        };
        await response.WriteAsJsonAsync(problem);
    }
});

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Serilog request context enrichment and completion logging (F0033-S0001)
app.UseMiddleware<RequestLogContextMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", System.Diagnostics.Activity.Current?.TraceId.ToString() ?? string.Empty);
        diagnosticContext.Set("StatusCode", httpContext.Response.StatusCode);
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value ?? string.Empty);
    };
});

app.UseRateLimiter();

// OpenAPI/Swagger in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Health endpoint
app.MapHealthChecks("/healthz").AllowAnonymous();

// API endpoints
app.MapAuthEndpoints();
app.MapBrokerEndpoints();
app.MapAccountEndpoints();
app.MapContactEndpoints();
app.MapReferenceDataEndpoints();
app.MapSubmissionEndpoints();
app.MapPolicyEndpoints();
app.MapRenewalEndpoints();
app.MapDocumentEndpoints();
app.MapDashboardEndpoints();
app.MapTaskEndpoints();
app.MapUserEndpoints();
app.MapTimelineEndpoints();

app.Run();

// Required for WebApplicationFactory integration tests
public partial class Program { }
