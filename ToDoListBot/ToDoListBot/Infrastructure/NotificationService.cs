using LinqToDB;
using LinqToDB.Async;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.Infrastructure.DataAccess;
using ToDoListBot.Infrastructure.DataAccess.Models;

namespace ToDoListBot.Infrastructure;

// Работает с БД напрямую, без репозиториев
public class NotificationService(IDataContextFactory<ToDoDataContext> factory) : INotificationService
{
    public async Task<bool> ScheduleNotification(
        Guid userId,
        string type,
        string text,
        DateTime scheduledAt,
        CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var exists = await dbContext.Notifications
            .AnyAsync(n => n.UserId == userId && n.Type == type, ct);

        if (exists)
            return false;

        await dbContext.InsertAsync(new NotificationModel
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            Text = text,
            ScheduledAt = ModelMapper.ToUtc(scheduledAt),
            IsNotified = false,
            NotifiedAt = null
        }, token: ct);

        return true;
    }

    public async Task<IReadOnlyList<Notification>> GetScheduledNotification(DateTime scheduledBefore, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        var before = ModelMapper.ToUtc(scheduledBefore);

        var models = await dbContext.Notifications
            .LoadWith(n => n.User)
            .Where(n => !n.IsNotified && n.ScheduledAt <= before)
            .OrderBy(n => n.ScheduledAt)
            .ToListAsync(ct);

        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task MarkNotified(Guid notificationId, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        var now = DateTime.UtcNow;

        await dbContext.Notifications
            .Where(n => n.Id == notificationId)
            .Set(n => n.IsNotified, true)
            .Set(n => n.NotifiedAt, (DateTime?)now)
            .UpdateAsync(ct);
    }
}
