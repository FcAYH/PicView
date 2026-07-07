using PicView.Core.DebugTools;

namespace PicView.Core.FileHandling;

public static class TempFileManager
{
    private static List<string>? _tempFiles;
    private static Lock? _lock;

    public static string GetNewTempFilePath(string fileName)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempPath);
        var filePath = Path.Combine(tempPath, fileName);
        _lock ??= new Lock();
        lock (_lock)
        {
            _tempFiles ??= [];
            _tempFiles.Add(filePath);
        }

        return filePath;
    }

    public static void Cleanup()
    {
        if (_lock is null || _tempFiles is null)
        {
            return;
        }
        
        lock (_lock)
        {
            foreach (var file in _tempFiles)
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                    
                    var dir = Path.GetDirectoryName(file);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        Directory.Delete(dir, true);
                    }
                }
                catch (Exception e)
                {
                    DebugHelper.LogDebug(nameof(TempFileManager), nameof(Cleanup), e);
                }
            }
            _tempFiles.Clear();
        }
    }
}