using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.UnixCompatibility;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabDxvk : SettingsTab
{
    private SettingsEntry<DxvkVersion> dxvkVersionSetting;
    private SettingsEntry<DxvkHud> dxvkHudSetting;
    private SettingsEntry<MangoHud> mangoHudSetting;

    public SettingsTabDxvk()
    {
        Entries = new SettingsEntry[]
        {
            dxvkVersionSetting = new SettingsEntry<DxvkVersion>("DXVK Version", "Choose which version of DXVK to use.", () => Program.Config.DxvkVersion ?? DxvkVersion.v1_10_3, type => Program.Config.DxvkVersion = type)
            {
                CheckWarning = type =>
                {
                    if (new [] {DxvkVersion.v2_1, DxvkVersion.v2_2}.Contains(type))
                        return "May not work with older graphics cards. AMD users may need to use env variable RADV_PERFTEST=gpl";
                    return null;
                },
            },
            new SettingsEntry<bool>("Enable DXVK ASYNC", "Enable DXVK ASYNC patch.", () => Program.Config.DxvkAsyncEnabled ?? true, b => Program.Config.DxvkAsyncEnabled = b)
            {
                CheckVisibility = () => (new [] {DxvkVersion.v1_10_3, DxvkVersion.v2_0}.Contains(dxvkVersionSetting.Value)),
                CheckWarning = b =>
                {
                    if (!b && dxvkVersionSetting.Value == DxvkVersion.v2_0)
                        return "AMD users may need to use env variable RADV_PERFTEST=gpl";
                    return null;
                },
            },
            dxvkHudSetting = new SettingsEntry<DxvkHud>("DXVK Overlay", "DXVK Hud is included with Dxvk. It doesn't work if Dxvk is disabled.", () => Program.Config.DxvkHud ?? DxvkHud.None, x => Program.Config.DxvkHud = x)
            {
                CheckVisibility = () => dxvkVersionSetting.Value != DxvkVersion.Disabled,
            },
            new SettingsEntry<string>("DXVK Hud Custom String", "Set a custom string for the built in DXVK Hud. Warning: If it's invalid, the game may hang.", () => Program.Config.DxvkHudCustom ?? Dxvk.DXVK_HUD, s => Program.Config.DxvkHudCustom = s)
            {
                CheckVisibility = () => dxvkHudSetting.Value == DxvkHud.Custom && dxvkVersionSetting.Value != DxvkVersion.Disabled,
                CheckWarning = s =>
                {
                    if(!DxvkSettings.DxvkHudStringIsValid(s))
                        return "That's not a valid hud string";
                    return null;
                },
            },
            mangoHudSetting = new SettingsEntry<MangoHud>("MangoHud Overlay", "MangoHud must be installed separately. Flatpak users need the flatpak version of MangoHud.", () => Program.Config.MangoHud ?? MangoHud.None, x => Program.Config.MangoHud = x)
            {
                CheckVisibility = () => dxvkVersionSetting.Value != DxvkVersion.Disabled && Dxvk.MangoHudInstalled,
                CheckWarning = x =>
                {
                    if (dxvkHudSetting.Value != DxvkHud.None && x != MangoHud.None)
                        return "You probably shouldn't run MangoHud and DxvkHud at the same time.";
                    return null;
                }
            },
            new SettingsEntry<string>("MangoHud Custom String", "Set a custom string for MangoHud config.", () => Program.Config.MangoHudCustomString ?? Dxvk.MANGOHUD_CONFIG, s => Program.Config.MangoHudCustomString = s)
            {
                CheckVisibility = () => mangoHudSetting.Value == MangoHud.CustomString && dxvkVersionSetting.Value != DxvkVersion.Disabled && Dxvk.MangoHudInstalled,
                CheckWarning = s =>
                {
                    if (s.Contains(' '))
                        return "No spaces allowed in MangoHud config";
                    return null;
                }
            },
            new SettingsEntry<string>("MangoHud Custom Path", "Set a custom path for MangoHud config file.", () => Program.Config.MangoHudCustomFile ?? Dxvk.MANGOHUD_CONFIGFILE, s => Program.Config.MangoHudCustomFile = s)
            {
                CheckVisibility = () => mangoHudSetting.Value == MangoHud.CustomFile && dxvkVersionSetting.Value != DxvkVersion.Disabled && Dxvk.MangoHudInstalled,
                CheckWarning = s =>
                {
                    if(!File.Exists(s))
                        return "That's not a valid file.";
                    return null;
                },
            },
            new NumericSettingsEntry("Frame Rate Limit", "Set a frame rate limit, and DXVK will try not exceed it. Use 0 for unlimited.", () => Program.Config.DxvkFrameRateLimit ?? 0, i => Program.Config.DxvkFrameRateLimit = i, 0, 1000)
            {
                CheckVisibility = () => dxvkVersionSetting.Value != DxvkVersion.Disabled,
            },
        };
    }

    public override SettingsEntry[] Entries { get; }

    public override bool IsUnixExclusive => true;

    public override string Title => "DXVK";

    public override void Save()
    {
        base.Save();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Program.CreateCompatToolsInstance();
    }
}
