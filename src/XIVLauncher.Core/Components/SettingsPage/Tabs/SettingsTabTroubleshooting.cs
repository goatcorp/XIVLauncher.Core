using ImGuiNET;

using XIVLauncher.Common.Util;
using XIVLauncher.Core.Support;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabTroubleshooting : SettingsTab
{
    public override SettingsEntry[] Entries { get; } =
    {
        new SettingsEntry<bool>("Hack: Disable gameoverlayrenderer.so", "Fixes some stuttering issues after 40+ minutes, but may affect steam overlay and input.", () => Program.Config.FixLDP ?? false, x => Program.Config.FixLDP = x),
        new SettingsEntry<bool>("Hack: XMODIFIERS=\"@im=null\"", "Fixes some mouse-related issues, some stuttering issues", () => Program.Config.FixIM ?? false, x => Program.Config.FixIM = x),
        new SettingsEntry<bool>($"Hack: Force locale to {(!string.IsNullOrEmpty(Program.CType) ? Program.CType : "C.UTF-8 (exact value depends on distro)")}",
                                !string.IsNullOrEmpty(Program.CType) ? $"Sets LC_ALL and LC_CTYPE to \"{Program.CType}\". This can fix some issues with non-Latin unicode characters in file paths if LANG is not a UTF-8 type" : "Hack Disabled. Could not find a UTF-8 C locale. You may have to set LC_ALL manually if LANG is not a UTF-8 type.",
                                () => Program.Config.FixLocale ?? false, b => Program.Config.FixLocale = b)
        {
            CheckWarning = b =>
            {
                var lang = CoreEnvironmentSettings.GetCleanEnvironmentVariable("LANG");
                if (lang.ToUpper().Contains("UTF") && b)
                    return $"Your locale is \"{lang}\". You probably don't need this hack.";
                return null;
            }
        },
    };
    public override string Title => "Troubleshooting";

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
