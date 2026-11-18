namespace ToDoListBot.TelegramBot.Dto;

public class PagedListCallbackDto : ToDoListCallbackDto
{
    public int Page { get; set; }

    public static new PagedListCallbackDto FromString(string input)
    {
        var parts = input.Split('|');
        var listDto = ToDoListCallbackDto.FromString(input);
        return new PagedListCallbackDto
        {
            Action = listDto.Action,
            ToDoListId = listDto.ToDoListId,
            Page = parts.Length > 2 && int.TryParse(parts[2], out var page) ? page : 0
        };
    }

    public override string ToString() => $"{base.ToString()}|{Page}";
}
