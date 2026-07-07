using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Config;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using R3;

namespace PicView.Avalonia.MacOS.Views;

public partial class BatchResizeWindow : GenericWindow
{
    private readonly BatchResizeWindowConfig _config;
    public BatchResizeWindow(BatchResizeWindowConfig config)
    {
        _config = config;
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.BatchResize), false, config.WindowProperties);
    }
}