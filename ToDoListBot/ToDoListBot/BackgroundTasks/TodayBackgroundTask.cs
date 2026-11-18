using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Services;

namespace ToDoListBot.BackgroundTasks;

// Раз в день создает нотификации со списком задач, запланированных на сегодня
public class TodayBackgroundTask(
    INotificationService notificationService,
    IUserRepository userRepository,
    IToDoRepository toDoRepository)
    : BackgroundTask(TimeSpan.FromDays(1), nameof(TodayBackgroundTask))
{
    protected override async Task Execute(CancellationToken ct)
    {
        var users = await userRepository.GetUsers(ct);

        foreach (var user in users)
        {
            var today = DateTime.UtcNow.Date;
            var tasks = await toDoRepository.GetActiveWithDeadline(user.UserId, today, today.AddDays(1), ct);

            if (tasks.Count == 0)
                continue;

            var text = "Задачи на сегодня:\n" + string.Join("\n", tasks.Select((t, i) => $"{i + 1}. {t.Name}"));

            await notificationService.ScheduleNotification(
                user.UserId,
                $"Today_{DateOnly.FromDateTime(DateTime.UtcNow)}",
                text,
                DateTime.UtcNow,
                ct);
        }
    }
}
