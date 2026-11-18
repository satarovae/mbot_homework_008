using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using ToDoListBot.Core.Entities;
using ToDoListBot.Core.Exceptions;
using ToDoListBot.Core.Services;
using ToDoListBot.Helpers;
using ToDoListBot.TelegramBot.Dto;
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot.TelegramBot;

public class UpdateHandler(
    IUserService userService,
    IToDoService toDoService,
    IToDoReportService reportService,
    IToDoListService toDoListService,
    IEnumerable<IScenario> scenarios,
    IScenarioContextRepository contextRepository) : IUpdateHandler
{
    private static int _pageSize = 5;

    private readonly IReadOnlyList<IScenario> _scenarios = scenarios.ToList();

    public static readonly BotCommand[] Commands =
    [
        new() { Command = "start", Description = "Начать работу с ботом" },
        new() { Command = "addtask", Description = "Добавить задачу" },
        new() { Command = "show", Description = "Показать задачи" },
        new() { Command = "report", Description = "Статистика по задачам" },
        new() { Command = "find", Description = "Найти задачи по началу имени" },
        new() { Command = "cancel", Description = "Отменить текущее действие" },
        new() { Command = "help", Description = "Справка" },
        new() { Command = "info", Description = "Информация о боте" }
    ];

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        try
        {
            await (update switch
            {
                { Message: { } message } => OnMessage(bot, update, message, ct),
                { CallbackQuery: { } callbackQuery } => OnCallbackQuery(bot, update, callbackQuery, ct),
                _ => OnUnknown(update)
            });
        }
        catch (Exception ex) when (ex is TaskCountLimitException or TaskLengthLimitException
                                       or DuplicateTaskException or ToDoListValidationException or ArgumentException)
        {
            // Ошибка валидации: сценарий сбрасывается, пользователь получает сообщение
            var userId = update.GetTelegramUserId();
            await contextRepository.ResetContext(userId, ct);
            await bot.SendMessage(update.GetChatId(), ex.Message, replyMarkup: Keyboards.Main, cancellationToken: ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке update {update.Id}: {ex}");
            if (update.Message is not null || update.CallbackQuery?.Message is not null)
                await bot.SendMessage(update.GetChatId(), "Произошла непредвиденная ошибка", cancellationToken: ct);
        }
    }

    public Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, HandleErrorSource source, CancellationToken ct)
    {
        Console.WriteLine($"HandleError ({source}): {exception.Message}");
        return Task.CompletedTask;
    }

    private Task OnUnknown(Update update)
    {
        Console.WriteLine($"Необработанный тип update: {update.Type}");
        return Task.CompletedTask;
    }

    private async Task OnMessage(ITelegramBotClient bot, Update update, Message message, CancellationToken ct)
    {
        var text = message.Text?.Trim() ?? string.Empty;
        var chatId = message.Chat.Id;
        var telegramUserId = message.From?.Id ?? chatId;

        var user = await userService.GetUser(telegramUserId, ct);
        var command = text.Split(' ', 2)[0];
        var argument = text.Length > command.Length ? text[command.Length..].Trim() : string.Empty;

        switch (command)
        {
            case "/start":
                await HandleStart(bot, message, user, ct);
                return;
            case "/help":
                await bot.SendMessage(chatId, HelpText(user is not null), cancellationToken: ct);
                return;
            case "/info":
                await bot.SendMessage(chatId, "ToDoList бот. Версия 1.0. Хранение данных: PostgreSQL (linq2db)", cancellationToken: ct);
                return;
        }

        if (user is null)
        {
            await bot.SendMessage(chatId, "Вы не зарегистрированы. Нажмите /start", replyMarkup: Keyboards.Start, cancellationToken: ct);
            return;
        }

        if (command == "/cancel")
        {
            await contextRepository.ResetContext(telegramUserId, ct);
            await bot.SendMessage(chatId, "Действие отменено", replyMarkup: Keyboards.Main, cancellationToken: ct);
            return;
        }

        var context = await contextRepository.GetContext(telegramUserId, ct);
        if (context is not null)
        {
            await ProcessScenario(bot, context, update, ct);
            return;
        }

        switch (command)
        {
            case "/addtask":
                await StartScenario(bot, ScenarioType.AddTask, telegramUserId, update, ct);
                break;
            case "/show":
                await HandleShow(bot, chatId, user, ct);
                break;
            case "/report":
                await HandleReport(bot, chatId, user, ct);
                break;
            case "/find":
                await HandleFind(bot, chatId, user, argument, ct);
                break;
            default:
                await bot.SendMessage(chatId, "Неизвестная команда. Введите /help", replyMarkup: Keyboards.Main, cancellationToken: ct);
                break;
        }
    }

    private async Task OnCallbackQuery(ITelegramBotClient bot, Update update, CallbackQuery query, CancellationToken ct)
    {
        await bot.AnswerCallbackQuery(query.Id, cancellationToken: ct);

        if (query.Message is null || query.Data is null)
            return;

        // Незарегистрированным пользователям CallbackQuery не обрабатываем
        var user = await userService.GetUser(query.From.Id, ct);
        if (user is null)
            return;

        var context = await contextRepository.GetContext(query.From.Id, ct);
        if (context is not null)
        {
            await ProcessScenario(bot, context, update, ct);
            return;
        }

        var chatId = query.Message.Chat.Id;
        var messageId = query.Message.MessageId;
        var callbackDto = CallbackDto.FromString(query.Data);

        switch (callbackDto.Action)
        {
            case "show":
                await ShowItems(bot, chatId, messageId, user, PagedListCallbackDto.FromString(query.Data), ToDoItemState.Active, ct);
                break;

            case "show_completed":
                await ShowItems(bot, chatId, messageId, user, PagedListCallbackDto.FromString(query.Data), ToDoItemState.Completed, ct);
                break;

            case "showtask":
                await ShowItem(bot, chatId, messageId, ToDoItemCallbackDto.FromString(query.Data).ToDoItemId, ct);
                break;

            case "completetask":
            {
                var itemId = ToDoItemCallbackDto.FromString(query.Data).ToDoItemId;
                await toDoService.MarkCompleted(itemId, ct);
                await bot.EditMessageText(chatId, messageId, "✅ Задача выполнена", cancellationToken: ct);
                break;
            }

            case "deletetask":
                await StartScenario(bot, ScenarioType.DeleteTask, query.From.Id, update, ct);
                break;

            case "addlist":
                await StartScenario(bot, ScenarioType.AddList, query.From.Id, update, ct);
                break;

            case "deletelist":
                await StartScenario(bot, ScenarioType.DeleteList, query.From.Id, update, ct);
                break;
        }
    }

    private IScenario GetScenario(ScenarioType scenario)
        => _scenarios.FirstOrDefault(s => s.CanHandle(scenario))
           ?? throw new InvalidOperationException($"Сценарий {scenario} не найден");

    private async Task StartScenario(ITelegramBotClient bot, ScenarioType scenario, long telegramUserId, Update update, CancellationToken ct)
    {
        var context = new ScenarioContext(scenario) { UserId = telegramUserId };
        await ProcessScenario(bot, context, update, ct);
    }

    private async Task ProcessScenario(ITelegramBotClient bot, ScenarioContext context, Update update, CancellationToken ct)
    {
        var scenario = GetScenario(context.CurrentScenario);
        var result = await scenario.HandleMessageAsync(bot, context, update, ct);

        if (result == ScenarioResult.Completed)
            await contextRepository.ResetContext(context.UserId, ct);
        else
            await contextRepository.SetContext(context.UserId, context, ct);
    }

    private async Task HandleStart(ITelegramBotClient bot, Message message, ToDoUser? user, CancellationToken ct)
    {
        if (user is null)
        {
            var from = message.From ?? throw new InvalidOperationException("Не удалось определить пользователя");
            user = await userService.RegisterUser(from.Id, from.Username ?? from.FirstName, ct);
            await bot.SendMessage(message.Chat.Id, $"Привет, {user.TelegramUserName}! Вы зарегистрированы.",
                replyMarkup: Keyboards.Main, cancellationToken: ct);
            return;
        }

        await bot.SendMessage(message.Chat.Id, $"С возвращением, {user.TelegramUserName}!",
            replyMarkup: Keyboards.Main, cancellationToken: ct);
    }

    private async Task HandleShow(ITelegramBotClient bot, long chatId, ToDoUser user, CancellationToken ct)
    {
        var lists = await toDoListService.GetUserLists(user.UserId, ct);

        var rows = new List<InlineKeyboardButton[]>
        {
            new[] { InlineKeyboardButton.WithCallbackData("📌Без списка", new PagedListCallbackDto { Action = "show" }.ToString()) }
        };
        rows.AddRange(lists.Select(l => new[]
        {
            InlineKeyboardButton.WithCallbackData(l.Name, new PagedListCallbackDto { Action = "show", ToDoListId = l.Id }.ToString())
        }));
        rows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🆕Добавить", "addlist"),
            InlineKeyboardButton.WithCallbackData("❌Удалить", "deletelist")
        });

        await bot.SendMessage(chatId, "Выберите список", replyMarkup: new InlineKeyboardMarkup(rows), cancellationToken: ct);
    }

    private async Task ShowItems(ITelegramBotClient bot, long chatId, int messageId, ToDoUser user,
        PagedListCallbackDto listDto, ToDoItemState state, CancellationToken ct)
    {
        var items = (await toDoService.GetByUserIdAndList(user.UserId, listDto.ToDoListId, ct))
            .Where(i => i.State == state)
            .ToList();

        var title = state == ToDoItemState.Active ? "Активные задачи" : "Выполненные задачи";
        if (listDto.ToDoListId is { } listId && await toDoListService.Get(listId, ct) is { } list)
            title += $" списка \"{list.Name}\"";

        var buttons = items
            .Select(i => new KeyValuePair<string, string>(
                $"{i.Name} (до {i.Deadline:dd.MM.yyyy})",
                new ToDoItemCallbackDto { Action = "showtask", ToDoItemId = i.Id }.ToString()))
            .ToList();

        var markup = BuildPagedButtons(buttons, listDto);

        if (state == ToDoItemState.Active)
        {
            var completedDto = new PagedListCallbackDto { Action = "show_completed", ToDoListId = listDto.ToDoListId, Page = 0 };
            markup.AddNewRow(InlineKeyboardButton.WithCallbackData("☑️Посмотреть выполненные", completedDto.ToString()));
        }

        var text = items.Count == 0 ? $"{title}:\nЗадач нет" : $"{title}:";
        await bot.EditMessageText(chatId, messageId, text, replyMarkup: markup, cancellationToken: ct);
    }

    private InlineKeyboardMarkup BuildPagedButtons(IReadOnlyList<KeyValuePair<string, string>> callbackData, PagedListCallbackDto listDto)
    {
        var totalPages = (int)Math.Ceiling(callbackData.Count / (double)_pageSize);

        var markup = new InlineKeyboardMarkup();
        foreach (var (text, data) in callbackData.GetBatchByNumber(_pageSize, listDto.Page))
            markup.AddNewRow(InlineKeyboardButton.WithCallbackData(text, data));

        var navigation = new List<InlineKeyboardButton>();
        if (listDto.Page > 0)
        {
            var prev = new PagedListCallbackDto { Action = listDto.Action, ToDoListId = listDto.ToDoListId, Page = listDto.Page - 1 };
            navigation.Add(InlineKeyboardButton.WithCallbackData("⬅️", prev.ToString()));
        }
        if (listDto.Page < totalPages - 1)
        {
            var next = new PagedListCallbackDto { Action = listDto.Action, ToDoListId = listDto.ToDoListId, Page = listDto.Page + 1 };
            navigation.Add(InlineKeyboardButton.WithCallbackData("➡️", next.ToString()));
        }
        if (navigation.Count > 0)
            markup.AddNewRow(navigation.ToArray());

        return markup;
    }

    private async Task ShowItem(ITelegramBotClient bot, long chatId, int messageId, Guid itemId, CancellationToken ct)
    {
        var item = await toDoService.Get(itemId, ct);
        if (item is null)
        {
            await bot.EditMessageText(chatId, messageId, "Задача не найдена", cancellationToken: ct);
            return;
        }

        var text =
            $"<b>{Html(item.Name)}</b>\n" +
            $"Список: {Html(item.List?.Name ?? "без списка")}\n" +
            $"Статус: {(item.State == ToDoItemState.Active ? "активна" : "выполнена")}\n" +
            $"Срок: {item.Deadline:dd.MM.yyyy}\n" +
            $"Создана: {item.CreatedAt:dd.MM.yyyy HH:mm:ss}\n" +
            $"Id: <code>{item.Id}</code>";

        InlineKeyboardMarkup? markup = null;
        if (item.State == ToDoItemState.Active)
        {
            markup = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("✅Выполнить", new ToDoItemCallbackDto { Action = "completetask", ToDoItemId = item.Id }.ToString()),
                InlineKeyboardButton.WithCallbackData("❌Удалить", new ToDoItemCallbackDto { Action = "deletetask", ToDoItemId = item.Id }.ToString())
            });
        }

        await bot.EditMessageText(chatId, messageId, text, ParseMode.Html, replyMarkup: markup, cancellationToken: ct);
    }

    private async Task HandleReport(ITelegramBotClient bot, long chatId, ToDoUser user, CancellationToken ct)
    {
        var (total, completed, active, generatedAt) = await reportService.GetUserStats(user.UserId, ct);
        await bot.SendMessage(chatId,
            $"Статистика по задачам на {generatedAt:dd.MM.yyyy HH:mm:ss}. Всего: {total}; Завершенных: {completed}; Активных: {active};",
            replyMarkup: Keyboards.Main, cancellationToken: ct);
    }

    private async Task HandleFind(ITelegramBotClient bot, long chatId, ToDoUser user, string prefix, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            await bot.SendMessage(chatId, "Укажите начало имени задачи. Пример: /find Купить", cancellationToken: ct);
            return;
        }

        var items = await toDoService.Find(user, prefix, ct);
        if (items.Count == 0)
        {
            await bot.SendMessage(chatId, "Задачи не найдены", cancellationToken: ct);
            return;
        }

        var lines = items.Select(i =>
            $"({(i.State == ToDoItemState.Active ? "Active" : "Completed")}) {Html(i.Name)} - {i.CreatedAt:dd.MM.yyyy HH:mm:ss} - <code>{i.Id}</code>");
        await bot.SendMessage(chatId, string.Join("\n", lines), ParseMode.Html, cancellationToken: ct);
    }

    private static string HelpText(bool registered)
    {
        if (!registered)
            return "Доступные команды:\n/start - регистрация\n/help - справка\n/info - информация о боте";

        return "Доступные команды:\n" +
               "/addtask - добавить задачу (название, срок dd.MM.yyyy, список)\n" +
               "/show - выбрать список и посмотреть задачи; выполнить или удалить задачу кнопками\n" +
               "/report - статистика по задачам\n" +
               "/find <начало имени> - поиск задач\n" +
               "/cancel - отменить текущее действие\n" +
               "/help - справка\n" +
               "/info - информация о боте";
    }

    private static string Html(string text) => WebUtility.HtmlEncode(text);
}
