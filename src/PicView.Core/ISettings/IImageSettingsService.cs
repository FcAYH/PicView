namespace PicView.Core.ISettings;

public interface IImageSettingsService
{
    void TriggerScalingModeUpdate(bool nearestNeighbor);
}
