using Avalonia;
using Avalonia.Controls;

namespace PicView.Avalonia.UI;

public abstract class SearchProperties : AvaloniaObject
{
    public static readonly AttachedProperty<string?> KeywordsProperty =
        AvaloniaProperty.RegisterAttached<SearchProperties, Control, string?>("Keywords");

    public static string? GetKeywords(Control element) => element.GetValue(KeywordsProperty);
    public static void SetKeywords(Control element, string? value) => element.SetValue(KeywordsProperty, value);
}