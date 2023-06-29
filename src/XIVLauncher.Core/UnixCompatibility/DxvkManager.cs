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

public static class DxvkManager
{
    public static DxvkSettings GetSettings()
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

        var hud = HudManager.GetSettings();
        foreach (var kvp in hud)
        {
            if (env.ContainsKey(kvp.Key))
                env[kvp.Key] = kvp.Value;
            else
                env.Add(kvp.Key, kvp.Value);
        }

        var settings = new DxvkSettings(folder, url, Program.storage.Root.FullName, env, isDxvk);
        return settings;
    }
}

