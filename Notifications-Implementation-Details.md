# Notifications Feature — Implementation Details

## Overview

This document describes all code files that were added or modified to implement the **Notifications Backend API & Frontend Integration** for the Nebula Insurance CRM. The implementation follows the existing Clean Architecture patterns used throughout the project (matching `TaskItem`, `Broker`, `Submission`, etc.).


---

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Nebula.Api                                  │
│  NotificationEndpoints.cs  →  4 Minimal API routes                  │
│  Program.cs (modified)     →  registers endpoints + DI              │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ depends on
┌────────────────────────────────▼────────────────────────────────────┐
│                     Nebula.Application                              │
│  NotificationService.cs     →  business logic + ownership checks    │
│  INotificationRepository.cs →  repository interface (abstraction)   │
│  NotificationDtos.cs        →  DTO record types                     │
└────────────────────────────────┬────────────────────────────────────┘
                                 │ depends on
┌────────────────────────────────▼────────────────────────────────────┐
│                       Nebula.Domain                                 │
│  Notification.cs            →  domain entity (inherits BaseEntity)  │
└─────────────────────────────────────────────────────────────────────┘
                                 │ implemented by
┌────────────────────────────────▼────────────────────────────────────┐
│                    Nebula.Infrastructure                             │
│  NotificationRepository.cs  →  EF Core implementation               │
│  NotificationConfiguration.cs → table config, indexes, query filter │
│  AppDbContext.cs (modified)  →  DbSet<Notification> added           │
│  DependencyInjection.cs (mod) → DI registration                    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## NEW Files (8 files)

### 1. `engine/src/Nebula.Domain/Entities/Notification.cs`
**Purpose:** Domain entity representing a user notification.

```csharp
public class Notification : BaseEntity
{
    public Guid RecipientUserId { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public string NotificationType { get; set; }  // e.g. "BrokerCreated", "TaskOverdue"
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? LinkedEntityType { get; set; }  // e.g. "Broker", "Submission"
    public Guid? LinkedEntityId { get; set; }
}
```

**Key decisions:**
- Inherits `BaseEntity` which provides: `Id`, `CreatedAt`, `CreatedByUserId`, `UpdatedAt`, `UpdatedByUserId`, `IsDeleted`, `DeletedAt`, `DeletedByUserId`, `RowVersion`
- `RecipientUserId` — who receives the notification (used for IDOR prevention)
- `LinkedEntityType`/`LinkedEntityId` — optional link to any CRM entity for deep-linking
- `NotificationType` — string-based for extensibility (not an enum)

---

### 2. `engine/src/Nebula.Application/DTOs/NotificationDtos.cs`
**Purpose:** Data Transfer Objects as C# record types.

```csharp
public record NotificationDto(
    Guid Id, string Title, string Message, string NotificationType,
    bool IsRead, DateTime? ReadAt, string? LinkedEntityType,
    Guid? LinkedEntityId, DateTime CreatedAt);

public record NotificationListResponseDto(
    IReadOnlyList<NotificationDto> Notifications,
    int TotalCount, int UnreadCount);
```

**Key decisions:**
- Uses `record` types (immutable, value equality) — matches existing pattern (e.g. `BrokerDto`)
- `NotificationListResponseDto` wraps the list with counts for the frontend badge

---

### 3. `engine/src/Nebula.Application/Interfaces/INotificationRepository.cs`
**Purpose:** Repository interface in the Application layer (dependency inversion).

```csharp
public interface INotificationRepository
{
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<(IReadOnlyList<Notification>, int TotalCount)> GetByRecipientAsync(
        Guid recipientUserId, bool? unreadOnly, int limit, CancellationToken ct);
    Task<int> GetUnreadCountAsync(Guid recipientUserId, CancellationToken ct);
    Task AddAsync(Notification notification, CancellationToken ct);
    Task MarkAllReadAsync(Guid recipientUserId, CancellationToken ct);
}
```

**Key decisions:**
- Follows the same pattern as `ITaskItemRepository`, `IBrokerRepository`
- `GetByRecipientAsync` returns a tuple with notifications and count (for pagination metadata)
- `MarkAllReadAsync` uses bulk `ExecuteUpdateAsync` for efficiency

---

### 4. `engine/src/Nebula.Application/Services/NotificationService.cs`
**Purpose:** Business logic with ownership verification and error handling.

**Key methods:**
| Method | What it does |
|--------|-------------|
| `GetMyNotificationsAsync` | Fetch notifications for a user, optionally filtering by unread |
| `MarkAsReadAsync` | Mark one notification as read (verifies ownership first) |
| `MarkAllReadAsync` | Mark all user's notifications as read |
| `DismissAsync` | Soft-delete a notification (verifies ownership first) |
| `CreateAsync` | Create a new notification (for internal use by other services) |

**Key decisions:**
- Uses `(Dto?, ErrorCode?)` tuple pattern for error handling (no exceptions) — matches `TaskService`
- **Ownership check**: `notification.RecipientUserId != user.UserId` prevents IDOR attacks
- Uses primary constructor injection: `NotificationService(INotificationRepository, IUnitOfWork, ILogger)`
- `DismissAsync` uses soft-delete (sets `IsDeleted = true`) rather than hard delete

---

### 5. `engine/src/Nebula.Infrastructure/Repositories/NotificationRepository.cs`
**Purpose:** EF Core implementation of `INotificationRepository`.

**Key decisions:**
- `GetByRecipientAsync` orders by `CreatedAt DESC` (newest first)
- `MarkAllReadAsync` uses `ExecuteUpdateAsync` for bulk update (single SQL query, not N+1)
- Primary constructor injection: `NotificationRepository(AppDbContext db)`
- Soft-delete is handled automatically by the query filter in `NotificationConfiguration`

---

### 6. `engine/src/Nebula.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs`
**Purpose:** EF Core entity configuration (table name, columns, indexes, query filter).

**Key decisions:**
- Table: `Notifications`
- Column constraints: `Title` max 255, `Message` max 2000, `NotificationType` max 50
- `IsRead` defaults to `false`, `IsDeleted` defaults to `false`
- **Concurrency token**: `RowVersion` mapped to PostgreSQL `xmin` system column
- **Query filter**: `builder.HasQueryFilter(e => !e.IsDeleted)` — soft-deleted rows excluded automatically
- **Indexes:**
  - `IX_Notifications_RecipientUserId_IsRead_CreatedAt` — optimizes unread filter queries
  - `IX_Notifications_RecipientUserId_CreatedAt` — optimizes "all" tab queries

---

### 7. `engine/src/Nebula.Api/Endpoints/NotificationEndpoints.cs`
**Purpose:** Minimal API endpoint definitions (thin layer, no business logic).

**Endpoints:**
| HTTP Method | Route | Handler |
|-------------|-------|---------|
| `GET` | `/my/notifications?tab=all\|unread&limit=20` | `GetMyNotifications` |
| `PATCH` | `/my/notifications/{notificationId}/read` | `MarkAsRead` |
| `POST` | `/my/notifications/mark-all-read` | `MarkAllRead` |
| `DELETE` | `/my/notifications/{notificationId}` | `Dismiss` |

**Key decisions:**
- All routes under `/my/` — convention for "current user's data"
- `RequireAuthorization()` + `RequireRateLimiting("authenticated")` on the group
- `MaxLimit = 50` hard cap to prevent abuse
- Error responses use `ProblemDetailsHelper.NotFound()` (RFC 7807 standard)
- No business logic in endpoints — all delegated to `NotificationService`

---

### 8. `engine/tests/Nebula.Tests/Unit/NotificationServiceTests.cs`
**Purpose:** 12 unit tests for `NotificationService` using stub repositories.

**Tests:**
1. `GetMyNotificationsAsync_ReturnsNotificationsForUser` — verifies list + counts
2. `GetMyNotificationsAsync_FiltersUnreadOnly` — verifies `tab=unread` filtering
3. `GetMyNotificationsAsync_ReturnsEmptyForNoNotifications` — empty state
4. `MarkAsReadAsync_MarksNotificationAsRead` — happy path
5. `MarkAsReadAsync_ReturnsNotFoundForMissingNotification` — 404 case
6. `MarkAsReadAsync_ReturnsNotFoundForOtherUsersNotification` — IDOR prevention
7. `MarkAllReadAsync_CallsRepositoryAndCommits` — bulk mark-all-read
8. `DismissAsync_SoftDeletesNotification` — verifies soft delete fields
9. `DismissAsync_ReturnsNotFoundForMissingNotification` — 404 case
10. `DismissAsync_ReturnsNotFoundForOtherUsersNotification` — IDOR prevention
11. `CreateAsync_CreatesNotification` — creation flow
12. `MarkAsReadAsync_AlreadyReadDoesNotUpdateAgain` — idempotency

---

## MODIFIED Files (9 files)

### 9. `engine/src/Nebula.Api/Program.cs`
**What changed:** Added one line to register notification endpoints.
```csharp
app.MapNotificationEndpoints();  // NEW LINE
```

### 10. `engine/src/Nebula.Infrastructure/DependencyInjection.cs`
**What changed:** Added DI registration for `NotificationRepository` and `NotificationService`.
```csharp
services.AddScoped<INotificationRepository, NotificationRepository>();  // NEW
services.AddScoped<NotificationService>();  // NEW
```

### 11. `engine/src/Nebula.Infrastructure/Persistence/AppDbContext.cs`
**What changed:** Added `DbSet<Notification>` property.
```csharp
public DbSet<Notification> Notifications => Set<Notification>();  // NEW
```

### 12. `experience/src/features/notifications/types.ts`
**What changed:** Added `NotificationDto` and `NotificationListResponseDto` TypeScript interfaces to match the backend API contract. Removed old `NotificationCategory` and `Assigned` tab type.

### 13. `experience/src/features/notifications/hooks/useNotifications.ts`
**What changed:** Complete rewrite — replaced hardcoded `INITIAL_NOTIFICATIONS` array with:
- `useQuery` to fetch from `GET /my/notifications` with 30-second auto-refetch
- `useMutation` for mark-read, mark-all-read, and dismiss with query cache invalidation
- `formatTimeLabel()` utility to convert ISO timestamps to relative labels ("2h ago")
- `mapActionLabel()` to generate "Open broker" / "Open submission" from `linkedEntityType`

### 14. `experience/src/features/notifications/components/NotificationDropdown.tsx`
**What changed:** 
- Removed the `Assigned` tab (backend only supports `all` and `unread`)
- Updated tab types from `'all' | 'unread' | 'assigned'` to `'all' | 'unread'`
- Added loading state handling

### 15. `experience/src/features/notifications/tests/NotificationDropdown.test.tsx`
**What changed:** Complete rewrite with OIDC mock setup:
- Added `vi.mock('@/features/auth/oidcUserManager')` to provide a valid test token
- 5 tests: opens dropdown, shows badge count, filters unread, closes on Escape, closes on outside click
- Uses `renderWithProviders` and MSW mock handlers

### 16. `experience/src/mocks/data.ts`
**What changed:** Added `notificationFixture` with 5 test notifications (3 unread, 2 read) for MSW handlers.

### 17. `experience/src/mocks/handlers.ts`
**What changed:** Added 4 MSW handlers:
- `GET /my/notifications` — returns fixture data, supports `tab=unread` filtering
- `PATCH /my/notifications/:id/read` — returns 204
- `POST /my/notifications/mark-all-read` — returns 204
- `DELETE /my/notifications/:id` — returns 204

---

## How the Architecture Layers Connect

```
User clicks bell icon in header
        │
        ▼
NotificationDropdown.tsx (UI Component)
        │ calls
        ▼
useNotifications.ts (Custom Hook)
        │ uses TanStack Query
        ▼
api.get('/my/notifications')  →  Vite proxy  →  localhost:5113
        │
        ▼
NotificationEndpoints.cs (GET /my/notifications)
        │ injects
        ▼
NotificationService.GetMyNotificationsAsync()
        │ calls
        ▼
INotificationRepository.GetByRecipientAsync()
        │ implemented by
        ▼
NotificationRepository (EF Core LINQ)
        │ queries
        ▼
PostgreSQL "Notifications" table
```

---

## Security Measures

1. **Authentication:** All endpoints require a valid JWT via `RequireAuthorization()`
2. **IDOR Prevention:** `NotificationService` checks `RecipientUserId == user.UserId` before any mutation
3. **Rate Limiting:** All endpoints use the `"authenticated"` rate limit policy
4. **Soft Delete:** Notifications are never physically deleted — `IsDeleted = true` with audit trail
5. **Concurrency:** PostgreSQL `xmin` column used as optimistic concurrency token

---

## Test Results

| Suite | Total | Passed | Failed | Notes |
|-------|-------|--------|--------|-------|
| Backend (NotificationServiceTests) | 12 | 12 | 0 | All ownership + CRUD tests pass |
| Frontend (NotificationDropdown.test) | 5 | 5 | 0 | OIDC mock + MSW handlers |
| Full Frontend Suite | 97 | 96 | 1 | 1 pre-existing failure in `api.test.ts` (unrelated) |

---

## Next Steps After Merge

1. **Generate EF Core Migration:**
   ```bash
   dotnet ef migrations add AddNotifications \
     --project engine/src/Nebula.Infrastructure \
     --startup-project engine/src/Nebula.Api
   ```
2. **Run Migration:** `dotnet ef database update`
3. **Seed Test Data:** Create sample notifications in the database for testing
4. **Integrate with Other Features:** Call `NotificationService.CreateAsync()` from other services when events occur (e.g., broker created, task overdue, submission received)
