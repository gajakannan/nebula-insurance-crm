using Microsoft.EntityFrameworkCore;
using System.Text;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;
using Nebula.Infrastructure.Repositories;
using Shouldly;

namespace Nebula.Tests.Unit.Billing;

public class BillingReconciliationServiceTests
{
    [Fact]
    public async Task Create_search_get_and_summarize_invoice_preserve_policy_context()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var service = NewService(db);
        var user = User(fixture.UserId);

        var (created, error) = await service.CreateInvoiceAsync(
            new BillingInvoiceCreateRequestDto(
                " inv-200 ", fixture.PolicyId, fixture.PolicyVersionId, fixture.AccountId, "USD", 400m,
                new DateOnly(2026, 7, 19), new DateOnly(2026, 8, 19)),
            user);

        error.ShouldBeNull();
        created.ShouldNotBeNull();
        created!.InvoiceNumber.ShouldBe("inv-200");
        (await service.GetInvoiceAsync(created.Id, user)).ShouldNotBeNull();
        var search = await service.SearchInvoicesAsync(
            new BillingInvoiceSearchQuery(null, fixture.PolicyId, fixture.AccountId, "Outstanding", false), user);
        search.TotalCount.ShouldBe(1);
        var summary = await service.GetPolicyBillingSummaryAsync(fixture.PolicyId, user);
        summary.ShouldNotBeNull();
        summary!.OutstandingAmount.ShouldBe(400m);

        var duplicate = await service.CreateInvoiceAsync(
            new BillingInvoiceCreateRequestDto(
                "INV-200", fixture.PolicyId, fixture.PolicyVersionId, fixture.AccountId, "USD", 400m,
                new DateOnly(2026, 7, 19), new DateOnly(2026, 8, 19)),
            user);
        duplicate.Error.ShouldBe("invoice_number_conflict");
    }

    [Fact]
    public async Task Manual_receipt_can_be_searched_and_duplicate_source_reference_is_rejected()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var service = NewService(db);
        var user = User(fixture.UserId);
        var request = new PaymentReceiptCreateRequestDto(
            " pay-200 ", new DateOnly(2026, 7, 19), "USD", 400m, " INV-200 ", " Bounded evidence ");

        var (created, error) = await service.CreateManualReceiptAsync(request, user);

        error.ShouldBeNull();
        created.ShouldNotBeNull();
        created!.ExternalReference.ShouldBe("pay-200");
        created.InvoiceReference.ShouldBe("INV-200");
        var search = await service.SearchReceiptsAsync(
            new PaymentReceiptSearchQuery("Unapplied", "PAY-200", "USD"), user);
        search.TotalCount.ShouldBe(1);
        (await service.CreateManualReceiptAsync(request, user)).Error.ShouldBe("receipt_reference_conflict");
    }

    [Fact]
    public async Task Mock_csv_import_records_created_duplicate_and_rejected_rows_without_retaining_bytes()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var service = NewService(db);
        var csv = string.Join('\n',
            "externalReference,receivedDate,currency,amount,invoiceReference,memo",
            "PAY-CSV-1,2026-07-19,USD,125.50,INV-200,First row",
            "PAY-CSV-1,2026-07-19,USD,125.50,INV-200,Duplicate row",
            "PAY-CSV-2,2026-07-19,USD,-1,INV-200,Invalid amount",
            "\"PAY-BROKEN,2026-07-19,USD,10,,Malformed quote");
        var bytes = Encoding.UTF8.GetBytes(csv);
        await using var stream = new MemoryStream(bytes);

        var (result, error) = await service.ImportMockReceiptsAsync(
            stream, "../mock-receipts.csv", bytes.Length, User(fixture.UserId));

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.CreatedCount.ShouldBe(1);
        result.DuplicateCount.ShouldBe(1);
        result.RejectedCount.ShouldBe(2);
        result.FileName.ShouldBe("mock-receipts.csv");
        result.FileSha256.Length.ShouldBe(64);
        (await db.ReconciliationExceptions.CountAsync()).ShouldBe(3);
        var fetched = await service.GetImportAsync(result.ImportBatchId, User(fixture.UserId));
        fetched.ShouldNotBeNull();
        fetched!.Outcomes.Count.ShouldBe(4);
        var exceptions = await service.SearchExceptionsAsync(
            new ReconciliationExceptionSearchQuery("Open", null), User(fixture.UserId));
        exceptions.TotalCount.ShouldBe(3);
    }

    [Fact]
    public async Task Mock_csv_import_rejects_invalid_utf8_without_persisting_a_batch()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        await using var stream = new MemoryStream([0xC3, 0x28]);

        var (result, error) = await NewService(db).ImportMockReceiptsAsync(
            stream, "invalid.csv", stream.Length, User(fixture.UserId));

        result.ShouldBeNull();
        error.ShouldBe("invalid_encoding");
        (await db.PaymentReceiptImportBatches.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Create_invoice_rejects_account_or_currency_outside_policy_version_context()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var service = NewService(db);
        var user = User(fixture.UserId);

        var (result, error) = await service.CreateInvoiceAsync(
            new BillingInvoiceCreateRequestDto(
                "INV-100", fixture.PolicyId, fixture.PolicyVersionId, Guid.NewGuid(), "USD", 250m,
                new DateOnly(2026, 7, 19), new DateOnly(2026, 8, 19)),
            user);

        result.ShouldBeNull();
        error.ShouldBe("policy_context_mismatch");
        (await db.BillingInvoices.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Create_invoice_authorizes_policy_context_before_returning_global_number_conflict()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var existing = Invoice(fixture, 250m);
        existing.InvoiceNumber = "INV-GLOBAL";
        existing.NormalizedInvoiceNumber = "INV-GLOBAL";
        db.Add(existing);
        await db.SaveChangesAsync();

        var (result, error) = await NewService(db).CreateInvoiceAsync(
            new BillingInvoiceCreateRequestDto(
                "INV-GLOBAL", fixture.PolicyId, fixture.PolicyVersionId, fixture.AccountId, "USD", 250m,
                new DateOnly(2026, 7, 19), new DateOnly(2026, 8, 19)),
            new ScopedTestUser(Guid.NewGuid(), [], []));

        result.ShouldBeNull();
        error.ShouldBe("policy_context_not_found");
    }

    [Fact]
    public async Task Exact_application_atomically_reconciles_invoice_and_receipt()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var receipt = Receipt(fixture.UserId, 250m, "USD");
        db.AddRange(invoice, receipt);
        await db.SaveChangesAsync();
        var service = NewService(db);

        var (result, error) = await service.ApplyExactPaymentAsync(
            new PaymentApplicationRequestDto(invoice.Id, receipt.Id, receipt.RowVersion.ToString()),
            invoice.RowVersion,
            User(fixture.UserId));

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.InvoiceOutstandingAfter.ShouldBe(0m);
        db.ChangeTracker.Clear();
        var storedInvoice = await db.BillingInvoices.SingleAsync(x => x.Id == invoice.Id);
        var storedReceipt = await db.PaymentReceipts.SingleAsync(x => x.Id == receipt.Id);
        storedInvoice.OutstandingAmount.ShouldBe(0m);
        storedInvoice.Status.ShouldBe("Reconciled");
        storedReceipt.ApplicationStatus.ShouldBe("Applied");
        (await db.PaymentApplications.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Invoice_detail_and_backlog_reload_exact_application_pending_correction_and_audit_context()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var receipt = Receipt(fixture.UserId, 250m, "USD");
        receipt.ApplicationStatus = "Applied";
        var application = new PaymentApplication
        {
            BillingInvoice = invoice,
            BillingInvoiceId = invoice.Id,
            PaymentReceipt = receipt,
            PaymentReceiptId = receipt.Id,
            Currency = "USD",
            AppliedAmount = 250m,
            InvoiceOutstandingBefore = 250m,
            InvoiceOutstandingAfter = 0m,
            AppliedAt = DateTime.UtcNow,
            AppliedByUserId = fixture.UserId,
        };
        var exception = OpenException(fixture.UserId, invoice, receipt, "AmountMismatch");
        var correction = new BillingCorrection
        {
            ReconciliationException = exception,
            ReconciliationExceptionId = exception.Id,
            BillingInvoice = invoice,
            BillingInvoiceId = invoice.Id,
            BeforeOutstandingAmount = 250m,
            CorrectionAmount = -50m,
            ProposedOutstandingAmount = 200m,
            Reason = "Source balance correction",
            EvidenceNote = "Verified evidence package.",
            Status = "Pending",
            RequestedByUserId = fixture.UserId,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = fixture.UserId,
            UpdatedByUserId = fixture.UserId,
        };
        var batch = new PaymentReceiptImportBatch
        {
            FileName = "mock.csv",
            FileSha256 = new string('a', 64),
            SubmittedCount = 2,
            DuplicateCount = 1,
            RejectedCount = 1,
            ImportedAt = DateTime.UtcNow,
            ImportedByUserId = fixture.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = fixture.UserId,
            UpdatedByUserId = fixture.UserId,
        };
        batch.Outcomes.Add(new PaymentReceiptImportRowOutcome
        {
            ImportBatch = batch,
            ImportBatchId = batch.Id,
            RowNumber = 1,
            Outcome = "Duplicate",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = fixture.UserId,
            UpdatedByUserId = fixture.UserId,
        });
        batch.Outcomes.Add(new PaymentReceiptImportRowOutcome
        {
            ImportBatch = batch,
            ImportBatchId = batch.Id,
            RowNumber = 2,
            Outcome = "Rejected",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = fixture.UserId,
            UpdatedByUserId = fixture.UserId,
        });
        db.AddRange(application, correction, batch, new ActivityTimelineEvent
        {
            EntityType = "BillingInvoice",
            EntityId = invoice.Id,
            EventType = "ExactPaymentApplied",
            EventDescription = "Exact payment receipt applied",
            ActorUserId = fixture.UserId,
            ActorDisplayName = "Finance User",
            OccurredAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var service = NewService(db);
        var detail = await service.GetInvoiceAsync(invoice.Id, User(fixture.UserId));
        var backlog = await service.GetBacklogAsync(User(fixture.UserId));
        var exceptions = await service.SearchExceptionsAsync(
            new ReconciliationExceptionSearchQuery("Open", null), User(fixture.UserId));

        detail.ShouldNotBeNull();
        detail!.Invoice.Id.ShouldBe(invoice.Id);
        detail.Applications.Single().PaymentReceiptId.ShouldBe(receipt.Id);
        detail.Receipts.Single().ExternalReference.ShouldBe(receipt.ExternalReference);
        detail.Exceptions.Single().PendingCorrection.ShouldNotBeNull();
        detail.AuditEvents.Single().EventType.ShouldBe("ExactPaymentApplied");
        exceptions.Data.Single().PendingCorrection!.Id.ShouldBe(correction.Id);
        backlog.ExactApplicationCount.ShouldBe(1);
        backlog.PendingCorrectionCount.ShouldBe(1);
        backlog.DuplicateImportRowCount.ShouldBe(1);
        backlog.RejectedImportRowCount.ShouldBe(1);
    }

    [Fact]
    public async Task Amount_mismatch_preserves_balances_and_opens_one_exception()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var receipt = Receipt(fixture.UserId, 200m, "USD");
        db.AddRange(invoice, receipt);
        await db.SaveChangesAsync();
        var service = NewService(db);
        var request = new PaymentApplicationRequestDto(invoice.Id, receipt.Id, receipt.RowVersion.ToString());

        var first = await service.ApplyExactPaymentAsync(request, invoice.RowVersion, User(fixture.UserId));
        var second = await service.ApplyExactPaymentAsync(request, invoice.RowVersion, User(fixture.UserId));

        first.Error.ShouldBe("amount_mismatch");
        second.Error.ShouldBe("amount_mismatch");
        invoice.OutstandingAmount.ShouldBe(250m);
        receipt.ApplicationStatus.ShouldBe("Unapplied");
        (await db.ReconciliationExceptions.CountAsync(x => x.Type == "AmountMismatch")).ShouldBe(1);
    }

    [Fact]
    public async Task Conflicting_source_reference_blocks_exact_application()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var receipt = Receipt(fixture.UserId, 250m, "USD");
        receipt.InvoiceReference = "A-DIFFERENT-INVOICE";
        db.AddRange(invoice, receipt);
        await db.SaveChangesAsync();

        var (result, error) = await NewService(db).ApplyExactPaymentAsync(
            new PaymentApplicationRequestDto(invoice.Id, receipt.Id, receipt.RowVersion.ToString()),
            invoice.RowVersion,
            User(fixture.UserId));

        result.ShouldBeNull();
        error.ShouldBe("invoice_reference_conflict");
        (await db.PaymentApplications.CountAsync()).ShouldBe(0);
        (await db.ReconciliationExceptions.CountAsync(x => x.Type == "InvoiceReferenceConflict")).ShouldBe(1);
    }

    [Fact]
    public async Task Currency_mismatch_preserves_balances_and_opens_exception()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var receipt = Receipt(fixture.UserId, 250m, "EUR");
        db.AddRange(invoice, receipt);
        await db.SaveChangesAsync();

        var result = await NewService(db).ApplyExactPaymentAsync(
            new PaymentApplicationRequestDto(invoice.Id, receipt.Id, receipt.RowVersion.ToString()),
            invoice.RowVersion,
            User(fixture.UserId));

        result.Error.ShouldBe("currency_mismatch");
        invoice.OutstandingAmount.ShouldBe(250m);
        receipt.ApplicationStatus.ShouldBe("Unapplied");
        (await db.ReconciliationExceptions.CountAsync(x => x.Type == "CurrencyMismatch")).ShouldBe(1);
    }

    [Fact]
    public async Task Reference_correction_updates_only_reference_and_resolves_exception()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var receipt = Receipt(fixture.UserId, 250m, "USD");
        receipt.InvoiceReference = "WRONG-INVOICE";
        var exception = OpenException(fixture.UserId, invoice, receipt, "InvoiceReferenceConflict");
        db.Add(exception);
        await db.SaveChangesAsync();

        var (result, error) = await NewService(db).CorrectReferenceAsync(
            exception.Id,
            new ReconciliationReferenceCorrectionRequestDto(invoice.Id, "ReferenceCorrected", "Verified against source evidence."),
            exception.RowVersion,
            User(fixture.UserId));

        error.ShouldBeNull();
        result.ShouldNotBeNull();
        result!.Status.ShouldBe("Resolved");
        receipt.InvoiceReference.ShouldBe(invoice.InvoiceNumber);
        invoice.OutstandingAmount.ShouldBe(250m);
    }

    [Fact]
    public async Task Different_principal_can_approve_consistent_correction_and_resolve_exception()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var exception = OpenException(fixture.UserId, invoice, null, "AmountMismatch");
        db.Add(exception);
        await db.SaveChangesAsync();
        var service = NewService(db);

        var (requested, requestError) = await service.RequestCorrectionAsync(
            exception.Id,
            new BillingCorrectionRequestDto(-50m, 200m, "Source balance correction", "Verified evidence package."),
            exception.RowVersion,
            User(fixture.UserId));

        requestError.ShouldBeNull();
        requested.ShouldNotBeNull();
        requested!.Status.ShouldBe("Pending");
        db.ChangeTracker.Clear();

        var (decided, decisionError) = await service.DecideCorrectionAsync(
            requested.Id,
            new BillingCorrectionDecisionRequestDto("Approve", "Independent manager review complete."),
            uint.Parse(requested.RowVersion),
            User(Guid.NewGuid()));

        decisionError.ShouldBeNull();
        decided.ShouldNotBeNull();
        decided!.Status.ShouldBe("Approved");
        (await db.BillingInvoices.SingleAsync(x => x.Id == invoice.Id)).OutstandingAmount.ShouldBe(200m);
        (await db.ReconciliationExceptions.SingleAsync(x => x.Id == exception.Id)).Status.ShouldBe("Resolved");
    }

    [Fact]
    public async Task Same_principal_cannot_decide_own_correction_even_as_admin()
    {
        await using var db = NewDb();
        var fixture = SeedPolicy(db);
        var invoice = Invoice(fixture, 250m);
        var exception = new ReconciliationException
        {
            Type = "AmountMismatch",
            BillingInvoice = invoice,
            BillingInvoiceId = invoice.Id,
            Status = "Open",
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = fixture.UserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = fixture.UserId,
            UpdatedByUserId = fixture.UserId,
        };
        var correction = new BillingCorrection
        {
            ReconciliationException = exception,
            ReconciliationExceptionId = exception.Id,
            BillingInvoice = invoice,
            BillingInvoiceId = invoice.Id,
            BeforeOutstandingAmount = 250m,
            CorrectionAmount = -50m,
            ProposedOutstandingAmount = 200m,
            Reason = "Source evidence correction",
            EvidenceNote = "Verified against bounded operational evidence.",
            Status = "Pending",
            RequestedByUserId = fixture.UserId,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = fixture.UserId,
            UpdatedByUserId = fixture.UserId,
        };
        db.Add(correction);
        await db.SaveChangesAsync();

        var (result, error) = await NewService(db).DecideCorrectionAsync(
            correction.Id,
            new BillingCorrectionDecisionRequestDto("Approve", "Reviewed."),
            correction.RowVersion,
            User(fixture.UserId));

        result.ShouldBeNull();
        error.ShouldBe("same_user_decision_denied");
        correction.Status.ShouldBe("Pending");
        invoice.OutstandingAmount.ShouldBe(250m);
    }

    private static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static BillingReconciliationService NewService(AppDbContext db) =>
        new(new BillingRepository(db), new RecordingTimelineRepository(), db);

    private static (Guid PolicyId, Guid PolicyVersionId, Guid AccountId, Guid UserId) SeedPolicy(AppDbContext db)
    {
        var accountId = Guid.NewGuid();
        var policyId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var policy = new Policy
        {
            Id = policyId,
            PolicyNumber = "POL-100",
            AccountId = accountId,
            BrokerId = Guid.NewGuid(),
            CarrierId = Guid.NewGuid(),
            LineOfBusiness = "Property",
            EffectiveDate = DateTime.UtcNow,
            ExpirationDate = DateTime.UtcNow.AddYears(1),
            TotalPremium = 250m,
            PremiumCurrency = "USD",
            AccountDisplayNameAtLink = "Example Account",
            AccountStatusAtRead = "Active",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };
        var version = new PolicyVersion
        {
            Id = versionId,
            Policy = policy,
            PolicyId = policyId,
            VersionNumber = 1,
            VersionReason = "IssuedInitial",
            EffectiveDate = policy.EffectiveDate,
            ExpirationDate = policy.ExpirationDate,
            LineOfBusiness = policy.LineOfBusiness,
            LobProductVersionId = Guid.NewGuid(),
            TotalPremium = policy.TotalPremium,
            PremiumCurrency = policy.PremiumCurrency,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };
        policy.Versions.Add(version);
        db.Add(policy);
        db.SaveChanges();
        return (policyId, versionId, accountId, userId);
    }

    private static BillingInvoice Invoice((Guid PolicyId, Guid PolicyVersionId, Guid AccountId, Guid UserId) fixture, decimal amount) => new()
    {
        InvoiceNumber = $"INV-{Guid.NewGuid():N}",
        NormalizedInvoiceNumber = $"INV-{Guid.NewGuid():N}".ToUpperInvariant(),
        PolicyId = fixture.PolicyId,
        PolicyVersionId = fixture.PolicyVersionId,
        AccountId = fixture.AccountId,
        Currency = "USD",
        OriginalAmount = amount,
        OutstandingAmount = amount,
        InvoiceDate = new DateOnly(2026, 7, 19),
        DueDate = new DateOnly(2026, 8, 19),
        Status = "Outstanding",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = fixture.UserId,
        UpdatedByUserId = fixture.UserId,
    };

    private static PaymentReceipt Receipt(Guid userId, decimal amount, string currency) => new()
    {
        Source = "Manual",
        ExternalReference = $"PAY-{Guid.NewGuid():N}",
        NormalizedExternalReference = $"PAY-{Guid.NewGuid():N}".ToUpperInvariant(),
        ReceivedDate = new DateOnly(2026, 7, 19),
        Currency = currency,
        Amount = amount,
        ApplicationStatus = "Unapplied",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
        CreatedByUserId = userId,
        UpdatedByUserId = userId,
    };

    private static ReconciliationException OpenException(
        Guid userId,
        BillingInvoice invoice,
        PaymentReceipt? receipt,
        string type) => new()
        {
            Type = type,
            BillingInvoice = invoice,
            BillingInvoiceId = invoice.Id,
            PaymentReceipt = receipt,
            PaymentReceiptId = receipt?.Id,
            Status = "Open",
            OpenedAt = DateTime.UtcNow,
            OpenedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };

    private static ICurrentUserService User(Guid userId) => new TestUser(userId);

    private sealed record TestUser(Guid UserId) : ICurrentUserService
    {
        public string? DisplayName => "F0026 Test User";
        public IReadOnlyList<string> Roles => ["Admin"];
        public IReadOnlyList<string> Regions => ["West"];
        public string? BrokerTenantId => null;
    }

    private sealed record ScopedTestUser(
        Guid UserId,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Regions) : ICurrentUserService
    {
        public string? DisplayName => "Scoped F0026 Test User";
        public string? BrokerTenantId => null;
    }

    private sealed class RecordingTimelineRepository : ITimelineRepository
    {
        public List<ActivityTimelineEvent> Events { get; } = [];

        public Task AddEventAsync(ActivityTimelineEvent evt, CancellationToken ct = default)
        {
            Events.Add(evt);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsAsync(string entityType, Guid? entityId, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>([]);

        public Task<PaginatedResult<ActivityTimelineEvent>> ListEventsPagedAsync(string entityType, Guid? entityId, int page, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new PaginatedResult<ActivityTimelineEvent>([], page, pageSize, 0));

        public Task<IReadOnlyList<ActivityTimelineEvent>> ListEventsForBrokerUserAsync(IReadOnlyList<Guid> brokerIds, int limit, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ActivityTimelineEvent>>([]);
    }
}
