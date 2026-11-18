using LinqToDB.Data;

namespace ToDoListBot.Infrastructure.DataAccess;

// Создает новый DataConnection на каждую сессию (вызов метода репозитория)
// https://linq2db.github.io/articles/general/Managing-data-connection.html
public interface IDataContextFactory<TDataContext> where TDataContext : DataConnection
{
    TDataContext CreateDataContext();
}
