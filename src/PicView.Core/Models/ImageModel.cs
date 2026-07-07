using PicView.Core.ImageDecoding;
using PicView.Core.Navigation.Tiff;

namespace PicView.Core.Models;

public record ImageModel
{
    public object? Image { get; set; }
    public FileInfo? FileInfo { get; set; }
    public uint PixelWidth { get; set; }
    public uint PixelHeight { get; set; }
    public ImageType ImageType { get; set; }
    public TiffNavigationInfo? TiffNavigation { get; set; }
}