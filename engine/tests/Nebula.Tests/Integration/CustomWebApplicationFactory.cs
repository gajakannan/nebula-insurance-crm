using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Nebula.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set the connection string BEFORE services are configured so health checks don't fail
        builder.UseSetting("ConnectionStrings:DefaultConnection", _postgres.GetConnectionString());
        builder.UseSetting("RateLimiting:AuthenticatedPermitLimit", "1000000");
        builder.UseSetting("RateLimiting:AnonymousPermitLimit", "1000000");
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            // Use Testcontainers PostgreSQL
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace authentication with test scheme
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", _ => { });
        });
    }

    /// <summary>
    /// Sets BrokerTenantId on an existing broker (F0009 test helper — bypasses the API).
    /// </summary>
    public async Task SetBrokerTenantIdAsync(Guid brokerId, string brokerTenantId)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var broker = await db.Brokers.FindAsync(brokerId);
        if (broker is not null)
        {
            broker.BrokerTenantId = brokerTenantId;
            await db.SaveChangesAsync();
        }
    }

    public new async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    async Task IAsyncLifetime.InitializeAsync() => await InitializeAsync();
    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();
}
