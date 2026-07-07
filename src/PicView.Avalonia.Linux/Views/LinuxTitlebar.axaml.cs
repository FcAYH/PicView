using Avalonia.Controls;
using Avalonia.Input;
using PicView.Avalonia.ColorManagement;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.DragAndDrop;
using PicView.Avalonia.UI;
using PicView.Avalonia.WindowBehavior;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Avalonia.Linux.Views;

public partial class LinuxTitlebar : MainTitleBar
{
    public LinuxTitlebar()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            if (Settings.Theme.GlassTheme)
            {
                ApplyGlassThemeStyles();
            }

            InitializeEventHandlers();
        };
    }

    // Extract method: centralize glass theme styling to remove duplication
    private void ApplyGlassThemeStyles()
    {
        GlassThemeHelper.ApplyTransparentStyle(TopWindowBorder);
        GlassThemeHelper.ApplyTransparentStyle(LogoBorder);
        GlassThemeHelper.ApplyTransparentStyle(EditableTitlebar);
        GlassThemeHelper.ApplyTransparentStyle(CloseButton);
        GlassThemeHelper.ApplyTransparentStyle(MinimizeButton);
        GlassThemeHelper.ApplyTransparentStyle(RestoreButton);
        GlassThemeHelper.ApplyTransparentStyle(FullscreenButton);
        GlassThemeHelper.ApplyTransparentStyle(DropDownMenuButton);
        GlassThemeHelper.ApplyTransparentStyle(MenuButton);
        GlassThemeHelper.ApplyTransparentStyle(MainMenu);

        var glassForeground = UIHelper.GetBrush("SecondaryTextColor");
        EditableTitlebar.Foreground = glassForeground;
        CloseButton.Foreground = glassForeground;
        MinimizeButton.Foreground = glassForeground;
        RestoreButton.Foreground = glassForeground;
        DropDownMenuButton.Foreground = glassForeground;
        MenuButton.Foreground = glassForeground;
    }
    
    private void InitializeEventHandlers()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }
        
        PointerExited += (_, _) => { DragAndDropHelper.RemoveDragDropView(); };
        MainMenu.Closed += (_, _) => { CloseMenu(); };
        
        Observable.EveryValueChanged(vm.TopTitlebarViewModel.IsMainMenuVisible, x => x.Value,
                UIHelper2.GetFrameProvider)
            .Subscribe(isVisible =>
            {
                if (isVisible)
                {
                    // Overflow buttons if the window is too small
                    if (Bounds.Width - SearchButton.Bounds.Width - DropDownMenuButton.Bounds.Width - CreateTabButton.Bounds.Width < SizeDefaults.WindowMinSize)
                    {
                        HideButtons(vm);
                    }
                    else
                    {
                        ShowButtons(vm);
                    }
                    
                    MainMenu.Open();
                    FileMenuItem.Open();
                }
                else
                {
                    MainMenu.Close();
                    ShowButtons(vm);
                }
            });
    }

    private void HideButtons(MainWindowViewModel vm)
    {
        vm.TopTitlebarViewModel.IsBtnPanelVisible.Value = false;
        SearchButton.IsVisible = false;
        DropDownMenuButton.IsVisible = false;
        CreateTabButton.IsVisible = false;
    }
    
    private void ShowButtons(MainWindowViewModel vm)
    {
        vm.TopTitlebarViewModel.IsBtnPanelVisible.Value = true;
        SearchButton.IsVisible = true;
        DropDownMenuButton.IsVisible = true;
        CreateTabButton.IsVisible = true;
    }

    private void CloseMenu()
    {
        MainMenu.Close();

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.TopTitlebarViewModel.CloseMenu();
    }
}