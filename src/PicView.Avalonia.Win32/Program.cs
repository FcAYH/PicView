using Avalonia;
using Avalonia.Controls;

namespace PicView.Avalonia.Win32;

// ReSharper disable once ClassNeverInstantiated.Global
internal class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
#if DEBUG
            .LogToTrace()
#endif
            .UseR3()
            .With(new SkiaOptions
            {
                MaxGpuResourceSizeBytes = 256_000_000,
                UseOpacitySaveLayer = true
            })
            .UseWin32()
            .UseHarfBuzz()
            .UseSkia();
    }
}