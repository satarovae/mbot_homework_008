namespace ToDoListBot.Core.Exceptions;

public class DuplicateTaskException(string task)
    : Exception($"Задача '{task}' уже существует");
