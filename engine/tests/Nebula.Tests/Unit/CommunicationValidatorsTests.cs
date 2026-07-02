using Nebula.Application.DTOs;
using Nebula.Application.Validators;
using Shouldly;

namespace Nebula.Tests.Unit;

public class CommunicationValidatorsTests
{
    private readonly Guid _entityId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private readonly Guid _assigneeId = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");

    [Fact]
    public void CreateRequest_ValidNoteWithPrimaryLink_Passes()
    {
        var validator = new CommunicationEventCreateRequestValidator();

        var result = validator.Validate(CreateRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CreateRequest_MultiplePrimaryLinks_Fails()
    {
        var validator = new CommunicationEventCreateRequestValidator();
        var request = CreateRequest(links:
        [
            new CommunicationLinkDto("Account", _entityId, true),
            new CommunicationLinkDto("Broker", Guid.NewGuid(), true),
        ]);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Exactly one primary link is required.");
    }

    [Fact]
    public void CreateRequest_EmailReferenceRequiresMessageId()
    {
        var validator = new CommunicationEventCreateRequestValidator();
        var request = CreateRequest(
            type: "EmailReference",
            emailReference: new CommunicationEmailReferenceDto("Exchange", "", "Renewal thread", DateTime.UtcNow));

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == "EmailReference.MessageId");
    }

    [Fact]
    public void FollowUpRequest_UnsupportedPriority_Fails()
    {
        var validator = new CommunicationEventFollowUpRequestValidator();
        var request = new CommunicationEventFollowUpRequestDto(
            "Call broker",
            null,
            "Urgent",
            DateTime.UtcNow.AddDays(1),
            _assigneeId,
            "Account",
            _entityId);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "Priority must be Low, Normal, or High.");
    }

    [Fact]
    public void CorrectionRequest_RequiresCorrectedFieldsForCorrectAction()
    {
        var validator = new CommunicationEventCorrectionRequestValidator();
        var request = new CommunicationEventCorrectionRequestDto("Correct", "Typo", null, null, null, null);

        var result = validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.ErrorMessage == "At least one correction field is required.");
    }

    private CommunicationEventCreateRequestDto CreateRequest(
        string type = "Note",
        IReadOnlyList<CommunicationLinkDto>? links = null,
        CommunicationEmailReferenceDto? emailReference = null)
    {
        return new CommunicationEventCreateRequestDto(
            type,
            "Internal",
            "Discussed renewal readiness",
            "Capture follow-up context.",
            DateTime.UtcNow,
            emailReference,
            [new CommunicationParticipantDto("Jane Broker", "jane@example.com", "External", "Broker", "Broker", Guid.NewGuid())],
            links ?? [new CommunicationLinkDto("Account", _entityId, true)],
            null);
    }
}
