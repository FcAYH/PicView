using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.Linux.Views;
using PicView.Avalonia.ViewModels;
using PicView.Avalonia.WindowBehavior;

namespace PicView.Avalonia.Linux.WindowImpl;

public static class LinuxWindow
{
    public static bool IsChangingWindowState { get; private set; }
    
    public static async Task ToggleFullscreen(LinuxMainWindow? window, MainViewModel? vm, bool saveSettings)
    {
        if (Settings.WindowProperties.Fullscreen)
        {
            await Restore(window, vm, saveSettings);
        }
        else
        {
            await Fullscreen(window, vm, saveSettings);
        }
    }
    
    public static async Task ToggleMaximize(LinuxMainWindow? window, MainViewModel? vm, bool saveSettings = true)
    {
        if (window.WindowState == WindowState.Maximized || Settings.WindowProperties.Maximized)
        {
            await Restore(window, vm, saveSettings); 
        }
        else
        {
            await Maximize(window, vm, saveSettings);
        }
    }

    public static async Task Restore(LinuxMainWindow? window, MainViewModel? vm, bool saveSettings = true)
    {
        IsChangingWindowState = true;
        
        // Update settings
        Settings.WindowProperties.Maximized = false;
        Settings.WindowProperties.Fullscreen = false;

        // Update UI state
        vm.MainWindow.IsMaximized.Value = false;
        vm.MainWindow.IsFullscreen.Value = false;

        WindowFunctions.RestoreInterface(vm);

        // Update window state
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            window.WindowState = WindowState.Normal;
            
            if (Settings.WindowProperties.AutoFit)
            {
                vm.MainWindow.SizeToContent.Value = SizeToContent.WidthAndHeight;
                vm.MainWindow.CanResize.Value = false;
                vm.GlobalSettings.IsAutoFit.Value = true;
                window.SizeToContent = SizeToContent.WidthAndHeight; // Fixes sizeToContent not being applied
            }
            else
            {
                vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
                vm.MainWindow.CanResize.Value = true;
                vm.GlobalSettings.IsAutoFit.Value = false;
            }
        });
        
        await WindowResizing.SetSizeAsync(vm);
        
        if (Settings.WindowProperties.KeepCentered)
        {
            WindowFunctions.CenterWindowOnScreen();
        }
        else
        {
            WindowFunctions.InitializeWindowSizeAndPosition(window);
        }
        
        Dispatcher.UIThread.Post(() => IsChangingWindowState = false, DispatcherPriority.SystemIdle);

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task Fullscreen(LinuxMainWindow? window, MainViewModel? vm, bool saveSettings = true)
    {
        // Need to set changing state to true, to prevent image resize subscription from firing
        IsChangingWindowState = true;
        
        // Save window size, so that restoring it will return to the same size and position
        WindowResizing.SaveSize(window);
        
        Settings.WindowProperties.Maximized = false;
        Settings.WindowProperties.Fullscreen = true;
        
        vm.MainWindow.IsTopToolbarShown.Value = false;
        vm.MainWindow.IsBottomToolbarShown.Value = false;
        
        vm.MainWindow.IsFullscreen.Value = true;
        vm.MainWindow.IsMaximized.Value = false;
        vm.MainWindow.CanResize.Value = true;
        
        window.WindowState = WindowState.FullScreen;
        
        await WindowResizing.SetSizeAsync(vm);
        
        // Reset changing state flag so subscription can fire again. Need to be delayed by dispatcher to not be misfired. 
        Dispatcher.UIThread.Post(() => IsChangingWindowState = false, DispatcherPriority.SystemIdle);
        
        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static async Task Maximize(LinuxMainWindow? window, MainViewModel? vm, bool saveSettings = true)
    {
        IsChangingWindowState = true;
        
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // Save window size, so that restoring it will return to the same size and position
            WindowResizing.SaveSize(window);
            
            if (Settings.WindowProperties.AutoFit || window.SizeToContent == SizeToContent.WidthAndHeight)
            {
                vm.MainWindow.SizeToContent.Value = SizeToContent.Manual;
            }

            window.WindowState = WindowState.Maximized;
            Settings.WindowProperties.Maximized = true;
            WindowResizing.SetSize(vm);
            WindowFunctions.CenterWindowOnScreen();
        });

        vm.MainWindow.IsMaximized.Value = true;
        vm.MainWindow.IsFullscreen.Value = false;
        vm.MainWindow.CanResize.Value = false;
        
        Dispatcher.UIThread.Post(() => IsChangingWindowState = false, DispatcherPriority.SystemIdle);

        if (saveSettings)
        {
            await SaveSettingsAsync().ConfigureAwait(false);
        }
    }

    public static void Minimize(LinuxMainWindow linuxMainWindow)
    {
        linuxMainWindow.WindowState = WindowState.Minimized;
    }
}