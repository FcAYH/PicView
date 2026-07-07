namespace PicView.Core.FileHandling.Interfaces;

public interface ITempFileService
{
    /// <summary>
    /// Creates a temporary file path with the given file name.
    /// The service tracks this file for cleanup.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The full path to the temporary file.</returns>
    string GetNewTempFilePath(string fileName);

    /// <summary>
    /// Cleans up all temporary files tracked by this service.
    /// </summary>
    void Cleanup();
}