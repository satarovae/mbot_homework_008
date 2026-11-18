using Telegram.Bot;
using Telegram.Bot.Types;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.TelegramBot.Dto;

namespace ToDoListBot.TelegramBot.Scenarios;

public class DeleteTaskScenario(IToDoService toDoService) : IScenario
{
    private const string ItemKey = "Item";

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteTask;

    public async Task<ScenarioResult> HandleMessageAsync(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
    {
        var chatId = update.GetChatId();

        switch (context.CurrentStep)
        {
            case null:
            {
                // Сценарий запускается кнопкой "❌Удалить" с callbackData "deletetask|{id}"
                if (update.CallbackQuery?.Data is not { } data
                    || await toDoService.Get(ToDoItemCallbackDto.FromString(data).ToDoItemId, ct) is not { } item)
                {
                    await bot.SendMessage(chatId, "Задача не найдена", replyMarkup: Keyboards.Main, cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                context.Data[ItemKey] = item;
                await bot.SendMessage(chatId, $"Подтверждаете удаление задачи \"{item.Name}\"?",
                    replyMarkup: Keyboards.YesNo, cancellationToken: ct);
                context.CurrentStep = "Delete";
                return ScenarioResult.Transition;
            }

            case "Delete":
            {
                switch (update.CallbackQuery?.Data)
                {
                    case "yes":
                    {
                        var item = (ToDoItem)context.Data[ItemKey];
                        await toDoService.Delete(item.Id, ct);
                        await bot.SendMessage(chatId, $"Задача \"{item.Name}\" удалена", replyMarkup: Keyboards.Main, cancellationToken: ct);
                        return ScenarioResult.Completed;
                    }
                    case "no":
                        await bot.SendMessage(chatId, "Удаление отменено", replyMarkup: Keyboards.Main, cancellationToken: ct);
                        return ScenarioResult.Completed;
                    default:
                        await bot.SendMessage(chatId, "Подтвердите удаление кнопками ✅Да / ❌Нет", cancellationToken: ct);
                        return ScenarioResult.Transition;
                }
            }

            default:
                throw new InvalidOperationException($"Неизвестный шаг сценария: {context.CurrentStep}");
        }
    }
}
