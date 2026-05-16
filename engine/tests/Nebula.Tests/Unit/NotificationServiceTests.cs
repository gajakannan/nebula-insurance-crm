using Shouldly;
using Nebula.Application.Common;
using Nebula.Application.Interfaces;
using Nebula.Application.Services;
using Nebula.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;

namespace Nebula.Tests.Unit;

public class NotificationServiceTests
{
    private readonly StubNotificationRepository _notifRepo = new();
    private readonly StubUnitOfWork _unitOfWork = new();
    private readonly StubCurrentUserService _user = new(Guid.Parse("aaaa0000-0000-0000-0000-000000000001"));

    private NotificationService CreateService() => new(
        _notifRepo,
        _unitOfWork,
        NullLogger<NotificationService>.Instance);

    // ═══════════════════════════════════════════════════════════════════════
    //  GetMyNotificationsAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMyNotificationsAsync_ReturnsOnlyRecipientNotifications()
    {
        var svc = CreateService();
        SeedNotification(_user.UserId, "Test 1");
        SeedNotification(_user.UserId, "Test 2");
        SeedNotification(Guid.NewGuid(), "Other User Notification");

        var result = await svc.GetMyNotificationsAsync(_user.UserId, null, 50);

        result.Notifications.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(2);
        result.Notifications.ShouldAllBe(n => n.Title == "Test 1" || n.Title == "Test 2");
    }

    [Fact]
    public async Task GetMyNotificationsAsync_UnreadTab_FiltersToUnreadOnly()
    {
        var svc = CreateService();
        SeedNotification(_user.UserId, "Unread One", isRead: false);
        SeedNotification(_user.UserId, "Read One", isRead: true);

        var result = await svc.GetMyNotificationsAsync(_user.UserId, "unread", 50);

        result.Notifications.Count.ShouldBe(1);
        result.Notifications[0].Title.ShouldBe("Unread One");
    }

    [Fact]
    public async Task GetMyNotificationsAsync_ReturnsCorrectUnreadCount()
    {
        var svc = CreateService();
        SeedNotification(_user.UserId, "Unread 1", isRead: false);
        SeedNotification(_user.UserId, "Unread 2", isRead: false);
        SeedNotification(_user.UserId, "Read 1", isRead: true);

        var result = await svc.GetMyNotificationsAsync(_user.UserId, null, 50);

        result.UnreadCount.ShouldBe(2);
        result.TotalCount.ShouldBe(3);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MarkAsReadAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarkAsReadAsync_SetsIsReadAndReadAt()
    {
        var svc = CreateService();
        var notif = SeedNotification(_user.UserId, "To Mark");

        var (dto, error) = await svc.MarkAsReadAsync(notif.Id, _user);

        error.ShouldBeNull();
        dto.ShouldNotBeNull();
        dto!.IsRead.ShouldBeTrue();
        dto.ReadAt.ShouldNotBeNull();
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNotFound_WhenBelongsToDifferentUser()
    {
        var svc = CreateService();
        var otherUserId = Guid.NewGuid();
        var notif = SeedNotification(otherUserId, "Other User's Notification");

        var (dto, error) = await svc.MarkAsReadAsync(notif.Id, _user);

        error.ShouldBe("not_found");
        dto.ShouldBeNull();
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task MarkAsReadAsync_ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        var svc = CreateService();

        var (dto, error) = await svc.MarkAsReadAsync(Guid.NewGuid(), _user);

        error.ShouldBe("not_found");
        dto.ShouldBeNull();
    }

    [Fact]
    public async Task MarkAsReadAsync_AlreadyRead_DoesNotCommitAgain()
    {
        var svc = CreateService();
        var notif = SeedNotification(_user.UserId, "Already Read", isRead: true);

        var (dto, error) = await svc.MarkAsReadAsync(notif.Id, _user);

        error.ShouldBeNull();
        dto.ShouldNotBeNull();
        dto!.IsRead.ShouldBeTrue();
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  MarkAllReadAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task MarkAllReadAsync_MarksAllUnreadForCaller()
    {
        var svc = CreateService();
        SeedNotification(_user.UserId, "Unread 1", isRead: false);
        SeedNotification(_user.UserId, "Unread 2", isRead: false);

        await svc.MarkAllReadAsync(_user);

        _notifRepo.MarkAllReadCalledForUserId.ShouldBe(_user.UserId);
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  DismissAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DismissAsync_SoftDeletesNotification()
    {
        var svc = CreateService();
        var notif = SeedNotification(_user.UserId, "To Dismiss");

        var error = await svc.DismissAsync(notif.Id, _user);

        error.ShouldBeNull();
        notif.IsDeleted.ShouldBeTrue();
        notif.DeletedAt.ShouldNotBeNull();
        notif.DeletedByUserId.ShouldBe(_user.UserId);
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    [Fact]
    public async Task DismissAsync_ReturnsNotFound_WhenNotificationDoesNotExist()
    {
        var svc = CreateService();

        var error = await svc.DismissAsync(Guid.NewGuid(), _user);

        error.ShouldBe("not_found");
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    [Fact]
    public async Task DismissAsync_ReturnsNotFound_WhenBelongsToDifferentUser()
    {
        var svc = CreateService();
        var notif = SeedNotification(Guid.NewGuid(), "Other User's");

        var error = await svc.DismissAsync(notif.Id, _user);

        error.ShouldBe("not_found");
        _unitOfWork.CommitCount.ShouldBe(0);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  CreateAsync
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateAsync_PersistsAndReturnsDto()
    {
        var svc = CreateService();
        var recipientId = Guid.NewGuid();

        var dto = await svc.CreateAsync(
            recipientId, "New Alert", "Something happened", "SystemAlert",
            "Submission", Guid.NewGuid(), _user);

        dto.Title.ShouldBe("New Alert");
        dto.NotificationType.ShouldBe("SystemAlert");
        dto.IsRead.ShouldBeFalse();
        dto.LinkedEntityType.ShouldBe("Submission");
        _notifRepo.Added.Count.ShouldBe(1);
        _unitOfWork.CommitCount.ShouldBe(1);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════════

    private Notification SeedNotification(Guid recipientUserId, string title, bool isRead = false)
    {
        var notification = new Notification
        {
            RecipientUserId = recipientUserId,
            Title = title,
            Message = $"{title} message",
            NotificationType = "SystemAlert",
            IsRead = isRead,
            ReadAt = isRead ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = recipientUserId,
            UpdatedAt = DateTime.UtcNow,
            UpdatedByUserId = recipientUserId,
        };
        _notifRepo.Seed(notification);
        return notification;
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Test doubles
// ═══════════════════════════════════════════════════════════════════════════

internal class StubNotificationRepository : INotificationRepository
{
    private readonly Dictionary<Guid, Notification> _notifications = new();
    public List<Notification> Added { get; } = [];
    public Guid? MarkAllReadCalledForUserId { get; private set; }

    public void Seed(Notification notification) => _notifications[notification.Id] = notification;

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_notifications.GetValueOrDefault(id));

    public Task<(IReadOnlyList<Notification> Notifications, int TotalCount)> GetByRecipientAsync(
        Guid recipientUserId, bool? unreadOnly, int limit, CancellationToken ct = default)
    {
        var items = _notifications.Values
            .Where(n => n.RecipientUserId == recipientUserId && !n.IsDeleted);

        if (unreadOnly == true)
            items = items.Where(n => !n.IsRead);

        var list = items.OrderByDescending(n => n.CreatedAt).Take(limit).ToList();
        return Task.FromResult<(IReadOnlyList<Notification>, int)>((list, list.Count));
    }

    public Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken ct = default)
    {
        var count = _notifications.Values
            .Count(n => n.RecipientUserId == recipientUserId && !n.IsRead && !n.IsDeleted);
        return Task.FromResult(count);
    }

    public Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        Added.Add(notification);
        _notifications[notification.Id] = notification;
        return Task.CompletedTask;
    }

    public Task MarkAllReadAsync(Guid recipientUserId, CancellationToken ct = default)
    {
        MarkAllReadCalledForUserId = recipientUserId;
        foreach (var n in _notifications.Values.Where(n => n.RecipientUserId == recipientUserId && !n.IsRead))
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }
}
