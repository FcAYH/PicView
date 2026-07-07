namespace PicView.Core.Preloading;

public static class PreLoaderConfig
{
    public static int PositiveIterations => Settings.Navigation.PositiveIterations;
    public static int NegativeIterations => Settings.Navigation.NegativeIterations;
    
    /// Total items to preload forward and backward.
    public static int MaxCount => PositiveIterations + NegativeIterations; 
    
    /// Leave a few cores for the UI thread and other system processes to ensure responsiveness.
    public static int MaxParallelism { get; } = Environment.ProcessorCount - 1;
}