using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;

namespace PicView.Tests;

public class AvaloniaTest
{
    // MacOS-specific tests disabled due to MacOS project being excluded on Windows
    // [assembly: AvaloniaTestApplication(typeof(AvaloniaTest))]

    [AvaloniaFact]
    public async Task TestPreloader()
    {
        // await LoadSettingsAsync();
        // var vm = new MainViewModel();
        //await vm.StartUpTask();
    }
}