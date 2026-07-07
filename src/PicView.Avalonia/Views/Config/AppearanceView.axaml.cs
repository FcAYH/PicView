using Avalonia.Controls;
using PicView.Avalonia.ColorManagement;

namespace PicView.Avalonia.Views.Config;

public partial class AppearanceView : UserControl
{
    public AppearanceView()
    {
        InitializeComponent();
        CheckerboardButton.Background = BackgroundManager.CreateCheckerboardBrush(default, default,10);
        CheckerboardAltButton.Background = BackgroundManager.CreateCheckerboardBrushAlt(25);
    }
}