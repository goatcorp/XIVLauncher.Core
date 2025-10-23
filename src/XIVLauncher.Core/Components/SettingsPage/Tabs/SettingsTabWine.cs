using System.Numerics;
using System.Runtime.InteropServices;


#if HEXA
using Hexa.NET.ImGui;
#endif
#if VELDRID
using ImGuiNET;
#endif

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

            dxvkVersionSetting = new SettingsEntry<DxvkVersion>(Strings.DXVKVersionSetting, Strings.DXVKVersionSettingDescription, () => Program.Config.DxvkVersion ?? DxvkVersion.Stable, x => Program.Config.DxvkVersion = x),

            new SettingsEntry<bool>(Strings.DXVKEnableAsyncSetting, Strings.DXVKEnableAsyncSettingDescription, () => Program.Config.DxvkAsyncEnabled ?? true, b => Program.Config.DxvkAsyncEnabled = b)
            {
                CheckVisibility = () => dxvkVersionSetting.Value != DxvkVersion.Disabled
            },

            new SettingsEntry<bool>(Strings.EnableFeralGameModeSetting, Strings.EnableFeralGameModeSettingDescription, () => Program.Config.GameModeEnabled ?? true, b => Program.Config.GameModeEnabled = b)
            {
                CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                CheckValidity = b =>
                {
                    var handle = IntPtr.Zero;
                    if (b == true && !NativeLibrary.TryLoad("libgamemodeauto.so.0", out handle))
                        return Strings.EnableFeralGameModeNotFoundValidation;
                    NativeLibrary.Free(handle);
                    return null;
                }
            },

            new SettingsEntry<bool>(Strings.EnableESyncSetting, Strings.EnableESyncSettingDescription, () => Program.Config.ESyncEnabled ?? true, b => Program.Config.ESyncEnabled = b),
            new SettingsEntry<bool>(Strings.EnableFSyncSetting, Strings.EnableFSyncSettingDescription, () => Program.Config.FSyncEnabled ?? true, b => Program.Config.FSyncEnabled = b)
            {
                CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                CheckValidity = b =>
                {
                    if (b == true && (Environment.OSVersion.Version.Major < 5 && (Environment.OSVersion.Version.Minor < 16 || Environment.OSVersion.Version.Major < 6)))
                        return Strings.EnableFSyncSettingMinKernelValidation;

                    return null;
                }
            },

            new SettingsEntry<bool>(Strings.SetWindows7Setting, Strings.SetWindows7SettingDescription, () => Program.Config.SetWin7 ?? true, b => Program.Config.SetWin7 = b),

            new SettingsEntry<DxvkHudType>(Strings.EnableDXVKOverlaySetting, Strings.EnableDXVKOverlaySettingDescription, () => Program.Config.DxvkHudType, type => Program.Config.DxvkHudType = type),
            new SettingsEntry<string>(Strings.WineDebugAdditionalVarSetting, Strings.WineDebugAdditionalVarSettingDescription, () => Program.Config.WineDebugVars ?? string.Empty, s => Program.Config.WineDebugVars = s)
        };
    }

    public override SettingsEntry[] Entries { get; }

    public override bool IsUnixExclusive => true;

    public override string Title => Strings.WineTitle;

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
