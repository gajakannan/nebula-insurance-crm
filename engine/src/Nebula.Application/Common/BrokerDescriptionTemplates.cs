namespace Nebula.Application.Common;

/// <summary>
/// Predefined broker-safe description templates for BrokerUser-visible timeline events.
/// Source: BROKER-VISIBILITY-MATRIX.md §BrokerDescription Template Ownership.
///
/// Templates must NOT include: internal user names, user IDs, system references,
/// policy codes, or any InternalOnly field values.
/// Template additions require Security Agent approval.
/// </summary>
public static class BrokerDescriptionTemplates
{
    public const string BrokerCreated = "Broker record created.";
    public const string BrokerUpdated = "Broker profile updated.";
    /// <summary>Format with {0} = newStatus value.</summary>
    public const string BrokerStatusChanged = "Broker status changed to {0}.";
    public const string ContactAdded = "Contact added to broker.";
    public const string ContactUpdated = "Contact record updated.";
}
