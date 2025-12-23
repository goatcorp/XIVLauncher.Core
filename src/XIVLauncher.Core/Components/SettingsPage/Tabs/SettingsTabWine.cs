using System.Numerics;
using System.Runtime.InteropServices;

using ImGuiNET;

using XIVLauncher.Common.Unix.Compatibility.Dxvk;
using XIVLauncher.Common.Unix.Compatibility.Nvapi;
using XIVLauncher.Common.Unix.Compatibility.Wine;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabWine : SettingsTab
{
    private SettingsEntry<WineStartupType> startupTypeSetting;

    private SettingsEntry<WineManagedVersion> wineVersionSetting;

    private SettingsEntry<DxvkVersion> dxvkVersionSetting;

    public SettingsTabWine()
    {
        Entries = new SettingsEntry[]
        {
            startupTypeSetting = new SettingsEntry<WineStartupType>(Strings.WineInstallSetting, Strings.WineInstallSettingDescription,
                () => Program.Config.WineStartupType ?? WineStartupType.Managed, x => Program.Config.WineStartupType = x),

            wineVersionSetting = new SettingsEntry<WineManagedVersion>(Strings.WineVersionSetting, Strings.WineVersionSettingDescription, () => Program.Config.WineManagedVersion ?? WineManagedVersion.Stable,
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

            new SettingsEntry<NvapiVersion>(Strings.NvapiVersionSetting, Strings.NvapiVersionSettingDescription, () => Program.Config.NvapiVersion ?? NvapiVersion.Stable, x => Program.Config.NvapiVersion = x)
            {
                CheckVisibility = () => dxvkVersionSetting.Value != DxvkVersion.Disabled,
                CheckWarning = x =>
                {
                    string warning = "";
                    if (dxvkVersionSetting.Value == DxvkVersion.Legacy)
                        warning += Strings.NvapiLegacyDxvkWarning + "\n";
                    if (startupTypeSetting.Value == WineStartupType.Custom)
                        warning += Strings.NvapiCustomWineWarning;
                    else if (wineVersionSetting.Value == WineManagedVersion.Legacy)
                        warning += Strings.NvapiLegacyWineWarning;

                    warning = warning.Trim();
                    
                    return string.IsNullOrEmpty(warning) ? null : warning;
                }
            },

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
