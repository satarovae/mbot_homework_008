namespace ToDoListBot.Core.Exceptions;

public class TaskCountLimitException(int taskCountLimit)
    : Exception($"Превышено максимальное количество задач равное {taskCountLimit}");
