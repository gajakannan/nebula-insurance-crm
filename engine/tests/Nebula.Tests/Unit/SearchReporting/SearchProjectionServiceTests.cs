using Microsoft.EntityFrameworkCore;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;
using Nebula.Infrastructure.Services;
using Shouldly;

namespace Nebula.Tests.Unit.SearchReporting;

public class SearchProjectionServiceTests
{
    [Fact]
    public async Task BackfillAsync_IndexesPolicyNumberWithAccountRegionAndProducerOwner()
    {
        await using var db = NewDb();
        var now = DateTime.UtcNow;
        var actorId = Guid.NewGuid();
        var producer = new UserProfile
        {
            IdpIssuer = "test",
            IdpSubject = "producer-001",
            Email = "producer@example.local",
            DisplayName = "Pat Producer",
            Department = "Underwriting",
            RolesJson = "[\"Underwriter\"]",
            RegionsJson = "[\"West\"]",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var account = new Account
        {
            Name = "Beacon Hardware",
            StableDisplayName = "Beacon Hardware",
            Status = AccountStatuses.Active,
            Region = "West",
            PrimaryState = "CA",
            PrimaryProducerUserId = producer.Id,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actorId,
            UpdatedByUserId = actorId,
        };
        var broker = new Broker
        {
            LegalName = "Beacon Brokerage",
            LicenseNumber = "LIC-1",
            State = "CA",
            Status = "Active",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actorId,
            UpdatedByUserId = actorId,
        };
        var carrier = new CarrierRef
        {
            Name = "Nebula Mutual",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actorId,
            UpdatedByUserId = actorId,
        };
        var policy = new Policy
        {
            PolicyNumber = "NEB-GL-2026-000001",
            AccountId = account.Id,
            BrokerId = broker.Id,
            CarrierId = carrier.Id,
            LineOfBusiness = "GeneralLiability",
            EffectiveDate = now.Date,
            ExpirationDate = now.Date.AddYears(1),
            TotalPremium = 12000m,
            CurrentStatus = PolicyStatuses.Issued,
            ImportSource = "manual",
            AccountDisplayNameAtLink = account.StableDisplayName,
            AccountStatusAtRead = account.Status,
            ProducerUserId = producer.Id,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = actorId,
            UpdatedByUserId = actorId,
        };

        db.AddRange(producer, account, broker, carrier, policy);
        await db.SaveChangesAsync();

        var service = new SearchProjectionService(
            db,
            new SearchDocumentRepository(db),
            new OperationalReportProjectionRepository(db));

        var result = await service.BackfillAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        result.Errors.ShouldBe(0);
        var doc = await db.SearchDocuments.SingleAsync(d => d.ObjectType == "Policy");
        doc.Title.ShouldBe(policy.PolicyNumber);
        doc.SearchText.ShouldContain(policy.PolicyNumber);
        doc.Region.ShouldBe(account.Region);
        doc.OwnerUserId.ShouldBe(producer.Id);
        doc.OwnerDisplayName.ShouldBe(producer.DisplayName);
    }

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"search-projection-{Guid.NewGuid():N}")
            .Options;
        return new AppDbContext(options);
    }
}
