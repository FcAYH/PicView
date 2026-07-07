namespace PicView.Core.Generators;

public static class TabIDGenerator
{
    private static uint _counter = 0;

    public static uint GetNextId() =>
        // Interlocked safely adds 1 to the counter, even across multiple threads
        Interlocked.Increment(ref _counter);
}