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

public enum HudType
{
    [SettingsDescription("None", "Show nothing")]
    None,

    [SettingsDescription("DXVK Hud FPS", "Only show FPS")]
    Fps,

    [SettingsDescription("DXVK Hud Custom", "Use a custom DXVK_HUD string")]
    Custom,

    [SettingsDescription("DXVK Hud Full", "Show everything")]
    Full,

    [SettingsDescription("MangoHud Default", "Uses no config file.")]
    MangoHud,

    [SettingsDescription("MangoHud Custom", "Specify a custom config file")]
    MangoHudCustom,

    [SettingsDescription("MangoHud Full", "Show (almost) everything")]
    MangoHudFull,
}

public static class HudManager
{
    private const string ALLOWED_CHARS = "^[0-9a-zA-Z,=.]+$";

    private const string ALLOWED_WORDS = "^(?:devinfo|fps|frametimes|submissions|drawcalls|pipelines|descriptors|memory|gpuload|version|api|cs|compiler|samplers|scale=(?:[0-9])*(?:.(?:[0-9])+)?)$";

    public static Dictionary<string, string> GetSettings()
    {
        var rootfolder = Program.storage.Root.FullName;
        var env = new Dictionary<string, string>();
        var hudType = Program.Config.HudType;
        if (FindMangoHud() is null && new [] {HudType.MangoHud, HudType.MangoHudCustom, HudType.MangoHudFull}.Contains(hudType))
        {
            hudType = HudType.None;
            Program.Config.HudType = HudType.None;
        }
        var dxvkHudCustom = Program.Config.DxvkHudCustom ?? "fps,frametimes,gpuload,version";
        var mangoHudConfig = string.IsNullOrEmpty(Program.Config.MangoHudCustom) ? null : new FileInfo(Program.Config.MangoHudCustom);
        switch (hudType)
        {
             case HudType.Fps:
                env.Add("DXVK_HUD","fps");
                env.Add("MANGOHUD","0");
                break;

            case HudType.Custom:
                if (!CheckDxvkHudString(Program.Config.DxvkHudCustom))
                    dxvkHudCustom = "fps,frametimes,gpuload,version";
                env.Add("DXVK_HUD", Program.Config.DxvkHudCustom);
                env.Add("MANGOHUD","0");
                break;

            case HudType.Full:
                env.Add("DXVK_HUD","full");
                env.Add("MANGOHUD","0");
                break;

            case HudType.MangoHud:
                env.Add("DXVK_HUD","0");
                env.Add("MANGOHUD","1");
                env.Add("MANGOHUD_CONFIG", "");
                break;

            case HudType.MangoHudCustom:
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

            case HudType.MangoHudFull:
                env.Add("DXVK_HUD","0");
                env.Add("MANGOHUD","1");
                env.Add("MANGOHUD_CONFIG","full");
                break;

            case HudType.None:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return env;
    }

    public static bool CheckDxvkHudString(string? customHud)
    {
        if (string.IsNullOrWhiteSpace(customHud)) return false;
        if (customHud == "1") return true;
        if (!Regex.IsMatch(customHud,ALLOWED_CHARS)) return false;

        string[] hudvars = customHud.Split(",");

        return hudvars.All(hudvar => Regex.IsMatch(hudvar, ALLOWED_WORDS));
    }

    public static string? FindMangoHud()
    {
        var usrLib = Path.Combine("/usr", "lib", "mangohud", "libMangoHud.so"); // fedora uses this
        var usrLib64 = Path.Combine("/usr", "lib64", "mangohud", "libMangoHud.so"); // arch and openSUSE use this
        var flatpak = Path.Combine(new string[] { "/usr", "lib", "extensions", "vulkan", "lib", "x86_64-linux-gnu", "libMangoHud.so"});
        var debuntu = Path.Combine(new string[] { "/usr", "lib", "x86_64-linux-gnu", "mangohud", "libMangoHud.so"});
        if (File.Exists(usrLib64)) return usrLib64;
        if (File.Exists(usrLib)) return usrLib;
        if (File.Exists(flatpak)) return flatpak;
        if (File.Exists(debuntu)) return debuntu;
        return null;
    }
}