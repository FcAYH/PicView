namespace PicView.Core.Navigation.Tiff;

public class TiffNavigationInfo
{
    public int PageCount { get; init; }
    public int CurrentPage { get; set; }

    public object[]? Pages { get; set; }
}