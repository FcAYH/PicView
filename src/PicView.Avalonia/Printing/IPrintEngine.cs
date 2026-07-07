using PicView.Core.ViewModels;

namespace PicView.Avalonia.Printing;

public interface IPrintEngine
{
    ValueTask UpdatePreviewAsync(TabViewModel tab, PrintPreviewViewModel previewVm);
    ValueTask RunPrintAsync(TabViewModel tab, PrintPreviewViewModel preview);
}
