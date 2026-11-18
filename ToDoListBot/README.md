# ToDoListBot: ДЗ «Подключение ORM» (linq2db + PostgreSQL)

## Запуск

1. Создать БД и таблицы (PostgreSQL):
   ```sql
   CREATE DATABASE "ToDoList";
   ```
   Затем выполнить в БД `ToDoList` скрипты `ToDoListDb.sql` (таблицы, ключи, индексы) и при желании `ToDoListDb_Insert.sql` (тестовые данные).
2. Задать токен бота (в коде его нет):
   - Windows (PowerShell): `$env:TELEGRAM_BOT_TOKEN="123:ABC"`
   - Linux/macOS: `export TELEGRAM_BOT_TOKEN=123:ABC`
3. Строка подключения по умолчанию указана в `Program.cs`:
   `Host=localhost;Port=5432;Database=ToDoList;Username=postgres;Password=postgres`.
   Ее можно поменять там или задать переменной окружения `TODO_DB_CONNECTION`.
4. `dotnet run --project ToDoListBot`

## Что сделано по пунктам задания

| Пункт | Где |
|---|---|
| Пакеты `linq2db.PostgreSQL` (+ `linq2db`, `Npgsql`) | `ToDoListBot.csproj` |
| Анемичные сущности без конструкторов, все свойства `get; set;` | `Core/Entities` |
| Модели с `[Table]`, `[PrimaryKey]`, `[Column]`, `[Association]` | `Core/DataAccess/Models` |
| `ToDoDataContext : DataConnection` со свойствами `ToDoUsers`, `ToDoLists`, `ToDoItems` | `Infrastructure/DataAccess/ToDoDataContext.cs` |
| `IDataContextFactory<TDataContext>` и `DataContextFactory` | `Infrastructure/DataAccess` |
| `ModelMapper` (все 6 методов) | `Infrastructure/DataAccess/ModelMapper.cs` |
| `SqlToDoRepository` (с `LoadWith`), `SqlToDoListRepository`, `SqlUserRepository` | `Infrastructure/DataAccess` |
| SQL-репозитории вместо File-реализаций, connectionString | `Program.cs` |


# ДЗ «Фоновые задачи»

| Пункт | Где |
|---|---|
| 1. `IBackgroundTask`, `BackgroundTaskRunner` | `BackgroundTasks/IBackgroundTask.cs`, `BackgroundTasks/BackgroundTaskRunner.cs` |
| 2. Абстрактный `BackgroundTask` | `BackgroundTasks/BackgroundTask.cs` |
| 3. `ScenarioContext.CreatedAt` (= `DateTime.UtcNow` при создании), `GetContexts` | `TelegramBot/Scenarios/ScenarioContext.cs`, `IScenarioContextRepository.cs`, `InMemoryScenarioContextRepository.cs` |
| 4. `ResetScenarioBackgroundTask`: запуск раз в час, сброс сценариев старше 1 часа, сообщение с клавиатурой `/addtask /show /report` | `BackgroundTasks/ResetScenarioBackgroundTask.cs` |
| 5. `BackgroundTaskRunner` в `Program.cs`: `AddTask`, `StartTasks` до `StartReceiving`, `StopTasks` до `cts.Cancel()` | `Program.cs` |

Сообщение о сбросе отправляется в чат с Id = `ScenarioContext.UserId`: в личном чате с ботом Id чата совпадает с Id пользователя. Ошибка отправки одному пользователю (например, он заблокировал бота) не мешает сбросить остальные сценарии.


# ДЗ «Нотификации»

Перед запуском выполнить обновленный `ToDoListDb.sql` (добавлена таблица `Notification` и индекс по `UserId`).

| Пункт | Где |
|---|---|
| Сущность `Notification` | `Core/Entities/Notification.cs` |
| `NotificationModel` (Linq2Db), `ITable Notifications` в `ToDoDataContext` | `Infrastructure/DataAccess/Models/NotificationModel.cs`, `Infrastructure/DataAccess/ToDoDataContext.cs` |
| Таблица `Notification` + индекс `UserId` | `ToDoListDb.sql` |
| `INotificationService`, `NotificationService` (напрямую через `IDataContextFactory`) | `Core/Services/INotificationService.cs`, `Infrastructure/NotificationService.cs` |
| `NotificationBackgroundTask` (раз в минуту отправляет нотификации и помечает `MarkNotified`) | `BackgroundTasks/NotificationBackgroundTask.cs` |
| `GetActiveWithDeadline`, `GetUsers` | `IToDoRepository`, `IUserRepository` и SQL-реализации |
| `DeadlineBackgroundTask` (раз в час, тип `Deadline_{task.Id}`) | `BackgroundTasks/DeadlineBackgroundTask.cs` |
| `TodayBackgroundTask` (раз в день, тип `Today_{дата}`, список задач) | `BackgroundTasks/TodayBackgroundTask.cs` |
| Регистрация задач через `AddTask` | `Program.cs` |
