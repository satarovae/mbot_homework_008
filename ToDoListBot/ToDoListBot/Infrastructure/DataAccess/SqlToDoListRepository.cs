using LinqToDB;
using LinqToDB.Async;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.DataAccess.Models;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess;

public class SqlToDoListRepository(IDataContextFactory<ToDoDataContext> factory) : IToDoListRepository
{
    public async Task<ToDoList?> Get(Guid id, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var model = await ListsWithRelations(dbContext)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task<IReadOnlyList<ToDoList>> GetByUserId(Guid userId, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var models = await ListsWithRelations(dbContext)
            .Where(l => l.UserId == userId)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(ct);

        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task Add(ToDoList list, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        await dbContext.InsertAsync(ModelMapper.MapToModel(list), token: ct);
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        await dbContext.ToDoLists
            .Where(l => l.Id == id)
            .DeleteAsync(ct);
    }

    public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        var lowerName = name.ToLower();

        return await dbContext.ToDoLists
            .AnyAsync(l => l.UserId == userId && l.Name.ToLower() == lowerName, ct);
    }

    private static IQueryable<ToDoListModel> ListsWithRelations(ToDoDataContext dbContext)
        => dbContext.ToDoLists.LoadWith(l => l.User);
}
