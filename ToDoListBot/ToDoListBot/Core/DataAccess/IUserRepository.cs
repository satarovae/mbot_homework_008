using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.DataAccess;

public interface IUserRepository
{
    Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct);
    Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct);
    //Возвращает всех пользователей
    Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct);
    Task Add(ToDoUser user, CancellationToken ct);
}
