using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using PicView.Avalonia.Animations;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.StartUp;
using PicView.Avalonia.UI;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Clipboard;

/// <summary>
/// Handles clipboard operations related to files (MVVM refactor)
/// </summary>
public static class ClipboardFileOperations
{
    /// <summary>
    /// Duplicates the specified file, either the current file or another one specified by path.
    /// If the current file is being duplicated, the view model will navigate to the duplicated file.
    /// </summary>
    /// <param name="path">Path to the file to duplicate, or null to duplicate the current file.</param>
    /// <param name="vm">The main window view model</param>
    public static async Task Duplicate(string? path, MainWindowViewModel vm, MainWindow mainWindow)
    {
        var currentFile = vm.WindowTabs.ActiveTab.CurrentValue.Model.FileInfo?.FullName;
        
        // If path is null/empty, we assume we want to duplicate the current file
        var targetPath = string.IsNullOrWhiteSpace(path) ? currentFile : path;

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }
        
        try
        {
            vm.IsLoadingIndicatorShown.Value = true;
            
            // If we are duplicating the currently viewing file, we want to perform navigation to the new file
            if (targetPath == currentFile)
            {
                await DuplicateCurrentFile(vm, mainWindow);
            }
            else
            {
                await DuplicateFile(targetPath, mainWindow);
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardFileOperations), nameof(Duplicate), ex);
            TooltipHelper.ShowTooltipMessage(TranslationManager.Translation?.UnexpectedError);
        }
        finally
        {
            vm.IsLoadingIndicatorShown.Value = false;
        }
    }

    /// <summary>
    /// Duplicates the current file and navigates to it
    /// </summary>
    private static async Task DuplicateCurrentFile(MainWindowViewModel vm, MainWindow mainWindow)
    {
        var activeTab = vm.WindowTabs.ActiveTab.CurrentValue;
        
        if (activeTab.ImageIterator is null || vm.WindowTabs.SharedNavigation is null)
        {
            return;
        }

        try
        {
            var currentPath = activeTab.Model.FileInfo?.FullName;
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                return;
            }

            var duplicatedPath =
                await FileHelper.DuplicateAndReturnFileNameAsync(currentPath);

            if (string.IsNullOrWhiteSpace(duplicatedPath) || !File.Exists(duplicatedPath))
            {
                return;
            }
            
            _ = AnimationsHelper.CopyAnimation(mainWindow);
            await vm.WindowTabs.SharedNavigation.LoadFromFileAsync(duplicatedPath, activeTab, activeTab.GetTabCancellation());
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardFileOperations), nameof(DuplicateCurrentFile), ex);
        }
    }
    
    /// <summary>
    /// Duplicates the specified file and plays a copy animation when done. The original file is not navigated away from.
    /// </summary>
    private static async Task DuplicateFile(string path, MainWindow mainWindow)
    {
        var duplicatedPath = await FileHelper.DuplicateAndReturnFileNameAsync(path);
        if (!string.IsNullOrWhiteSpace(duplicatedPath))
        {
            await AnimationsHelper.CopyAnimation(mainWindow);
        }
    }

    /// <summary>
    /// Copies a file to the clipboard
    /// </summary>
    public static async Task CopyFileToClipboard(string? filePath, MainWindow mainWindow)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        var clipboard = ClipboardService.GetClipboard(mainWindow);
        if (clipboard == null)
        {
            return;
        }
        
        var animTask = AnimationsHelper.CopyAnimation(mainWindow);
        var storageFile = await mainWindow.StorageProvider.TryGetFileFromPathAsync(Path.GetFullPath(filePath));
        
        if (storageFile != null)
        {
             var fileTask = clipboard.SetFileAsync(storageFile);
             await Task.WhenAll(animTask, fileTask);
        }
    }

    /// <summary>
    /// Cuts a file to the clipboard (copy + mark for deletion on paste)
    /// </summary>
    /// <param name="filePath">Path to the file</param>
    /// <returns>A task representing the asynchronous operation</returns>
    public static Task<bool> CutFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Task.FromResult(false);
        }

        // TODO implement cut
        return Task.FromResult(false);
    }

    public static async ValueTask PasteFiles(object files, MainWindowViewModel vm, MainWindow mainWindow)
    {
        try
        {
            switch (files)
            {
                case IEnumerable<IStorageItem> items:
                    await ProcessStorageItems(items.ToArray(), vm, mainWindow);
                    break;
                case IStorageItem singleFile:
                {
                    var path = singleFile.Path.LocalPath;
                    if (path.IsArchive())
                    {
                        await vm.WindowTabs.LoadFromArchiveAsync(path);
                    }
                    else
                    {
                        await vm.WindowTabs.LoadFromFileAsync(path).ConfigureAwait(false);
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(ClipboardFileOperations), nameof(PasteFiles), ex);
        }
    }
    
    private static async ValueTask ProcessStorageItems(IStorageItem[] storageItems, MainWindowViewModel vm, MainWindow mainWindow)
    {
        if (storageItems.Length == 0)
        {
            return;
        }

        // Load the first file
        var firstItem = storageItems[0].Path.LocalPath;
        if (firstItem.IsArchive())
        {
            await vm.WindowTabs.LoadFromArchiveAsync(firstItem).ConfigureAwait(false);
        }
        else
        {
            await vm.WindowTabs.LoadFromFileAsync(firstItem).ConfigureAwait(false);
        }

        if (vm.WindowTabs.ActiveTab.CurrentValue.Gallery.IsGalleryDocked.CurrentValue)
        {
            // TODO: Consecutive tabs or windows currently not supported when gallery is enabled
            return;
        }
        
        // Open consecutive files in a new tab
        foreach (var file in storageItems.Skip(1))
        {
            var path = file.Path.LocalPath;
            var fileInfo = new FileInfo(path);
            await vm.WindowTabs.CreateNewTabFromFileAsync(fileInfo);
            if (Application.Current.DataContext is CoreViewModel core)
            {
                TabNavigationInitializer.Initialize(core, fileInfo, mainWindow);
            }
            file.Dispose();
        }
    }
}
