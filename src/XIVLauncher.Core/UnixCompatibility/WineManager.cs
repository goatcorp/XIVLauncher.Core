using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;
using XIVLauncher.Common;
using XIVLauncher.Common.Unix.Compatibility;

namespace XIVLauncher.Core.UnixCompatibility;

public enum WineType
{
    [SettingsDescription("Managed by XIVLauncher", "Choose a patched version of wine made specifically for XIVLauncher")]
    Managed,

    [SettingsDescription("Custom", "Point XIVLauncher to a custom location containing wine binaries to run the game with.")]
    Custom,
}

public enum WineVersion
{
    [SettingsDescription("Wine-xiv 7.10 (Default)", "A patched version of Wine, based on 7.10. The current default.")]
    Wine7_10,

    [SettingsDescription("Wine-xiv 8.5", "A newer patched version of Wine-staging 8.5. May be faster, but less stable.")]
    Wine8_5,
}

public static class WineManager
{
    private static string package = "ubuntu";

    public static WineSettings GetSettings()
    {
        var isManaged = true;
        var winepath = "";
        var folder = "";
        var url = "";
        var version = Program.Config.WineVersion ?? WineVersion.Wine7_10;
        var wineLogFile = new FileInfo(Path.Combine(Program.storage.GetFolder("logs").FullName, "wine.log"));
        var winePrefix = Program.storage.GetFolder("wineprefix");

        switch (Program.Config.WineType ?? WineType.Managed)
        {
            case WineType.Custom:
                winepath = Program.Config.WineBinaryPath ?? "/usr/bin";
                isManaged = false;
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
                url = $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{package}-8.5.r4.g4211bac7.tar.xz";
                break;

            case WineVersion.Wine7_10:
                folder = "wine-xiv-staging-fsync-git-7.10.r3.g560db77d";
                url = $"https://github.com/goatcorp/wine-xiv-git/releases/download/7.10.r3.g560db77d/wine-xiv-staging-fsync-git-{package}-7.10.r3.g560db77d.tar.xz";
                break;

            default:
                throw new ArgumentOutOfRangeException("Bad value for WineVersion");
        }

        return new WineSettings(isManaged, winepath, folder, url, Program.storage.Root.FullName, Program.Config.WineDebugVars, wineLogFile, winePrefix, Program.Config.ESyncEnabled, Program.Config.FSyncEnabled);
    }

    private static void ParseOSRelease()
    {
        try
        {
            if (!File.Exists("/etc/os-release"))
            {
                package = "ubuntu";
                return;
            }
            var osRelease = File.ReadAllLines("/etc/os-release");
            var osInfo = new Dictionary<string, string>();
            foreach (var line in osRelease)
            {
                var keyValue = line.Split('=', 2);
                if (keyValue.Length == 1)
                    osInfo.Add(keyValue[0], "");
                else
                    osInfo.Add(keyValue[0], keyValue[1]);
            }

            foreach (var kvp in osInfo)
            {
                if (kvp.Value.ToLower().Contains("fedora"))
                    package = "fedora";
                if (kvp.Value.ToLower().Contains("tumbleweed"))
                    package = "fedora";
                if (kvp.Value.ToLower().Contains("ubuntu"))
                    package = "ubuntu";
                if (kvp.Value.ToLower().Contains("debian"))
                    package = "ubuntu";
                if (kvp.Value.ToLower().Contains("arch"))
                    package = "arch";
            }
        }
        catch (Exception ex)
        {
            // If there's any kind of error opening the file or even finding it, just go with default.
            Log.Error(ex, "There was an error while parsing /etc/os-release");
            package = "ubuntu";
        }
    }
}