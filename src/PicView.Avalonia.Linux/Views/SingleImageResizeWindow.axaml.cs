using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using PicView.Avalonia.UI;
using PicView.Core.Extensions;
using PicView.Core.Localization;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Linux.Views;

public partial class SingleImageResizeWindow : Window
{
    public SingleImageResizeWindow(MainWindowViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
        GenericWindowHelper.GenericWindowInitialize(this, StringExtensions.CombineWithAppName(TranslationManager.Translation.ResizeImage));
        if (!Settings.Theme.Dark || Settings.Theme.GlassTheme)
        {
            XAboutView.Background = Brushes.Transparent;
        }
    }

    private void MoveWindow(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}