using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class CarrierMarketEndpoints
{
    public static RouteGroupBuilder MapCarrierMarketEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/carrier-markets")
            .WithTags("Carrier Markets")
            .RequireAuthorization();

        group.MapGet("/", ListCarrierMarkets);
        group.MapPost("/", CreateCarrierMarket);
        group.MapGet("/{carrierMarketId:guid}", GetCarrierMarket);
        group.MapPut("/{carrierMarketId:guid}", UpdateCarrierMarket);
        group.MapDelete("/{carrierMarketId:guid}", DeleteCarrierMarket);

        group.MapPost("/{carrierMarketId:guid}/contacts", AddContact);
        group.MapPut("/{carrierMarketId:guid}/contacts/{contactId:guid}", UpdateContact);
        group.MapDelete("/{carrierMarketId:guid}/contacts/{contactId:guid}", DeleteContact);

        group.MapPost("/{carrierMarketId:guid}/appetite-notes", AddAppetiteNote);
        group.MapPut("/{carrierMarketId:guid}/appetite-notes/{noteId:guid}", UpdateAppetiteNote);
        group.MapDelete("/{carrierMarketId:guid}/appetite-notes/{noteId:guid}", DeleteAppetiteNote);

        group.MapPost("/{carrierMarketId:guid}/appointments", AddAppointment);
        group.MapPut("/{carrierMarketId:guid}/appointments/{appointmentId:guid}", UpdateAppointment);
        group.MapDelete("/{carrierMarketId:guid}/appointments/{appointmentId:guid}", DeleteAppointment);

        group.MapPost("/{carrierMarketId:guid}/activity-links", AddActivityLink);

        return group;
    }

    private static async Task<IResult> ListCarrierMarkets(
        string? search,
        string? status,
        string? marketType,
        int? page,
        int? pageSize,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "search"))
            return ProblemDetailsHelper.Forbidden();

        var result = await svc.ListAsync(new CarrierMarketListQuery(search, status, marketType, page ?? 1, pageSize ?? 20), ct);
        return Results.Ok(new
        {
            data = result.Data,
            page = result.Page,
            pageSize = result.PageSize,
            totalCount = result.TotalCount,
            totalPages = result.TotalPages,
        });
    }

    private static async Task<IResult> GetCarrierMarket(
        Guid carrierMarketId,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "read"))
            return ProblemDetailsHelper.Forbidden();

        var result = await svc.GetByIdAsync(carrierMarketId, ct);
        return result is null ? ProblemDetailsHelper.NotFound("Carrier market", carrierMarketId) : Results.Ok(result);
    }

    private static async Task<IResult> CreateCarrierMarket(
        CarrierMarketCreateDto dto,
        IValidator<CarrierMarketCreateDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "create"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.CreateAsync(dto, user, ct);
        return error switch
        {
            "duplicate_code" => Results.Problem(title: "Duplicate carrier market code", detail: "A carrier market with the given code already exists.", statusCode: 409, extensions: Ext("duplicate_carrier_market_code")),
            _ => Results.Created($"/carrier-markets/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateCarrierMarket(
        Guid carrierMarketId,
        CarrierMarketUpdateDto dto,
        IValidator<CarrierMarketUpdateDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "update"))
            return ProblemDetailsHelper.Forbidden();

        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return Results.Problem(title: "If-Match header required", statusCode: 428);

        try
        {
            var (result, error) = await svc.UpdateAsync(carrierMarketId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Carrier market", carrierMarketId),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.ConcurrencyConflict();
        }
    }

    private static async Task<IResult> DeleteCarrierMarket(
        Guid carrierMarketId,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "update"))
            return ProblemDetailsHelper.Forbidden();

        var error = await svc.DeleteAsync(carrierMarketId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier market", carrierMarketId),
            _ => Results.NoContent(),
        };
    }

    private static async Task<IResult> AddContact(
        Guid carrierMarketId,
        CarrierMarketContactUpsertDto dto,
        IValidator<CarrierMarketContactUpsertDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_contact"))
            return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.AddContactAsync(carrierMarketId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier market", carrierMarketId),
            _ => Results.Created($"/carrier-markets/{carrierMarketId}/contacts/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateContact(
        Guid carrierMarketId,
        Guid contactId,
        CarrierMarketContactUpsertDto dto,
        IValidator<CarrierMarketContactUpsertDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_contact"))
            return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return Results.Problem(title: "If-Match header required", statusCode: 428);

        try
        {
            var (result, error) = await svc.UpdateContactAsync(carrierMarketId, contactId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Carrier market contact", contactId),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.ConcurrencyConflict();
        }
    }

    private static async Task<IResult> DeleteContact(
        Guid carrierMarketId,
        Guid contactId,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_contact"))
            return ProblemDetailsHelper.Forbidden();
        var error = await svc.DeleteContactAsync(carrierMarketId, contactId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier market contact", contactId),
            _ => Results.NoContent(),
        };
    }

    private static async Task<IResult> AddAppetiteNote(
        Guid carrierMarketId,
        CarrierAppetiteNoteUpsertDto dto,
        IValidator<CarrierAppetiteNoteUpsertDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_appetite"))
            return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.AddAppetiteNoteAsync(carrierMarketId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier market", carrierMarketId),
            _ => Results.Created($"/carrier-markets/{carrierMarketId}/appetite-notes/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateAppetiteNote(
        Guid carrierMarketId,
        Guid noteId,
        CarrierAppetiteNoteUpsertDto dto,
        IValidator<CarrierAppetiteNoteUpsertDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_appetite"))
            return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return Results.Problem(title: "If-Match header required", statusCode: 428);

        try
        {
            var (result, error) = await svc.UpdateAppetiteNoteAsync(carrierMarketId, noteId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Carrier appetite note", noteId),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.ConcurrencyConflict();
        }
    }

    private static async Task<IResult> DeleteAppetiteNote(
        Guid carrierMarketId,
        Guid noteId,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_appetite"))
            return ProblemDetailsHelper.Forbidden();
        var error = await svc.DeleteAppetiteNoteAsync(carrierMarketId, noteId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier appetite note", noteId),
            _ => Results.NoContent(),
        };
    }

    private static async Task<IResult> AddAppointment(
        Guid carrierMarketId,
        CarrierAppointmentUpsertDto dto,
        IValidator<CarrierAppointmentUpsertDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_appointment"))
            return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.AddAppointmentAsync(carrierMarketId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier market", carrierMarketId),
            _ => Results.Created($"/carrier-markets/{carrierMarketId}/appointments/{result!.Id}", result),
        };
    }

    private static async Task<IResult> UpdateAppointment(
        Guid carrierMarketId,
        Guid appointmentId,
        CarrierAppointmentUpsertDto dto,
        IValidator<CarrierAppointmentUpsertDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_appointment"))
            return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);
        if (!TryGetRowVersion(httpContext, out var rowVersion)) return Results.Problem(title: "If-Match header required", statusCode: 428);

        try
        {
            var (result, error) = await svc.UpdateAppointmentAsync(carrierMarketId, appointmentId, dto, rowVersion, user, ct);
            return error switch
            {
                "not_found" => ProblemDetailsHelper.NotFound("Carrier appointment", appointmentId),
                _ => Results.Ok(result),
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return ProblemDetailsHelper.ConcurrencyConflict();
        }
    }

    private static async Task<IResult> DeleteAppointment(
        Guid carrierMarketId,
        Guid appointmentId,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "manage_appointment"))
            return ProblemDetailsHelper.Forbidden();
        var error = await svc.DeleteAppointmentAsync(carrierMarketId, appointmentId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier appointment", appointmentId),
            _ => Results.NoContent(),
        };
    }

    private static async Task<IResult> AddActivityLink(
        Guid carrierMarketId,
        CarrierMarketActivityLinkCreateDto dto,
        IValidator<CarrierMarketActivityLinkCreateDto> validator,
        CarrierMarketService svc,
        ICurrentUserService user,
        IAuthorizationService authz,
        CancellationToken ct)
    {
        if (!await HasAccessAsync(user, authz, "carrier_market", "link_activity"))
            return ProblemDetailsHelper.Forbidden();
        var validation = await validator.ValidateAsync(dto, ct);
        if (!validation.IsValid) return ValidationProblem(validation);

        var (result, error) = await svc.AddActivityLinkAsync(carrierMarketId, dto, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Carrier market", carrierMarketId),
            "related_not_found" => Results.Problem(title: "Related entity not found", detail: "The related submission or policy could not be found.", statusCode: 404, extensions: Ext("related_not_found")),
            _ => Results.Created($"/carrier-markets/{carrierMarketId}/activity-links/{result!.Id}", result),
        };
    }

    private static IResult ValidationProblem(FluentValidation.Results.ValidationResult validation) =>
        ProblemDetailsHelper.ValidationError(
            validation.Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));

    private static async Task<bool> HasAccessAsync(
        ICurrentUserService user,
        IAuthorizationService authz,
        string resource,
        string action)
    {
        foreach (var role in user.Roles)
        {
            if (await authz.AuthorizeAsync(role, resource, action))
                return true;
        }

        return false;
    }

    private static bool TryGetRowVersion(HttpContext httpContext, out uint rowVersion)
    {
        var ifMatch = httpContext.Request.Headers.IfMatch.FirstOrDefault();
        return uint.TryParse(ifMatch?.Trim('"'), out rowVersion);
    }

    private static Dictionary<string, object?> Ext(string code) => new()
    {
        ["code"] = code,
        ["traceId"] = System.Diagnostics.Activity.Current?.Id,
    };
}
