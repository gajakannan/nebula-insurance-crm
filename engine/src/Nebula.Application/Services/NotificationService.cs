using Microsoft.Extensions.Logging;
using Nebula.Application.Common;
using Nebula.Application.DTOs;
using Nebula.Application.Interfaces;
using Nebula.Domain.Entities;

namespace Nebula.Application.Services;

public class NotificationService(
    INotificationRepository notificationRepo,
    IUnitOfWork unitOfWork,
    ILogger<NotificationService> logger)
{
    private readonly ILogger<NotificationService> _logger = logger;

    public async Task<NotificationListResponseDto> GetMyNotificationsAsync(
        Guid recipientUserId, string? tab, int limit, CancellationToken ct = default)
    {
        bool? unreadOnly = tab == "unread" ? true : null;

        var (notifications, totalCount) = await notificationRepo.GetByRecipientAsync(
            recipientUserId, unreadOnly, limit, ct);

        var unreadCount = await notificationRepo.GetUnreadCountAsync(recipientUserId, ct);

        var dtos = notifications.Select(MapToDto).ToList();
        return new NotificationListResponseDto(dtos, totalCount, unreadCount);
    }

    public async Task<(NotificationDto? Dto, string? ErrorCode)> MarkAsReadAsync(
        Guid notificationId, ICurrentUserService user, CancellationToken ct = default)
    {
        var notification = await notificationRepo.GetByIdAsync(notificationId, ct);
        if (notification is null || notification.RecipientUserId != user.UserId)
            return (null, "not_found");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            notification.UpdatedByUserId = user.UserId;
            await unitOfWork.CommitAsync(ct);
        }

        return (MapToDto(notification), null);
    }

    public async Task MarkAllReadAsync(
        ICurrentUserService user, CancellationToken ct = default)
    {
        await notificationRepo.MarkAllReadAsync(user.UserId, ct);
        await unitOfWork.CommitAsync(ct);
    }

    public async Task<string?> DismissAsync(
        Guid notificationId, ICurrentUserService user, CancellationToken ct = default)
    {
        var notification = await notificationRepo.GetByIdAsync(notificationId, ct);
        if (notification is null || notification.RecipientUserId != user.UserId)
            return "not_found";

        notification.IsDeleted = true;
        notification.DeletedAt = DateTime.UtcNow;
        notification.DeletedByUserId = user.UserId;
        notification.UpdatedAt = DateTime.UtcNow;
        notification.UpdatedByUserId = user.UserId;
        await unitOfWork.CommitAsync(ct);

        return null;
    }

    public async Task<NotificationDto> CreateAsync(
        Guid recipientUserId, string title, string message, string notificationType,
        string? linkedEntityType, Guid? linkedEntityId,
        ICurrentUserService user, CancellationToken ct = default)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Title = title,
            Message = message,
            NotificationType = notificationType,
            LinkedEntityType = linkedEntityType,
            LinkedEntityId = linkedEntityId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.UserId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = user.UserId,
        };

        await notificationRepo.AddAsync(notification, ct);
        await unitOfWork.CommitAsync(ct);

        return MapToDto(notification);
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id,
        n.Title,
        n.Message,
        n.NotificationType,
        n.IsRead,
        n.ReadAt,
        n.LinkedEntityType,
        n.LinkedEntityId,
        n.CreatedAt);
}
