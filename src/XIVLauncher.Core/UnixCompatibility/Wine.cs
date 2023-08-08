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

    public static string FolderName => Program.Config.WineVersion switch
    {
        WineVersion.Wine7_10 => "wine-xiv-staging-fsync-git-7.10.r3.g560db77d",
        WineVersion.Wine8_5 => "wine-xiv-staging-fsync-git-8.5.r4.g4211bac7",
        _ => throw new ArgumentOutOfRangeException(),
    };

    public static string DownloadUrl =>Program.Config.WineVersion switch
    {
                WineVersion.Wine7_10 => $"https://github.com/goatcorp/wine-xiv-git/releases/download/7.10.r3.g560db77d/wine-xiv-staging-fsync-git-{OSInfo.Package.ToString()}-7.10.r3.g560db77d.tar.xz",
                WineVersion.Wine8_5 => $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{OSInfo.Package.ToString()}-8.5.r4.g4211bac7.tar.xz",
                _ => throw new ArgumentOutOfRangeException(),
    };

    public static string DebugVars => Program.Config.WineDebugVars ?? "-all";

    public static FileInfo LogFile => new FileInfo(Path.Combine(Program.storage.GetFolder("logs").FullName, "wine.log"));

    public static DirectoryInfo Prefix => Program.storage.GetFolder("wineprefix");

    public static bool ESyncEnabled => Program.Config.ESyncEnabled ?? true;

    public static bool FSyncEnabled => Program.Config.FSyncEnabled ?? false;
}

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