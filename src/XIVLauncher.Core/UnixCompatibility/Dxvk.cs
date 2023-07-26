using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;
using XIVLauncher.Common;
using XIVLauncher.Common.Unix.Compatibility;

namespace XIVLauncher.Core.UnixCompatibility;

public static class Dxvk
{
    public static bool Enabled => Program.Config.DxvkVersion != DxvkVersion.Disabled;

    public static string FolderName => Program.Config.DxvkVersion switch
    {
        DxvkVersion.Disabled => "",
        DxvkVersion.v1_10_3 => "dxvk-async-1.10.3",
        DxvkVersion.v2_0 => "dxvk-async-2.0",
        DxvkVersion.v2_1 => "dxvk-2.1",
        DxvkVersion.v2_2 => "dxvk-2.2",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public static string DownloadUrl => Program.Config.DxvkVersion switch
    {
        DxvkVersion.Disabled => "",
        DxvkVersion.v1_10_3 => "https://github.com/Sporif/dxvk-async/releases/download/1.10.3/dxvk-async-1.10.3.tar.gz",
        DxvkVersion.v2_0 => "https://github.com/Sporif/dxvk-async/releases/download/2.0/dxvk-async-2.0.tar.gz",
        DxvkVersion.v2_1 => "https://github.com/doitsujin/dxvk/releases/download/v2.1/dxvk-2.1.tar.gz",
        DxvkVersion.v2_2 => "https://github.com/doitsujin/dxvk/releases/download/v2.2/dxvk-2.2.tar.gz",
        _ => throw new ArgumentOutOfRangeException(),        
    };

    public static int FrameRateLimit => Program.Config.DxvkFrameRateLimit ?? 0;

    public static bool AsyncEnabled => Program.Config.DxvkAsyncEnabled ?? false;

    public static bool DxvkHudEnabled => Program.Config.DxvkHud != DxvkHud.None;

    public static string DxvkHudString => Program.Config.DxvkHud switch
    {
        DxvkHud.None => "",
        DxvkHud.Custom => Program.Config.DxvkHudCustom,
        DxvkHud.Default => "1",
        DxvkHud.Fps => "fps",
        DxvkHud.Full => "full",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public static bool MangoHudInstalled => UnixHelpers.MangoHudIsInstalled();

    public static bool MangoHudEnabled => Program.Config.MangoHud != MangoHud.None;

    public static bool MangoHudCustomIsFile => Program.Config.MangoHud == MangoHud.CustomFile;

    public static string MangoHudString => Program.Config.MangoHud switch
    {
        MangoHud.None => "",
        MangoHud.Default => "",
        MangoHud.Full => "full",
        MangoHud.CustomString => Program.Config.MangoHudCustomString,
        MangoHud.CustomFile => Program.Config.MangoHudCustomFile,
        _ => throw new ArgumentOutOfRangeException(),
    };

    public static string DXVK_HUD => "fps,frametimes,gpuload,version";

    public static string MANGOHUD_CONFIG => "ram,vram,resolution,vulkan_driver,engine_version,wine,frame_timing=0";

    public static string MANGOHUD_CONFIGFILE => Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config", "MangoHud", "MangoHud.conf");

}

public enum DxvkVersion
{
    [SettingsDescription("1.10.3 (default)", "Current version of 1.10 branch of DXVK.")]
    v1_10_3,

    [SettingsDescription("2.0", "Newer version of DXVK. Last version with Async patch")]
    v2_0,

    [SettingsDescription("2.1 (No Async)", "Newer version of DXVK, using graphics pipeline library. No Async patch.")]
    v2_1,

        [SettingsDescription("2.2 (No Async)", "Newest version of DXVK, using graphics pipeline library. No Async patch.")]
    v2_2,     

    [SettingsDescription("Disabled", "Disable Dxvk, use WineD3D with OpenGL instead.")]
    Disabled,
}

public enum DxvkHud
{
    [SettingsDescription("None", "Disable DXVK Hud")]
    None,

    [SettingsDescription("FPS", "Only show FPS")]
    Fps,

    [SettingsDescription("Default", "Equivalent to DXVK_HUD=1")]
    Default,

    [SettingsDescription("Custom", "Use a custom DXVK_HUD string")]
    Custom,

    [SettingsDescription("Full", "Show everything")]
    Full,
}

public enum MangoHud
{
    [SettingsDescription("None", "Disable MangoHud")]
    None,

    [SettingsDescription("Default", "Uses no config file.")]
    Default,

    [SettingsDescription("Custom File", "Specify a custom config file")]
    CustomFile,

    [SettingsDescription("Custom String", "Specify a config via string")]
    CustomString,

    [SettingsDescription("Full", "Show (almost) everything")]
    Full,
}

