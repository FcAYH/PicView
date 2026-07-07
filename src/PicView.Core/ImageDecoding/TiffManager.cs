using ImageMagick;

namespace PicView.Core.ImageDecoding;

public static class TiffManager
{
    public static bool IsTiff(string path)
    {
        return path.EndsWith(".tif", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase);
    }
    
    public static bool IsTiff(FileInfo fileInfo)
    {
        return fileInfo.Extension.Equals(".tif", StringComparison.OrdinalIgnoreCase) ||
               fileInfo.Extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase);
    }

    public static MagickImageCollection? LoadTiffPages(string path)
    {
        var settings = new MagickReadSettings
        {
            // Specify that we want to read a TIFF format image
            Format = MagickFormat.Tiff
        };

        var pageCollection = new MagickImageCollection(path, settings);
        foreach (var page in pageCollection)
        {
            page.Quality = 100;
        }

        return pageCollection;
    }
    
    public static int? GetTiffPageCount(string path)
    {
        using var image = new MagickImage();
        image.Ping(path);
        var settings = new MagickReadSettings
        {
            // Specify that we want to read a TIFF format image
            Format = MagickFormat.Tiff
        };
        return new MagickImageCollection(path, settings).Count;
    }

    public class TiffNavigationInfo : IDisposable
    {
        public int PageCount { get; set; }
        public int CurrentPage { get; set; }

        public MagickImageCollection? Pages { get; set; }

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (Pages == null)
            {
                return;
            }

            Pages.Dispose();
            Pages = null;
        }

        #endregion
    }
}