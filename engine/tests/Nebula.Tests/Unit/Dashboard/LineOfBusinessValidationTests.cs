using Shouldly;
using Nebula.Application.DTOs;
using Nebula.Application.Validators;

namespace Nebula.Tests.Unit.Dashboard;

public class LineOfBusinessValidationTests
{
    [Fact]
    public void SubmissionCreateValidator_AcceptsKnownLineOfBusiness()
    {
        var validator = new SubmissionCreateValidator();
        var model = ValidModel() with { LineOfBusiness = "Property" };

        var result = validator.Validate(model);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SubmissionCreateValidator_AcceptsNullLineOfBusiness()
    {
        var validator = new SubmissionCreateValidator();
        var model = ValidModel() with { LineOfBusiness = null };

        var result = validator.Validate(model);

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SubmissionCreateValidator_RejectsInvalidLineOfBusiness()
    {
        var validator = new SubmissionCreateValidator();
        var model = ValidModel() with { LineOfBusiness = "Aviation" };

        var result = validator.Validate(model);

        result.IsValid.ShouldBeFalse();
        var lobError = result.Errors.Single(error => error.PropertyName == "LineOfBusiness");
        lobError.ShouldNotBeNull();
        result.Errors.Count(error => error.PropertyName == "LineOfBusiness").ShouldBe(1);
    }

    private static SubmissionCreateDto ValidModel() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        DateTime.UtcNow,
        Guid.NewGuid(),
        "Property",
        150000m,
        DateTime.UtcNow.AddMonths(12),
        "Test submission");
}
