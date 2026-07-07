using PicView.Core.DebugTools;

namespace PicView.Core.Navigation;

public static class IterationHelper
{
    public static (int, bool) GetIteration(int index, int count, NavigateTo navigation, SkipAmount skipAmount)
    {
        if (count is 0)
        {
            return (-1, false);
        }
        
        var skip = SkipAmountToInt(skipAmount);

        switch (navigation)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var indexChange = navigation == NavigateTo.Next ? skip : -skip;
                var isReversed = navigation == NavigateTo.Previous;

                if (Settings.UIProperties.Looping)
                {
                    var loopedIndex = (index + indexChange) % count;
                    if (loopedIndex < 0)
                    {
                        loopedIndex += count;
                    }
                    return (loopedIndex, isReversed);
                }

                var newIndex = index + indexChange;
                return (Math.Clamp(newIndex, 0, count - 1), isReversed);

            case NavigateTo.First:
                return (0, true);

            case NavigateTo.Last:
                return (count - 1, false);

            default:
#if DEBUG
                DebugHelper.LogDebug(nameof(IterationHelper), nameof(GetIteration), $"{navigation} is not a valid NavigateTo value.");
#endif
                return (-1, false);
        }
    }

    /// <summary>
    /// Calculates the indices for navigating a dual-view image pane based on the provided navigation settings.
    /// </summary>
    /// <param name="index">The current index in the collection.</param>
    /// <param name="count">The total number of items in the collection.</param>
    /// <param name="navigation">The navigation direction to determine the next indices.</param>
    /// <param name="skipAmount">The amount by which the indices should be skipped. Use none to get current indices.</param>
    /// <returns>
    /// A tuple containing:
    /// - The primary index of the item to navigate to.
    /// - The secondary index of the item for dual-pane views.
    /// - A boolean indicating whether the navigation is reversed.
    /// Returns (-1, -1, false) if invalid navigation is requested or the count of items is zero.
    /// </returns>
    public static (int, int, bool) GetIterations(int index, int count, NavigateTo navigation, SkipAmount skipAmount)
    {
        switch (count)
        {
            // Handle edge cases where we don't have enough files for a proper dual view
            case 0:
                return (-1, -1, false);
            case 1:
                return (0, 0, false);
        }

        var skip = SkipAmountToInt(skipAmount);

        // For a dual pane view, we skip by pairs (multiply the skip amount by 2)
        var jump = skip * 2;

        switch (navigation)
        {
            case NavigateTo.Next:
            case NavigateTo.Previous:
                var indexChange = navigation == NavigateTo.Next ? jump : -jump;
                var isReversed = navigation == NavigateTo.Previous;

                if (Settings.UIProperties.Looping)
                {
                    // Calculate the first index with wrap-around logic
                    var first = (index + indexChange) % count;
                    if (first < 0)
                    {
                        first += count;
                    }
                    
                    // The second index is just the next image, also wrapped
                    var second = (first + 1) % count;
                    return (first, second, isReversed);
                }
                else
                {
                    // Calculate raw indices without wrapping
                    var first = index + indexChange;
                    var second = first + 1;

                    // Clamp to the beginning of the list if we go too far back
                    if (first < 0)
                    {
                        return (0, 1, isReversed);
                    }

                    // Clamp to the end of the list if the second index goes out of bounds
                    if (second >= count)
                    {
                        return (count - 2, count - 1, isReversed);
                    }

                    return (first, second, isReversed);
                }

            case NavigateTo.First:
                return (0, 1, true);

            case NavigateTo.Last:
                return (count - 2, count - 1, false);

            default:
#if DEBUG
                DebugHelper.LogDebug(nameof(IterationHelper), nameof(GetIterations), $"{navigation} is not a valid NavigateTo value.");
#endif
                return (-1, -1, false);
        }
    }
    
    private static int SkipAmountToInt(SkipAmount skipAmount)
    {
        return skipAmount switch
        {
            SkipAmount.One => 1,
            SkipAmount.Two => 2,
            SkipAmount.Ten => 10,
            SkipAmount.Hundred => 100,
            SkipAmount.None => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(skipAmount), skipAmount, null)
        };
    }
}