namespace ToDoListBot.TelegramBot.Scenarios;

public enum ScenarioResult
{
    //Переход к следующему шагу. Сообщение обработано, но сценарий еще не завершен
    Transition,
    //Сценарий завершен
    Completed
}
