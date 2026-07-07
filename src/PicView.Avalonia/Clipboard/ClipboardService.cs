using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;

namespace PicView.Avalonia.Clipboard;

/// <summary>
/// Base service for clipboard operations
/// </summary>
public static class ClipboardService
{
    /// <summary>
    /// Gets the clipboard instance from the provided visual or current application
    /// </summary>
    /// <param name="visual">The visual to find the TopLevel/Clipboard from</param>
    /// <returns>The clipboard instance or null if not available</returns>
    public static IClipboard? GetClipboard(Visual? visual = null)
    {
        if (visual is not null)
        {
            var topLevel = TopLevel.GetTopLevel(visual);
            if (topLevel?.Clipboard is { } clipboard)
            {
                return clipboard;
            }
        }
        
        // Fallback for when we don't have a visual, but maybe we can find one via Application (legacy style)
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
             // This should be the correct window, as the desktop.MainWindow is set on Activated()
            return desktop.MainWindow?.Clipboard;
        }

        return null;
    }
}
