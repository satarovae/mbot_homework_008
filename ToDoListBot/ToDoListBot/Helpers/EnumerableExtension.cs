namespace ToDoListBot.Helpers;

public static class EnumerableExtension
{
    public static IEnumerable<TSource> GetBatchByNumber<TSource>(this IEnumerable<TSource> source, int batchSize, int batchNumber)
        => source.Skip(batchSize * batchNumber).Take(batchSize);
}
