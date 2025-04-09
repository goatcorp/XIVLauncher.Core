using System.IO;
using System;

namespace XIVLauncher.Common.Unix.Compatibility;

public enum WineStartupType
{
    [SettingsDescription("Managed by XIVLauncher", "The game installation and wine setup is managed by XIVLauncher - you can leave it up to us.")]
    Managed,

    [SettingsDescription("Custom", "Point XIVLauncher to a custom location containing wine binaries to run the game with.")]
    Custom,
}

public enum WineManagedVersion
{
    [SettingsDescription("Current", "Based on Wine 10.5, with patches for Dalamud and XIVLauncher")]
    Current,
    
    [SettingsDescription("Legacy", "Based on Wine 8.5. Useful for certain third-party plugins")]
    Legacy,
}

public class WineSettings
{
    public WineStartupType StartupType { get; private set; }
#if WINE_XIV_ARCH_LINUX
    private const string DISTRO = "arch";
#elif WINE_XIV_FEDORA_LINUX
    private const string DISTRO = "fedora";
#else
    private const string DISTRO = "ubuntu";
#endif
    // These lines need to be changed to point at an official release
    private const string CURRENT_RELEASE = "wine-xiv-staging-fsync-git-10.5.r0.g835c92a2";
    private string CURRENT_RELEASE_URL => $"https://github.com/rankynbass/unofficial-wine-xiv-git/releases/download/10.5.r0.g835c92a2/wine-xiv-staging-fsync-git-{DISTRO}-10.5.r0.g835c92a2.tar.xz";

    // These point to the current release, which will hopefully become the legacy release
    private const string LEGACY_RELEASE = "wine-xiv-staging-fsync-git-8.5.r4.g4211bac7";
    private string LEGACY_RELEASE_URL => $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{DISTRO}-8.5.r4.g4211bac7.tar.xz";

    public string ReleaseName { get; private set; }
    public string ReleaseUrl { get; private set; }
    
    public string CustomBinPath { get; private set; }

    public string EsyncOn { get; private set; }
    public string FsyncOn { get; private set; }

    public string DebugVars { get; private set; }
    public FileInfo LogFile { get; private set; }

    public DirectoryInfo Prefix { get; private set; }

    public WineSettings(WineStartupType? startupType, WineManagedVersion? managedWine, string customBinPath, string debugVars, FileInfo logFile, DirectoryInfo prefix, bool? esyncOn, bool? fsyncOn)
    {
        this.StartupType = startupType ?? WineStartupType.Custom;
        this.ReleaseName = managedWine switch
        {
            WineManagedVersion.Current => CURRENT_RELEASE,
            WineManagedVersion.Legacy => LEGACY_RELEASE,
            _ => throw new ArgumentOutOfRangeException(),
        };
        this.ReleaseUrl = managedWine switch
        {
            WineManagedVersion.Current => CURRENT_RELEASE_URL,
            WineManagedVersion.Legacy => LEGACY_RELEASE_URL,
            _ => throw new ArgumentOutOfRangeException(),
        };

        this.CustomBinPath = customBinPath;
        this.EsyncOn = (esyncOn ?? false) ? "1" : "0";
        this.FsyncOn = (fsyncOn ?? false) ? "1" : "0";
        this.DebugVars = debugVars;
        this.LogFile = logFile;
        this.Prefix = prefix;
    }
}