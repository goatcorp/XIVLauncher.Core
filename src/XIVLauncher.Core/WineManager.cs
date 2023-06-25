using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using XIVLauncher.Common;
using XIVLauncher.Common.Unix.Compatibility;

namespace XIVLauncher.Core;

public enum WineType
{
    [SettingsDescription("Managed by XIVLauncher", "Choose a patched version of wine made specifically for XIVLauncher")]
    Managed,

    [SettingsDescription("Custom", "Point XIVLauncher to a custom location containing wine binaries to run the game with.")]
    Custom,
}

public enum WineVersion
{
    [SettingsDescription("Wine-xiv 8.5", "A patched version of Wine-staging 8.5. The current default.")]
    Wine8_5,

    [SettingsDescription("Wine-xiv 7.10", "A legacy patched version of Wine, based on 7.10. A previous default")]
    Wine7_10,
}

public static class WineManager
{
    public static WineRunner Initialize()
    {
        var winepath = "";
        var wineargs = "";
        var folder = "";
        var url = "";
        var version = Program.Config.WineVersion ?? WineVersion.Wine8_5;
        switch (Program.Config.WineType ?? WineType.Managed)
        {
            case WineType.Custom:
                winepath = Program.Config.WineBinaryPath ?? "/usr/bin";
                break;

            case WineType.Managed:
                break;

            default:
                throw new ArgumentOutOfRangeException("Bad value for WineType");
        }

        switch (version)
        {
            case WineVersion.Wine8_5:
                folder = "wine-xiv-staging-fsync-git-8.5.r4.g4211bac7";
                url = $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{Program.Distro}-8.5.r4.g4211bac7.tar.xz";
                break;

            case WineVersion.Wine7_10:
                folder = "wine-xiv-staging-fsync-git-7.10.r3.g560db77d";
                url = $"https://github.com/goatcorp/wine-xiv-git/releases/download/7.10.r3.g560db77d/wine-xiv-staging-fsync-git-{Program.Distro}-7.10.r3.g560db77d.tar.xz";
                break;

            default:
                throw new ArgumentOutOfRangeException("Bad value for WineVersion");
        }

        var env = new Dictionary<string, string>();
        if (Program.Config.GameModeEnabled ?? false)
        {
            var ldPreload = Environment.GetEnvironmentVariable("LD_PRELOAD") ?? "";
            if (!ldPreload.Contains("libgamemodeauto.so.0"))
                ldPreload = (ldPreload.Equals("")) ? "libgamemodeauto.so.0" : ldPreload + ":libgamemodeauto.so.0";
            env.Add("LD_PRELOAD", ldPreload);
        }
        if (!string.IsNullOrEmpty(Program.Config.WineDebugVars))
            env.Add("WINEDEBUG", Program.Config.WineDebugVars);
        if (Program.Config.ESyncEnabled ?? true) env.Add("WINEESYNC", "1");
        if (Program.Config.FSyncEnabled ?? false) env.Add("WINEFSYNC", "1");
        env.Add("WINEPREFIX", Path.Combine(Program.storage.Root.FullName, "wineprefix"));
        
        return new WineRunner(winepath, wineargs, folder, url, Program.storage.Root.FullName, env);
    }

    private static string GetDistro()
    {
        if (File.Exists("/etc/arch-release")) return "arch";
        if (File.Exists("/etc/fedora-release")) return "fedora";
        return "ubuntu";
    }
}




