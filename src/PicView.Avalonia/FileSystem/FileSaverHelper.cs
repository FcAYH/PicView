using Avalonia;
using Avalonia.Media.Imaging;
using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using PicView.Core.ImageDecoding;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.FileSystem;

public static class FileSaverHelper
{
    public static async ValueTask<bool> SaveCurrentFile(MainWindowViewModel vm)
    {
        bool isSaved;
        if (vm.WindowTabs.ActiveTab.CurrentValue.FileInfo is null)
        {
            isSaved = await SaveFileAs(vm).ConfigureAwait(false);
        }
        else
        {
            isSaved = await SaveFileAsync(vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.FullName,
                vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.FullName, vm).ConfigureAwait(false);
        }
        
        if (isSaved)
        {
            // TODO: Add visual design to tell whether file was saved
        }
        
        return isSaved;
    }

    public static async ValueTask<bool> SaveFileAs(MainWindowViewModel vm)
    {
        // Suggest random filename for saving, if it is not an existing file
        var fileName = vm.WindowTabs.ActiveTab.CurrentValue?.FileInfo?.CurrentValue is null
            ? Path.GetRandomFileName()
            : vm.WindowTabs.ActiveTab.CurrentValue.FileInfo.CurrentValue.Name;
        
        var isSaved = await FilePicker.PickAndSaveFileAsAsync(fileName, vm);
        if (isSaved)
        {
            // TODO: Add visual design to tell whether file was saved
        }

        return isSaved;
    }

    public static async ValueTask<bool> SaveFileAsync(string? filename, string destination, MainWindowViewModel vm)
    {
        if (Application.Current.DataContext is not CoreViewModel core)
        {
            return false;
        }
        if (core.Effects?.ProcessedImage is not null)
        {
            return await SaveImageFromBitmap();
        }
        
        if (!string.IsNullOrWhiteSpace(filename) && File.Exists(filename))
        {
            return await SaveImageFromFile();
        }
        
        return await SaveImageFromBitmap();
        
        async ValueTask<bool> SaveImageFromFile()
        {
            return await SaveImageFileHelper.SaveImageAsync(null,
                filename,
                destination,
                null,
                null,
                null,
                Path.GetExtension(destination),
                vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue,
                null,
                false,
                false,
                true,
                vm.WindowTabs.ActiveTab.CurrentValue.ScaleX.Value == -1);
        }
        
        async ValueTask<bool> SaveImageFromBitmap()
        {
            try
            {
                switch (vm.WindowTabs.ActiveTab.CurrentValue.ImageType.CurrentValue)
                {
                    case ImageType.AnimatedGif: // TODO: Add animated GIF support
                    case ImageType.AnimatedWebp: // TODO: Add animated WebP support
                    case ImageType.Bitmap:
                    {
                        if (vm.WindowTabs.ActiveTab.CurrentValue.Image.CurrentValue is not Bitmap bitmap)
                        {
                            throw new InvalidOperationException("No bitmap available for saving.");
                        }
        
                        const uint quality = 100; // TODO: Add quality slider to user settings
                        var stream = new FileStream(destination, FileMode.Create);
                        bitmap.Save(stream, (int)quality);
                        await stream.DisposeAsync().ConfigureAwait(false);
                        var ext = Path.GetExtension(destination);
                        // Add rotation, apply image conversion
                        if (ext.IsSupported())
                        {
                            await SaveImageFileHelper.SaveImageAsync(
                                null,
                                destination,
                                destination,
                                null,
                                null,
                                quality,
                                ext,
                                vm.WindowTabs.ActiveTab.CurrentValue.RotationAngle.CurrentValue);
                        }
        
                        break;
                    }
                    case ImageType.Svg:
                        // TODO convert svg to bitmap and save
                        throw new InvalidOperationException("No bitmap available for saving.");
                    default:
                        throw new InvalidOperationException("No bitmap available for saving.");
                }
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(FileSaverHelper), nameof(SaveFileAsync), e);
                return false;
            }
        
            return true;
        }
    }
}