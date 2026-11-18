using LinqToDB;
using LinqToDB.Data;
using ToDoListBot.Core.DataAccess.Models;
using ToDoListBot.Infrastructure.DataAccess.Models;

namespace ToDoListBot.Infrastructure.DataAccess;

public class ToDoDataContext : DataConnection
{
    // В linq2db 6.x конструктор base(ProviderName.PostgreSQL, connectionString) помечен [Obsolete],
    // поэтому используется рекомендуемый эквивалент через DataOptions.
    public ToDoDataContext(string connectionString)
        : base(new DataOptions().UseConnectionString(ProviderName.PostgreSQL, connectionString))
    {
    }

    public ITable<ToDoUserModel> ToDoUsers => this.GetTable<ToDoUserModel>();
    public ITable<ToDoListModel> ToDoLists => this.GetTable<ToDoListModel>();
    public ITable<ToDoItemModel> ToDoItems => this.GetTable<ToDoItemModel>();
    public ITable<NotificationModel> Notifications => this.GetTable<NotificationModel>();
}
