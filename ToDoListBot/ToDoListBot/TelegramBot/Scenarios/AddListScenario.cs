using Telegram.Bot;
using Telegram.Bot.Types;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;

namespace ToDoListBot.TelegramBot.Scenarios;

public class AddListScenario(IUserService userService, IToDoListService toDoListService) : IScenario
{
    private const string UserKey = "User";

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddList;

    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
    {
        var chatId = update.GetChatId();

        switch (context.CurrentStep)
        {
            case null:
            {
                var user = await userService.GetUser(update.GetTelegramUserId(), ct)
                           ?? throw new InvalidOperationException("Пользователь не зарегистрирован");
                context.Data[UserKey] = user;

                await bot.SendMessage(chatId, "Введите название списка:", replyMarkup: Keyboards.Cancel, cancellationToken: ct);
                context.CurrentStep = "Name";
                return ScenarioResult.Transition;
            }

            case "Name":
            {
                var name = update.Message?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    await bot.SendMessage(chatId, "Введите название списка:", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                var list = await toDoListService.Add((ToDoUser)context.Data[UserKey], name, ct);
                await bot.SendMessage(chatId, $"Список \"{list.Name}\" добавлен", replyMarkup: Keyboards.Main, cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            default:
                throw new InvalidOperationException($"Неизвестный шаг сценария: {context.CurrentStep}");
        }
    }
}
