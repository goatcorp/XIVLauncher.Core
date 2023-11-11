using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.Support;
using XIVLauncher.Core;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabTroubleshooting : SettingsTab
{
    public override SettingsEntry[] Entries { get; } =
    {
        new SettingsEntry<bool>("Hack: Disable gameoverlayrenderer.so", "Fixes some stuttering issues after 40+ minutes, but may affect steam overlay and input.", () => Program.Config.FixLDP ?? false, x => Program.Config.FixLDP = x),
        new SettingsEntry<bool>("Hack: XMODIFIERS=\"@im=null\"", "Fixes some mouse-related issues, some stuttering issues", () => Program.Config.FixIM ?? false, x => Program.Config.FixIM = x),
    };
    public override string Title => "Troubleshooting";

    private SettingsPage parentPage;

    public override void Draw()
    {
        base.Draw();

        ImGui.Separator();

        ImGui.Text("\nClear the Wine Prefix - delete the ~/.xlcore/wineprefix folder");
        if (ImGui.Button("Clear Prefix"))
        {
            Program.ClearPrefix();
        }

        ImGui.Text("\nClear the managed Wine and DXVK installs. Custom versions won't be touched.");
        if (ImGui.Button("Clear Wine & DXVK"))
        {
            Program.ClearTools(true);
        }

        ImGui.Text("\nClear all the files and folders related to Dalamud. This will not uninstall your plugins or their configurations.");
        if (ImGui.Button("Clear Dalamud"))
        {
            Program.ClearDalamud(true);
        }

        ImGui.Text("\nClear the installedPlugins folder. This will uninstall your plugins, but will not remove their configurations.");
        if (ImGui.Button("Clear Plugins"))
        {
            Program.ClearPlugins();
        }

        ImGui.Text("\nClear all the log files.");
        if (ImGui.Button("Clear Logs"))
        {
            Program.ClearLogs(true);
        }

        ImGui.Text("\nReset settings to default.");
        if (ImGui.Button("Clear Settings"))
        {
            Program.ClearSettings(true);
        }

        ImGui.Text("\nDo all of the above.");
        if (ImGui.Button("Clear Everything"))
        {
            Program.ClearAll(true);
        }

        ImGui.Text("\nOpen the .xlcore folder in your file browser.");
        if (ImGui.Button("Open .xlcore"))
        {
            PlatformHelpers.OpenBrowser(Program.storage.Root.FullName);
        }

        ImGui.Text("\nGenerate a troubleshooting pack to upload to the official Discord channel");
        if (ImGui.Button("Generate tspack"))
        {
            PackGenerator.SavePack(Program.storage);
            PlatformHelpers.OpenBrowser(Program.storage.GetFolder("logs").FullName);
        }
    }
}