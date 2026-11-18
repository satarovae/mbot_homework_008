namespace ToDoListBot.TelegramBot.Dto;

public class CallbackDto
{
    public string Action { get; set; } = string.Empty;

    public static CallbackDto FromString(string input)
        => new() { Action = input.Split('|')[0] };

    public override string ToString() => Action;
}
