using Shouldly;
using Nebula.Domain.Entities;

namespace Nebula.Tests;

public class SmokeTests
{
    [Fact]
    public void Domain_BaseEntity_HasRequiredAuditFields()
    {
        var properties = typeof(BaseEntity).GetProperties();
        var names = properties.Select(p => p.Name).ToList();

        names.ShouldContain("Id");
        names.ShouldContain("CreatedAt");
        names.ShouldContain("CreatedByUserId");
        names.ShouldContain("UpdatedAt");
        names.ShouldContain("UpdatedByUserId");
        names.ShouldContain("IsDeleted");
        names.ShouldContain("DeletedAt");
        names.ShouldContain("DeletedByUserId");
        names.ShouldContain("RowVersion");
    }

    [Fact]
    public void Domain_BaseEntity_GeneratesNewGuidById()
    {
        var entity = new TestEntity();
        entity.Id.ShouldNotBe(Guid.Empty);
    }

    [Theory]
    [InlineData(typeof(Account), new[] { "Name", "Industry", "PrimaryState", "Region", "Status" })]
    [InlineData(typeof(MGA), new[] { "Name", "ExternalCode", "Status" })]
    [InlineData(typeof(Nebula.Domain.Entities.Program), new[] { "Name", "ProgramCode", "MgaId", "ManagedByUserId" })]
    [InlineData(typeof(Broker), new[] { "LegalName", "LicenseNumber", "State", "Status", "Email", "Phone", "BrokerTenantId", "ManagedByUserId", "MgaId", "PrimaryProgramId" })]
    [InlineData(typeof(Contact), new[] { "BrokerId", "AccountId", "FullName", "Email", "Phone", "Role" })]
    [InlineData(typeof(Submission), new[] { "AccountId", "BrokerId", "ProgramId", "LineOfBusiness", "CurrentStatus", "EffectiveDate", "PremiumEstimate", "AssignedToUserId" })]
    [InlineData(typeof(Renewal), new[] { "AccountId", "BrokerId", "PolicyId", "LineOfBusiness", "CurrentStatus", "PolicyExpirationDate", "TargetOutreachDate", "AssignedToUserId", "LostReasonCode", "LostReasonDetail", "BoundPolicyId", "RenewalSubmissionId" })]
    [InlineData(typeof(TaskItem), new[] { "Title", "Description", "Status", "Priority", "DueDate", "AssignedToUserId", "LinkedEntityType", "LinkedEntityId", "CompletedAt" })]
    public void Domain_BaseEntityDescendant_HasExpectedProperties(Type entityType, string[] expectedProperties)
    {
        entityType.IsSubclassOf(typeof(BaseEntity)).ShouldBeTrue();
        var names = entityType.GetProperties().Select(p => p.Name).ToList();
        foreach (var prop in expectedProperties)
        {
            names.ShouldContain(prop, $"{entityType.Name} should have property {prop}");
        }
    }

    [Theory]
    [InlineData(typeof(ActivityTimelineEvent), new[] { "Id", "EntityType", "EntityId", "EventType", "EventPayloadJson", "EventDescription", "BrokerDescription", "ActorUserId", "ActorDisplayName", "OccurredAt" })]
    [InlineData(typeof(WorkflowTransition), new[] { "Id", "WorkflowType", "EntityId", "FromState", "ToState", "Reason", "ActorUserId", "OccurredAt" })]
    public void Domain_AppendOnlyEntity_DoesNotInheritBaseEntity(Type entityType, string[] expectedProperties)
    {
        entityType.IsSubclassOf(typeof(BaseEntity)).ShouldBeFalse();
        var names = entityType.GetProperties().Select(p => p.Name).ToList();
        foreach (var prop in expectedProperties)
        {
            names.ShouldContain(prop, $"{entityType.Name} should have property {prop}");
        }
    }

    [Fact]
    public void Domain_UserProfile_HasOwnAuditFieldsNotBaseEntity()
    {
        typeof(UserProfile).IsSubclassOf(typeof(BaseEntity)).ShouldBeFalse();
        var names = typeof(UserProfile).GetProperties().Select(p => p.Name).ToList();
        names.ShouldContain("IdpIssuer");
        names.ShouldContain("IdpSubject");
        names.ShouldContain("Email");
        names.ShouldContain("DisplayName");
        names.ShouldContain("Department");
        names.ShouldContain("RegionsJson");
        names.ShouldContain("RolesJson");
        names.ShouldContain("IsActive");
        names.ShouldContain("CreatedAt");
        names.ShouldContain("UpdatedAt");
    }

    [Fact]
    public void Domain_BrokerRegion_HasCompositePKProperties()
    {
        typeof(BrokerRegion).IsSubclassOf(typeof(BaseEntity)).ShouldBeFalse();
        var names = typeof(BrokerRegion).GetProperties().Select(p => p.Name).ToList();
        names.ShouldContain("BrokerId");
        names.ShouldContain("Region");
    }

    [Theory]
    [InlineData(typeof(ReferenceTaskStatus), new[] { "Code", "DisplayName", "DisplayOrder" })]
    [InlineData(typeof(ReferenceSubmissionStatus), new[] { "Code", "DisplayName", "Description", "IsTerminal", "DisplayOrder", "ColorGroup" })]
    [InlineData(typeof(ReferenceRenewalStatus), new[] { "Code", "DisplayName", "Description", "IsTerminal", "DisplayOrder", "ColorGroup" })]
    public void Domain_ReferenceEntity_HasCodeAndDisplayFields(Type entityType, string[] expectedProperties)
    {
        entityType.IsSubclassOf(typeof(BaseEntity)).ShouldBeFalse();
        var names = entityType.GetProperties().Select(p => p.Name).ToList();
        foreach (var prop in expectedProperties)
        {
            names.ShouldContain(prop, $"{entityType.Name} should have property {prop}");
        }
    }

    private class TestEntity : BaseEntity { }
}
