using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class BillingReconciliationService(
    IBillingRepository billingRepo,
    ITimelineRepository timelineRepo,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedResult<BillingInvoiceDto>> SearchInvoicesAsync(
        BillingInvoiceSearchQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var result = await billingRepo.SearchInvoicesAsync(query, user, ct);
        return new PaginatedResult<BillingInvoiceDto>(
            result.Data.Select(MapInvoice).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<BillingInvoiceDetailDto?> GetInvoiceAsync(
        Guid invoiceId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var invoice = await billingRepo.GetInvoiceDetailAsync(invoiceId, user, ct);
        if (invoice is null) return null;

        var applications = invoice.PaymentApplication is null
            ? []
            : new List<PaymentApplicationDto> { MapApplication(invoice.PaymentApplication) };
        var receipts = invoice.PaymentApplication is null
            ? invoice.ReconciliationExceptions
                .Where(exception => exception.PaymentReceipt is not null)
                .Select(exception => exception.PaymentReceipt!)
                .DistinctBy(receipt => receipt.Id)
                .Select(MapReceipt)
                .ToList()
            : invoice.ReconciliationExceptions
                .Where(exception => exception.PaymentReceipt is not null)
                .Select(exception => exception.PaymentReceipt!)
                .Append(invoice.PaymentApplication.PaymentReceipt)
                .DistinctBy(receipt => receipt.Id)
                .Select(MapReceipt)
                .ToList();
        var auditEvents = await billingRepo.GetInvoiceAuditEventsAsync(invoiceId, user, ct);
        return new BillingInvoiceDetailDto(
            MapInvoice(invoice),
            applications,
            receipts,
            invoice.ReconciliationExceptions.OrderByDescending(exception => exception.OpenedAt).Select(MapException).ToList(),
            auditEvents.Select(MapTimelineEvent).ToList());
    }

    public async Task<(BillingInvoiceDto? Result, string? Error)> CreateInvoiceAsync(
        BillingInvoiceCreateRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var normalizedNumber = NormalizeIdentifier(dto.InvoiceNumber);
        var context = await billingRepo.GetPolicyContextAsync(dto.PolicyId, dto.PolicyVersionId, user, ct);
        if (context is null)
            return (null, "policy_context_not_found");
        if (context.AccountId != dto.AccountId || !EqualsOrdinal(context.Currency, dto.Currency))
            return (null, "policy_context_mismatch");
        if (await billingRepo.InvoiceNumberExistsAsync(normalizedNumber, ct))
            return (null, "invoice_number_conflict");

        var now = DateTime.UtcNow;
        var invoice = new BillingInvoice
        {
            InvoiceNumber = dto.InvoiceNumber.Trim(),
            NormalizedInvoiceNumber = normalizedNumber,
            PolicyId = dto.PolicyId,
            PolicyVersionId = dto.PolicyVersionId,
            AccountId = dto.AccountId,
            Currency = dto.Currency,
            OriginalAmount = dto.OriginalAmount,
            OutstandingAmount = dto.OriginalAmount,
            InvoiceDate = dto.InvoiceDate,
            DueDate = dto.DueDate,
            Status = "Outstanding",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        await billingRepo.AddInvoiceAsync(invoice, ct);
        await AddTimelineAsync("BillingInvoice", invoice.Id, "BillingInvoiceCreated", "Agency-bill invoice created", user, now,
            new { invoice.Id, invoice.InvoiceNumber, invoice.PolicyId, invoice.PolicyVersionId, invoice.AccountId, invoice.Currency, invoice.OriginalAmount }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapInvoice(invoice), null);
    }

    public async Task<PaginatedResult<PaymentReceiptDto>> SearchReceiptsAsync(
        PaymentReceiptSearchQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var result = await billingRepo.SearchReceiptsAsync(query, user, ct);
        return new PaginatedResult<PaymentReceiptDto>(
            result.Data.Select(MapReceipt).ToList(),
            result.Page,
            result.PageSize,
            result.TotalCount);
    }

    public async Task<(PaymentReceiptDto? Result, string? Error)> CreateManualReceiptAsync(
        PaymentReceiptCreateRequestDto dto,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var normalizedReference = NormalizeIdentifier(dto.ExternalReference);
        if (await billingRepo.ReceiptReferenceExistsAsync("Manual", normalizedReference, ct))
            return (null, "receipt_reference_conflict");

        var now = DateTime.UtcNow;
        var receipt = NewReceipt(dto, "Manual", normalizedReference, now, user.UserId);
        await billingRepo.AddReceiptAsync(receipt, ct);
        await AddTimelineAsync("PaymentReceipt", receipt.Id, "PaymentReceiptRecorded", "Manual payment receipt recorded", user, now,
            new { receipt.Id, receipt.Source, receipt.ExternalReference, receipt.Currency, receipt.Amount, receipt.InvoiceReference }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapReceipt(receipt), null);
    }

    public async Task<(PaymentReceiptImportResultDto? Result, string? Error)> ImportMockReceiptsAsync(
        Stream input,
        string fileName,
        long length,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        const int maximumBytes = 1024 * 1024;
        const int maximumRows = 1000;
        if (length <= 0) return (null, "empty_file");
        if (length > maximumBytes) return (null, "file_too_large");

        using var memory = new MemoryStream((int)length);
        await input.CopyToAsync(memory, ct);
        if (memory.Length > maximumBytes) return (null, "file_too_large");
        var bytes = memory.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            return (null, "invalid_encoding");
        }
        if (text.Length > 0 && text[0] == '\uFEFF') text = text[1..];
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        while (lines.Length > 0 && string.IsNullOrWhiteSpace(lines[^1]))
            lines = lines[..^1];
        if (lines.Length < 2) return (null, "missing_data_rows");
        if (lines.Length - 1 > maximumRows) return (null, "too_many_rows");

        var header = ParseCsvLine(lines[0]);
        var expectedHeader = new[] { "externalReference", "receivedDate", "currency", "amount", "invoiceReference", "memo" };
        if (header is null || header.Count != expectedHeader.Length || !header.SequenceEqual(expectedHeader, StringComparer.Ordinal))
            return (null, "invalid_header");

        var now = DateTime.UtcNow;
        var batch = new PaymentReceiptImportBatch
        {
            ContractVersion = "mock-payment-receipt-row-v1",
            FileName = Path.GetFileName(fileName),
            FileSha256 = hash,
            Status = "Completed",
            SubmittedCount = lines.Length - 1,
            ImportedAt = now,
            ImportedByUserId = user.UserId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };

        for (var index = 1; index < lines.Length; index++)
        {
            var rowNumber = index + 1;
            var fields = ParseCsvLine(lines[index]);
            var externalReference = fields is { Count: > 0 } ? Normalize(fields[0]) : null;
            var outcome = NewOutcome(batch, rowNumber, externalReference, now, user.UserId);

            if (fields is null)
            {
                Reject(outcome, "invalid_source_data", "CSV quoting is malformed.");
                batch.RejectedCount++;
                batch.Outcomes.Add(outcome);
                continue;
            }

            if (!TryParseImportRow(fields, out var row, out var reason))
            {
                Reject(outcome, "invalid_source_data", reason);
                batch.RejectedCount++;
                batch.Outcomes.Add(outcome);
                continue;
            }

            var normalizedReference = NormalizeIdentifier(row!.ExternalReference);
            if (await billingRepo.ReceiptReferenceExistsAsync("MockVendorCsv", normalizedReference, ct)
                || batch.Receipts.Any(receipt => receipt.NormalizedExternalReference == normalizedReference))
            {
                outcome.Outcome = "Duplicate";
                outcome.ReasonCode = "duplicate_receipt";
                outcome.ReasonDetail = "The source/reference pair was already recorded.";
                batch.DuplicateCount++;
                batch.Outcomes.Add(outcome);
                continue;
            }

            var receipt = NewReceipt(row, "MockVendorCsv", normalizedReference, now, user.UserId);
            receipt.ImportBatch = batch;
            receipt.ImportBatchId = batch.Id;
            receipt.ImportRowNumber = rowNumber;
            outcome.Outcome = "Created";
            outcome.PaymentReceipt = receipt;
            outcome.PaymentReceiptId = receipt.Id;
            batch.CreatedCount++;
            batch.Receipts.Add(receipt);
            batch.Outcomes.Add(outcome);
        }

        await billingRepo.AddImportBatchAsync(batch, ct);
        foreach (var outcome in batch.Outcomes.Where(item => item.Outcome is "Duplicate" or "Rejected"))
        {
            var type = outcome.Outcome == "Duplicate" ? "DuplicateReceipt" : "InvalidSourceData";
            await billingRepo.AddExceptionAsync(NewException(type, null, null, batch.Id, outcome.Id, now, user.UserId), ct);
        }

        await AddTimelineAsync("PaymentReceiptImportBatch", batch.Id, "PaymentReceiptImportCompleted", "Mock payment receipt CSV import completed", user, now,
            new { batch.Id, batch.ContractVersion, batch.FileName, batch.FileSha256, batch.SubmittedCount, batch.CreatedCount, batch.DuplicateCount, batch.RejectedCount }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapImport(batch), null);
    }

    public async Task<PaymentReceiptImportResultDto?> GetImportAsync(
        Guid batchId,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var batch = await billingRepo.GetImportBatchAsync(batchId, user, ct);
        return batch is null ? null : MapImport(batch);
    }

    public async Task<(PaymentApplicationDto? Result, string? Error)> ApplyExactPaymentAsync(
        PaymentApplicationRequestDto dto,
        uint invoiceRowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var invoice = await billingRepo.GetInvoiceForMutationAsync(dto.BillingInvoiceId, user, ct);
        var receipt = await billingRepo.GetReceiptForMutationAsync(dto.PaymentReceiptId, user, ct);
        if (invoice is null || receipt is null) return (null, "not_found");
        if (!uint.TryParse(dto.PaymentReceiptRowVersion, out var receiptRowVersion)
            || invoice.RowVersion != invoiceRowVersion
            || receipt.RowVersion != receiptRowVersion)
            return (null, "precondition_failed");
        if (invoice.Status != "Outstanding" || receipt.ApplicationStatus != "Unapplied")
            return (null, "already_applied");

        var now = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(receipt.InvoiceReference)
            && !string.Equals(NormalizeIdentifier(receipt.InvoiceReference), invoice.NormalizedInvoiceNumber, StringComparison.Ordinal))
        {
            await PreserveExceptionAsync(invoice.Id, receipt.Id, "InvoiceReferenceConflict", user, now, ct);
            await unitOfWork.CommitAsync(ct);
            return (null, "invoice_reference_conflict");
        }
        if (!EqualsOrdinal(invoice.Currency, receipt.Currency))
        {
            await PreserveExceptionAsync(invoice.Id, receipt.Id, "CurrencyMismatch", user, now, ct);
            await unitOfWork.CommitAsync(ct);
            return (null, "currency_mismatch");
        }
        if (receipt.Amount != invoice.OutstandingAmount)
        {
            await PreserveExceptionAsync(invoice.Id, receipt.Id, "AmountMismatch", user, now, ct);
            await unitOfWork.CommitAsync(ct);
            return (null, "amount_mismatch");
        }

        invoice.RowVersion = invoiceRowVersion;
        receipt.RowVersion = receiptRowVersion;
        var before = invoice.OutstandingAmount;
        invoice.OutstandingAmount = 0m;
        invoice.Status = "Reconciled";
        invoice.UpdatedAt = now;
        invoice.UpdatedByUserId = user.UserId;
        receipt.ApplicationStatus = "Applied";
        receipt.UpdatedAt = now;
        receipt.UpdatedByUserId = user.UserId;

        var application = new PaymentApplication
        {
            BillingInvoiceId = invoice.Id,
            PaymentReceiptId = receipt.Id,
            Currency = invoice.Currency,
            AppliedAmount = receipt.Amount,
            InvoiceOutstandingBefore = before,
            InvoiceOutstandingAfter = 0m,
            AppliedAt = now,
            AppliedByUserId = user.UserId,
        };
        await billingRepo.AddApplicationAsync(application, ct);
        await AddTimelineAsync("BillingInvoice", invoice.Id, "ExactPaymentApplied", "Exact payment receipt applied", user, now,
            new { application.Id, application.BillingInvoiceId, application.PaymentReceiptId, application.Currency, application.AppliedAmount, application.InvoiceOutstandingBefore, application.InvoiceOutstandingAfter }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapApplication(application), null);
    }

    public async Task<PaginatedResult<ReconciliationExceptionDto>> SearchExceptionsAsync(
        ReconciliationExceptionSearchQuery query,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var result = await billingRepo.SearchExceptionsAsync(query, user, ct);
        return new PaginatedResult<ReconciliationExceptionDto>(
            result.Data.Select(MapException).ToList(), result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<(ReconciliationExceptionDto? Result, string? Error)> CorrectReferenceAsync(
        Guid exceptionId,
        ReconciliationReferenceCorrectionRequestDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var exception = await billingRepo.GetExceptionForMutationAsync(exceptionId, user, ct);
        if (exception is null) return (null, "not_found");
        if (exception.RowVersion != rowVersion) return (null, "precondition_failed");
        if (exception.Status != "Open") return (null, "exception_not_open");
        if (exception.Type is not ("MissingInvoiceReference" or "InvoiceReferenceConflict") || exception.PaymentReceipt is null)
            return (null, "reference_not_correctable");

        var invoice = await billingRepo.GetInvoiceForMutationAsync(dto.BillingInvoiceId, user, ct);
        if (invoice is null) return (null, "invoice_not_found");

        var now = DateTime.UtcNow;
        exception.RowVersion = rowVersion;
        exception.BillingInvoiceId = invoice.Id;
        exception.PaymentReceipt.InvoiceReference = invoice.InvoiceNumber;
        exception.PaymentReceipt.UpdatedAt = now;
        exception.PaymentReceipt.UpdatedByUserId = user.UserId;
        exception.Status = "Resolved";
        exception.ResolvedAt = now;
        exception.ResolvedByUserId = user.UserId;
        exception.ResolutionCode = dto.ResolutionCode.Trim();
        exception.ResolutionNote = dto.ResolutionNote.Trim();
        exception.UpdatedAt = now;
        exception.UpdatedByUserId = user.UserId;
        await AddTimelineAsync("ReconciliationException", exception.Id, "ReconciliationReferenceCorrected", "Reconciliation invoice reference corrected", user, now,
            new { exception.Id, exception.PaymentReceiptId, exception.BillingInvoiceId, exception.ResolutionCode }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapException(exception), null);
    }

    public async Task<(BillingCorrectionDto? Result, string? Error)> RequestCorrectionAsync(
        Guid exceptionId,
        BillingCorrectionRequestDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var exception = await billingRepo.GetExceptionForMutationAsync(exceptionId, user, ct);
        if (exception is null) return (null, "not_found");
        if (exception.RowVersion != rowVersion) return (null, "precondition_failed");
        if (exception.Status != "Open" || !exception.BillingInvoiceId.HasValue)
            return (null, "exception_not_eligible");
        if (await billingRepo.PendingCorrectionExistsAsync(exceptionId, ct))
            return (null, "correction_pending");

        var invoice = await billingRepo.GetInvoiceForMutationAsync(exception.BillingInvoiceId.Value, user, ct);
        if (invoice is null) return (null, "invoice_not_found");
        if (invoice.OutstandingAmount + dto.CorrectionAmount != dto.ProposedOutstandingAmount
            || dto.ProposedOutstandingAmount > invoice.OriginalAmount)
            return (null, "correction_amount_inconsistent");

        var now = DateTime.UtcNow;
        exception.RowVersion = rowVersion;
        var correction = new BillingCorrection
        {
            ReconciliationExceptionId = exception.Id,
            BillingInvoiceId = invoice.Id,
            BeforeOutstandingAmount = invoice.OutstandingAmount,
            CorrectionAmount = dto.CorrectionAmount,
            ProposedOutstandingAmount = dto.ProposedOutstandingAmount,
            Reason = dto.Reason.Trim(),
            EvidenceNote = dto.EvidenceNote.Trim(),
            Status = "Pending",
            RequestedByUserId = user.UserId,
            RequestedAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = user.UserId,
            UpdatedByUserId = user.UserId,
        };
        await billingRepo.AddCorrectionAsync(correction, ct);
        await AddTimelineAsync("BillingCorrection", correction.Id, "BillingCorrectionRequested", "Billing correction requested", user, now,
            new { correction.Id, correction.ReconciliationExceptionId, correction.BillingInvoiceId, correction.BeforeOutstandingAmount, correction.CorrectionAmount, correction.ProposedOutstandingAmount }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapCorrection(correction), null);
    }

    public async Task<(BillingCorrectionDto? Result, string? Error)> DecideCorrectionAsync(
        Guid correctionId,
        BillingCorrectionDecisionRequestDto dto,
        uint rowVersion,
        ICurrentUserService user,
        CancellationToken ct = default)
    {
        var correction = await billingRepo.GetCorrectionForMutationAsync(correctionId, user, ct);
        if (correction is null) return (null, "not_found");
        if (correction.RowVersion != rowVersion) return (null, "precondition_failed");
        if (correction.Status != "Pending") return (null, "correction_not_pending");
        if (correction.RequestedByUserId == user.UserId) return (null, "same_user_decision_denied");

        var now = DateTime.UtcNow;
        correction.RowVersion = rowVersion;
        correction.Status = dto.Decision == "Approve" ? "Approved" : "Rejected";
        correction.DecisionByUserId = user.UserId;
        correction.DecisionAt = now;
        correction.DecisionNote = dto.DecisionNote.Trim();
        correction.UpdatedAt = now;
        correction.UpdatedByUserId = user.UserId;

        if (dto.Decision == "Approve")
        {
            correction.BillingInvoice.OutstandingAmount = correction.ProposedOutstandingAmount;
            correction.BillingInvoice.Status = correction.ProposedOutstandingAmount == 0m ? "Reconciled" : "Outstanding";
            correction.BillingInvoice.UpdatedAt = now;
            correction.BillingInvoice.UpdatedByUserId = user.UserId;
            correction.ReconciliationException.Status = "Resolved";
            correction.ReconciliationException.ResolvedAt = now;
            correction.ReconciliationException.ResolvedByUserId = user.UserId;
            correction.ReconciliationException.ResolutionCode = "ApprovedCorrection";
            correction.ReconciliationException.ResolutionNote = dto.DecisionNote.Trim();
            correction.ReconciliationException.UpdatedAt = now;
            correction.ReconciliationException.UpdatedByUserId = user.UserId;
        }

        var eventType = dto.Decision == "Approve" ? "BillingCorrectionApproved" : "BillingCorrectionRejected";
        await AddTimelineAsync("BillingCorrection", correction.Id, eventType, $"Billing correction {correction.Status.ToLowerInvariant()}", user, now,
            new { correction.Id, correction.ReconciliationExceptionId, correction.BillingInvoiceId, correction.Status, correction.BeforeOutstandingAmount, correction.ProposedOutstandingAmount }, ct);
        await unitOfWork.CommitAsync(ct);
        return (MapCorrection(correction), null);
    }

    public Task<ReconciliationBacklogResponseDto> GetBacklogAsync(ICurrentUserService user, CancellationToken ct = default) =>
        billingRepo.GetBacklogAsync(user, ct);

    public Task<PolicyBillingSummaryDto?> GetPolicyBillingSummaryAsync(Guid policyId, ICurrentUserService user, CancellationToken ct = default) =>
        billingRepo.GetPolicyBillingSummaryAsync(policyId, user, ct);

    private async Task PreserveExceptionAsync(
        Guid invoiceId,
        Guid receiptId,
        string type,
        ICurrentUserService user,
        DateTime now,
        CancellationToken ct)
    {
        if (await billingRepo.FindOpenExceptionAsync(invoiceId, receiptId, type, ct) is null)
            await billingRepo.AddExceptionAsync(NewException(type, invoiceId, receiptId, null, null, now, user.UserId), ct);
    }

    private Task AddTimelineAsync(
        string entityType,
        Guid entityId,
        string eventType,
        string description,
        ICurrentUserService user,
        DateTime occurredAt,
        object payload,
        CancellationToken ct) =>
        timelineRepo.AddEventAsync(new ActivityTimelineEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            EventType = eventType,
            EventDescription = description,
            EventPayloadJson = JsonSerializer.Serialize(payload),
            ActorUserId = user.UserId,
            ActorDisplayName = user.DisplayName,
            OccurredAt = occurredAt,
        }, ct);

    private static PaymentReceipt NewReceipt(
        PaymentReceiptCreateRequestDto dto,
        string source,
        string normalizedReference,
        DateTime now,
        Guid userId) => new()
        {
            Source = source,
            ExternalReference = dto.ExternalReference.Trim(),
            NormalizedExternalReference = normalizedReference,
            ReceivedDate = dto.ReceivedDate,
            Currency = dto.Currency,
            Amount = dto.Amount,
            InvoiceReference = Normalize(dto.InvoiceReference),
            Memo = Normalize(dto.Memo),
            ApplicationStatus = "Unapplied",
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };

    private static PaymentReceiptImportRowOutcome NewOutcome(
        PaymentReceiptImportBatch batch,
        int rowNumber,
        string? externalReference,
        DateTime now,
        Guid userId) => new()
        {
            ImportBatch = batch,
            ImportBatchId = batch.Id,
            RowNumber = rowNumber,
            ExternalReference = externalReference,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };

    private static ReconciliationException NewException(
        string type,
        Guid? invoiceId,
        Guid? receiptId,
        Guid? batchId,
        Guid? outcomeId,
        DateTime now,
        Guid userId) => new()
        {
            Type = type,
            BillingInvoiceId = invoiceId,
            PaymentReceiptId = receiptId,
            ImportBatchId = batchId,
            ImportRowOutcomeId = outcomeId,
            Status = "Open",
            OpenedAt = now,
            OpenedByUserId = userId,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
            UpdatedByUserId = userId,
        };

    private static void Reject(PaymentReceiptImportRowOutcome outcome, string code, string detail)
    {
        outcome.Outcome = "Rejected";
        outcome.ReasonCode = code;
        outcome.ReasonDetail = detail;
    }

    private static bool TryParseImportRow(
        IReadOnlyList<string> fields,
        out PaymentReceiptCreateRequestDto? row,
        out string reason)
    {
        row = null;
        if (fields.Count != 6)
        {
            reason = "Expected exactly six CSV columns.";
            return false;
        }

        var externalReference = fields[0].Trim();
        var currency = fields[2].Trim();
        if (externalReference.Length is < 1 or > 120 || externalReference.Any(char.IsControl))
        {
            reason = "externalReference is required, must be at most 120 characters, and cannot contain control characters.";
            return false;
        }
        if (!DateOnly.TryParseExact(fields[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var receivedDate))
        {
            reason = "receivedDate must use YYYY-MM-DD.";
            return false;
        }
        if (currency.Length != 3 || currency.Any(ch => ch is < 'A' or > 'Z'))
        {
            reason = "currency must contain exactly three uppercase ASCII letters.";
            return false;
        }
        if (!decimal.TryParse(fields[3].Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount)
            || amount <= 0m || amount > 9999999999999999.99m || decimal.Round(amount, 2) != amount)
        {
            reason = "amount must be positive, at most two decimal places, and within the contract maximum.";
            return false;
        }

        var invoiceReference = Normalize(fields[4]);
        var memo = Normalize(fields[5]);
        if (invoiceReference?.Length > 80 || memo?.Length > 500)
        {
            reason = "invoiceReference or memo exceeds the contract limit.";
            return false;
        }

        row = new PaymentReceiptCreateRequestDto(externalReference, receivedDate, currency, amount, invoiceReference, memo);
        reason = string.Empty;
        return true;
    }

    private static IReadOnlyList<string>? ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == ',' && !quoted)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }
        if (quoted) return null;
        fields.Add(current.ToString());
        return fields;
    }

    public static BillingInvoiceDto MapInvoice(BillingInvoice invoice) => new(
        invoice.Id, invoice.InvoiceNumber, invoice.PolicyId, invoice.PolicyVersionId, invoice.AccountId,
        invoice.Currency, invoice.OriginalAmount, invoice.OutstandingAmount, invoice.InvoiceDate,
        invoice.DueDate, invoice.Status, invoice.CreatedAt, invoice.CreatedByUserId, invoice.RowVersion.ToString(CultureInfo.InvariantCulture));

    public static PaymentReceiptDto MapReceipt(PaymentReceipt receipt) => new(
        receipt.Id, receipt.Source, receipt.ExternalReference, receipt.ReceivedDate, receipt.Currency,
        receipt.Amount, receipt.InvoiceReference, receipt.Memo, receipt.ImportBatchId, receipt.ImportRowNumber,
        receipt.ApplicationStatus, receipt.RowVersion.ToString(CultureInfo.InvariantCulture));

    public static PaymentApplicationDto MapApplication(PaymentApplication application) => new(
        application.Id, application.BillingInvoiceId, application.PaymentReceiptId, application.Currency,
        application.AppliedAmount, application.InvoiceOutstandingBefore, application.InvoiceOutstandingAfter,
        application.AppliedAt, application.AppliedByUserId);

    public static ReconciliationExceptionDto MapException(ReconciliationException exception) => new(
        exception.Id, exception.Type, exception.BillingInvoiceId, exception.PaymentReceiptId, exception.ImportBatchId,
        exception.ImportRowOutcomeId, exception.Status, exception.OpenedAt, exception.OpenedByUserId,
        exception.ResolvedAt, exception.ResolvedByUserId, exception.ResolutionCode, exception.ResolutionNote,
        exception.Corrections.Where(correction => correction.Status == "Pending")
            .OrderByDescending(correction => correction.RequestedAt)
            .Select(MapCorrection)
            .FirstOrDefault(),
        exception.RowVersion.ToString(CultureInfo.InvariantCulture));

    public static BillingCorrectionDto MapCorrection(BillingCorrection correction) => new(
        correction.Id, correction.ReconciliationExceptionId, correction.BillingInvoiceId,
        correction.BeforeOutstandingAmount, correction.CorrectionAmount, correction.ProposedOutstandingAmount,
        correction.Reason, correction.EvidenceNote, correction.Status, correction.RequestedByUserId,
        correction.RequestedAt, correction.DecisionByUserId, correction.DecisionAt, correction.DecisionNote,
        correction.RowVersion.ToString(CultureInfo.InvariantCulture));

    public static TimelineEventDto MapTimelineEvent(ActivityTimelineEvent activity) => new(
        activity.Id,
        activity.EntityType,
        activity.EntityId,
        activity.EventType,
        activity.EventDescription,
        null,
        activity.ActorDisplayName ?? "Unknown User",
        activity.OccurredAt);

    private static PaymentReceiptImportResultDto MapImport(PaymentReceiptImportBatch batch) => new(
        batch.Id, batch.ContractVersion, batch.FileName, batch.FileSha256, batch.Status, batch.SubmittedCount,
        batch.CreatedCount, batch.DuplicateCount, batch.RejectedCount,
        batch.Outcomes.OrderBy(outcome => outcome.RowNumber).Select(outcome => new PaymentReceiptImportRowOutcomeDto(
            outcome.RowNumber, outcome.ExternalReference, outcome.Outcome, outcome.PaymentReceiptId,
            outcome.ReasonCode, outcome.ReasonDetail)).ToList());

    private static string NormalizeIdentifier(string value) => value.Trim().ToUpperInvariant();
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool EqualsOrdinal(string left, string right) => string.Equals(left, right, StringComparison.Ordinal);
}
