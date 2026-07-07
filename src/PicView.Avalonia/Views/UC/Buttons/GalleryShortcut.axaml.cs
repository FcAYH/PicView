using Avalonia.Controls;
using PicView.Avalonia.UI;
using PicView.Core.ViewModels;

namespace PicView.Avalonia.Views.UC.Buttons;

public partial class GalleryShortcut : UserControl
{
    public GalleryShortcut()
    {
        InitializeComponent();
        Loaded += delegate
        {
            if (DataContext is not TabViewModel tab)
            {
                return;
            }

            if (tab.ParentWindowContext is not { } vm)
            {
                return;
            }
            _ = new HoverFadeButtonHandler(this, InnerButton);
        };
    }
}