using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace PicView.Avalonia.CustomControls;

public class GenericWindow : Window
{
    public void Close(object? sender, RoutedEventArgs e) => Close();
    public void Minimize(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    public void MoveWindow(object? sender, PointerPressedEventArgs e) => BeginMoveDrag(e);
}