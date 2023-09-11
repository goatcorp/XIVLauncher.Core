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

public static class Wine
{
    public static bool IsManagedWine => Program.Config.WineType == WineType.Managed;

    public static string CustomWinePath => Program.Config.WineBinaryPath ?? "/usr/bin";

    public static string FolderName => Program.Config.WineVersion ?? GetDefaultVersion();

    public static string DownloadUrl => GetDownloadUrl(Program.Config.WineVersion);

    public static string DebugVars => Program.Config.WineDebugVars ?? "-all";

    public static FileInfo LogFile => new FileInfo(Path.Combine(Program.storage.GetFolder("logs").FullName, "wine.log"));

    public static DirectoryInfo Prefix => Program.storage.GetFolder("wineprefix");

    public static bool ESyncEnabled => Program.Config.ESyncEnabled ?? true;

    public static bool FSyncEnabled => Program.Config.FSyncEnabled ?? false;

    public static Dictionary<string, Dictionary<string, string>> Versions { get; private set; }

    static Wine()
    {
        Versions = new Dictionary<string, Dictionary<string, string>>()
        {
            { "wine-xiv-staging-fsync-git-7.10.r3.g560db77d", new Dictionary<string, string>()
                {   {"name", "Wine-XIV 7.10"}, {"desc","Patched version of Wine Staging 7.10. Default."},
                    {"label", "Official"}, {"url", $"https://github.com/goatcorp/wine-xiv-git/releases/download/7.10.r3.g560db77d/wine-xiv-staging-fsync-git-{OSInfo.Package.ToString()}-7.10.r3.g560db77d.tar.xz"},
                    {"mark", "Download"}    }   },
            { "wine-xiv-staging-fsync-git-8.5.r4.g4211bac7", new Dictionary<string, string>()
                {   {"name", "Wine-XIV 8.5"}, {"desc", "Patched version of Wine Staging 8.5. Change Windows version to 7 for best results."},
                    {"label", "Official"}, {"url", $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{OSInfo.Package.ToString()}-8.5.r4.g4211bac7.tar.xz"},
                    {"mark", "Download"}    }   },
        };
    }

    public static void Initialize()
    {
        var toolDirectory = new DirectoryInfo(Path.Combine(Program.storage.Root.FullName, "compatibilitytool", "wine"));

        if (!toolDirectory.Exists)
        {
            Program.storage.GetFolder("compatibilitytool/wine");
            return;
        }

        foreach (var wineDir in toolDirectory.EnumerateDirectories())
        {
            if (File.Exists(Path.Combine(wineDir.FullName, "bin", "wine64")) ||
                File.Exists(Path.Combine(wineDir.FullName, "bin", "wine")))
            {
                if (Versions.ContainsKey(wineDir.Name))
                {
                    Versions[wineDir.Name].Remove("mark");
                    continue;
                }
                Versions.Add(wineDir.Name, new Dictionary<string, string>() {{"label", "Custom"}});
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
        if (Versions.ContainsKey("wine-xiv-staging-fsync-git-7.10.r3.g560db77d"))
            return "wine-xiv-staging-fsync-git-7.10.r3.g560db77d";
        if (Versions.ContainsKey("wine-xiv-staging-fsync-git-8.5.r4.g4211bac7"))
            return "wine-xiv-staging-fsync-git-8.5.r4.g4211bac7";
        return Versions.First().Key;
    }
}

public enum WineType
{
    [SettingsDescription("Managed by XIVLauncher", "Choose a patched version of wine made specifically for XIVLauncher")]
    Managed,

    [SettingsDescription("Custom", "Point XIVLauncher to a custom location containing wine binaries to run the game with.")]
    Custom,
}