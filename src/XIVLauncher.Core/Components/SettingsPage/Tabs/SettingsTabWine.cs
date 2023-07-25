using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.UnixCompatibility;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabWine : SettingsTab
{
    private SettingsEntry<WineType> wineTypeSetting;

    public SettingsTabWine()
    {
        Entries = new SettingsEntry[]
        {
            wineTypeSetting = new SettingsEntry<WineType>("Installation Type", "Choose how XIVLauncher will start and manage your game installation.",
                () => Program.Config.WineType ?? WineType.Managed, x => Program.Config.WineType = x),

            new SettingsEntry<WineVersion>("Wine Version", "Choose a patched wine version.", () => Program.Config.WineVersion ?? WineVersion.Wine7_10, x => Program.Config.WineVersion = x)
            {
                CheckVisibility = () => wineTypeSetting.Value == WineType.Managed
            },
            
            new SettingsEntry<string>("Wine Binary Path",
                "Set the path XIVLauncher will use to run applications via wine.\nIt should be an absolute path to a folder containing wine64 and wineserver binaries.",
                () => Program.Config.WineBinaryPath, s => Program.Config.WineBinaryPath = s)
            {
                CheckVisibility = () => wineTypeSetting.Value == WineType.Custom
            },

            new SettingsEntry<bool>("Enable Feral's GameMode", "Enable launching with Feral Interactive's GameMode CPU optimizations.", () => Program.Config.GameModeEnabled ?? true, b => Program.Config.GameModeEnabled = b)
            {
                CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                CheckValidity = b =>
                {
                    if (b == true && (!File.Exists("/usr/lib/libgamemodeauto.so.0") && !File.Exists("/app/lib/libgamemodeauto.so.0")))
                        return "GameMode was not detected on your system.";

                    return null;
                }
            },

            new SettingsEntry<bool>("Enable DXVK ASYNC", "Enable DXVK ASYNC patch.", () => Program.Config.DxvkAsyncEnabled ?? true, b => Program.Config.DxvkAsyncEnabled = b),
            new SettingsEntry<bool>("Enable ESync", "Enable eventfd-based synchronization.", () => Program.Config.ESyncEnabled ?? true, b => Program.Config.ESyncEnabled = b),
            new SettingsEntry<bool>("Enable FSync", "Enable fast user mutex (futex2).", () => Program.Config.FSyncEnabled ?? true, b => Program.Config.FSyncEnabled = b)
            {
                CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux),
                CheckValidity = b =>
                {
                    if (b == true && (Environment.OSVersion.Version.Major < 5 && (Environment.OSVersion.Version.Minor < 16 || Environment.OSVersion.Version.Major < 6)))
                        return "Linux kernel 5.16 or higher is required for FSync.";

                    return null;
                }
            },

            new SettingsEntry<bool>("Set Windows version to 7", "Default for Wine 8.1+ is Windows 10, but this causes issues with some Dalamud plugins. Windows 7 is recommended for now.", () => Program.Config.SetWin7 ?? true, b => Program.Config.SetWin7 = b),

            new SettingsEntry<DxvkHud>("DXVK Overlay", "Configure how much of the DXVK overlay is to be shown.", () => Program.Config.DxvkHud ?? DxvkHud.None, type => Program.Config.DxvkHud = type),
            new SettingsEntry<string>("WINEDEBUG Variables", "Configure debug logging for wine. Useful for troubleshooting.", () => Program.Config.WineDebugVars ?? string.Empty, s => Program.Config.WineDebugVars = s)
        };
    }

    public override SettingsEntry[] Entries { get; }

    public override bool IsUnixExclusive => true;

    public override string Title => "Wine";

    public override void Draw()
    {
        base.Draw();

        if (!Program.CompatibilityTools.IsToolDownloaded)
        {
            ImGui.BeginDisabled();
            ImGui.Text("Compatibility tool isn't set up. Please start the game at least once.");

            ImGui.Dummy(new Vector2(10));
        }

        if (ImGui.Button("Open prefix"))
        {
            PlatformHelpers.OpenBrowser(Program.CompatibilityTools.Settings.Prefix.FullName);
        }

        ImGui.SameLine();

        if (ImGui.Button("Open Wine configuration"))
        {
            Program.CompatibilityTools.RunInPrefix("winecfg");
        }

        ImGui.SameLine();

        if (ImGui.Button("Open Wine explorer (run apps in prefix)"))
        {
            Program.CompatibilityTools.RunInPrefix("explorer");
        }

        if (ImGui.Button("Kill all wine processes"))
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
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Program.CreateCompatToolsInstance();
    }
}
