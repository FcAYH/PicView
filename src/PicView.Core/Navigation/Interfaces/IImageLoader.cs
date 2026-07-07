using PicView.Core.Models;

namespace PicView.Core.Navigation.Interfaces;

public interface IImageLoader
{
    ValueTask<ImageModel> GetImageModelAsync(FileInfo file, CancellationToken ct);
}