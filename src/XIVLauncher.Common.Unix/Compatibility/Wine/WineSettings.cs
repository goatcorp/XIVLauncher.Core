using System;
using System.IO;

using XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

namespace XIVLauncher.Common.Unix.Compatibility.Wine;

public enum WineStartupType
{
    [SettingsDescription("Managed by XIVLauncher", "WINE setup is managed by XIVLauncher - you can leave it up to us.")]
    Managed,

    [SettingsDescription("Custom", "Point XIVLauncher to a custom location containing WINE binaries to run the game with.")]
    Custom,
}

public enum WineManagedVersion
{
    [SettingsDescription("Stable", "The most stable experience, based on WINE 10.8 - recommended for most users.")]
    Stable,

    [SettingsDescription("Beta", "Testing ground for the newest wine changes, based on WINE 10.8 with lsteamclient patches.")]
    Beta,

    [SettingsDescription("Legacy", "For older systems or compatibility, based on WINE 8.5.")]
    Legacy,
}

public enum WineSyncType
{
    [SettingsDescription("ESync", "Eventfd-based synchronization - recommended for older systems.")]
    ESync,
    [SettingsDescription("FSync", "Fast user mutex (futex2)-based synchronization, requires Linux Kernel 5.16+ - recommended for most users.")]
    FSync,
}

public readonly struct WineSettings
{
    public WineStartupType StartupType { get; init; }
    public IWineRelease Release { get; init; }
    public WineSyncType SyncType { get; init; }
    public string CustomBinPath { get; init; }
    public string DebugVars { get; init; }
    public FileInfo LogFile { get; init; }
    public DirectoryInfo Prefix { get; init; }

    public WineSettings(WineStartupType startupType, WineManagedVersion managedWine, string customBinPath, string debugVars, FileInfo logFile, DirectoryInfo prefix, WineSyncType wineSyncType)
    {
        this.StartupType = startupType;
        this.SyncType = wineSyncType;
        this.CustomBinPath = customBinPath;
        this.DebugVars = debugVars;
        this.LogFile = logFile;
        this.Prefix = prefix;
        if (startupType == WineStartupType.Managed)
        {
            var wineDistroId = CompatUtil.GetWineIdForDistro();
            this.Release = managedWine switch
            {
                WineManagedVersion.Stable => new WineStableRelease(wineDistroId),
                WineManagedVersion.Beta => new WineBetaRelease(wineDistroId),
                WineManagedVersion.Legacy => new WineLegacyRelease(wineDistroId),
                _ => throw new ArgumentOutOfRangeException(managedWine.ToString())
            };
        }
    }
}
