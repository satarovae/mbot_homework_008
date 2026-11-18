using ToDoListBot.Core.DataAccess.Models;
using ToDoListBot.Core.Entities;
using ToDoListBot.Infrastructure.DataAccess.Models;

namespace ToDoListBot.Infrastructure.DataAccess;

// Маппинг между сущностями (Core/Entities) и моделями БД (Core/DataAccess/Models)
internal static class ModelMapper
{
    public static ToDoUser MapFromModel(ToDoUserModel model) => new()
    {
        UserId = model.UserId,
        TelegramUserId = model.TelegramUserId,
        TelegramUserName = model.TelegramUserName,
        RegisteredAt = model.RegisteredAt
    };

    public static ToDoUserModel MapToModel(ToDoUser entity) => new()
    {
        UserId = entity.UserId,
        TelegramUserId = entity.TelegramUserId,
        TelegramUserName = entity.TelegramUserName,
        RegisteredAt = ToUtc(entity.RegisteredAt)
    };

    // Для корректного маппинга нужно загружать связанные сущности через LoadWith: User, List, List.User
    public static ToDoItem MapFromModel(ToDoItemModel model) => new()
    {
        Id = model.Id,
        User = MapFromModel(model.User),
        Name = model.Name,
        CreatedAt = model.CreatedAt,
        State = model.State,
        StateChangedAt = model.StateChangedAt,
        Deadline = model.Deadline,
        List = model.List is null ? null : MapFromModel(model.List)
    };

    public static ToDoItemModel MapToModel(ToDoItem entity) => new()
    {
        Id = entity.Id,
        UserId = entity.User.UserId,
        ListId = entity.List?.Id,
        Name = entity.Name,
        CreatedAt = ToUtc(entity.CreatedAt),
        State = entity.State,
        StateChangedAt = entity.StateChangedAt is { } changedAt ? ToUtc(changedAt) : null,
        Deadline = ToUtc(entity.Deadline)
    };

    public static ToDoList MapFromModel(ToDoListModel model) => new()
    {
        Id = model.Id,
        Name = model.Name,
        User = MapFromModel(model.User),
        CreatedAt = model.CreatedAt
    };

    public static ToDoListModel MapToModel(ToDoList entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        UserId = entity.User.UserId,
        CreatedAt = ToUtc(entity.CreatedAt)
    };

    // Для корректного маппинга нужно загружать связанного пользователя через LoadWith: User
    public static Notification MapFromModel(NotificationModel model) => new()
    {
        Id = model.Id,
        User = MapFromModel(model.User),
        Type = model.Type,
        Text = model.Text,
        ScheduledAt = model.ScheduledAt,
        IsNotified = model.IsNotified,
        NotifiedAt = model.NotifiedAt
    };

    public static NotificationModel MapToModel(Notification entity) => new()
    {
        Id = entity.Id,
        UserId = entity.User.UserId,
        Type = entity.Type,
        Text = entity.Text,
        ScheduledAt = ToUtc(entity.ScheduledAt),
        IsNotified = entity.IsNotified,
        NotifiedAt = entity.NotifiedAt is { } notifiedAt ? ToUtc(notifiedAt) : null
    };

    // Npgsql пишет в TIMESTAMPTZ только DateTime с Kind = Utc
    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
