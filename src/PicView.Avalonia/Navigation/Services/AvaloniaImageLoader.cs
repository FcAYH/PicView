using PicView.Avalonia.ImageHandling;
using PicView.Core.Models;
using PicView.Core.Navigation.Interfaces;

namespace PicView.Avalonia.Navigation.Services;

public class AvaloniaImageLoader : IImageLoader
{
    public async ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct) => 
        await GetImageModel.GetImageModelAsync(file).ConfigureAwait(false);
}