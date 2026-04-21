using System.Text.Json;
using Shouldly;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nebula.Api.Helpers;

namespace Nebula.Tests.Unit;

public class ProblemDetailsHelperTests
{
    [Theory]
    [InlineData("active_dependencies_exist")]
    [InlineData("concurrency_conflict")]
    [InlineData("invalid_transition")]
    public async Task Helpers_EmitExpectedProblemCodes(string code)
    {
        var result = code switch
        {
            "active_dependencies_exist" => ProblemDetailsHelper.ActiveDependenciesExist(),
            "concurrency_conflict" => ProblemDetailsHelper.ConcurrencyConflict(),
            "invalid_transition" => ProblemDetailsHelper.InvalidTransition("Received", "Binding"),
            _ => throw new InvalidOperationException($"Unknown code {code}"),
        };

        var (statusCode, payload) = await ExecuteAsync(result);

        statusCode.ShouldBe(StatusCodes.Status409Conflict);
        payload.GetProperty("code").GetString().ShouldBe(code);
    }

    [Fact]
    public async Task ValidationError_EmitsErrorsPayload()
    {
        var result = ProblemDetailsHelper.ValidationError(new Dictionary<string, string[]>
        {
            ["toState"] = ["'To State' must not be empty."],
        });

        var (statusCode, payload) = await ExecuteAsync(result);

        statusCode.ShouldBe(StatusCodes.Status400BadRequest);
        payload.GetProperty("code").GetString().ShouldBe("validation_error");
        payload.GetProperty("errors").GetProperty("toState")[0].GetString()
            .ShouldBe("'To State' must not be empty.");
    }

    private static async Task<(int StatusCode, JsonElement Payload)> ExecuteAsync(IResult result)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddProblemDetails()
            .BuildServiceProvider();
        httpContext.Response.Body = new MemoryStream();

        await result.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        return (httpContext.Response.StatusCode, document.RootElement.Clone());
    }
}
