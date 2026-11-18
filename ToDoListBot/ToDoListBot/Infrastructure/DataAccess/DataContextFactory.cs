namespace ToDoListBot.Infrastructure.DataAccess;

public class DataContextFactory(string connectionString) : IDataContextFactory<ToDoDataContext>
{
    public ToDoDataContext CreateDataContext() => new(connectionString);
}
