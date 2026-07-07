namespace PicView.Core.Sizing;

public readonly record struct ImageSize(
    double WindowWidth,
    double WindowHeight,
    double Width,
    double Height,
    double ScrollViewerWidth,
    double ScrollViewerHeight,
    double InitialZoom);