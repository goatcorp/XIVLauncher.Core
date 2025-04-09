﻿using System.IO;
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
    private const string CURRENT_RELEASE = "8.5.r4.g4211bac7";
    private const string LEGACY_RELEASE = "8.5.r4.g4211bac7";
    private string CURRENT_RELEASE_URL => $"https://github.com/goatcorp/wine-xiv-git/releases/download/{CURRENT_RELEASE}/wine-xiv-staging-fsync-git-{DISTRO}-{CURRENT_RELEASE}.tar.xz";
    private string LEGACY_RELEASE_URL => $"https://github.com/goatcorp/wine-xiv-git/releases/download/{LEGACY_RELEASE}/wine-xiv-staging-fsync-git-{DISTRO}-{LEGACY_RELEASE}.tar.xz";

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