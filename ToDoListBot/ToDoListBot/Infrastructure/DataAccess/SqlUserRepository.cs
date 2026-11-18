using LinqToDB;
using LinqToDB.Async;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess;

public class SqlUserRepository(IDataContextFactory<ToDoDataContext> factory) : IUserRepository
{
    public async Task<ToDoUser?> GetUser(Guid userId, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var model = await dbContext.ToDoUsers
            .FirstOrDefaultAsync(u => u.UserId == userId, ct);

        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<ToDoUser?> GetUserByTelegramUserId(long telegramUserId, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var model = await dbContext.ToDoUsers
            .FirstOrDefaultAsync(u => u.TelegramUserId == telegramUserId, ct);

        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<IReadOnlyList<ToDoUser>> GetUsers(CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var models = await dbContext.ToDoUsers
            .OrderBy(u => u.RegisteredAt)
            .ToListAsync(ct);

        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task Add(ToDoUser user, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        await dbContext.InsertAsync(ModelMapper.MapToModel(user), token: ct);
    }
}
