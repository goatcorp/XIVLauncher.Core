using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.Support;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabTroubleshooting : SettingsTab
{
    public override SettingsEntry[] Entries => Array.Empty<SettingsEntry>();
    public override string Title => "Troubleshooting";

    public override void Draw()
    {
        ImGui.Text("\nClear the Wine Prefix - delete the ~/.xlcore/wineprefix folder");
        if (ImGui.Button("Clear Prefix"))
        {
            Program.ClearPrefix();
        }

        ImGui.Text("\n\nClear the managed Wine install and DXVK");
        if (ImGui.Button("Clear Wine & DXVK"))
        {
            Program.ClearTools();
        }

        ImGui.Text("\n\nClear all the files and folders related to Dalamud. Your settings will not be touched,\nbut all your plugins will be uninstalled, including 3rd-party repos.");
        if (ImGui.Button("Clear Dalamud"))
        {
            Program.ClearPlugins(true);
        }

        ImGui.Text("\n\nClear all the log files.");
        if (ImGui.Button("Clear Logs"))
        {
            Program.ClearLogs();
        }

        ImGui.Text("\n\nDo all of the above.");
        if (ImGui.Button("Clear Everything"))
        {
            Program.ClearAll();
        }

        ImGui.Text("\n\nGenerate a troubleshooting pack to upload to the official Discord channel");
        if (ImGui.Button("Generate tspack"))
        {
            PackGenerator.SavePack(Program.storage);
            PlatformHelpers.OpenBrowser(Program.storage.GetFolder("logs").FullName);
        }

        base.Draw();
    }
}