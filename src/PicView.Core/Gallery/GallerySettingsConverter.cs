using PicView.Core.ViewModels;

namespace PicView.Core.Gallery;

public static class GallerySettingsConverter
{
    public static void UpdateDockPositionProperties(GalleryViewModel gallery)
    {
        var pos = Settings.Gallery.DockPosition;
        gallery.IsTopDocked.Value = pos == GalleryDockPosition.Top;
        gallery.IsBottomDocked.Value = pos == GalleryDockPosition.Bottom;
        gallery.IsLeftDocked.Value = pos == GalleryDockPosition.Left;
        gallery.IsRightDocked.Value = pos == GalleryDockPosition.Right;
    }

    public static void UpdateDockedGalleryStretchMode(GallerySharedSettingsViewModel gallerySettings, GalleryStretchMode mode)
    {
        gallerySettings.IsDockedStretchUniform.Value = false;
        gallerySettings.IsDockedStretchUniformToFill.Value = false;
        gallerySettings.IsDockedStretchToFill.Value = false;
        gallerySettings.IsDockedStretchNone.Value = false;
        gallerySettings.IsDockedStretchSquare.Value = false;
        gallerySettings.IsDockedStretchSquareFill.Value = false;
        switch (mode)
        {
            case GalleryStretchMode.Uniform:
                gallerySettings.IsDockedStretchUniform.Value = true;
                break;
            case GalleryStretchMode.UniformToFill:
                gallerySettings.IsDockedStretchUniformToFill.Value = true;
                break;
            case GalleryStretchMode.Fill:
                gallerySettings.IsDockedStretchToFill.Value = true;
                break;
            case GalleryStretchMode.None:
                gallerySettings.IsDockedStretchNone.Value = true;
                break;
            case GalleryStretchMode.Square:
                gallerySettings.IsDockedStretchSquare.Value = true;
                break;
            case GalleryStretchMode.FillSquare:
                gallerySettings.IsDockedStretchSquareFill.Value = true;
                break;
        }

        gallerySettings.DockedGalleryStretchMode.Value = (int)mode;
    }

    public static void UpdateExpandedGalleryStretchMode(GallerySharedSettingsViewModel gallerySettings, GalleryStretchMode mode)
    {
        gallerySettings.IsExpandedStretchUniform.Value = false;
        gallerySettings.IsExpandedStretchUniformToFill.Value = false;
        gallerySettings.IsExpandedStretchToFill.Value = false;
        gallerySettings.IsExpandedStretchNone.Value = false;
        gallerySettings.IsExpandedStretchSquare.Value = false;
        gallerySettings.IsExpandedStretchSquareFill.Value = false;
        switch (mode)
        {
            case GalleryStretchMode.Uniform:
                gallerySettings.IsExpandedStretchUniform.Value = true;
                break;
            case GalleryStretchMode.UniformToFill:
                gallerySettings.IsExpandedStretchUniformToFill.Value = false;
                break;
            case GalleryStretchMode.Fill:
                gallerySettings.IsExpandedStretchToFill.Value = false;
                break;
            case GalleryStretchMode.None:
                gallerySettings.IsExpandedStretchNone.Value = false;
                break;
            case GalleryStretchMode.Square:
                gallerySettings.IsExpandedStretchSquare.Value = false;
                break;
            case GalleryStretchMode.FillSquare:
                gallerySettings.IsExpandedStretchSquareFill.Value = false;
                break;
        }
        gallerySettings.ExpandedGalleryStretchMode.Value = (int)mode;
    }
}