using Serilog.Context;
using System.Diagnostics;

namespace Nebula.Api.Logging;

public sealed class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var traceId = LogContext.PushProperty(
            "TraceId",
            Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier);
        using var requestPath = LogContext.PushProperty("RequestPath", context.Request.Path.Value ?? "/");
        using var requestMethod = LogContext.PushProperty("RequestMethod", context.Request.Method);

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var subject = context.User.FindFirst("sub")?.Value;
            var roles = context.User.FindAll("nebula_roles").Select(c => c.Value).ToArray();

            using var userId = LogContext.PushProperty("IdpSubject", subject ?? string.Empty);
            using var userRoles = LogContext.PushProperty("UserRoles", roles, destructureObjects: true);
            await _next(context);
            return;
        }

        await _next(context);
    }
}
