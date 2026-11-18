using Telegram.Bot.Types.ReplyMarkups;
using ToDoListBot.Core.Entities;
using ToDoListBot.TelegramBot.Dto;

namespace ToDoListBot.TelegramBot;

public static class Keyboards
{
    public static ReplyKeyboardMarkup Start => new(new[] { new KeyboardButton("/start") }) { ResizeKeyboard = true };

    public static ReplyKeyboardMarkup Main => new(new[]
    {
        new KeyboardButton("/addtask"),
        new KeyboardButton("/show"),
        new KeyboardButton("/report")
    }) { ResizeKeyboard = true };

    public static ReplyKeyboardMarkup Cancel => new(new[] { new KeyboardButton("/cancel") }) { ResizeKeyboard = true };

    // Кнопки выбора списка: "📌Без списка" + списки пользователя
    public static InlineKeyboardMarkup Lists(IEnumerable<ToDoList> lists, string action)
    {
        var rows = new List<InlineKeyboardButton[]>
        {
            new[] { InlineKeyboardButton.WithCallbackData("📌Без списка", new ToDoListCallbackDto { Action = action }.ToString()) }
        };

        rows.AddRange(lists.Select(l => new[]
        {
            InlineKeyboardButton.WithCallbackData(l.Name, new ToDoListCallbackDto { Action = action, ToDoListId = l.Id }.ToString())
        }));

        return new InlineKeyboardMarkup(rows);
    }

    public static InlineKeyboardMarkup YesNo => new(new[]
    {
        InlineKeyboardButton.WithCallbackData("✅Да", "yes"),
        InlineKeyboardButton.WithCallbackData("❌Нет", "no")
    });
}
