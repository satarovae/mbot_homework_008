using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services;

public class ToDoReportService(IToDoRepository toDoRepository) : IToDoReportService
{
    public async Task<(int total, int completed, int active, DateTime generatedAt)> GetUserStats(Guid userId, CancellationToken ct)
    {
        var items = await toDoRepository.GetAllByUserId(userId, ct);

        var total = items.Count;
        var completed = items.Count(i => i.State == ToDoItemState.Completed);
        var active = items.Count(i => i.State == ToDoItemState.Active);

        return (total, completed, active, DateTime.UtcNow);
    }
}
