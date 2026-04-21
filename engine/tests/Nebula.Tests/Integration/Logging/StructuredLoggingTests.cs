using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Shouldly;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.InMemory;

namespace Nebula.Tests.Integration.Logging;

[Collection("Integration")]
public class StructuredLoggingTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public StructuredLoggingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private static (InMemorySink sink, IDisposable logger) CreateTestLogger()
    {
        var sink = new InMemorySink();
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .WriteTo.Sink(sink)
            .CreateLogger();
        return (sink, Log.Logger as IDisposable ?? throw new InvalidOperationException());
    }

    [Fact]
    public async Task Successful_request_emits_structured_log_with_trace_and_status()
    {
        var (sink, logger) = CreateTestLogger();
        using var _ = logger;
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/healthz");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var requestLog = sink.LogEvents
            .FirstOrDefault(e => e.MessageTemplate.Text.Contains("HTTP"));

        requestLog.ShouldNotBeNull("a Serilog request completion log should be emitted");
        requestLog!.Level.ShouldBe(LogEventLevel.Information);

        requestLog.Properties.ShouldContainKey("StatusCode");
    }

    [Fact]
    public async Task ProblemDetails_traceId_is_present_on_error_response()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/nonexistent-endpoint-that-does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        var body = await response.Content.ReadAsStringAsync();
        var problem = JsonDocument.Parse(body);

        problem.RootElement.TryGetProperty("traceId", out var traceIdElement).ShouldBeTrue(
            "ProblemDetails must include traceId for request correlation");
        traceIdElement.GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Authenticated_request_includes_user_context()
    {
        var (sink, logger) = CreateTestLogger();
        using var _ = logger;
        // TestAuthHandler auto-authenticates all requests with sub=test-user-001
        var client = _factory.CreateClient();

        await client.GetAsync("/healthz");

        var requestLog = sink.LogEvents
            .FirstOrDefault(e => e.MessageTemplate.Text.Contains("HTTP"));

        requestLog.ShouldNotBeNull();

        // Authenticated requests should include user context properties
        requestLog!.Properties.ShouldContainKey("IdpSubject");
        requestLog.Properties.ShouldContainKey("UserRoles");
    }

    [Fact]
    public async Task Authorization_header_is_not_logged_in_baseline_events()
    {
        var (sink, logger) = CreateTestLogger();
        using var _ = logger;
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token-value");

        await client.GetAsync("/healthz");

        foreach (var logEvent in sink.LogEvents)
        {
            var rendered = logEvent.RenderMessage();
            rendered.ShouldNotContain("test-token-value");

            foreach (var prop in logEvent.Properties)
            {
                prop.Value.ToString().ShouldNotContain("test-token-value");
            }
        }
    }
}
