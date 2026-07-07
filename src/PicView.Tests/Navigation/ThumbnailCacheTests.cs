using PicView.Core.Navigation;
using Xunit;

namespace PicView.Tests.Navigation;

public class ThumbnailCacheTests
{
    private readonly ThumbnailCache _cache;

    public ThumbnailCacheTests()
    {
        _cache = new ThumbnailCache();
    }

    [Fact]
    public void Add_And_Get_Works()
    {
        var owner = "owner1";
        var path = "path/to/file.jpg";
        var thumb = new object();

        _cache.Add(owner, path, thumb);

        Assert.True(_cache.TryGet(path, out var retrieved));
        Assert.Same(thumb, retrieved);
    }

    [Fact]
    public void RemoveOwner_RemovesFile_WhenNoOwnersLeft()
    {
        var owner1 = "owner1";
        var path = "file.jpg";
        var thumb = new object();

        _cache.Add(owner1, path, thumb);
        _cache.RemoveOwner(owner1);

        Assert.False(_cache.TryGet(path, out _));
    }

    [Fact]
    public void RemoveOwner_DoesNotRemoveFile_WhenOtherOwnerExists()
    {
        var owner1 = "owner1";
        var owner2 = "owner2";
        var path = "file.jpg";
        var thumb = new object();

        _cache.Add(owner1, path, thumb);
        _cache.Add(owner2, path, thumb);

        _cache.RemoveOwner(owner1);

        Assert.True(_cache.TryGet(path, out var retrieved));
        Assert.Same(thumb, retrieved);

        _cache.RemoveOwner(owner2);
        Assert.False(_cache.TryGet(path, out _));
    }

    [Fact]
    public void Remove_RemovesFile_RegardlessOfOwners()
    {
        var owner1 = "owner1";
        var path = "file.jpg";
        var thumb = new object();

        _cache.Add(owner1, path, thumb);
        
        _cache.Remove(path);

        Assert.False(_cache.TryGet(path, out _));
        
        // Ensure internal state is clean (owner no longer thinks it owns it? Or owner still thinks it owns it but file is gone?)
        // My implementation: Remove(path) removes from _thumbnails and _ownersByFile, and cleans up _filesByOwner.
        // So owners should be clean.
    }
}
