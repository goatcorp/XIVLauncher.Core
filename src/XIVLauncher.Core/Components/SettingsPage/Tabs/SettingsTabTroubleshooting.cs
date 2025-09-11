using ImGuiNET;

using XIVLauncher.Common.Util;
using XIVLauncher.Core.Resources.Localization;
using XIVLauncher.Core.Support;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabTroubleshooting : SettingsTab
{
    public override SettingsEntry[] Entries { get; } =
    {
        new SettingsEntry<bool>(Strings.DisableGameoverlayRendererHack, Strings.DisableGameoverlayRendererHackDescription, () => Program.Config.FixLDP ?? false, x => Program.Config.FixLDP = x)
        {
            CheckVisibility = () => Environment.OSVersion.Platform == PlatformID.Unix
        },
        new SettingsEntry<bool>(Strings.XModifiersHack, Strings.XModifiersHackDescription, () => Program.Config.FixIM ?? false, x => Program.Config.FixIM = x)
        {
            CheckVisibility = () => Environment.OSVersion.Platform == PlatformID.Unix
        },
        new SettingsEntry<bool>(Strings.FixLibICUUCErrorHack, Strings.FixLibICUUCErrorHackDescription, () => Program.Config.FixError127 ?? false, x => Program.Config.FixError127 = x)
        {
            CheckVisibility = () => Environment.OSVersion.Platform == PlatformID.Unix
        },
        new SettingsEntry<bool>(string.Format(Strings.ForceLocaleHack, (!string.IsNullOrEmpty(Program.CType) ? Program.CType : Strings.ForceLocaleHackCUTF8)),
                                !string.IsNullOrEmpty(Program.CType) ? string.Format(Strings.ForceLocaleHackDescription, Program.CType) : "Hack Disabled. Could not find a UTF-8 C locale. You may have to set LC_ALL manually if LANG is not a UTF-8 type.",
                                () => Program.Config.FixLocale ?? false, b => Program.Config.FixLocale = b)
        {
            CheckVisibility = () => Environment.OSVersion.Platform == PlatformID.Unix,
            CheckWarning = b =>
            {
                var lang = CoreEnvironmentSettings.GetCleanEnvironmentVariable("LANG");
                if (lang.ToUpper().Contains("UTF") && b)
                    return string.Format(Strings.ForceLocaleHackUTFValidation, lang);
                return null;
            },
        },
    };
    public override string Title => Strings.TroubleshootingTitle;

    public override void Draw()
    {
        ImGui.TextDisabled("Fixes");
        ImGui.Spacing();
        base.Draw();

        ImGui.Separator();
        ImGui.TextDisabled("Logs");
        ImGui.Spacing();
        if (ImGui.Button(Strings.GenerateTSPackTroubleshootingButton))
        {
            PackGenerator.SavePack(Program.storage);
            PlatformHelpers.OpenBrowser(Program.storage.GetFolder("logs").FullName);
        }
        ImGui.TextColored(ImGuiColors.DalamudGrey, Strings.GenerateTSPackTroubleshooting);

        ImGui.Separator();
        ImGui.TextDisabled("Cleanup");
        ImGui.TextColored(ImGuiColors.DalamudRed, Strings.TroubleshootingDestructiveActionWarning);
        ImGui.Spacing();

        if (ImGui.Button(Strings.ClearWINEPrefixTroubleshootingButton))
        {
            Program.ClearPrefix();
        }
        ImGui.TextColored(ImGuiColors.DalamudGrey, Strings.ClearWINEPrefixTroubleshooting);

        if (ImGui.Button(Strings.ClearManagedCompatToolsTroubleshootingButton))
        {
            Program.ClearTools(true);
        }
        ImGui.TextColored(ImGuiColors.DalamudGrey, Strings.ClearManagedCompatToolsTroubleshooting);

        if (ImGui.Button(Strings.ClearDalamudTroubleshootingButton))
        {
            Program.ClearPlugins(true);
        }
        ImGui.TextColored(ImGuiColors.DalamudGrey, Strings.ClearDalamudTroubleshooting);

        if (ImGui.Button(Strings.ClearAllLogsTroubleshootingButton))
        {
            Program.ClearLogs(true);
        }
        ImGui.TextColored(ImGuiColors.DalamudGrey, Strings.ClearAllLogsTroubleshooting);

        if (ImGui.Button(Strings.ResetSettingsTroubleshootingButton))
        {
            Program.ClearSettings(true);
        }
        ImGui.TextColored(ImGuiColors.DalamudGrey, Strings.ResetSettingsTroubleshooting);

        if (ImGui.Button(Strings.ResetAllTroubleshootingButton))
        {
            Program.ClearAll(true);
        }
        ImGui.TextColored(ImGuiColors.DalamudGrey, Strings.ResetAllTroubleshooting);
    }
}
