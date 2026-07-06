using FluentValidation;
using Nebula.Application.DTOs;

namespace Nebula.Application.Validators;

public class CarrierMarketCreateValidator : AbstractValidator<CarrierMarketCreateDto>
{
    public CarrierMarketCreateValidator() => Include(new CarrierMarketBaseValidator());
}

public class CarrierMarketUpdateValidator : AbstractValidator<CarrierMarketUpdateDto>
{
    public CarrierMarketUpdateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NaicCode).MaximumLength(10);
        RuleFor(x => x.AmBestRating).MaximumLength(10);
        RuleFor(x => x.Status).Must(CarrierMarketValidation.AllowedStatuses.Contains);
        RuleFor(x => x.MarketType).Must(CarrierMarketValidation.AllowedMarketTypes.Contains);
        RuleFor(x => x.WebsiteUrl).MaximumLength(500);
        RuleFor(x => x.GeneralEmail).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.GeneralEmail));
        RuleFor(x => x.MainPhone).Matches(CarrierMarketValidation.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.MainPhone));
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class CarrierMarketContactUpsertValidator : AbstractValidator<CarrierMarketContactUpsertDto>
{
    public CarrierMarketContactUpsertValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Title).MaximumLength(120);
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Phone).Matches(CarrierMarketValidation.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.Phone));
        RuleFor(x => x.Roles).NotEmpty();
        RuleForEach(x => x.Roles).Must(CarrierMarketValidation.AllowedContactRoles.Contains);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class CarrierAppetiteNoteUpsertValidator : AbstractValidator<CarrierAppetiteNoteUpsertDto>
{
    public CarrierAppetiteNoteUpsertValidator()
    {
        RuleFor(x => x.LineOfBusiness).MaximumLength(120);
        RuleFor(x => x.Region).MaximumLength(120);
        RuleFor(x => x.AppetiteLevel).Must(CarrierMarketValidation.AllowedAppetiteLevels.Contains);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(240);
        RuleFor(x => x.Detail).MaximumLength(4000);
        RuleFor(x => x.Source).MaximumLength(200);
        RuleFor(x => x).Must(x => !x.EffectiveFrom.HasValue || !x.EffectiveTo.HasValue || x.EffectiveFrom <= x.EffectiveTo)
            .WithMessage("EffectiveFrom must be on or before EffectiveTo.");
    }
}

public class CarrierAppointmentUpsertValidator : AbstractValidator<CarrierAppointmentUpsertDto>
{
    public CarrierAppointmentUpsertValidator()
    {
        RuleFor(x => x.AppointmentStatus).Must(CarrierMarketValidation.AllowedAppointmentStatuses.Contains);
        RuleForEach(x => x.States).Matches("^[A-Z]{2}$");
        RuleFor(x => x.LineOfBusiness).MaximumLength(120);
        RuleFor(x => x.AppointmentNumber).MaximumLength(80);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x).Must(x => !x.EffectiveDate.HasValue || !x.ExpirationDate.HasValue || x.EffectiveDate <= x.ExpirationDate)
            .WithMessage("EffectiveDate must be on or before ExpirationDate.");
    }
}

public class CarrierMarketActivityLinkCreateValidator : AbstractValidator<CarrierMarketActivityLinkCreateDto>
{
    public CarrierMarketActivityLinkCreateValidator()
    {
        RuleFor(x => x.RelatedEntityType).Must(CarrierMarketValidation.AllowedRelatedEntityTypes.Contains);
        RuleFor(x => x.RelatedEntityId).NotEmpty();
        RuleFor(x => x.RelationshipKind).Must(CarrierMarketValidation.AllowedRelationshipKinds.Contains);
        RuleFor(x => x.Note).MaximumLength(1000);
    }
}

internal class CarrierMarketBaseValidator : AbstractValidator<CarrierMarketCreateDto>
{
    public CarrierMarketBaseValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.NaicCode).MaximumLength(10);
        RuleFor(x => x.AmBestRating).MaximumLength(10);
        RuleFor(x => x.Status).Must(CarrierMarketValidation.AllowedStatuses.Contains);
        RuleFor(x => x.MarketType).Must(CarrierMarketValidation.AllowedMarketTypes.Contains);
        RuleFor(x => x.WebsiteUrl).MaximumLength(500);
        RuleFor(x => x.GeneralEmail).EmailAddress().MaximumLength(320).When(x => !string.IsNullOrWhiteSpace(x.GeneralEmail));
        RuleFor(x => x.MainPhone).Matches(CarrierMarketValidation.PhonePattern).When(x => !string.IsNullOrWhiteSpace(x.MainPhone));
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal static class CarrierMarketValidation
{
    public const string PhonePattern = @"^\+?[1-9]\d{7,14}$";
    public static readonly string[] AllowedStatuses = ["Active", "Inactive", "Prospect"];
    public static readonly string[] AllowedMarketTypes = ["Admitted", "NonAdmitted", "MGA", "Wholesaler", "Other"];
    public static readonly string[] AllowedContactRoles = ["Underwriter", "Marketing", "Claims", "LossControl", "Executive", "Operations", "Other"];
    public static readonly string[] AllowedAppetiteLevels = ["Preferred", "Open", "Selective", "Restricted", "Closed"];
    public static readonly string[] AllowedAppointmentStatuses = ["NotAppointed", "InProgress", "Appointed", "Suspended", "Terminated"];
    public static readonly string[] AllowedRelatedEntityTypes = ["Submission", "Policy"];
    public static readonly string[] AllowedRelationshipKinds = ["Marketed", "Quoted", "Bound", "Declined", "AppointedContext", "GeneralReference"];
}
