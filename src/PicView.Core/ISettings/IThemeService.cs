namespace PicView.Core.ISettings;

public interface IThemeService
{
    void SetTheme(int themeIndex);
    void SetColorTheme(int colorIndex);
    void SetBackground(int backgroundIndex);
}
