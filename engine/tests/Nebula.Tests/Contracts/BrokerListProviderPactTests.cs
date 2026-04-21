using PactNet;
using PactNet.Verifier;
using Nebula.Tests.Integration;

namespace Nebula.Tests.Contracts;

[Collection("Integration")]
public sealed class BrokerListProviderPactTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BrokerListProviderPactTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact(Skip = "Run after consumer pact file is generated via: pnpm --dir experience test:contracts")]
    public void VerifyBrokerListContract()
    {
        var pactPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..",
            "experience", "pacts", "nebula-experience-nebula-api.json");

        if (!File.Exists(pactPath))
        {
            throw new FileNotFoundException(
                $"Pact file not found at {pactPath}. Run consumer tests first: pnpm --dir experience test:contracts");
        }

        using var pactVerifier = new PactVerifier("nebula-api");
        pactVerifier
            .WithHttpEndpoint(_factory.Server.BaseAddress!)
            .WithFileSource(new FileInfo(pactPath))
            .Verify();
    }
}
