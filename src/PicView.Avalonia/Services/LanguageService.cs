using Avalonia;
using Avalonia.Threading;
using PicView.Core.Localization;
using PicView.Core.ViewModels;
using ZLinq;

namespace PicView.Avalonia.Services;

public class LanguageService : ILanguageService
{
    public async ValueTask UpdateLanguageAsync(string languageCode)
    {
        await TranslationManager.LoadLanguage(languageCode).ConfigureAwait(false);

        var core = await Dispatcher.UIThread.InvokeAsync(() => Application.Current.DataContext as CoreViewModel);

        core?.Translation.UpdateLanguage();
    }

    public IEnumerable<(string Code, string DisplayName)> GetAvailableLanguages()
    {
        return TranslationManager.GetLanguages().ToArray()
                .Select(filePath =>
                {
                    var langCode = Path.GetFileNameWithoutExtension(filePath.Name);
                    var displayName = new System.Globalization.CultureInfo(langCode).DisplayName;
                    return (LanguageCode: langCode, DisplayName: displayName);
                })
                .OrderBy(x => x.DisplayName)
                .Select(x => (x.LanguageCode, x.DisplayName));
    }
}
