using Avalonia;
using Avalonia.Media;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.Linux.Views;

public partial class ImageInfoWindow : GenericWindow, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly ImageInfoWindowConfig _config;
    public ImageInfoWindow(ImageInfoWindowConfig config)
    {
        _config = config;
        InitializeComponent();
        if (Settings.Theme.GlassTheme)
        {
            WindowBorder.Background = Brushes.Transparent;
        }
        else if (!Settings.Theme.Dark)
        {
            XExifView.Background = UIHelper.GetMenuBackgroundColor();
        }
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.ImageInfo));
        Loaded += delegate
        {
            ClientSizeProperty.Changed.ToObservable()
                .ObserveOn(UIHelper.GetFrameProvider)
                .Debounce(TimeSpan.FromMilliseconds(10))
                .Subscribe(UpdateWindowSize)
                .AddTo(_disposables);
            PositionChanged += (_, __) => UpdateWindowPosition();
        };
    }
    
    private void UpdateWindowPosition()
    {
        _config.WindowProperties.Left = Position.X;
        _config.WindowProperties.Top = Position.Y;
    }
    
    private void UpdateWindowSize(AvaloniaPropertyChangedEventArgs<Size> size)
        => WindowFunctions.SetWindowSize(this, size, _config.WindowProperties);
    
    public void Dispose()
    {
        Disposable.Dispose(_disposables);
        GC.SuppressFinalize(this);
    }

    ~ImageInfoWindow()
    {
        Disposable.Dispose(_disposables);
    }
}