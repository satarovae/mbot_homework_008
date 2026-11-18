using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.TelegramBot.Dto;

namespace ToDoListBot.TelegramBot.Scenarios;

public class DeleteListScenario(IUserService userService, IToDoListService toDoListService, IToDoService toDoService) : IScenario
{
    private const string UserKey = "User";
    private const string ListKey = "List";

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.DeleteList;

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

                var lists = await toDoListService.GetUserLists(user.UserId, ct);
                if (lists.Count == 0)
                {
                    await bot.SendMessage(chatId, "У вас нет списков", replyMarkup: Keyboards.Main, cancellationToken: ct);
                    return ScenarioResult.Completed;
                }

                var buttons = lists.Select(l => new[]
                {
                    InlineKeyboardButton.WithCallbackData(l.Name,
                        new ToDoListCallbackDto { Action = "deletelist", ToDoListId = l.Id }.ToString())
                });

                await bot.SendMessage(chatId, "Выберете список для удаления:",
                    replyMarkup: new InlineKeyboardMarkup(buttons), cancellationToken: ct);
                context.CurrentStep = "Approve";
                return ScenarioResult.Transition;
            }

            case "Approve":
            {
                if (update.CallbackQuery?.Data is not { } data
                    || ToDoListCallbackDto.FromString(data).ToDoListId is not { } listId
                    || await toDoListService.Get(listId, ct) is not { } list)
                {
                    await bot.SendMessage(chatId, "Выберите список с помощью кнопок", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data[ListKey] = list;
                await bot.SendMessage(chatId, $"Подтверждаете удаление списка {list.Name} и всех его задач",
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
                        var user = (ToDoUser)context.Data[UserKey];
                        var list = (ToDoList)context.Data[ListKey];

                        var items = await toDoService.GetByUserIdAndList(user.UserId, list.Id, ct);
                        foreach (var item in items)
                            await toDoService.Delete(item.Id, ct);

                        await toDoListService.Delete(list.Id, ct);
                        await bot.SendMessage(chatId, $"Список \"{list.Name}\" удален", replyMarkup: Keyboards.Main, cancellationToken: ct);
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
