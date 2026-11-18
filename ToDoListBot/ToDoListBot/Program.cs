using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using ToDoListBot.BackgroundTasks;
using ToDoListBot.Infrastructure;
using ToDoListBot.Core.Services;
using ToDoListBot.Infrastructure.DataAccess;
using ToDoListBot.TelegramBot;
using ToDoListBot.TelegramBot.Scenarios;

const int maxTaskCount = 100;
const int maxTaskLength = 100;

// Токен бота не хранится в коде: задается переменной окружения TELEGRAM_BOT_TOKEN
var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
if (string.IsNullOrWhiteSpace(token))
{
    Console.WriteLine("Не задана переменная окружения TELEGRAM_BOT_TOKEN");
    return;
}

// Строка подключения к БД из ДЗ "Модель базы данных".
// Можно переопределить переменной окружения TODO_DB_CONNECTION
var connectionString = Environment.GetEnvironmentVariable("TODO_DB_CONNECTION")
                       ?? "Host=localhost;Port=5432;Database=ToDoList;Username=postgres;Password=postgres";

// DataAccess (linq2db + PostgreSQL)
var dataContextFactory = new DataContextFactory(connectionString);
var userRepository = new SqlUserRepository(dataContextFactory);
var toDoRepository = new SqlToDoRepository(dataContextFactory);
var toDoListRepository = new SqlToDoListRepository(dataContextFactory);

// Services
var userService = new UserService(userRepository);
var toDoService = new ToDoService(toDoRepository, maxTaskCount, maxTaskLength);
var toDoListService = new ToDoListService(toDoListRepository);
var reportService = new ToDoReportService(toDoRepository);
var notificationService = new NotificationService(dataContextFactory);

// Scenarios
IScenario[] scenarios =
[
    new AddTaskScenario(userService, toDoService, toDoListService),
    new AddListScenario(userService, toDoListService),
    new DeleteListScenario(userService, toDoListService, toDoService),
    new DeleteTaskScenario(toDoService)
];
var contextRepository = new InMemoryScenarioContextRepository();

var handler = new UpdateHandler(userService, toDoService, reportService, toDoListService, scenarios, contextRepository);

using var cts = new CancellationTokenSource();
var botClient = new TelegramBotClient(token, cancellationToken: cts.Token);

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
    DropPendingUpdates = true
};

// Фоновые задачи
using var backgroundTaskRunner = new BackgroundTaskRunner();
backgroundTaskRunner.AddTask(new ResetScenarioBackgroundTask(TimeSpan.FromHours(1), contextRepository, botClient));
backgroundTaskRunner.AddTask(new NotificationBackgroundTask(notificationService, botClient));
backgroundTaskRunner.AddTask(new DeadlineBackgroundTask(notificationService, userRepository, toDoRepository));
backgroundTaskRunner.AddTask(new TodayBackgroundTask(notificationService, userRepository, toDoRepository));
// Запуск фоновых задач до запуска бота
backgroundTaskRunner.StartTasks(cts.Token);

await botClient.SetMyCommands(UpdateHandler.Commands, cancellationToken: cts.Token);
botClient.StartReceiving(handler, receiverOptions, cts.Token);

var me = await botClient.GetMe(cts.Token);
Console.WriteLine($"{me.FirstName} запущен!");

while (!cts.IsCancellationRequested)
{
    Console.WriteLine("Нажмите клавишу A для выхода");
    var key = Console.ReadKey(intercept: true);

    if (key.Key == ConsoleKey.A)
    {
        // Остановка фоновых задач до отмены CancellationTokenSource
        await backgroundTaskRunner.StopTasks(CancellationToken.None);
        cts.Cancel();
        Console.WriteLine("Бот остановлен");
    }
    else
    {
        var info = await botClient.GetMe(cts.Token);
        Console.WriteLine($"Бот: {info.FirstName} (@{info.Username}), Id: {info.Id}");
    }
}
