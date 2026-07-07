using ImageMagick;

namespace PicView.Core.BatchResize;

public struct BatchThumb(
    string saveDestination,
    Percentage? percentage = null,
    double? width = null,
    double? height = null)
{
    public string SaveDestination = saveDestination;
    public Percentage? Percentage = percentage;
    public double? Width = width;
    public double? Height = height;
}