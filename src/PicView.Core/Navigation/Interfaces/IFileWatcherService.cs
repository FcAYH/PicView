using PicView.Core.ViewModels;

namespace PicView.Core.Navigation.Interfaces;

public interface IFileWatcherService
{
    void Watch(TabViewModel tab, string? directory = null);
    void Unwatch(TabViewModel tab);
}
