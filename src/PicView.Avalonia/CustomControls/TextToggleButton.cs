using Avalonia;
using Avalonia.Controls.Primitives;

namespace PicView.Avalonia.CustomControls;
public class TextToggleButton : ToggleButton
{
    public static readonly StyledProperty<double> TextMaxWidthProperty =
        AvaloniaProperty.Register<TextIconButton, double>(nameof(TextMaxWidth), double.PositiveInfinity);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<TextIconButton, string?>(nameof(Text));

    public static readonly StyledProperty<Thickness> TextMarginProperty =
        AvaloniaProperty.Register<TextIconButton, Thickness>(nameof(TextMargin));

    protected override Type StyleKeyOverride => typeof(TextToggleButton);

    public double TextMaxWidth
    {
        get => GetValue(TextMaxWidthProperty);
        set => SetValue(TextMaxWidthProperty, value);
    }

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public Thickness TextMargin
    {
        get => GetValue(TextMarginProperty);
        set => SetValue(TextMarginProperty, value);
    }
}