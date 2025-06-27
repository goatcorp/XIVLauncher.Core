using System;
using System.IO;

using XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

namespace XIVLauncher.Common.Unix.Compatibility.Wine;

public enum WineStartupType
{
    [SettingsDescription("Managed by XIVLauncher", "Wine setup is managed by XIVLauncher - you can leave it up to us.")]
    Managed,

    [SettingsDescription("Custom", "Point XIVLauncher to a custom location containing wine binaries to run the game with.")]
    Custom,
}

public enum WineManagedVersion
{
    [SettingsDescription("Stable", "Based on Wine 10.8 - recommended for most users.")]
    Stable,

    [SettingsDescription("LSteamClient", "Based on Wine 10.8 with lsteamclient patches. Better Steam integration.")]
    LSteamClient,

    [SettingsDescription("Legacy", "Based on Wine 8.5 - use for compatibility with some plugins.")]
    Legacy,
}

public class WineSettings
{
    public WineStartupType StartupType { get; private set; }
    public IWineRelease WineRelease { get; private set; }

    public string CustomBinPath { get; private set; }
    public string EsyncOn { get; private set; }
    public string FsyncOn { get; private set; }
    public string DebugVars { get; private set; }
    public FileInfo LogFile { get; private set; }
    public DirectoryInfo Prefix { get; private set; }

    public WineSettings(WineStartupType startupType, WineManagedVersion managedWine, string customBinPath, string debugVars, FileInfo logFile, DirectoryInfo prefix, bool esyncOn, bool fsyncOn)
    {
        this.StartupType = startupType;

        var wineDistroId = CompatUtil.GetWineIdForDistro();
        switch (managedWine)
        {
            case WineManagedVersion.Stable:
                this.WineRelease = new WineStableRelease(wineDistroId);
                break;
            case WineManagedVersion.LSteamClient:
                this.WineRelease = new WineLSteamClientRelease(wineDistroId);
                break;
            case WineManagedVersion.Legacy:
                this.WineRelease = new WineLegacyRelease(wineDistroId);
                break;
            default:
                throw new ArgumentOutOfRangeException(managedWine.ToString());
        }
        this.CustomBinPath = customBinPath;
        this.EsyncOn = esyncOn ? "1" : "0";
        this.FsyncOn = fsyncOn ? "1" : "0";
        this.DebugVars = debugVars;
        this.LogFile = logFile;
        this.Prefix = prefix;
    }
}
