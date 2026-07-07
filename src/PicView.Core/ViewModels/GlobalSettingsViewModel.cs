using R3;

namespace PicView.Core.ViewModels;

public class GlobalSettingsViewModel
{
    public BindableReactiveProperty<bool> IsIncludingSubdirectories { get; } =
        new(Settings.Sorting.IncludeSubDirectories);

    public BindableReactiveProperty<bool> IsLooping { get; } = new(Settings.UIProperties.Looping);

    public BindableReactiveProperty<bool> IsFileHistoryEnabled { get; } = new(Settings.Navigation.IsFileHistoryEnabled);

    public BindableReactiveProperty<bool> IsShowingTaskbarProgress { get; } = new(Settings.UIProperties.IsTaskbarProgressEnabled);
    
    public BindableReactiveProperty<bool> ShowSetAsWallpaper { get; } = new(Settings.UIProperties.ShowSetAsWallpaper);
    
    public BindableReactiveProperty<object?> ImageBackground { get; } = new();
    
    public BindableReactiveProperty<object?> ConstrainedImageBackground { get; } = new();
    
    public BindableReactiveProperty<int> BackgroundChoice { get; } = new();
}