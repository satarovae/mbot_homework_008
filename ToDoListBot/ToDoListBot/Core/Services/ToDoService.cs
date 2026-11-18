using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Exceptions;

namespace ToDoListBot.Core.Services;

public class ToDoService(IToDoRepository toDoRepository, int maxTaskCount, int maxTaskLength) : IToDoService
{
    public Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct)
        => toDoRepository.GetAllByUserId(userId, ct);

    public Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct)
        => toDoRepository.GetActiveByUserId(userId, ct);

    public async Task<ToDoItem> Add(ToDoUser user, string name, DateTime deadline, ToDoList? list, CancellationToken ct)
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя задачи не может быть пустым");

        if (name.Length > maxTaskLength)
            throw new TaskLengthLimitException(name.Length, maxTaskLength);

        if (await toDoRepository.CountActive(user.UserId, ct) >= maxTaskCount)
            throw new TaskCountLimitException(maxTaskCount);

        if (await toDoRepository.ExistsByName(user.UserId, name, ct))
            throw new DuplicateTaskException(name);

        var item = new ToDoItem
        {
            Id = Guid.NewGuid(),
            User = user,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            State = ToDoItemState.Active,
            StateChangedAt = null,
            Deadline = deadline,
            List = list
        };

        await toDoRepository.Add(item, ct);
        return item;
    }

    public async Task MarkCompleted(Guid id, CancellationToken ct)
    {
        var item = await toDoRepository.Get(id, ct)
                   ?? throw new ArgumentException($"Задача с Id {id} не найдена");

        item.State = ToDoItemState.Completed;
        item.StateChangedAt = DateTime.UtcNow;

        await toDoRepository.Update(item, ct);
    }

    public Task Delete(Guid id, CancellationToken ct) => toDoRepository.Delete(id, ct);

    public Task<IReadOnlyList<ToDoItem>> Find(ToDoUser user, string namePrefix, CancellationToken ct)
        => toDoRepository.Find(user.UserId,
            i => i.Name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase), ct);

    public Task<IReadOnlyList<ToDoItem>> GetByUserIdAndList(Guid userId, Guid? listId, CancellationToken ct)
        => toDoRepository.Find(userId, i => i.List?.Id == listId, ct);

    public Task<ToDoItem?> Get(Guid toDoItemId, CancellationToken ct) => toDoRepository.Get(toDoItemId, ct);
}
