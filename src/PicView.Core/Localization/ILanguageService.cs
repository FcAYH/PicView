namespace PicView.Core.Localization;

public interface ILanguageService
{
    ValueTask UpdateLanguageAsync(string languageCode);
    IEnumerable<(string Code, string DisplayName)> GetAvailableLanguages();
}
