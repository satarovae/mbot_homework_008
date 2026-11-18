using Telegram.Bot;
using ToDoListBot.Core.Services;

namespace ToDoListBot.BackgroundTasks;

// Раз в минуту отправляет запланированные нотификации
public class NotificationBackgroundTask(
    INotificationService notificationService,
    ITelegramBotClient bot)
    : BackgroundTask(TimeSpan.FromMinutes(1), nameof(NotificationBackgroundTask))
{
    protected override async Task Execute(CancellationToken ct)
    {
        var notifications = await notificationService.GetScheduledNotification(DateTime.UtcNow, ct);

        foreach (var notification in notifications)
        {
            try
            {
                await bot.SendMessage(notification.User.TelegramUserId, notification.Text, cancellationToken: ct);
                await notificationService.MarkNotified(notification.Id, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ошибка отправки одному пользователю не должна мешать остальным нотификациям
                Console.WriteLine($"{nameof(NotificationBackgroundTask)}. Не удалось отправить нотификацию {notification.Id}: {ex.Message}");
            }
        }
    }
}
