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

namespace XIVLauncher.Core;

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

    [SettingsDescription("Disabled", "Disable Dxvk, use WineD3D / OpenGL instead.")]
    Disabled,
}

public enum DxvkHudType
{
    [SettingsDescription("None", "Show nothing")]
    None,

    [SettingsDescription("FPS", "Only show FPS")]
    Fps,

    [SettingsDescription("DXVK Hud Custom", "Use a custom DXVK_HUD string")]
    Custom,

    [SettingsDescription("Full", "Show everything")]
    Full,

    [SettingsDescription("MangoHud Default", "Uses no config file.")]
    MangoHud,

    [SettingsDescription("MangoHud Custom", "Specify a custom config file")]
    MangoHudCustom,

    [SettingsDescription("MangoHud Full", "Show (almost) everything")]
    MangoHudFull,
}

public static class DxvkManager
{
    private const string ALLOWED_CHARS = "^[0-9a-zA-Z,=.]+$";

    private const string ALLOWED_WORDS = "^(?:devinfo|fps|frametimes|submissions|drawcalls|pipelines|descriptors|memory|gpuload|version|api|cs|compiler|samplers|scale=(?:[0-9])*(?:.(?:[0-9])+)?)$";


    public static DxvkSettings Initialize()
    {
        var isDxvk = true;
        var folder = "";
        var url = "";
        var rootfolder = Program.storage.Root.FullName;
        var dxvkfolder = Path.Combine(rootfolder, "compatibilitytool", "dxvk");
        var async = (Program.Config.DxvkAsyncEnabled ?? true) ? "1" : "0";
        var framerate = Program.Config.DxvkFrameRate ?? 0;
        var env = new Dictionary<string, string>
        {
            { "DXVK_LOG_PATH", Path.Combine(rootfolder, "logs") },
            { "DXVK_CONFIG_FILE", Path.Combine(dxvkfolder, "dxvk.conf") },
        };
        if (framerate != 0)
            env.Add("DXVK_FRAME_RATE", framerate.ToString());
        switch (Program.Config.DxvkVersion)
        {
            case DxvkVersion.v1_10_3:
                folder = "dxvk-async-1.10.3";
                url = "https://github.com/Sporif/dxvk-async/releases/download/1.10.3/dxvk-async-1.10.3.tar.gz";
                env.Add("DXVK_ASYNC", async);
                break;
            
            case DxvkVersion.v2_0:
                folder = "dxvk-async-2.0";
                url = "https://github.com/Sporif/dxvk-async/releases/download/2.0/dxvk-async-2.0.tar.gz";
                env.Add("DXVK_ASYNC", async);
                break;

            case DxvkVersion.v2_1:
                folder = "dxvk-2.1";
                url = "https://github.com/doitsujin/dxvk/releases/download/v2.1/dxvk-2.1.tar.gz";
                break;

            case DxvkVersion.v2_2:
                folder = "dxvk-2.2";
                url = "https://github.com/doitsujin/dxvk/releases/download/v2.2/dxvk-2.2.tar.gz";
                break;
            

            case DxvkVersion.Disabled:
                env.Add("PROTON_USE_WINED3D", "1");
                env.Add("MANGHUD_DLSYM", "1");
                isDxvk = false;
                break;

            default:
                throw new ArgumentOutOfRangeException("Bad value for DxvkVersion");
        }

        if (isDxvk)
        {
            var dxvkCachePath = new DirectoryInfo(Path.Combine(dxvkfolder, "cache"));
            if (!dxvkCachePath.Exists) dxvkCachePath.Create();
            env.Add("DXVK_STATE_CACHE_PATH", Path.Combine(dxvkCachePath.FullName, folder));
        }

        var hudType = Program.Config.DxvkHudType;
        if (!isDxvk)
        {
            if (!Program.Config.WineD3DUseVK.Value)
                hudType = DxvkHudType.None;
            else if (new [] {DxvkHudType.Custom, DxvkHudType.Fps, DxvkHudType.Full}.Contains(Program.Config.DxvkHudType))
                hudType = DxvkHudType.None;
        }
        var dxvkHudCustom = Program.Config.DxvkHudCustom ?? "fps,frametimes,gpuload,version";
        var mangoHudConfig = string.IsNullOrEmpty(Program.Config.DxvkMangoCustom) ? null : new FileInfo(Program.Config.DxvkMangoCustom);
        switch (hudType)
        {
             case DxvkHudType.Fps:
                env.Add("DXVK_HUD","fps");
                env.Add("MANGOHUD","0");
                break;

            case DxvkHudType.Custom:
                if (!CheckDxvkHudString(Program.Config.DxvkHudCustom))
                    dxvkHudCustom = "fps,frametimes,gpuload,version";
                env.Add("DXVK_HUD", Program.Config.DxvkHudCustom);
                env.Add("MANGOHUD","0");
                break;

            case DxvkHudType.Full:
                env.Add("DXVK_HUD","full");
                env.Add("MANGOHUD","0");
                break;

            case DxvkHudType.MangoHud:
                env.Add("DXVK_HUD","0");
                env.Add("MANGOHUD","1");
                env.Add("MANGOHUD_CONFIG", "");
                break;

            case DxvkHudType.MangoHudCustom:
                env.Add("DXVK_HUD","0");
                env.Add("MANGOHUD","1");

                if (mangoHudConfig is null)
                {
                    var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var conf1 = Path.Combine(rootfolder, "MangoHud.conf");
                    var conf2 = Path.Combine(home, ".config", "MangoHud", "wine-ffxiv_dx11.conf");
                    var conf3 = Path.Combine(home, ".config", "MangoHud", "MangoHud.conf");
                    if (File.Exists(conf1))
                        mangoHudConfig = new FileInfo(conf1);
                    else if (File.Exists(conf2))
                        mangoHudConfig = new FileInfo(conf2);
                    else if (File.Exists(conf3))
                        mangoHudConfig = new FileInfo(conf3);
                }

                if (mangoHudConfig is not null && mangoHudConfig.Exists)
                    env.Add("MANGOHUD_CONFIGFILE", mangoHudConfig.FullName);
                else
                    env.Add("MANGOHUD_CONFIG", "");
                break;

            case DxvkHudType.MangoHudFull:
                env.Add("DXVK_HUD","0");
                env.Add("MANGOHUD","1");
                env.Add("MANGOHUD_CONFIG","full");
                break;

            case DxvkHudType.None:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        var settings = new DxvkSettings(folder, url, Program.storage.Root.FullName, env, isDxvk);
        return settings;
    }

    public static bool CheckDxvkHudString(string? customHud)
    {
        if (string.IsNullOrWhiteSpace(customHud)) return false;
        if (customHud == "1") return true;
        if (!Regex.IsMatch(customHud,ALLOWED_CHARS)) return false;

        string[] hudvars = customHud.Split(",");

        return hudvars.All(hudvar => Regex.IsMatch(hudvar, ALLOWED_WORDS));
    }
}

