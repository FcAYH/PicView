using PicView.Core.Update;

namespace PicView.Core.IPlatform;

public interface IPlatformSpecificUpdate
{
    public Task HandlePlatformUpdate(UpdateInfo updateInfo, string tempPath);
}