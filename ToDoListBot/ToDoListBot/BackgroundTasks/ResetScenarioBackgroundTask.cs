using Telegram.Bot;
using ToDoListBot.TelegramBot;
using ToDoListBot.TelegramBot.Scenarios;

namespace ToDoListBot.BackgroundTasks;

// Раз в час сбрасывает сценарии, по которым не было ответа дольше resetScenarioTimeout
public class ResetScenarioBackgroundTask(
    TimeSpan resetScenarioTimeout,
    IScenarioContextRepository scenarioRepository,
    ITelegramBotClient bot)
    : BackgroundTask(TimeSpan.FromHours(1), nameof(ResetScenarioBackgroundTask))
{
    protected override async Task Execute(CancellationToken ct)
    {
        var contexts = await scenarioRepository.GetContexts(ct);

        foreach (var context in contexts)
        {
            if (DateTime.UtcNow - context.CreatedAt <= resetScenarioTimeout)
                continue;

            await scenarioRepository.ResetContext(context.UserId, ct);

            try
            {
                // В личном чате Id чата совпадает с Id пользователя в Telegram
                await bot.SendMessage(
                    context.UserId,
                    $"Сценарий отменен, так как не поступил ответ в течение {resetScenarioTimeout}",
                    replyMarkup: Keyboards.Main, // ["/addtask", "/show", "/report"]
                    cancellationToken: ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Ошибка отправки одному пользователю не должна мешать сбросу остальных сценариев
                Console.WriteLine($"{nameof(ResetScenarioBackgroundTask)}. Не удалось отправить сообщение {context.UserId}: {ex.Message}");
            }
        }
    }
}
