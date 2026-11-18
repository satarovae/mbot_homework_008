using LinqToDB.Mapping;
using ToDoListBot.Core.Entities;

namespace ToDoListBot.Core.DataAccess.Models;

[Table("ToDoItem")]
public class ToDoItemModel
{
    [PrimaryKey]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("UserId"), NotNull]
    public Guid UserId { get; set; }

    [Column("ListId"), Nullable]
    public Guid? ListId { get; set; }

    [Column("Name"), NotNull]
    public string Name { get; set; } = string.Empty;

    [Column("CreatedAt"), NotNull]
    public DateTime CreatedAt { get; set; }

    // В БД хранится как INT
    [Column("State"), NotNull]
    public ToDoItemState State { get; set; }

    [Column("StateChangedAt"), Nullable]
    public DateTime? StateChangedAt { get; set; }

    [Column("Deadline"), NotNull]
    public DateTime Deadline { get; set; }

    // ToDoItem.UserId -> ToDoUser
    [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.UserId), CanBeNull = false)]
    public ToDoUserModel User { get; set; } = null!;

    // ToDoItem.ListId -> ToDoList
    [Association(ThisKey = nameof(ListId), OtherKey = nameof(ToDoListModel.Id), CanBeNull = true)]
    public ToDoListModel? List { get; set; }
}
