using PicView.Core.DebugTools;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Gallery;

public static class GalleryLoader
{
    public static async Task LoadGalleryAsync(TabViewModel tab, IReadOnlyList<FileInfo> files, IThumbnailLoader thumbnailLoader, IThumbnailCache thumbnailCache, CancellationToken ct)
    {
        if (tab.Gallery.LoadingState != GalleryLoadingState.NotLoaded)
        {
            return;
        }

        tab.Gallery.LoadingState = GalleryLoadingState.Loading;
        tab.Gallery.GalleryItems.Clear();

        var dockedHeight = Settings.Gallery.DockedGalleryItemSize;
        var expandedHeight = Settings.Gallery.ExpandedGalleryItemSize;
        var maxHeight = Math.Max(dockedHeight, expandedHeight);
        if (maxHeight <= 0)
        {
            maxHeight = GalleryDefaults.DefaultBottomGalleryHeight;
        }

        const int batchSize = 20;
        var batchList = new List<GalleryItemViewModel>(batchSize);

        // Populate items with metadata
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            
            var item = new GalleryItemViewModel
            {
                FileInfo = file
            };
            
            var thumbData = GalleryThumbInfo.GalleryThumbHolder.GetThumbData(file);
            item.FileName.Value = thumbData.FileName;
            item.FileSize.Value = thumbData.FileSize;
            item.FileDate.Value = thumbData.FileDate;
            item.FileLocation.Value = thumbData.FileLocation;
            
            batchList.Add(item);

            if (batchList.Count >= batchSize)
            {
                tab.Gallery.GalleryItems.AddRange(batchList);
                batchList.Clear();
            }
        }

        // Add any remaining items in the final batch
        if (batchList.Count > 0)
        {
            tab.Gallery.GalleryItems.AddRange(batchList);
        }

        // Load thumbnails asynchronously
        var parallelOptions = new ParallelOptions 
        { 
            CancellationToken = ct, 
            MaxDegreeOfParallelism = Environment.ProcessorCount - 2
        };
        
        try 
        {
            if (thumbnailCache.IsEmpty())
            {
                await Parallel.ForAsync(0, tab.Gallery.GalleryItems.Count, parallelOptions,
                async (i, _) =>
                {
                    var item = tab.Gallery.GalleryItems[i];
                    await LoadItem(item).ConfigureAwait(false);
                });
            }
            else
            {
                await Parallel.ForAsync(0, tab.Gallery.GalleryItems.Count, parallelOptions,
                async (i, _) =>
                {
                    var item = tab.Gallery.GalleryItems[i];
                    await CheckAndLoad(item).ConfigureAwait(false);
                });
            }
        }
        catch (OperationCanceledException)
        {
            tab.Gallery.LoadingState = GalleryLoadingState.NotLoaded;
            return;
        }
        
        tab.Gallery.LoadingState = GalleryLoadingState.Loaded;
        return;

        async ValueTask CheckAndLoad(GalleryItemViewModel item)
        {
            if (item.FileInfo is null)
            {
                DebugHelper.LogDebug(nameof(GalleryLoader), nameof(LoadGalleryAsync), "Invalid file");
                return;
            }
            
            object? thumb;
            if (thumbnailCache.TryGet(item.FileInfo.FullName, out var cached))
            {
                thumb = cached;
            }
            else
            {
                thumb = await thumbnailLoader.GetThumbnailAsync(item.FileInfo, (uint)maxHeight).ConfigureAwait(false);
            }

            if (thumb != null)
            {
                thumbnailCache.Add(tab.Id, item.FileInfo.FullName, thumb);
            }
            item.Image.Value = thumb;
        }
        
        async ValueTask LoadItem(GalleryItemViewModel item)
        {
            if (item.FileInfo is null)
            {
                DebugHelper.LogDebug(nameof(GalleryLoader), nameof(LoadGalleryAsync), "Invalid file");
                return;
            }
            
            var thumb = await thumbnailLoader.GetThumbnailAsync(item.FileInfo, (uint)maxHeight).ConfigureAwait(false);
            if (thumb != null)
            {
                thumbnailCache.Add(tab.Id, item.FileInfo.FullName, thumb);
            }
            item.Image.Value = thumb;
        }
    }
    
    public static async ValueTask LoadGalleryIfDockedOrExpanded(TabViewModel tabViewModel, GalleryMode2 mode, IThumbnailCache thumbnailCache, IThumbnailLoader thumbnailLoader)
    {
        if (mode is GalleryMode2.Docked or GalleryMode2.Expanded)
        {
            if (tabViewModel.Gallery.LoadingState is GalleryLoadingState.NotLoaded)
            {
                await LoadGalleryAsync(tabViewModel,
                        tabViewModel.ImageIterator.Files,
                        thumbnailLoader,
                        thumbnailCache,
                        tabViewModel.GetTabCancellation().Token)
                    .ConfigureAwait(false);
            }
        }
    }
    
    public static async ValueTask ToggleGalleryAndLoadItem(TabViewModel tabViewModel, int index)
    {
        var gallery = tabViewModel.Gallery;
        if (gallery.IsGalleryExpanded.Value)
        {
            GalleryManager.ToggleGallery(gallery);
        }

        await tabViewModel.ImageIterator.SkipToIndexAsync(index, tabViewModel.GetTabCancellation()).ConfigureAwait(false);
    }
}