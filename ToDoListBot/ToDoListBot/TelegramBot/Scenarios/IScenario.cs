using Telegram.Bot;
using Telegram.Bot.Types;

namespace ToDoListBot.TelegramBot.Scenarios;

public interface IScenario
{
    //Проверяет, может ли текущий сценарий обрабатывать указанный тип сценария.
    bool CanHandle(ScenarioType scenario);
    //Обрабатывает входящее сообщение от пользователя в рамках текущего сценария.
    Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct);
}
