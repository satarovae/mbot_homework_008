using System.Globalization;
using Telegram.Bot;
using Telegram.Bot.Types;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Services;
using ToDoListBot.TelegramBot.Dto;

namespace ToDoListBot.TelegramBot.Scenarios;

public class AddTaskScenario(IUserService userService, IToDoService toDoService, IToDoListService toDoListService) : IScenario
{
    private const string UserKey = "User";
    private const string NameKey = "Name";
    private const string DeadlineKey = "Deadline";

    public bool CanHandle(ScenarioType scenario) => scenario == ScenarioType.AddTask;

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

                await bot.SendMessage(chatId, "Введите название задачи:", replyMarkup: Keyboards.Cancel, cancellationToken: ct);
                context.CurrentStep = "Name";
                return ScenarioResult.Transition;
            }

            case "Name":
            {
                var name = update.Message?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    await bot.SendMessage(chatId, "Введите название задачи:", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data[NameKey] = name;
                await bot.SendMessage(chatId, "Введите срок выполнения задачи в формате dd.MM.yyyy:", cancellationToken: ct);
                context.CurrentStep = "Deadline";
                return ScenarioResult.Transition;
            }

            case "Deadline":
            {
                var text = update.Message?.Text?.Trim();
                if (!DateTime.TryParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var deadline))
                {
                    await bot.SendMessage(chatId, "Неверный формат даты. Введите срок в формате dd.MM.yyyy:", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                context.Data[DeadlineKey] = DateTime.SpecifyKind(deadline, DateTimeKind.Utc);

                var user = (ToDoUser)context.Data[UserKey];
                var lists = await toDoListService.GetUserLists(user.UserId, ct);
                await bot.SendMessage(chatId, "Выберите список:", replyMarkup: Keyboards.Lists(lists, "addtask_list"), cancellationToken: ct);
                context.CurrentStep = "List";
                return ScenarioResult.Transition;
            }

            case "List":
            {
                if (update.CallbackQuery?.Data is not { } data)
                {
                    await bot.SendMessage(chatId, "Выберите список с помощью кнопок", cancellationToken: ct);
                    return ScenarioResult.Transition;
                }

                var listDto = ToDoListCallbackDto.FromString(data);
                var list = listDto.ToDoListId is { } listId ? await toDoListService.Get(listId, ct) : null;

                var user = (ToDoUser)context.Data[UserKey];
                var name = (string)context.Data[NameKey];
                var deadline = (DateTime)context.Data[DeadlineKey];

                var item = await toDoService.Add(user, name, deadline, list, ct);

                await bot.SendMessage(chatId,
                    $"Задача \"{item.Name}\" добавлена{(list is null ? string.Empty : $" в список \"{list.Name}\"")}",
                    replyMarkup: Keyboards.Main, cancellationToken: ct);
                return ScenarioResult.Completed;
            }

            default:
                throw new InvalidOperationException($"Неизвестный шаг сценария: {context.CurrentStep}");
        }
    }
}
