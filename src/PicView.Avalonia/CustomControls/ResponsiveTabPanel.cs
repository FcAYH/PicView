using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using R3;

namespace PicView.Avalonia.CustomControls;

public class ResponsiveTabPanel : Panel
{
    public static readonly StyledProperty<double> MinTabWidthProperty =
        AvaloniaProperty.Register<ResponsiveTabPanel, double>(nameof(MinTabWidth), 140.0);

    private ScrollViewer? _parentScrollViewer;
    private IDisposable? _subscription;

    public double MinTabWidth
    {
        get => GetValue(MinTabWidthProperty);
        set => SetValue(MinTabWidthProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Find the generic ScrollViewer usually found in TabStrip templates
        _parentScrollViewer = this.FindAncestorOfType<ScrollViewer>();

        if (_parentScrollViewer != null)
        {
            // Subscribe to the ScrollViewer's size/viewport changes.
            // When the window resizes, the ScrollViewer changes, and we must force 
            // this panel to re-measure itself to fill the new space.
            _subscription = _parentScrollViewer
                .GetObservable(ScrollViewer.ViewportProperty)
                .ToObservable()
                .Debounce(TimeSpan.FromMilliseconds(16))
                .Subscribe(_ => InvalidateMeasure());
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _subscription?.Dispose();
        _parentScrollViewer = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override Size MeasureOverride(Size available)
    {
        var n = Children.Count;
        if (n == 0)
        {
            return default;
        }

        var width = available.Width;

        // 1. Get the actual Viewport width (Visible space)
        // If infinite (inside ScrollViewer), grab the viewport from the parent.
        if (double.IsInfinity(width))
        {
            width = _parentScrollViewer != null 
                ? _parentScrollViewer.Viewport.Width : Bounds.Width;
        }

        // Safety for initialization
        if (width <= 0)
        {
            return new Size(MinTabWidth * n, available.Height);
        }

        double maxH = 0;

        // 2. Determine Item Width based on count
        var itemWidth = n switch
        {
            1 => width,
            >= 2 and <= 4 => width / n, // Always split evenly, ignoring MinTabWidth
            _ => Math.Max(MinTabWidth, width / n)
        };

        // 3. Measure Children
        foreach (var c in Children)
        {
            c.Measure(new Size(itemWidth, available.Height));
            maxH = Math.Max(maxH, c.DesiredSize.Height);
        }

        // Return the calculated total width.
        // If itemWidth was calculated via (width / n), this returns exactly 'width'.
        // If itemWidth was MinTabWidth, this returns (MinTabWidth * n), enabling scrollbars if needed.
        return new Size(itemWidth * n, maxH);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var n = Children.Count;
        if (n == 0)
        {
            return finalSize;
        }

        // Whether we are scrolling or stretching, the MeasureOverride has already 
        // requested the correct size. 'finalSize.Width' is now that requested size.
        // We simply divide it evenly among the children.

        var rawEach = finalSize.Width / n;
        double x = 0;

        for (var i = 0; i < n; ++i)
        {
            // Use the same logic as your 2-4 case for ALL cases now.
            // This ensures pixel-perfect filling of the space provided.
            var w = i == n - 1 ? Math.Max(0, finalSize.Width - x) : rawEach;

            Children[i].Arrange(new Rect(x, 0, w, finalSize.Height));
            x += w;
        }

        return finalSize;
    }
}