using PicView.Core.Linux.Printing;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Linux.Printing;

public static class LinuxPrintInitialization
{
    public static async ValueTask InitializeAsync(MainWindowViewModel vm, string path)
    {
        // 1. Printers via CUPS
        var printers = LinuxPrint.GetAvailablePrinters().ToList(); // includes "Save as PDF" first
        var defaultPrinter = printers.FirstOrDefault() ?? string.Empty;

        // 2. Paper sizes
        var paperSizes = new List<string>();

        await PicView.Avalonia.Printing.PrintInitialization.InitializeAsync(
            vm, path, new LinuxPrintEngine(), printers, paperSizes, defaultPrinter);
    }
}