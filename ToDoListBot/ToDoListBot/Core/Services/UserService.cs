using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.Services;

public class UserService(IUserRepository userRepository) : IUserService
{
    public async Task<ToDoUser> RegisterUser(long telegramUserId, string telegramUserName, CancellationToken ct)
    {
        var user = new ToDoUser
        {
            UserId = Guid.NewGuid(),
            TelegramUserId = telegramUserId,
            TelegramUserName = telegramUserName,
            RegisteredAt = DateTime.UtcNow
        };

        await userRepository.Add(user, ct);
        return user;
    }

    public Task<ToDoUser?> GetUser(long telegramUserId, CancellationToken ct)
        => userRepository.GetUserByTelegramUserId(telegramUserId, ct);
}
