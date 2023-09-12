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

        ImGui.Text("\nReset settings to default.");
        if (ImGui.Button("Clear Settings"))
        {
            Program.ClearSettings(true);
        }        

        ImGui.Text("\nClear the Wine Prefix - delete the ~/.xlcore/wineprefix folder");
        if (ImGui.Button("Clear Prefix"))
        {
            Program.ClearPrefix();
        }

        ImGui.Text("\nClear the managed Wine install and DXVK");
        if (ImGui.Button("Clear Wine & DXVK"))
        {
            Program.ClearTools(true);
        }

        ImGui.Text("\nClear all the files and folders related to Dalamud. Your settings will not be touched,\nbut all your plugins will be uninstalled, including custom repos.");
        if (ImGui.Button("Clear Dalamud"))
        {
            Program.ClearPlugins(true);
        }

        ImGui.Text("\nClear all the log files.");
        if (ImGui.Button("Clear Logs"))
        {
            Program.ClearLogs(true);
        }

        ImGui.Text("\nDo all of the above.");
        if (ImGui.Button("Clear Everything"))
        {
            Program.ClearAll(true);
        }

        ImGui.Text("\nGenerate a troubleshooting pack to upload to the official Discord channel");
        if (ImGui.Button("Generate tspack"))
        {
            PackGenerator.SavePack(Program.storage);
            PlatformHelpers.OpenBrowser(Program.storage.GetFolder("logs").FullName);
        }
    }
}