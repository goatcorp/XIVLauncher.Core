using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabDXVK : SettingsTab
{
    private SettingsEntry<Dxvk.DxvkVersion> dxvkVersionSetting;
    private SettingsEntry<Dxvk.DxvkHudType> dxvkHudSetting;

    public SettingsTabDXVK()
    {
        Entries = new SettingsEntry[]
        {
            dxvkVersionSetting = new SettingsEntry<Dxvk.DxvkVersion>("DXVK Version", "Choose which version of DXVK to use.", () => Program.Config.DxvkVersion ?? Dxvk.DxvkVersion.v1_10_3, type => Program.Config.DxvkVersion = type)
            {
                CheckWarning = type =>
                {
                    if (new [] {Dxvk.DxvkVersion.v2_1, Dxvk.DxvkVersion.v2_2}.Contains(type))
                        return "May not work with pre-8.0 or non-proton wine builds. AMD users may need to use env variable RADV_PERFTEST=gpl";
                    return null;
                },
            },
            new SettingsEntry<bool>("Enable DXVK ASYNC", "Enable DXVK ASYNC patch.", () => Program.Config.DxvkAsyncEnabled ?? true, b => Program.Config.DxvkAsyncEnabled = b)
            {
                CheckVisibility = () => !(new [] {Dxvk.DxvkVersion.v2_1, Dxvk.DxvkVersion.v2_2}.Contains(dxvkVersionSetting.Value)),
                CheckWarning = b =>
                {
                    if (!b && dxvkVersionSetting.Value == Dxvk.DxvkVersion.v2_0)
                        return "AMD users may need to use env variable RADV_PERFTEST=gpl";
                    return null;
                },
            },
            dxvkHudSetting = new SettingsEntry<Dxvk.DxvkHudType>("DXVK Overlay", "DXVK Hud is included. MangoHud must be installed separately.\nFlatpak XIVLauncher needs flatpak MangoHud.", () => Program.Config.DxvkHudType, type => Program.Config.DxvkHudType = type)
            {
                CheckValidity = type =>
                {
                    if ((type == Dxvk.DxvkHudType.MangoHud || type == Dxvk.DxvkHudType.MangoHudCustom || type == Dxvk.DxvkHudType.MangoHudFull)
                        && (!File.Exists("/usr/lib/mangohud/libMangoHud.so") && !File.Exists("/usr/lib64/mangohud/libMangoHud.so") && !File.Exists("/usr/lib/extensions/vulkan/MangoHud/lib/x86_64-linux-gnu/libMangoHud.so")))
                        return "MangoHud not detected.";

                    return null;
                }
            },
            new SettingsEntry<string>("DXVK Hud Custom String", "Set a custom string for the built in DXVK Hud. Warning: If it's invalid, the game may hang.", () => Program.Config.DxvkHudCustom, s => Program.Config.DxvkHudCustom = s)
            {
                CheckVisibility = () => dxvkHudSetting.Value == Dxvk.DxvkHudType.Custom,
                CheckWarning = s =>
                {
                    if(!DxvkSettings.CheckDxvkHudString(s))
                        return "That's not a valid hud string";
                    return null;
                },
            },
            new SettingsEntry<string>("MangoHud Custom Path", "Set a custom path for MangoHud config file.", () => Program.Config.DxvkMangoCustom, s => Program.Config.DxvkMangoCustom = s)
            {
                CheckVisibility = () => dxvkHudSetting.Value == Dxvk.DxvkHudType.MangoHudCustom,
                CheckWarning = s =>
                {
                    if(!File.Exists(s))
                        return "That's not a valid file.";
                    return null;
                },
            },
            new NumericSettingsEntry("Frame Rate Limit", "Set a frame rate limit, and DXVK will try not exceed it. Use 0 for unlimited.", () => Program.Config.DxvkFrameRate ?? 0, i => Program.Config.DxvkFrameRate = i, 0, 1000),
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