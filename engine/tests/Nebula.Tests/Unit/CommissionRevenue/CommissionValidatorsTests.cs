using Nebula.Application.DTOs;
using Nebula.Application.Validators;
using Shouldly;

namespace Nebula.Tests.Unit.CommissionRevenue;

public class CommissionValidatorsTests
{
    [Fact]
    public async Task ScheduleValidator_RequiresRateOrFlatAmountAndSourceNote()
    {
        var validator = new CommissionScheduleUpsertValidator();
        var result = await validator.ValidateAsync(new CommissionScheduleUpsertDto(
            Guid.NewGuid(),
            "Cyber",
            null,
            null,
            "premium_percent",
            null,
            null,
            new DateOnly(2026, 1, 1),
            null,
            ""));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage.Contains("RatePercent or FlatAmount"));
        result.Errors.ShouldContain(error => error.PropertyName == "SourceNote");
    }

    [Fact]
    public async Task SplitValidator_RequiresParticipantsToTotalExactlyOneHundred()
    {
        var validator = new ProducerSplitAssignmentUpsertValidator();
        var result = await validator.ValidateAsync(new ProducerSplitAssignmentUpsertDto(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            null,
            "New split",
            [
                new ProducerSplitParticipantUpsertDto(Guid.NewGuid(), 70m),
                new ProducerSplitParticipantUpsertDto(Guid.NewGuid(), 20m),
            ]));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage.Contains("total 100"));
    }

    [Fact]
    public async Task SplitValidator_DeniesDuplicateProducerParticipants()
    {
        var producerId = Guid.NewGuid();
        var validator = new ProducerSplitAssignmentUpsertValidator();
        var result = await validator.ValidateAsync(new ProducerSplitAssignmentUpsertDto(
            Guid.NewGuid(),
            new DateOnly(2026, 1, 1),
            null,
            "Duplicate split",
            [
                new ProducerSplitParticipantUpsertDto(producerId, 50m),
                new ProducerSplitParticipantUpsertDto(producerId, 50m),
            ]));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage.Contains("once"));
    }

    [Fact]
    public async Task RollupValidator_RejectsInvalidDateWindow()
    {
        var validator = new RevenueAttributionRollupQueryValidator();
        var result = await validator.ValidateAsync(new RevenueAttributionRollupQuery(
            new DateOnly(2026, 2, 1),
            new DateOnly(2026, 1, 1),
            "producer",
            null,
            null,
            null,
            null,
            null,
            null));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage.Contains("EndDate"));
    }
}
