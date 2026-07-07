using System.Diagnostics;
using ObservableCollections;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Navigation;
using R3;

namespace PicView.Core.ViewModels;

public class GalleryViewModel : IDisposable
{
    private DisposableBag _disposables;
    public ReactiveCommand<GalleryMode2> SetGalleryModeCommand { get; } = new();
    public ReactiveCommand<Unit> ContractToDockedOrCloseGalleryCommand { get; } = new();
    public ReactiveCommand<Unit> ToggleGalleryCommand { get; } = new();
    public ReactiveCommand<GalleryDockPosition> SetDockPositionCommand { get; } = new();
    public ReactiveCommand<Unit> CloseGalleryCommand { get; } = new();
    public ReactiveCommand<NavigateTo> NavigateGalleryCommand { get; } = new();
    public ReactiveCommand<int> OpenSelectedItemCommand { get; } = new();

    public ObservableList<GalleryItemViewModel> GalleryItems { get; } = new([]);
    public BindableReactiveProperty<GalleryMode2> GalleryMode { get; } = new(GalleryMode2.Closed);

    public BindableReactiveProperty<bool> IsGalleryExpanded { get; } = new();
    public BindableReactiveProperty<bool> IsDockedGalleryVisible { get; } = new(Settings.Gallery.IsGalleryDocked);
    public BindableReactiveProperty<double> ItemSpacing { get; } = new(Settings.Gallery.ItemSpacing);
    public BindableReactiveProperty<double> LineSpacing { get; } = new(Settings.Gallery.LineSpacing);
    public BindableReactiveProperty<bool> IsGalleryDocked { get; } = new(Settings.Gallery.IsGalleryDocked);
    public BindableReactiveProperty<int> SelectedGalleryItemIndex { get; } = new(-1);
    
    public BindableReactiveProperty<bool> IsTopDocked { get; } = new();
    public BindableReactiveProperty<bool> IsBottomDocked { get; } = new();
    public BindableReactiveProperty<bool> IsLeftDocked { get; } = new();
    public BindableReactiveProperty<bool> IsRightDocked { get; } = new();

    public GalleryLoadingState LoadingState { get; set; }

    public void Initialize()
    {
        Debug.Assert(Settings.Gallery is not null);
        GallerySettingsConverter.UpdateDockPositionProperties(this);
        Observable.EveryValueChanged(Settings.Gallery, g => g.IsGalleryDocked)
        .Subscribe(isDocked =>
        {
            if (isDocked && GalleryMode.Value == GalleryMode2.Closed)
            {
                GalleryMode.Value = GalleryMode2.Docked;
            }
            else if (!isDocked && GalleryMode.Value == GalleryMode2.Docked)
            {
                GalleryMode.Value = GalleryMode2.Closed;
            }
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);

        Observable.EveryValueChanged(Settings.Gallery, g => g.ItemSpacing)
        .Subscribe(x =>
        {
            if (IsGalleryExpanded.CurrentValue)
            {
                ItemSpacing.Value = x;
            }
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);

        Observable.EveryValueChanged(Settings.Gallery, g => g.LineSpacing)
        .Subscribe(x =>
        {
            if (IsGalleryExpanded.CurrentValue)
            {
                LineSpacing.Value = x;
            }
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);

        GalleryMode.Subscribe(mode =>
        {
            IsGalleryExpanded.Value = mode == GalleryMode2.Expanded;
            IsDockedGalleryVisible.Value = mode == GalleryMode2.Docked;
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);
        
        SetGalleryModeCommand.Subscribe(mode =>
        {
            GalleryMode.Value = mode;
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);
        
        ToggleGalleryCommand.Subscribe(_ =>
        {
            GalleryManager.ToggleGallery(this);
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);
        
        ContractToDockedOrCloseGalleryCommand.Subscribe(_ =>
        {
            if (IsGalleryExpanded.CurrentValue)
            {
                if (Settings.Gallery.IsGalleryDocked)
                {
                    GalleryMode.Value = GalleryMode2.Docked;
                }
                else
                {
                    IsLeftDocked.Value = IsRightDocked.Value = IsTopDocked.Value = IsBottomDocked.Value = false;
                    GalleryMode.Value = GalleryMode2.Closed;
                    Settings.Gallery.IsGalleryDocked = false;
                }
            }
            else if (Settings.Gallery.IsGalleryDocked && !IsGalleryExpanded.CurrentValue)
            {
                IsLeftDocked.Value = IsRightDocked.Value = IsTopDocked.Value = IsBottomDocked.Value = false;
                GalleryMode.Value = GalleryMode2.Closed;
                Settings.Gallery.IsGalleryDocked = false;
            }
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);
        
        CloseGalleryCommand.SubscribeAwait(async (_, ct) =>
        {
            IsGalleryDocked.Value = false;
            await GalleryManager.CloseDockedGalleryAsync(ct);
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);

        SetDockPositionCommand.Subscribe(pos =>
        {
            Settings.Gallery.IsGalleryDocked = true;
            Settings.Gallery.DockPosition = pos;
            IsGalleryDocked.Value = true;
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);
        
        Observable.EveryValueChanged(Settings.Gallery, x => x.IsGalleryDocked)
        .Skip(1)
        .Subscribe(x =>
        {
            IsGalleryDocked.Value = x;
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);

        IsGalleryDocked
        .Skip(1)
        .SubscribeAwait(async (isDocked, ct) =>
        {
            if (!isDocked)
            {
                await GalleryManager.CloseDockedGalleryAsync(ct);
            }
        }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);
        
        Observable.EveryValueChanged(Settings.Gallery, x => x.DockPosition)
        .Skip(1)
        .Subscribe(_ => { GallerySettingsConverter.UpdateDockPositionProperties(this); }, DebugHelper.LogError(nameof(GalleryViewModel), nameof(Initialize)))
        .AddTo(ref _disposables);
    }

    public void Navigate(NavigateTo direction)
    {
        NavigateGalleryCommand.Execute(direction);
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
        Disposable.Dispose(
            SetGalleryModeCommand,
            ContractToDockedOrCloseGalleryCommand,
            ToggleGalleryCommand,
            SetDockPositionCommand,
            CloseGalleryCommand,
            NavigateGalleryCommand,
            OpenSelectedItemCommand,
            GalleryMode,
            IsGalleryExpanded,
            IsDockedGalleryVisible,
            ItemSpacing,
            LineSpacing,
            IsGalleryDocked,
            SelectedGalleryItemIndex,
            IsTopDocked,
            IsBottomDocked,
            IsLeftDocked,
            IsRightDocked
        );
        GC.SuppressFinalize(this);
    }
}