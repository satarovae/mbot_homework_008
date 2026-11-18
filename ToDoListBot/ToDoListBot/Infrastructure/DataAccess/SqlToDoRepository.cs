using LinqToDB;
using LinqToDB.Async;
using ToDoListBot.Core.DataAccess;
using ToDoListBot.Core.DataAccess.Models;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Infrastructure.DataAccess;

public class SqlToDoRepository(IDataContextFactory<ToDoDataContext> factory) : IToDoRepository
{
    public async Task<IReadOnlyList<ToDoItem>> GetAllByUserId(Guid userId, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var models = await ItemsWithRelations(dbContext)
            .Where(i => i.UserId == userId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task<IReadOnlyList<ToDoItem>> GetActiveByUserId(Guid userId, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var models = await ItemsWithRelations(dbContext)
            .Where(i => i.UserId == userId && i.State == ToDoItemState.Active)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync(ct);

        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task<ToDoItem?> Get(Guid id, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        var model = await ItemsWithRelations(dbContext)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

        return model is null ? null : ModelMapper.MapFromModel(model);
    }

    public async Task Add(ToDoItem item, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        await dbContext.InsertAsync(ModelMapper.MapToModel(item), token: ct);
    }

    public async Task Update(ToDoItem item, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        await dbContext.UpdateAsync(ModelMapper.MapToModel(item), token: ct);
    }

    public async Task Delete(Guid id, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        await dbContext.ToDoItems
            .Where(i => i.Id == id)
            .DeleteAsync(ct);
    }

    public async Task<bool> ExistsByName(Guid userId, string name, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        var lowerName = name.ToLower();

        return await dbContext.ToDoItems
            .AnyAsync(i => i.UserId == userId && i.Name.ToLower() == lowerName, ct);
    }

    public async Task<int> CountActive(Guid userId, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();

        return await dbContext.ToDoItems
            .CountAsync(i => i.UserId == userId && i.State == ToDoItemState.Active, ct);
    }

    public async Task<IReadOnlyList<ToDoItem>> GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct)
    {
        using var dbContext = factory.CreateDataContext();
        // Npgsql принимает для TIMESTAMPTZ только DateTime с Kind = Utc
        var fromUtc = ModelMapper.ToUtc(from);
        var toUtc = ModelMapper.ToUtc(to);

        var models = await ItemsWithRelations(dbContext)
            .Where(i => i.UserId == userId
                        && i.State == ToDoItemState.Active
                        && i.Deadline >= fromUtc
                        && i.Deadline < toUtc)
            .OrderBy(i => i.Deadline)
            .ToListAsync(ct);

        return models.Select(ModelMapper.MapFromModel).ToList();
    }

    public async Task<IReadOnlyList<ToDoItem>> Find(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
    {
        // Func<> нельзя транслировать в SQL, поэтому фильтр по пользователю выполняется в БД,
        // а predicate применяется к уже загруженным сущностям
        var items = await GetAllByUserId(userId, ct);
        return items.Where(predicate).ToList();
    }

    // Eager loading связанных сущностей
    private static IQueryable<ToDoItemModel> ItemsWithRelations(ToDoDataContext dbContext)
        => dbContext.ToDoItems
            .LoadWith(i => i.User)
            .LoadWith(i => i.List)
            .LoadWith(i => i.List!.User);
}
