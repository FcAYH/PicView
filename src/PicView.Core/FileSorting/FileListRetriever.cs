using PicView.Core.DebugTools;
using PicView.Core.FileHandling;
using ZLinq;

namespace PicView.Core.FileSorting;

public static class FileListRetriever
{
    public static List<FileInfo> RetrieveFiles(FileInfo fileInfo, Func<string, string, int> platformService)
    {
        var directoryPath = fileInfo switch
        {
            null => null,
            { Attributes: var attr } when attr.HasFlag(FileAttributes.Directory) => fileInfo.FullName,
            _ => fileInfo.DirectoryName
        };

        if (string.IsNullOrEmpty(directoryPath))
        {
            return [];
        }

        try
        {
            if (Settings.Sorting.IncludeSubDirectories)
            {
                var recurseList = new DirectoryInfo(directoryPath)
                    .DescendantsAndSelf()
                    .OfType<FileInfo>()
                    .Where(x => x.IsSupported());
                return FileSortOrder.SortIEnumerable(recurseList, platformService);
            }
            var list = new DirectoryInfo(directoryPath)
                .ChildrenAndSelf()
                .OfType<FileInfo>()
                .Where(x => x.IsSupported());
            return FileSortOrder.SortIEnumerable(list, platformService);
        }
        catch (Exception exception)
        {
            DebugHelper.LogDebug(nameof(FileListRetriever), nameof(RetrieveFiles), exception);
            return [];
        }
    }
}