using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Services;

namespace ToDoListBot.BackgroundTasks;

// Раз в час создает нотификации по задачам, у которых прошел дедлайн
public class DeadlineBackgroundTask(
    INotificationService notificationService,
    IUserRepository userRepository,
    IToDoRepository toDoRepository)
    : BackgroundTask(TimeSpan.FromHours(1), nameof(DeadlineBackgroundTask))
{
    protected override async Task Execute(CancellationToken ct)
    {
        var users = await userRepository.GetUsers(ct);

        foreach (var user in users)
        {
            // Активные задачи с дедлайном, который прошел вчера
            var tasks = await toDoRepository.GetActiveWithDeadline(
                user.UserId,
                DateTime.UtcNow.AddDays(-1).Date,
                DateTime.UtcNow.Date,
                ct);

            foreach (var task in tasks)
            {
                // Если нотификация с таким типом уже есть, повторно она не создается
                await notificationService.ScheduleNotification(
                    user.UserId,
                    $"Deadline_{task.Id}",
                    $"Ой! Вы пропустили дедлайн по задаче {task.Name}",
                    DateTime.UtcNow,
                    ct);
            }
        }
    }
}
