namespace ToDoListBot.Core.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public ToDoUser User { get; set; } = null!;
    // Тип нотификации. Например: Deadline_{ToDoItem.Id}, Today_{DateOnly.FromDateTime(DateTime.UtcNow)}
    public string Type { get; set; } = string.Empty;
    // Текст, который будет отправлен
    public string Text { get; set; } = string.Empty;
    // Запланированная дата отправки
    public DateTime ScheduledAt { get; set; }
    // Флаг отправки
    public bool IsNotified { get; set; }
    // Фактическая дата отправки
    public DateTime? NotifiedAt { get; set; }
}
