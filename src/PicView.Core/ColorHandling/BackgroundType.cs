using System.Diagnostics.CodeAnalysis;

namespace PicView.Core.ColorHandling;

/// <summary>
/// Defines the available background choices for the image viewer
/// </summary>
public enum BackgroundType
{
    Transparent = 0,
    NoiseTexture = 1,
    Checkerboard = 2,
    CheckerboardAlternative = 3,
    White = 4,
    LightGray = 5,
    MediumGray = 6,
    SemiTransparentDarkGray = 7,
    SemiTransparentDarkerGray = 8,
    NearBlack = 9,
    DarkGray = 10,
    MainBackgroundColor = 11,
        
    // For cycling purpose
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated")] 
    MaxValue = 10
}