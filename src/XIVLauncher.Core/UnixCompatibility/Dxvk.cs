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
    public static bool Enabled => Program.Config.DxvkVersion != "DISABLED";

    public static string FolderName => Program.Config.DxvkVersion ?? GetDefaultVersion();

    public static string DownloadUrl => GetDownloadUrl(Program.Config.DxvkVersion);

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

    public static bool MangoHudInstalled { get; }

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

    public static Dictionary<string, Dictionary<string, string>> Versions { get; private set; }

    static Dxvk()
    {
        Versions = new Dictionary<string, Dictionary<string, string>>();
        MangoHudInstalled = DxvkSettings.MangoHudIsInstalled();
    }

    public static void Initialize()
    {
        // Add default versions.
        Versions["dxvk-2.3"] = new Dictionary<string, string>()
        {
            {"name", "DXVK 2.3"}, {"desc", "Latest version, using Graphics Pipeline Libs. Async no longer needed."},
            {"label", "Current"}, {"url", "https://github.com/doitsujin/dxvk/releases/download/v2.3/dxvk-2.3.tar.gz"},
            {"mark", "Download" }
        };
        Versions["dxvk-2.2"] = new Dictionary<string, string>()
        {
            {"name", "DXVK 2.2"}, {"desc", "Previous version, using Graphics Pipeline Libs. Use this if you use ReShade Effects Toggler."},
            {"label", "Current"}, {"url", "https://github.com/doitsujin/dxvk/releases/download/v2.2/dxvk-2.2.tar.gz"},
            {"mark", "Download" }
        };
        Versions["dxvk-async-1.10.3"] = new Dictionary<string, string>()
        {
            {"name", "DXVK 1.10.3"}, {"desc", "Legacy version with high compatibility. Includes async patch."},
            {"label", "Legacy"}, {"url", "https://github.com/Sporif/dxvk-async/releases/download/1.10.3/dxvk-async-1.10.3.tar.gz"},
            {"mark", "Download" }
        };
        Versions["DISABLED"] = new Dictionary<string, string>()
        {
            {"name", "WineD3D"}, {"desc", "Use WineD3D (OpenGL) instead of DXVK. For old GPUs without Vulkan support."},
            {"label", "Disabled"}
        };

        var toolDirectory = new DirectoryInfo(Path.Combine(Program.storage.Root.FullName, "compatibilitytool", "dxvk"));

        if (!toolDirectory.Exists)
        {
            Program.storage.GetFolder("compatibilitytool/dxvk");
            return;
        }

        foreach (var dxvkDir in toolDirectory.EnumerateDirectories().OrderBy(x => x.Name))
        {
            if (Directory.Exists(Path.Combine(dxvkDir.FullName, "x64")) && Directory.Exists(Path.Combine(dxvkDir.FullName, "x32")))
            {
                if (Versions.ContainsKey(dxvkDir.Name))
                {
                    if (dxvkDir.Name == "DISABLED")
                        Log.Error("Cannot use custom DXVK with folder name DISABLED. Skipping.");
                    else
                        Versions[dxvkDir.Name].Remove("mark");
                    continue;
                }
                Versions[dxvkDir.Name] = new Dictionary<string, string>() { {"label", "Custom"} };
            }
        }
    }

    private static string GetDownloadUrl(string? name)
    {
        name ??= GetDefaultVersion();
        if (Versions.ContainsKey(name))
            return Versions[name].ContainsKey("url") ? Versions[name]["url"] : "";
        return Versions[GetDefaultVersion()]["url"];
    }

    public static string GetDefaultVersion()
    {
        if (Versions.ContainsKey("dxvk-async-1.10.3"))
            return "dxvk-async-1.10.3";
        if (Versions.ContainsKey("dxvk-2.3"))
            return "dxvk-2.3";
        return Versions.First().Key;
    }

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

