namespace Nebula.Domain.Entities;

public class CarrierMarket : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? NaicCode { get; set; }
    public string? AmBestRating { get; set; }
    public string Status { get; set; } = "Prospect";
    public string MarketType { get; set; } = "Other";
    public Guid? RelationshipOwnerUserId { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? GeneralEmail { get; set; }
    public string? MainPhone { get; set; }
    public string? Notes { get; set; }

    public ICollection<CarrierMarketContact> Contacts { get; set; } = new List<CarrierMarketContact>();
    public ICollection<CarrierAppetiteNote> AppetiteNotes { get; set; } = new List<CarrierAppetiteNote>();
    public ICollection<CarrierAppointment> Appointments { get; set; } = new List<CarrierAppointment>();
    public ICollection<CarrierMarketActivityLink> ActivityLinks { get; set; } = new List<CarrierMarketActivityLink>();
}
