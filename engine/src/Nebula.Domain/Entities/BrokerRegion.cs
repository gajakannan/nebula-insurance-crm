namespace Nebula.Domain.Entities;

public class BrokerRegion
{
    public Guid BrokerId { get; set; }
    public string Region { get; set; } = default!;

    public Broker Broker { get; set; } = default!;
}
