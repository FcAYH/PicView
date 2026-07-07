using PicView.Core.Config;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Preloading;

namespace PicView.Tests.Navigation;

public class SharedImageCacheTests
{
    private readonly SharedImageCache _cache;
    private readonly Func<FileInfo, ValueTask<ImageModel>> _mockLoader;

    public SharedImageCacheTests()
    {
        SettingsManager.SetDefaults(); // Initialize settings
        _mockLoader = f => new ValueTask<ImageModel>(new ImageModel { FileInfo = f });
        _cache = new SharedImageCache(_mockLoader);
    }

    [Fact]
    public async Task LoadAsync_ShouldLoadImage()
    {
        var ownerId = "tab1";
        var file = new FileInfo("test.jpg");
        var list = new List<FileInfo> { file };
        _cache.RegisterOwner(ownerId);
        
        var model = await _cache.LoadAsync(ownerId, 0, list);

        Assert.NotNull(model);
        Assert.Equal(file.FullName, model.FileInfo.FullName);
        
        // Verify it is in cache
        Assert.True(_cache.TryGet(file, out _));
        Assert.True(_cache.TryGet(ownerId, 0, out _));
    }

    [Fact]
    public void Add_ShouldAddDirectly()
    {
        var ownerId = "tab1";
        var file = new FileInfo("direct.jpg");
        var model = new ImageModel { FileInfo = file };
        _cache.RegisterOwner(ownerId);
        var preLoadValue = new PreLoadValue(model);

        _cache.Add(ownerId, 0, preLoadValue, 1, false);

        Assert.True(_cache.TryGet(file, out var retrieved));
        Assert.Same(model, retrieved!.ImageModel);
    }

    [Fact]
    public void Resynchronize_ShouldUpdateIndices()
    {
        var ownerId = "tab1";
        _cache.RegisterOwner(ownerId);
        var files = new List<FileInfo>
        {
            new("0.jpg"),
            new("1.jpg"),
            new("2.jpg")
        };

        // Add item at index 0 (0.jpg)
        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = files[0] });
        _cache.Add(ownerId, 0, preLoadValue, files.Count, false);

        // Resynchronize: Move 0.jpg to index 2
        var newFiles = new List<FileInfo>
        {
            new("1.jpg"),
            new("2.jpg"),
            new("0.jpg")
        };

        _cache.Resynchronize(ownerId, newFiles);

        Assert.False(_cache.TryGet(ownerId, 0, out _));
        Assert.True(_cache.TryGet(ownerId, 2, out var moved));
        Assert.Equal("0.jpg", moved!.ImageModel.FileInfo.Name);
    }

    [Fact]
    public void Resynchronize_ShouldEvictRemovedItems()
    {
        var ownerId = "tab1";
        _cache.RegisterOwner(ownerId);
        var files = new List<FileInfo> { new("0.jpg") };

        var preLoadValue = new PreLoadValue(new ImageModel { FileInfo = files[0] });
        _cache.Add(ownerId, 0, preLoadValue, files.Count, false);

        _cache.Resynchronize(ownerId, new List<FileInfo>());

        Assert.False(_cache.TryGet(ownerId, 0, out _));
        Assert.False(_cache.TryGet(files[0], out _));
    }

    [Fact]
    public async Task MultiOwner_ShouldShareData()
    {
        var owner1 = "tab1";
        var owner2 = "tab2";
        var file = new FileInfo("shared.jpg");
        var list = new List<FileInfo> { file };
        _cache.RegisterOwner(owner1);
        _cache.RegisterOwner(owner2);

        // Load for Owner 1
        var model1 = await _cache.LoadAsync(owner1, 0, list);
        
        // Load for Owner 2 (same file)
        var model2 = await _cache.LoadAsync(owner2, 0, list);

        Assert.NotNull(model1);
        Assert.Same(model1, model2); // Should be same instance

        // Cache should map both
        Assert.True(_cache.TryGet(owner1, 0, out var val1));
        Assert.True(_cache.TryGet(owner2, 0, out var val2));
        Assert.Same(val1, val2);
    }
}