using Microsoft.EntityFrameworkCore;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class BillingRepository(AppDbContext db) : IBillingRepository
{
    public async Task<PaginatedResult<BillingInvoice>> SearchInvoicesAsync(
        BillingInvoiceSearchQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var visiblePolicies = VisiblePolicies(user);
        var invoices = db.BillingInvoices.AsNoTracking()
            .Where(invoice => visiblePolicies.Any(policy => policy.Id == invoice.PolicyId));

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var needle = query.Q.Trim().ToUpperInvariant();
            invoices = invoices.Where(invoice =>
                invoice.NormalizedInvoiceNumber.Contains(needle)
                || invoice.Policy.PolicyNumber.ToUpper().Contains(needle)
                || invoice.Account.Name.ToUpper().Contains(needle)
                || (invoice.PaymentApplication != null
                    && invoice.PaymentApplication.PaymentReceipt.NormalizedExternalReference.Contains(needle)));
        }

        if (query.PolicyId.HasValue)
            invoices = invoices.Where(invoice => invoice.PolicyId == query.PolicyId.Value);
        if (query.AccountId.HasValue)
            invoices = invoices.Where(invoice => invoice.AccountId == query.AccountId.Value);
        if (!string.IsNullOrWhiteSpace(query.Status))
            invoices = invoices.Where(invoice => invoice.Status == query.Status);
        if (query.HasOpenException.HasValue)
        {
            invoices = query.HasOpenException.Value
                ? invoices.Where(invoice => invoice.ReconciliationExceptions.Any(exception => exception.Status == "Open"))
                : invoices.Where(invoice => !invoice.ReconciliationExceptions.Any(exception => exception.Status == "Open"));
        }

        var total = await invoices.CountAsync(ct);
        var data = await invoices
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenBy(invoice => invoice.InvoiceNumber)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);
        return new PaginatedResult<BillingInvoice>(data, query.Page, query.PageSize, total);
    }

    public Task<BillingInvoice?> GetInvoiceDetailAsync(Guid invoiceId, ICurrentUserService user, CancellationToken ct = default)
    {
        var visiblePolicies = VisiblePolicies(user);
        return db.BillingInvoices.AsNoTracking()
            .Include(invoice => invoice.PaymentApplication!)
                .ThenInclude(application => application.PaymentReceipt)
            .Include(invoice => invoice.ReconciliationExceptions)
                .ThenInclude(exception => exception.PaymentReceipt)
            .Include(invoice => invoice.ReconciliationExceptions)
                .ThenInclude(exception => exception.Corrections)
            .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId
                && visiblePolicies.Any(policy => policy.Id == invoice.PolicyId), ct);
    }

    public async Task<IReadOnlyList<ActivityTimelineEvent>> GetInvoiceAuditEventsAsync(
        Guid invoiceId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var visiblePolicies = VisiblePolicies(user);
        var visibleInvoice = db.BillingInvoices.AsNoTracking()
            .Where(invoice => invoice.Id == invoiceId
                && visiblePolicies.Any(policy => policy.Id == invoice.PolicyId));
        var receiptIds = db.PaymentReceipts.AsNoTracking()
            .Where(receipt => receipt.PaymentApplication != null
                && visibleInvoice.Any(invoice => invoice.Id == receipt.PaymentApplication.BillingInvoiceId))
            .Select(receipt => receipt.Id);
        var exceptionIds = db.ReconciliationExceptions.AsNoTracking()
            .Where(exception => exception.BillingInvoiceId == invoiceId && visibleInvoice.Any())
            .Select(exception => exception.Id);
        var correctionIds = db.BillingCorrections.AsNoTracking()
            .Where(correction => correction.BillingInvoiceId == invoiceId && visibleInvoice.Any())
            .Select(correction => correction.Id);

        return await db.ActivityTimelineEvents.AsNoTracking()
            .Where(activity =>
                (activity.EntityType == "BillingInvoice" && activity.EntityId == invoiceId && visibleInvoice.Any())
                || (activity.EntityType == "PaymentReceipt" && receiptIds.Contains(activity.EntityId))
                || (activity.EntityType == "ReconciliationException" && exceptionIds.Contains(activity.EntityId))
                || (activity.EntityType == "BillingCorrection" && correctionIds.Contains(activity.EntityId)))
            .OrderByDescending(activity => activity.OccurredAt)
            .ThenByDescending(activity => activity.Id)
            .Take(200)
            .ToListAsync(ct);
    }

    public Task<BillingInvoice?> GetInvoiceForMutationAsync(Guid invoiceId, ICurrentUserService user, CancellationToken ct = default)
    {
        var visiblePolicies = VisiblePolicies(user);
        return db.BillingInvoices.AsTracking()
            .FirstOrDefaultAsync(invoice => invoice.Id == invoiceId
                && visiblePolicies.Any(policy => policy.Id == invoice.PolicyId), ct);
    }

    public async Task<BillingPolicyContext?> GetPolicyContextAsync(
        Guid policyId,
        Guid policyVersionId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var visiblePolicies = VisiblePolicies(user);
        return await db.PolicyVersions.AsNoTracking()
            .Where(version => version.Id == policyVersionId
                && version.PolicyId == policyId
                && visiblePolicies.Any(policy => policy.Id == version.PolicyId))
            .Select(version => new BillingPolicyContext(
                version.PolicyId,
                version.Id,
                version.Policy.AccountId,
                version.PremiumCurrency))
            .FirstOrDefaultAsync(ct);
    }

    public Task<bool> InvoiceNumberExistsAsync(string normalizedInvoiceNumber, CancellationToken ct = default) =>
        db.BillingInvoices.AnyAsync(invoice => invoice.NormalizedInvoiceNumber == normalizedInvoiceNumber, ct);

    public Task AddInvoiceAsync(BillingInvoice invoice, CancellationToken ct = default) =>
        db.BillingInvoices.AddAsync(invoice, ct).AsTask();

    public async Task<PaginatedResult<PaymentReceipt>> SearchReceiptsAsync(
        PaymentReceiptSearchQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var receipts = VisibleReceipts(user).AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.ApplicationStatus))
            receipts = receipts.Where(receipt => receipt.ApplicationStatus == query.ApplicationStatus);
        if (!string.IsNullOrWhiteSpace(query.ExternalReference))
        {
            var needle = query.ExternalReference.Trim().ToUpperInvariant();
            receipts = receipts.Where(receipt => receipt.NormalizedExternalReference.Contains(needle));
        }
        if (!string.IsNullOrWhiteSpace(query.Currency))
            receipts = receipts.Where(receipt => receipt.Currency == query.Currency);

        var total = await receipts.CountAsync(ct);
        var data = await receipts
            .OrderByDescending(receipt => receipt.ReceivedDate)
            .ThenBy(receipt => receipt.ExternalReference)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);
        return new PaginatedResult<PaymentReceipt>(data, query.Page, query.PageSize, total);
    }

    public Task<PaymentReceipt?> GetReceiptForMutationAsync(Guid receiptId, ICurrentUserService user, CancellationToken ct = default) =>
        VisibleReceipts(user).AsTracking().FirstOrDefaultAsync(receipt => receipt.Id == receiptId, ct);

    public Task<bool> ReceiptReferenceExistsAsync(string source, string normalizedExternalReference, CancellationToken ct = default) =>
        db.PaymentReceipts.AnyAsync(receipt => receipt.Source == source
            && receipt.NormalizedExternalReference == normalizedExternalReference, ct);

    public Task AddReceiptAsync(PaymentReceipt receipt, CancellationToken ct = default) =>
        db.PaymentReceipts.AddAsync(receipt, ct).AsTask();

    public Task AddImportBatchAsync(PaymentReceiptImportBatch batch, CancellationToken ct = default) =>
        db.PaymentReceiptImportBatches.AddAsync(batch, ct).AsTask();

    public Task<PaymentReceiptImportBatch?> GetImportBatchAsync(Guid batchId, ICurrentUserService user, CancellationToken ct = default)
    {
        var visibleReceipts = VisibleReceipts(user);
        var isAdmin = HasRole(user.Roles, "Admin");
        return db.PaymentReceiptImportBatches.AsNoTracking()
            .Include(batch => batch.Outcomes)
            .Where(batch => batch.Id == batchId
                && (isAdmin || batch.ImportedByUserId == user.UserId
                    || visibleReceipts.Any(receipt => receipt.ImportBatchId == batch.Id)))
            .FirstOrDefaultAsync(ct);
    }

    public Task AddApplicationAsync(PaymentApplication application, CancellationToken ct = default) =>
        db.PaymentApplications.AddAsync(application, ct).AsTask();

    public Task<ReconciliationException?> FindOpenExceptionAsync(
        Guid? invoiceId,
        Guid? receiptId,
        string type,
        CancellationToken ct = default) =>
        db.ReconciliationExceptions.FirstOrDefaultAsync(exception =>
            exception.Status == "Open"
            && exception.Type == type
            && exception.BillingInvoiceId == invoiceId
            && exception.PaymentReceiptId == receiptId, ct);

    public Task AddExceptionAsync(ReconciliationException exception, CancellationToken ct = default) =>
        db.ReconciliationExceptions.AddAsync(exception, ct).AsTask();

    public async Task<PaginatedResult<ReconciliationException>> SearchExceptionsAsync(
        ReconciliationExceptionSearchQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        IQueryable<ReconciliationException> exceptions = VisibleExceptions(user).AsNoTracking()
            .Include(exception => exception.Corrections);
        if (!string.IsNullOrWhiteSpace(query.Status))
            exceptions = exceptions.Where(exception => exception.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Type))
            exceptions = exceptions.Where(exception => exception.Type == query.Type);

        var total = await exceptions.CountAsync(ct);
        var data = await exceptions
            .OrderByDescending(exception => exception.OpenedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);
        return new PaginatedResult<ReconciliationException>(data, query.Page, query.PageSize, total);
    }

    public Task<ReconciliationException?> GetExceptionForMutationAsync(
        Guid exceptionId,
        ICurrentUserService user,
        CancellationToken ct = default) =>
        VisibleExceptions(user)
            .Include(exception => exception.PaymentReceipt)
            .Include(exception => exception.Corrections)
            .AsTracking()
            .FirstOrDefaultAsync(exception => exception.Id == exceptionId, ct);

    public Task<bool> PendingCorrectionExistsAsync(Guid exceptionId, CancellationToken ct = default) =>
        db.BillingCorrections.AnyAsync(correction => correction.ReconciliationExceptionId == exceptionId
            && correction.Status == "Pending", ct);

    public Task AddCorrectionAsync(BillingCorrection correction, CancellationToken ct = default) =>
        db.BillingCorrections.AddAsync(correction, ct).AsTask();

    public Task<BillingCorrection?> GetCorrectionForMutationAsync(
        Guid correctionId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var visiblePolicies = VisiblePolicies(user);
        return db.BillingCorrections.AsTracking()
            .Include(correction => correction.BillingInvoice)
            .Include(correction => correction.ReconciliationException)
            .FirstOrDefaultAsync(correction => correction.Id == correctionId
                && visiblePolicies.Any(policy => policy.Id == correction.BillingInvoice.PolicyId), ct);
    }

    public async Task<ReconciliationBacklogResponseDto> GetBacklogAsync(
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var open = VisibleExceptions(user).AsNoTracking().Where(exception => exception.Status == "Open");
        var visiblePolicies = VisiblePolicies(user);
        var exactApplicationCount = await db.PaymentApplications.AsNoTracking()
            .CountAsync(application => visiblePolicies.Any(policy => policy.Id == application.BillingInvoice.PolicyId), ct);
        var pendingCorrectionCount = await db.BillingCorrections.AsNoTracking()
            .CountAsync(correction => correction.Status == "Pending"
                && visiblePolicies.Any(policy => policy.Id == correction.BillingInvoice.PolicyId), ct);

        var visibleBatches = HasRole(user.Roles, "Admin")
            ? db.PaymentReceiptImportBatches.AsNoTracking()
            : db.PaymentReceiptImportBatches.AsNoTracking().Where(batch =>
                batch.ImportedByUserId == user.UserId
                || batch.Receipts.Any(receipt => receipt.PaymentApplication != null
                    && visiblePolicies.Any(policy => policy.Id == receipt.PaymentApplication.BillingInvoice.PolicyId)));
        var visibleOutcomes = db.PaymentReceiptImportRowOutcomes.AsNoTracking()
            .Where(outcome => visibleBatches.Any(batch => batch.Id == outcome.ImportBatchId));
        var rejectedImportRowCount = await visibleOutcomes.CountAsync(outcome => outcome.Outcome == "Rejected", ct);
        var duplicateImportRowCount = await visibleOutcomes.CountAsync(outcome => outcome.Outcome == "Duplicate", ct);

        var openCount = await open.CountAsync(ct);
        var oldest = await open.OrderBy(exception => exception.OpenedAt)
            .Select(exception => (DateTime?)exception.OpenedAt)
            .FirstOrDefaultAsync(ct);
        var byTypeRows = await open.GroupBy(exception => exception.Type)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .OrderBy(row => row.Type)
            .ToListAsync(ct);
        var byType = byTypeRows
            .Select(row => new ReconciliationBacklogRowDto(row.Type, row.Count))
            .ToList();
        int? oldestDays = oldest.HasValue
            ? Math.Max(0, (DateTime.UtcNow.Date - oldest.Value.Date).Days)
            : null;
        return new ReconciliationBacklogResponseDto(
            openCount,
            exactApplicationCount,
            pendingCorrectionCount,
            rejectedImportRowCount,
            duplicateImportRowCount,
            oldestDays,
            byType);
    }

    public async Task<PolicyBillingSummaryDto?> GetPolicyBillingSummaryAsync(
        Guid policyId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var visiblePolicies = VisiblePolicies(user);
        if (!await visiblePolicies.AnyAsync(policy => policy.Id == policyId, ct))
            return null;

        var invoices = db.BillingInvoices.AsNoTracking().Where(invoice => invoice.PolicyId == policyId);
        var currency = await visiblePolicies.Where(policy => policy.Id == policyId)
            .Select(policy => policy.PremiumCurrency)
            .FirstAsync(ct);
        return new PolicyBillingSummaryDto(
            policyId,
            currency,
            await invoices.CountAsync(ct),
            await invoices.CountAsync(invoice => invoice.Status == "Outstanding", ct),
            await invoices.Where(invoice => invoice.Status == "Outstanding").SumAsync(invoice => invoice.OutstandingAmount, ct),
            await invoices.Where(invoice => invoice.Status == "Outstanding")
                .OrderBy(invoice => invoice.DueDate)
                .Select(invoice => (DateOnly?)invoice.DueDate)
                .FirstOrDefaultAsync(ct),
            DateTime.UtcNow);
    }

    private IQueryable<PaymentReceipt> VisibleReceipts(ICurrentUserService user)
    {
        if (HasRole(user.Roles, "Admin"))
            return db.PaymentReceipts;

        var visiblePolicies = VisiblePolicies(user);
        return db.PaymentReceipts.Where(receipt =>
            receipt.CreatedByUserId == user.UserId
            || (receipt.PaymentApplication != null
                && visiblePolicies.Any(policy => policy.Id == receipt.PaymentApplication.BillingInvoice.PolicyId))
            || db.BillingInvoices.Any(invoice =>
                visiblePolicies.Any(policy => policy.Id == invoice.PolicyId)
                && receipt.InvoiceReference != null
                && invoice.NormalizedInvoiceNumber == receipt.InvoiceReference.ToUpper()));
    }

    private IQueryable<ReconciliationException> VisibleExceptions(ICurrentUserService user)
    {
        if (HasRole(user.Roles, "Admin"))
            return db.ReconciliationExceptions;

        var visiblePolicies = VisiblePolicies(user);
        return db.ReconciliationExceptions.Where(exception =>
            exception.OpenedByUserId == user.UserId
            || (exception.BillingInvoiceId.HasValue
                && db.BillingInvoices.Any(invoice => invoice.Id == exception.BillingInvoiceId.Value
                    && visiblePolicies.Any(policy => policy.Id == invoice.PolicyId)))
            || (exception.PaymentReceipt != null && exception.PaymentReceipt.CreatedByUserId == user.UserId));
    }

    private IQueryable<Policy> VisiblePolicies(ICurrentUserService user)
    {
        // Callers control tracking on the result root. Keeping the authorization
        // subquery neutral avoids propagating no-tracking semantics into mutation
        // queries on providers that compose query annotations across subqueries.
        var policies = db.Policies;
        if (HasRole(user.Roles, "Admin") || HasRole(user.Roles, "Underwriter") || HasRole(user.Roles, "ProgramManager"))
            return policies;

        var regions = user.Regions
            .Where(region => !string.IsNullOrWhiteSpace(region))
            .Select(region => region.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var includeRegional = HasRole(user.Roles, "DistributionUser")
            || HasRole(user.Roles, "DistributionManager")
            || HasRole(user.Roles, "FinanceOperationsAnalyst")
            || HasRole(user.Roles, "FinanceManager");
        var includeRelationshipManager = HasRole(user.Roles, "RelationshipManager");

        return policies.Where(policy =>
            (includeRegional && policy.Account.Region != null && regions.Contains(policy.Account.Region))
            || (includeRelationshipManager && policy.Broker.ManagedByUserId == user.UserId));
    }

    private static bool HasRole(IReadOnlyList<string> roles, string role) =>
        roles.Any(existing => string.Equals(existing, role, StringComparison.OrdinalIgnoreCase));
}
