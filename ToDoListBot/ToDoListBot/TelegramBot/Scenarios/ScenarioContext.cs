namespace ToDoListBot.TelegramBot.Scenarios;

public class ScenarioContext
{
    public ScenarioContext(ScenarioType scenario)
    {
        CurrentScenario = scenario;
    }

    //Id пользователя в Telegram
    public long UserId { get; set; }
    public ScenarioType CurrentScenario { get; set; }
    //Текущий шаг сценария
    public string? CurrentStep { get; set; }
    //Дополнительная инфрмация, необходимая для работы сценария
    public Dictionary<string, object> Data { get; } = new();
    //Время создания контекста (UTC). Используется для сброса сценария по таймауту
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
