using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class BillingEndpoints
{
    public static RouteGroupBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var billing = app.MapGroup("/")
            .WithTags("Billing")
            .RequireAuthorization();

        billing.MapGet("/billing-invoices", ListInvoices);
        billing.MapPost("/billing-invoices", CreateInvoice);
        billing.MapGet("/billing-invoices/{invoiceId:guid}", GetInvoice);
        billing.MapGet("/policies/{policyId:guid}/billing-summary", GetPolicyBillingSummary);
        billing.MapGet("/payment-receipts", ListReceipts);
        billing.MapPost("/payment-receipts", CreateReceipt);
        billing.MapPost("/payment-receipt-imports", ImportReceipts).DisableAntiforgery();
        billing.MapGet("/payment-receipt-imports/{batchId:guid}", GetImport);
        billing.MapPost("/payment-applications", ApplyPayment);
        billing.MapGet("/reconciliation-exceptions", ListExceptions);
        billing.MapPatch("/reconciliation-exceptions/{exceptionId:guid}/reference", CorrectReference);
        billing.MapPost("/reconciliation-exceptions/{exceptionId:guid}/corrections", RequestCorrection);
        billing.MapPost("/billing-corrections/{correctionId:guid}/decision", DecideCorrection);
        billing.MapGet("/reconciliation-backlog", GetBacklog);

        return billing;
    }

    private static async Task<IResult> ListInvoices(
        string? q,
        Guid? policyId,
        Guid? accountId,
        string? status,
        bool? hasOpenException,
        int? page,
        int? pageSize,
        BillingReconciliationService service,
        IValidator<BillingInvoiceSearchQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "read")) return ProblemDetailsHelper.Forbidden();
        var query = new BillingInvoiceSearchQuery(q, policyId, accountId, status, hasOpenException, page ?? 1, pageSize ?? 25);
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        var result = await service.SearchInvoicesAsync(query, user, ct);
        return Page(result.Data, result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    private static async Task<IResult> CreateInvoice(
        BillingInvoiceCreateRequestDto dto,
        IValidator<BillingInvoiceCreateRequestDto> validator,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "invoice_create")) return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        try
        {
            var (result, error) = await service.CreateInvoiceAsync(dto, user, ct);
            return error switch
            {
                "invoice_number_conflict" => Conflict("billing_invoice_number_conflict", "Invoice number already exists."),
                "policy_context_not_found" => Unprocessable("billing_policy_context_not_found", "The policy/version context is not available in the caller's source scope."),
                "policy_context_mismatch" => Unprocessable("billing_policy_context_mismatch", "Account and currency must match the selected policy version."),
                _ => Results.Created($"/billing-invoices/{result!.Id}", result),
            };
        }
        catch (DbUpdateException)
        {
            return Conflict("billing_invoice_number_conflict", "Invoice number already exists.");
        }
    }

    private static async Task<IResult> GetInvoice(
        Guid invoiceId,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "read")) return ProblemDetailsHelper.Forbidden();
        var result = await service.GetInvoiceAsync(invoiceId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Billing invoice", invoiceId) : Results.Ok(result);
    }

    private static async Task<IResult> GetPolicyBillingSummary(
        Guid policyId,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "summary_read")) return ProblemDetailsHelper.Forbidden();
        var result = await service.GetPolicyBillingSummaryAsync(policyId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Policy", policyId) : Results.Ok(result);
    }

    private static async Task<IResult> ListReceipts(
        string? applicationStatus,
        string? externalReference,
        string? currency,
        int? page,
        int? pageSize,
        BillingReconciliationService service,
        IValidator<PaymentReceiptSearchQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "read")) return ProblemDetailsHelper.Forbidden();
        var query = new PaymentReceiptSearchQuery(applicationStatus, externalReference, currency, page ?? 1, pageSize ?? 25);
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        var result = await service.SearchReceiptsAsync(query, user, ct);
        return Page(result.Data, result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    private static async Task<IResult> CreateReceipt(
        PaymentReceiptCreateRequestDto dto,
        IValidator<PaymentReceiptCreateRequestDto> validator,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "receipt_record")) return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        try
        {
            var (result, error) = await service.CreateManualReceiptAsync(dto, user, ct);
            return error == "receipt_reference_conflict"
                ? Conflict("payment_receipt_reference_conflict", "The manual source/reference pair already exists.")
                : Results.Created($"/payment-receipts/{result!.Id}", result);
        }
        catch (DbUpdateException)
        {
            return Conflict("payment_receipt_reference_conflict", "The manual source/reference pair already exists.");
        }
    }

    private static async Task<IResult> ImportReceipts(
        IFormFile file,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "receipt_import")) return ProblemDetailsHelper.Forbidden();
        if (file.Length > 1024 * 1024)
            return Results.Problem(title: "Import file is too large", statusCode: StatusCodes.Status413PayloadTooLarge, extensions: Ext("payment_receipt_import_too_large"));

        await using var stream = file.OpenReadStream();
        var (result, error) = await service.ImportMockReceiptsAsync(stream, file.FileName, file.Length, user, ct);
        return error switch
        {
            "file_too_large" => Results.Problem(title: "Import file is too large", statusCode: StatusCodes.Status413PayloadTooLarge, extensions: Ext("payment_receipt_import_too_large")),
            null => Results.Created($"/payment-receipt-imports/{result!.ImportBatchId}", result),
            _ => Unprocessable($"payment_receipt_import_{error}", "The file does not satisfy the mock-payment-receipt-row-v1 contract."),
        };
    }

    private static async Task<IResult> GetImport(
        Guid batchId,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "read")) return ProblemDetailsHelper.Forbidden();
        var result = await service.GetImportAsync(batchId, user, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Payment receipt import", batchId) : Results.Ok(result);
    }

    private static async Task<IResult> ApplyPayment(
        PaymentApplicationRequestDto dto,
        IValidator<PaymentApplicationRequestDto> validator,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "application_apply")) return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var invoiceRowVersion)) return PreconditionRequired();
        try
        {
            var (result, error) = await service.ApplyExactPaymentAsync(dto, invoiceRowVersion, user, ct);
            return error switch
            {
                "not_found" => Results.NotFound(),
                "precondition_failed" => PreconditionFailed(),
                "already_applied" => Conflict("payment_application_conflict", "The invoice or receipt is no longer available for application."),
                "invoice_reference_conflict" => Unprocessable("payment_application_invoice_reference_conflict", "The receipt source reference conflicts with the selected invoice."),
                "currency_mismatch" => Unprocessable("payment_application_currency_mismatch", "Receipt and invoice currencies must match."),
                "amount_mismatch" => Unprocessable("payment_application_amount_mismatch", "Receipt amount must equal the full outstanding amount."),
                _ => Results.Created($"/payment-applications/{result!.Id}", result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return PreconditionFailed();
        }
    }

    private static async Task<IResult> ListExceptions(
        string? status,
        string? type,
        int? page,
        int? pageSize,
        BillingReconciliationService service,
        IValidator<ReconciliationExceptionSearchQuery> validator,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "backlog_read")) return ProblemDetailsHelper.Forbidden();
        var query = new ReconciliationExceptionSearchQuery(status, type, page ?? 1, pageSize ?? 25);
        var validation = await validator.ValidateAsync(query, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        var result = await service.SearchExceptionsAsync(query, user, ct);
        return Page(result.Data, result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    private static async Task<IResult> CorrectReference(
        Guid exceptionId,
        ReconciliationReferenceCorrectionRequestDto dto,
        IValidator<ReconciliationReferenceCorrectionRequestDto> validator,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "exception_manage")) return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return PreconditionRequired();
        try
        {
            var (result, error) = await service.CorrectReferenceAsync(exceptionId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Reconciliation exception", exceptionId),
                "invoice_not_found" => ProblemDetailsHelper.NotFound("Billing invoice", dto.BillingInvoiceId),
                "precondition_failed" => PreconditionFailed(),
                "exception_not_open" => Conflict("reconciliation_exception_not_open", "The exception is no longer open."),
                "reference_not_correctable" => Conflict("reconciliation_reference_not_correctable", "Only missing or conflicting invoice references can be corrected here."),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return PreconditionFailed();
        }
    }

    private static async Task<IResult> RequestCorrection(
        Guid exceptionId,
        BillingCorrectionRequestDto dto,
        IValidator<BillingCorrectionRequestDto> validator,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "correction_request")) return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return PreconditionRequired();
        try
        {
            var (result, error) = await service.RequestCorrectionAsync(exceptionId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Reconciliation exception", exceptionId),
                "invoice_not_found" => Results.NotFound(),
                "precondition_failed" => PreconditionFailed(),
                "correction_pending" => Conflict("billing_correction_pending", "A pending correction already exists for this exception."),
                "exception_not_eligible" => Conflict("billing_correction_exception_not_eligible", "The exception is not eligible for a balance correction."),
                "correction_amount_inconsistent" => Unprocessable("billing_correction_amount_inconsistent", "Proposed outstanding must equal current outstanding plus correction amount and cannot exceed original amount."),
                _ => Results.Created($"/billing-corrections/{result!.Id}", result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return PreconditionFailed();
        }
    }

    private static async Task<IResult> DecideCorrection(
        Guid correctionId,
        BillingCorrectionDecisionRequestDto dto,
        IValidator<BillingCorrectionDecisionRequestDto> validator,
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "correction_approve")) return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return PreconditionRequired();
        try
        {
            var (result, error) = await service.DecideCorrectionAsync(correctionId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Billing correction", correctionId),
                "precondition_failed" => PreconditionFailed(),
                "correction_not_pending" => Conflict("billing_correction_not_pending", "The correction already has a terminal decision."),
                "same_user_decision_denied" => ProblemDetailsHelper.Forbidden(),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return PreconditionFailed();
        }
    }

    private static async Task<IResult> GetBacklog(
        BillingReconciliationService service,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(authz, user, "backlog_summary_read")
            && !await HasAccessAsync(authz, user, "backlog_read"))
            return ProblemDetailsHelper.Forbidden();
        return Results.Ok(await service.GetBacklogAsync(user, ct));
    }

    private static Task<bool> HasAccessAsync(IAuthorizationService authz, ICurrentUserService user, string action) =>
        AuthzHelper.HasPermissionAsync(authz, user, "billing", action, new Dictionary<string, object> { ["subjectId"] = user.UserId });

    private static IResult Page<T>(IReadOnlyList<T> data, int page, int pageSize, int totalCount, int totalPages) =>
        Results.Ok(new { data, page, pageSize, totalCount, totalPages });

    private static IResult ValidationProblem(FluentValidation.Results.ValidationResult validation) =>
        Results.ValidationProblem(validation.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

    private static IResult Conflict(string code, string detail) =>
        Results.Problem(title: "Conflict", detail: detail, statusCode: StatusCodes.Status409Conflict, extensions: Ext(code));

    private static IResult Unprocessable(string code, string detail) =>
        Results.Problem(title: "Unprocessable content", detail: detail, statusCode: StatusCodes.Status422UnprocessableEntity, extensions: Ext(code));

    private static IResult PreconditionRequired() =>
        Results.Problem(title: "If-Match header required", statusCode: StatusCodes.Status428PreconditionRequired, extensions: Ext("if_match_required"));

    private static IResult PreconditionFailed() =>
        Results.Problem(
            title: "Precondition failed",
            detail: "The resource was modified by another user. Refresh and retry with current row versions.",
            statusCode: StatusCodes.Status412PreconditionFailed,
            extensions: Ext("precondition_failed"));

    private static Dictionary<string, object?> Ext(string code) => new() { ["code"] = code };

    private static bool TryGetRowVersion(HttpContext context, out uint rowVersion)
    {
        rowVersion = 0;
        if (!context.Request.Headers.TryGetValue("If-Match", out var values)) return false;
        var raw = values.FirstOrDefault()?.Trim('"');
        return uint.TryParse(raw, out rowVersion);
    }
}
