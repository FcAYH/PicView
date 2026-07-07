using ImageMagick;

namespace PicView.Core.Exif;

public static class ExifOrientationHelper
{
    public static ExifOrientation GetImageOrientation(MagickImage magickImage)
    {
        if (magickImage.Orientation is not OrientationType.Undefined)
        {
            return magickImage.Orientation switch
            {
                OrientationType.BottomLeft => ExifOrientation.MirrorVertical,
                OrientationType.BottomRight => ExifOrientation.Rotate180,
                OrientationType.TopLeft => ExifOrientation.Horizontal,
                OrientationType.TopRight => ExifOrientation.MirrorHorizontal,
                OrientationType.RightBottom => ExifOrientation.MirrorHorizontalRotate90Cw,
                OrientationType.RightTop => ExifOrientation.Rotate90Cw,
                OrientationType.LeftBottom => ExifOrientation.Rotated270Cw,
                OrientationType.LeftTop => ExifOrientation.MirrorHorizontalRotate270Cw,
                _ => ExifOrientation.None
            };
        }

        var profile = magickImage.GetExifProfile();
        // ReSharper disable once UseNullPropagation
        if (profile is null)
        {
            return ExifOrientation.None;
        }
        var orientationValue = profile.GetValue(ExifTag.Orientation);
        if (orientationValue is null)
        {
            return ExifOrientation.None;
        }

        return orientationValue.Value switch
        {
            0 => ExifOrientation.None,
            1 => ExifOrientation.Horizontal,
            2 => ExifOrientation.MirrorHorizontal,
            3 => ExifOrientation.Rotate180,
            4 => ExifOrientation.MirrorVertical,
            5 => ExifOrientation.MirrorHorizontalRotate270Cw,
            6 => ExifOrientation.Rotate90Cw,
            7 => ExifOrientation.MirrorHorizontalRotate90Cw,
            8 => ExifOrientation.Rotated270Cw,
            _ => ExifOrientation.None
        };
    }

    public static ExifOrientation GetImageOrientation(string filePath)
    {
        using var magickImage = new MagickImage();
        magickImage.Ping(filePath);
        return GetImageOrientation(magickImage);
    }

    public static ExifOrientation GetImageOrientation(FileInfo fileInfo) =>
        GetImageOrientation(fileInfo.FullName);
}