namespace ToDoListBot.Core.Exceptions;

public class TaskLengthLimitException(int taskLength, int taskLengthLimit)
    : Exception($"Длина задачи '{taskLength}' превышает максимально допустимое значение {taskLengthLimit}");
