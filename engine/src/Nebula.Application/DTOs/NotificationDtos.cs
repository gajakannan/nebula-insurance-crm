namespace Nebula.Application.DTOs;

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string NotificationType,
    bool IsRead,
    DateTime? ReadAt,
    string? LinkedEntityType,
    Guid? LinkedEntityId,
    DateTime CreatedAt);

public record NotificationListResponseDto(
    IReadOnlyList<NotificationDto> Notifications,
    int TotalCount,
    int UnreadCount);
