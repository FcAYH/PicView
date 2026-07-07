using PicView.Core.Models;

namespace PicView.Core.Preloading;

/// <summary>
/// Defines the contract for the background worker.
/// <para>
/// Implementations of this interface are responsible for calculating predictive indices 
/// (look-ahead) and executing the physical loading of images into the cache.
/// </para>
/// </summary>
public interface IPreloader
{
    /// <summary>
    /// Adds a fully loaded item directly to the cache.
    /// </summary>
    void Add(uint ownerId, int index, ImageModel model, IReadOnlyList<FileInfo> list);

    /// <summary>
    /// Asynchronously loads an item and adds it to the cache.
    /// </summary>
    ValueTask<ImageModel?> AddAsync(uint ownerId, int index, IReadOnlyList<FileInfo> list, bool isReverse = false,
        CancellationToken ct = default);
    
    void Resynchronize(uint ownerId, IReadOnlyList<FileInfo> files);
    
    /// <summary>
    /// Starts the predictive loop to load images ahead of the current view.
    /// </summary>
    void Preload(uint ownerId, int currentIndex, bool reversed, IReadOnlyList<FileInfo> files, CancellationToken cancellationToken);
}