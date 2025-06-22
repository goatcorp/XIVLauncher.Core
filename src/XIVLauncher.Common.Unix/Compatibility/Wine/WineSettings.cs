using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

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

        if (startupType == WineStartupType.Managed)
        {
            var wineDistroId = CompatUtil.GetWineIdForDistro();
            this.WineRelease = managedWine switch
            {
                WineManagedVersion.Stable => new WineStableRelease(wineDistroId),
                WineManagedVersion.Beta => new WineBetaRelease(wineDistroId),
                WineManagedVersion.Legacy => new WineLegacyRelease(wineDistroId),
                _ => throw new ArgumentOutOfRangeException(managedWine.ToString())
            };
        }

        this.CustomBinPath = customBinPath;
        this.EsyncOn = esyncOn ? "1" : "0";
        this.FsyncOn = fsyncOn ? "1" : "0";
        this.DebugVars = debugVars;
        this.LogFile = logFile;
        this.Prefix = prefix;
    }

    public static bool WineDLLOverrideIsValid(string dlls)
    {
        string[] invalid = { "msquic", "mscoree", "d3d9", "d3d11", "d3d10core", "dxgi" };
        var format = @"^(?:(?:[a-zA-Z0-9_\-\.]+,?)+=(?:n,b|b,n|n|b|d|,|);?)+$";

        if (string.IsNullOrEmpty(dlls)) return true;
        if (invalid.Any(s => dlls.Contains(s))) return false;
        if (Regex.IsMatch(dlls, format)) return true;

        return false;
    }
}
