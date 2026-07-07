using PicView.Core.DebugTools;
using PicView.Core.Extensions;
using PicView.Core.FileHandling;

namespace PicView.Core.FileSorting;

public class FileAndDirectoryNavigator(Func<string, string, int> stringComparer)
{
    public string? FindNextArchive(string currentDir, bool next, string currentFilePath)
    {
        // Look for next archive in the current directory after the current file
        var archives = FileSortOrder.GetSortedArchivesInDirectory(currentDir, stringComparer);
        var idx = archives.FindIndex(a => a.FullName.Equals(currentFilePath, StringComparison.OrdinalIgnoreCase));
        if (!next)
        {
            return idx switch
            {
                > 0 => archives[idx - 1].FullName,
                < 0 when archives.Length > 0 => archives[^1].FullName,
                _ => GetLastArchiveInPreviousSiblingOrAncestor(currentDir)
            };
        }
        switch (idx)
        {
            case >= 0 when idx + 1 < archives.Length:
                return archives[idx + 1].FullName;
            case < 0 when archives.Length > 0:
                return archives[0].FullName;
        }
    
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            return GetFirstArchiveInNextSiblingOrAncestor(currentDir);
        }
    
        var firstInChild = GetFirstArchiveInDescendants(currentDir);
        return firstInChild ?? GetFirstArchiveInNextSiblingOrAncestor(currentDir);
    }

    public string? GetFirstArchiveInDescendants(string path)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs);

            foreach (var sub in subDirs)
            {
                var archives = FileSortOrder.GetSortedArchivesInDirectory(sub.FullName, stringComparer);
                if (archives.Length > 0)
                {
                    return archives[0].FullName;
                }

                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstArchiveInDescendants(sub.FullName);
                if (child != null)
                {
                    return child;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetFirstArchiveInDescendants), ex);
        }
        return null;
    }

    public string? GetFirstArchiveInNextSiblingOrAncestor(string path)
    {
        var dir = new DirectoryInfo(path);
        var parent = dir.Parent;
        if (parent == null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings);
            var index = siblings.FindIndex(d => Equals(path, stringComparer));

            for (var i = index + 1; i < siblings.Length; i++)
            {
                var sibling = siblings[i];
                var archives = FileSortOrder.GetSortedArchivesInDirectory(sibling.FullName, stringComparer);
                if (archives.Length > 0)
                {
                    return archives[0].FullName;
                }

                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstArchiveInDescendants(sibling.FullName);
                if (child != null)
                {
                    return child;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetFirstArchiveInNextSiblingOrAncestor), ex);
        }

        return GetFirstArchiveInNextSiblingOrAncestor(parent.FullName);
    }

    public string? GetLastArchiveInPreviousSiblingOrAncestor(string currentPath)
    {
        var dir = new DirectoryInfo(currentPath);
        var parent = dir.Parent;
        if (parent is null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings);
            var index = siblings.FindIndex(d => d.FullName.Equals(currentPath, StringComparison.OrdinalIgnoreCase));

            if (index <= 0)
            {
                var parentArchives = FileSortOrder.GetSortedArchivesInDirectory(parent.FullName, stringComparer);
                if (parentArchives.Length > 0)
                {
                    return parentArchives[^1].FullName;
                }
                return GetLastArchiveInPreviousSiblingOrAncestor(parent.FullName);
            }

            for (var i = index - 1; i >= 0; i--)
            {
                var sibling = siblings[i];
                var lastChild = GetLastArchiveInDescendantOrSelf(sibling.FullName);
                if (lastChild != null)
                {
                    return lastChild;
                }
            }

            var parentArchivesFallback = FileSortOrder.GetSortedArchivesInDirectory(parent.FullName, stringComparer);
            return parentArchivesFallback.Length > 0 ?
                parentArchivesFallback[^1].FullName : GetLastArchiveInPreviousSiblingOrAncestor(parent.FullName);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetLastArchiveInPreviousSiblingOrAncestor), ex);
            return null;
        }
    }

    public string? GetLastArchiveInDescendantOrSelf(string path)
    {
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            var archives = FileSortOrder.GetSortedArchivesInDirectory(path, stringComparer);
            return archives.Length > 0 ? archives[^1].FullName : null;
        }

        try
        {
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs);

            for (var i = subDirs.Length - 1; i >= 0; i--)
            {
                var lastChild = GetLastArchiveInDescendantOrSelf(subDirs[i].FullName);
                if (lastChild != null) return lastChild;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetLastArchiveInDescendantOrSelf), ex);
        }

        var archivesHere = FileSortOrder.GetSortedArchivesInDirectory(path, stringComparer);
        return archivesHere.Length > 0 ? archivesHere[^1].FullName : null;
    }

    public string? FindNextValidDirectory(string currentPath)
    {
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            return GetNextSiblingOrAncestorSibling(currentPath);
        }

        var firstChild = GetFirstValidChild(currentPath);
        return firstChild ?? GetNextSiblingOrAncestorSibling(currentPath);
    }

    public string? GetFirstValidChild(string path)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs);

            foreach (var sub in subDirs)
            {
                if (IsDirectoryValid(sub.FullName)) return sub.FullName;
                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstValidChild(sub.FullName);
                if (child != null)
                {
                    return child;
                }
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetFirstValidChild), ex);
        }
        return null;
    }

    public string? GetNextSiblingOrAncestorSibling(string path)
    {
        var dir = new DirectoryInfo(path);
        var parent = dir.Parent;
        if (parent == null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings);
            var index = siblings.FindIndex(d => d.FullName.Equals(path, StringComparison.OrdinalIgnoreCase));

            for (var i = index + 1; i < siblings.Length; i++)
            {
                var sibling = siblings[i];
                if (IsDirectoryValid(sibling.FullName)) return sibling.FullName;
                if (!Settings.Sorting.IncludeSubDirectories)
                {
                    continue;
                }

                var child = GetFirstValidChild(sibling.FullName);
                if (child != null) return child;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetNextSiblingOrAncestorSibling), ex);
        }

        return GetNextSiblingOrAncestorSibling(parent.FullName);
    }

    public string? FindPreviousValidDirectory(string currentPath)
    {
        var dir = new DirectoryInfo(currentPath);
        var parent = dir.Parent;
        if (parent is null)
        {
            return null;
        }

        try
        {
            var siblings = parent.GetDirectories();
            SortDirectories(siblings);
            var index = siblings.FindIndex(d => d.FullName.Equals(currentPath, StringComparison.OrdinalIgnoreCase));

            if (index <= 0)
            {
                return IsDirectoryValid(parent.FullName)
                    ? parent.FullName
                    : FindPreviousValidDirectory(parent.FullName);
            }

            for (var i = index - 1; i >= 0; i--)
            {
                var sibling = siblings[i];
                var lastChild = GetLastValidDescendantOrSelf(sibling.FullName);
                if (lastChild != null)
                {
                    return lastChild;
                }
            }

            return IsDirectoryValid(parent.FullName) ?
                parent.FullName : FindPreviousValidDirectory(parent.FullName);
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(FindPreviousValidDirectory), ex);
            return null;
        }
    }

    public string? GetLastValidDescendantOrSelf(string path)
    {
        if (!Settings.Sorting.IncludeSubDirectories)
        {
            return IsDirectoryValid(path) ? path : null;
        }

        try
        {   
            var dir = new DirectoryInfo(path);
            var subDirs = dir.GetDirectories();
            SortDirectories(subDirs);

            for (var i = subDirs.Length - 1; i >= 0; i--)
            {
                var lastChild = GetLastValidDescendantOrSelf(subDirs[i].FullName);
                if (lastChild != null) return lastChild;
            }
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(GetLastValidDescendantOrSelf), ex);
        }

        return IsDirectoryValid(path) ? path : null;
    }

    public bool IsDirectoryValid(string path)
    {
        try
        {
            var dir = new DirectoryInfo(path);
            if (!dir.Exists)
            {
                return false;
            }
            
            return Settings.Sorting.IncludeSubDirectories ?
                dir.EnumerateFiles("*", SearchOption.AllDirectories).Any(f => f.FullName.IsSupported()) :
                dir.EnumerateFiles("*", SearchOption.TopDirectoryOnly).Any(f => f.FullName.IsSupported());
        }
        catch (Exception ex)
        {
            DebugHelper.LogDebug(nameof(FileAndDirectoryNavigator), nameof(IsDirectoryValid), ex);
            return false;
        }
    }

    public void SortDirectories(DirectoryInfo[] dirs)
    {
        if (!Settings.Sorting.Ascending)
        {
            dirs.Sort((x, y) => stringComparer(y.Name, x.Name));
        }
        else
        {
            dirs.Sort((x, y) => stringComparer(x.Name, y.Name));
        }
    }
}