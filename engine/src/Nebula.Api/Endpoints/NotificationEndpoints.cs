using Nebula.Api.Helpers;
using Nebula.Application.Common;
using Nebula.Application.Services;

namespace Nebula.Api.Endpoints;

public static class NotificationEndpoints
{
    private const int MaxLimit = 50;

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/my/notifications")
            .WithTags("Notifications")
            .RequireAuthorization()
            .RequireRateLimiting("authenticated");

        group.MapGet("/", GetMyNotifications);
        group.MapPatch("/{notificationId:guid}/read", MarkAsRead);
        group.MapPost("/mark-all-read", MarkAllRead);
        group.MapDelete("/{notificationId:guid}", Dismiss);

        return app;
    }

    private static async Task<IResult> GetMyNotifications(
        string? tab, int? limit,
        NotificationService svc, ICurrentUserService user,
        CancellationToken ct)
    {
        var effectiveLimit = Math.Min(limit ?? 20, MaxLimit);
        var result = await svc.GetMyNotificationsAsync(user.UserId, tab, effectiveLimit, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> MarkAsRead(
        Guid notificationId,
        NotificationService svc, ICurrentUserService user,
        CancellationToken ct)
    {
        var (dto, error) = await svc.MarkAsReadAsync(notificationId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Notification", notificationId),
            _ => Results.NoContent(),
        };
    }

    private static async Task<IResult> MarkAllRead(
        NotificationService svc, ICurrentUserService user,
        CancellationToken ct)
    {
        await svc.MarkAllReadAsync(user, ct);
        return Results.NoContent();
    }

    private static async Task<IResult> Dismiss(
        Guid notificationId,
        NotificationService svc, ICurrentUserService user,
        CancellationToken ct)
    {
        var error = await svc.DismissAsync(notificationId, user, ct);
        return error switch
        {
            "not_found" => ProblemDetailsHelper.NotFound("Notification", notificationId),
            _ => Results.NoContent(),
        };
    }
}
