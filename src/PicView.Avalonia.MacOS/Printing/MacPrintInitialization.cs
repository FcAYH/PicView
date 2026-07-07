using PicView.Core.MacOS.Printing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.MacOS.Printing;

public static class MacPrintInitialization
{
    public static async ValueTask InitializeAsync(MainWindowViewModel vm, string path)
    {
        // 1. Printers via CUPS
        var printers = MacOSPrint.GetAvailablePrinters().ToList(); // includes "Save as PDF" first
        var defaultPrinter = printers.FirstOrDefault() ?? string.Empty;

        // 2. Paper sizes - from printer or fallback
        var paperSizes = CupsPaperQuery.GetPaperSizes(defaultPrinter).ToList();

        await PicView.Avalonia.Printing.PrintInitialization.InitializeAsync(
            vm, path, new MacPrintEngine(), printers, paperSizes, defaultPrinter);
    }
}