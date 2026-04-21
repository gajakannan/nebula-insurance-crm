namespace Nebula.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }
    public uint RowVersion { get; set; }
}
