using Telegram.Bot.Types;

namespace ToDoListBot.TelegramBot;

internal static class UpdateExtensions
{
    public static long GetChatId(this Update update)
        => update.Message?.Chat.Id
           ?? update.CallbackQuery?.Message?.Chat.Id
           ?? throw new InvalidOperationException("Не удалось определить чат");

    public static long GetTelegramUserId(this Update update)
        => update.Message?.From?.Id
           ?? update.CallbackQuery?.From.Id
           ?? throw new InvalidOperationException("Не удалось определить пользователя");
}
