using R3;

namespace PicView.Core.ViewModels;

public class MainWindowOverviewViewModel
{
    // The collection of all open windows
    public List<MainWindowViewModel> MainWindows { get; } = [];

    // The single "Correct" window that currently has focus
    public BindableReactiveProperty<MainWindowViewModel?> ActiveWindow { get; } = new();
}