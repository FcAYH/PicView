using Avalonia;
using Avalonia.Controls;
using PicView.Core.Config;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.CustomControls;

public class PrintWindow: GenericWindow
{
    protected PrintWindowConfig? Config;

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.PrintPreview == null)
        {
            return;
        }

        var ps = vm.PrintPreview.PrintSettings.Value;
        var config = vm.PrintPreview.PrintWindowConfig;
        if (ps != null && config is { WindowProperties: not null })
        {
            var props = config.WindowProperties;
            props.PrinterName = ps.PrinterName.Value;
            props.PaperSize = ps.PaperSize.Value;
            props.Orientation = ps.Orientation.Value;
            props.ScaleMode = ps.ScaleMode.Value;
            props.ColorMode = ps.ColorMode.Value;
            props.Copies = ps.Copies.Value;
            props.MarginTop = ps.MarginTop.Value;
            props.MarginBottom = ps.MarginBottom.Value;
            props.MarginLeft = ps.MarginLeft.Value;
            props.MarginRight = ps.MarginRight.Value;

            props.Width = Bounds.Width;
            props.Height = Bounds.Height;
            
            props.Left = Position.X;
            props.Top = Position.Y;

            _ = config.SaveAsync();
        }

        vm.PrintPreview.Dispose();
        vm.PrintPreview = null;
    }

    protected void SetWindowSize()
    {
        if (Config.WindowProperties.Maximized)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            Width = Config.WindowProperties.Width ?? Width;
            Height = Config.WindowProperties.Height ?? Height;
            var left = Config.WindowProperties.Left;
            var top = Config.WindowProperties.Top;
            if (left.HasValue && top.HasValue)
            {
                Position = new PixelPoint(left.Value, top.Value);
            }
        }
    }
}