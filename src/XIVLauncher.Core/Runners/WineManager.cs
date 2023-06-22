using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using XIVLauncher.Common;
using XIVLauncher.Common.Unix.Compatibility;

namespace XIVLauncher.Core.Runners;

public enum WineType
{
    [SettingsDescription("Managed by XIVLauncher", "The game installation and wine setup is managed by XIVLauncher - you can leave it up to us.")]
    Managed,

    [SettingsDescription("User Managed", "Select from a list of patched wine versions. We'll download them for you.")]
    Other,

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
#if WINE_XIV_ARCH_LINUX
    private const string DISTRO = "arch";
#elif WINE_XIV_FEDORA_LINUX
    private const string DISTRO = "fedora";
#else
    private const string DISTRO = "ubuntu";
#endif

    public static WineRunner Initialize()
    {
        var winepath = "";
        var wineargs = "";
        var folder = "";
        var url = "";
        var prefix = new DirectoryInfo(Path.Combine(Program.storage.Root.FullName, "wineprefix"));
        var version = WineVersion.Wine8_5;
        switch (Program.Config.WineType ?? WineType.Managed)
        {
            case WineType.Custom:
                winepath = Program.Config.WineBinaryPath ?? "/usr/bin";
                break;

            case WineType.Managed:
                break;

            case WineType.Other:
                version = Program.Config.WineVersion ?? WineVersion.Wine8_5;
                break;
            
            default:
                throw new ArgumentOutOfRangeException("Bad value for WineVersion");
        }

        switch (version)
        {
            case WineVersion.Wine8_5:
                folder = "wine-xiv-staging-fsync-git-8.5.r4.g4211bac7";
                url = $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{DISTRO}-8.5.r4.g4211bac7.tar.xz";
                break;

            case WineVersion.Wine7_10:
                folder = "wine-xiv-staging-fsync-git-7.10.r3.g560db77d";
                url = $"https://github.com/goatcorp/wine-xiv-git/releases/download/7.10.r3.g560db77d/wine-xiv-staging-fsync-git-{DISTRO}-7.10.r3.g560db77d.tar.xz";
                break;

            default:
                throw new ArgumentOutOfRangeException("Bad value for WineVersion");
        }

        var env = new Dictionary<string, string>();
        if (Program.Config.GameModeEnabled ?? false)
        {
            var ldPreload = Environment.GetEnvironmentVariable("LD_PRELOAD") ?? "";
            if (!ldPreload.Contains("libgamemodeauto.so.0"))
                ldPreload = (ldPreload.Equals("")) ? "libgamemodeaudo.so" : ldPreload + ":libgamemodeauto.so.0";
            env.Add("LD_PRELOAD", ldPreload);
        }
        if (!string.IsNullOrEmpty(Program.Config.WineDebugVars))
            env.Add("WINEDEBUG", Program.Config.WineDebugVars);
        if (Program.Config.ESyncEnabled ?? true) env.Add("WINEESYNC", "1");
        if (Program.Config.FSyncEnabled ?? false) env.Add("WINEFSYNC", "1");
        
        return new WineRunner(winepath, wineargs, folder, url, Program.storage.Root, env);
    }
}




