using Microsoft.EntityFrameworkCore;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;
using Nebula.Infrastructure.Persistence;

namespace Nebula.Infrastructure.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<(IReadOnlyList<Notification> Notifications, int TotalCount)> GetByRecipientAsync(
        Guid recipientUserId, bool? unreadOnly, int limit, CancellationToken ct = default)
    {
        var query = db.Notifications
            .Where(n => n.RecipientUserId == recipientUserId);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        var totalCount = await query.CountAsync(ct);

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return (notifications, totalCount);
    }

    public async Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken ct = default) =>
        await db.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
            .CountAsync(ct);

    public async Task AddAsync(Notification notification, CancellationToken ct = default) =>
        await db.Notifications.AddAsync(notification, ct);

    public async Task MarkAllReadAsync(Guid recipientUserId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await db.Notifications
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now)
                .SetProperty(n => n.UpdatedAt, now)
                .SetProperty(n => n.UpdatedByUserId, recipientUserId), ct);
    }
}
