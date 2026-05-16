using Nebula.Domain.Entities;

namespace Nebula.Application.Interfaces;

public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Notification> Notifications, int TotalCount)> GetByRecipientAsync(
        Guid recipientUserId, bool? unreadOnly, int limit, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken ct = default);
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task MarkAllReadAsync(Guid recipientUserId, CancellationToken ct = default);
}
