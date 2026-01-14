using System.Numerics;
using System.Runtime.InteropServices;

using Hexa.NET.ImGui;

using XIVLauncher.Common.Unix.Compatibility.Dxvk;
using XIVLauncher.Common.Unix.Compatibility.Wine;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabWine : SettingsTab
{
    private SettingsEntry<WineStartupType> startupTypeSetting;

    private SettingsEntry<DxvkVersion> dxvkVersionSetting;

    public SettingsTabWine()
    {
        Entries = new SettingsEntry[]
        {
            // WINE
            startupTypeSetting = new SettingsEntry<WineStartupType>(Strings.WineInstallSetting, Strings.WineInstallSettingDescription,
                () => Program.Config.WineStartupType ?? WineStartupType.Managed, x => Program.Config.WineStartupType = x),
            new SettingsEntry<WineManagedVersion>(Strings.WineVersionSetting, Strings.WineVersionSettingDescription, () => Program.Config.WineManagedVersion ?? WineManagedVersion.Stable,
                x => Program.Config.WineManagedVersion = x )
            {
                CheckVisibility = () => startupTypeSetting.Value == WineStartupType.Managed
            },
            new SettingsEntry<string>(Strings.WineBinaryPathSetting,
                Strings.WineBinarySettingDescription,
                () => Program.Config.WineBinaryPath, s => Program.Config.WineBinaryPath = s)
            {
                CheckVisibility = () => startupTypeSetting.Value == WineStartupType.Custom
            },
            new SettingsEntry<WineSyncType>("WINE Sync Method", "How Wine synchronizes multi-threaded operations.", () => Program.Config.WineSyncType ?? WineSyncType.FSync, x => Program.Config.WineSyncType = x)
            {
                CheckValidity = b =>
                {
                    switch (WineUtility.SystemFsyncSupport())
                    {
                        case FsyncSupport.UnsupportedPlatform:
                            return "FSync is only available on Linux";
                        case FsyncSupport.OutdatedKernel:
                            return Strings.EnableFSyncSettingMinKernelValidation;
                        case FsyncSupport.Supported:
                        default:
                            return null;
                    }
                }
            },
            new SettingsEntry<string>(Strings.WineDebugAdditionalVarSetting, Strings.WineDebugAdditionalVarSettingDescription, () => Program.Config.WineDebugVars ?? string.Empty, s => Program.Config.WineDebugVars = s),

            // DXVK
            dxvkVersionSetting = new SettingsEntry<DxvkVersion>(Strings.DXVKVersionSetting, Strings.DXVKVersionSettingDescription, () => Program.Config.DxvkVersion ?? DxvkVersion.Stable, x => Program.Config.DxvkVersion = x),
            new SettingsEntry<DxvkHudType>(Strings.EnableDXVKOverlaySetting, Strings.EnableDXVKOverlaySettingDescription, () => Program.Config.DxvkHudType, type => Program.Config.DxvkHudType = type),
            new SettingsEntry<bool>(Strings.DXVKEnableAsyncSetting, Strings.DXVKEnableAsyncSettingDescription, () => Program.Config.DxvkAsyncEnabled ?? true, b => Program.Config.DxvkAsyncEnabled = b)
            {
                CheckVisibility = () => dxvkVersionSetting.Value != DxvkVersion.Disabled
            },

            // GameMode
            new SettingsEntry<bool>(Strings.EnableFeralGameModeSetting, Strings.EnableFeralGameModeSettingDescription, () => Program.Config.GameModeEnabled ?? true, b => Program.Config.GameModeEnabled = b)
            {
                CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                CheckValidity = b =>
                {
                    if (b == true && FeralGameModeFound == false)
                        return Strings.EnableFeralGameModeNotFoundValidation;
                    return null;
                }
            },
        };
    }

    public override SettingsEntry[] Entries { get; }

    public override bool IsUnixExclusive => true;

    public override string Title => Strings.WineTitle;

    private bool? feralGameModeFound = null;

    private bool FeralGameModeFound
    {
        get
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return false;
            if (feralGameModeFound != null) return feralGameModeFound ?? false;
            var handle = IntPtr.Zero;
            feralGameModeFound = (NativeLibrary.TryLoad("libgamemodeauto.so.0", out handle));
            NativeLibrary.Free(handle);
            return feralGameModeFound ?? false;
        }
    }

    public override void Draw()
    {
        base.Draw();

        if (!Program.CompatibilityTools.IsToolDownloaded)
        {
            ImGui.BeginDisabled();
            ImGui.Text(Strings.CompatibilityToolNotSetup);

            ImGui.Dummy(new Vector2(10));
        }

        if (ImGui.Button(Strings.OpenWINEPrefix))
        {
            PlatformHelpers.OpenBrowser(Program.CompatibilityTools.Settings.Prefix.FullName);
        }

        ImGui.SameLine();

        if (ImGui.Button(Strings.OpenWINEConfiguration))
        {
            Program.CompatibilityTools.RunInPrefix("winecfg");
        }

        ImGui.SameLine();

        if (ImGui.Button(Strings.OpenWINEExplorer))
        {
            Program.CompatibilityTools.RunInPrefix("explorer");
        }

        if (ImGui.Button(Strings.KillAllWINEProcesses))
        {
            Program.CompatibilityTools.Kill();
        }

        if (!Program.CompatibilityTools.IsToolDownloaded)
        {
            ImGui.EndDisabled();
        }
    }

    public override void Save()
    {
        base.Save();
        Program.CreateCompatToolsInstance();
    }
}
