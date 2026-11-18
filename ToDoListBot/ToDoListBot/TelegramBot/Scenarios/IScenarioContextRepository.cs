namespace ToDoListBot.TelegramBot.Scenarios;

//Репозиторий, который отвечает за доступ к контекстам пользователей
public interface IScenarioContextRepository
{
    //Получить контекст пользователя
    Task<ScenarioContext?> GetContext(long userId, CancellationToken ct);
    //Задать контекст пользователя
    Task SetContext(long userId, ScenarioContext context, CancellationToken ct);
    //Сбросить (очистить) контекст пользователя
    Task ResetContext(long userId, CancellationToken ct);
    //Получить все контексты
    Task<IReadOnlyList<ScenarioContext>> GetContexts(CancellationToken ct);
}
