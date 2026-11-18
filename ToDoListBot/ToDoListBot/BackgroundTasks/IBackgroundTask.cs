namespace ToDoListBot.BackgroundTasks;

public interface IBackgroundTask
{
    Task Start(CancellationToken ct);
}
