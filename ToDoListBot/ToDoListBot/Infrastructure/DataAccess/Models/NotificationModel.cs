using LinqToDB.Mapping;
using ToDoListBot.Core.DataAccess.Models;

namespace ToDoListBot.Infrastructure.DataAccess.Models;

[Table("Notification")]
public class NotificationModel
{
    [PrimaryKey]
    [Column("Id")]
    public Guid Id { get; set; }

    [Column("UserId"), NotNull]
    public Guid UserId { get; set; }

    [Column("Type"), NotNull]
    public string Type { get; set; } = string.Empty;

    [Column("Text"), NotNull]
    public string Text { get; set; } = string.Empty;

    [Column("ScheduledAt"), NotNull]
    public DateTime ScheduledAt { get; set; }

    [Column("IsNotified"), NotNull]
    public bool IsNotified { get; set; }

    [Column("NotifiedAt"), Nullable]
    public DateTime? NotifiedAt { get; set; }

    // Notification.UserId -> ToDoUser
    [Association(ThisKey = nameof(UserId), OtherKey = nameof(ToDoUserModel.UserId), CanBeNull = false)]
    public ToDoUserModel User { get; set; } = null!;
}
