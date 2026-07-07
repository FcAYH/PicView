namespace PicView.Core.Navigation.Tiff;

public interface ITiffService
{
    bool IsTiff(FileInfo f);
    IReadOnlyList<TiffNavigationInfo> LoadPages(FileInfo f);
}