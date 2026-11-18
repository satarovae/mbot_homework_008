using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Exceptions;

namespace ToDoListBot.Core.Services;

public class ToDoListService(IToDoListRepository toDoListRepository) : IToDoListService
{
    private const int MaxNameLength = 10;

    public async Task<ToDoList> Add(ToDoUser user, string name, CancellationToken ct)
    {
        name = name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            throw new ToDoListValidationException("Название списка не может быть пустым");

        if (name.Length > MaxNameLength)
            throw new ToDoListValidationException($"Название списка не может быть длиннее {MaxNameLength} символов");

        if (await toDoListRepository.ExistsByName(user.UserId, name, ct))
            throw new ToDoListValidationException($"Список с названием '{name}' уже существует");

        var list = new ToDoList
        {
            Id = Guid.NewGuid(),
            Name = name,
            User = user,
            CreatedAt = DateTime.UtcNow
        };

        await toDoListRepository.Add(list, ct);
        return list;
    }

    public Task<ToDoList?> Get(Guid id, CancellationToken ct) => toDoListRepository.Get(id, ct);

    public Task Delete(Guid id, CancellationToken ct) => toDoListRepository.Delete(id, ct);

    public Task<IReadOnlyList<ToDoList>> GetUserLists(Guid userId, CancellationToken ct)
        => toDoListRepository.GetByUserId(userId, ct);
}
