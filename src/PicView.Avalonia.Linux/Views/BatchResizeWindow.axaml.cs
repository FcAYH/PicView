using Avalonia;
using Avalonia.Controls;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.Linux.Views;

public partial class BatchResizeWindow : GenericWindow, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly BatchResizeWindowConfig _config;
    public BatchResizeWindow(BatchResizeWindowConfig config)
    {
        _config = config;
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.BatchResize));

        Loaded += delegate
        {
            ClientSizeProperty.Changed.ToObservable()
                .ObserveOn(UIHelper.GetFrameProvider)
                .Subscribe(size =>
                {
                    WindowResizing.HandleWindowResize(this, size);
                    UpdateWindowSize(size);
                })
                .AddTo(_disposables);
            PositionChanged += (_, _) => UpdateWindowPosition();

            Closing += async delegate
            {
                Hide();
                if (VisualRoot is null)
                {
                    return;
                }

                var hostWindow = (Window)VisualRoot;
                hostWindow?.Focus();
                await _config.SaveAsync();
            };
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
}