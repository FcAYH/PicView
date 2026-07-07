namespace PicView.Core.Update;

public class UpdateInfo
{
    public required string Version { get; set; }
    
    ////////////\\\\\\\\\\\\
    ///  Windows versions \\\
    ////////////\\\\\\\\\\\\\
    public string? X64Portable { get; set; }
    public string? X64Install { get; set; }
    public string? Arm64Portable { get; set; }
    public string? Arm64Install { get; set; }
    
    
    ////////////\\\\\\\\\\\
    ///  macOS versions \\\
    ////////////\\\\\\\\\\\\
    public string? MacIntel { get; set; }
    public string? MacArm64 { get; set; }
}
