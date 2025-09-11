using XIVLauncher.Common.Game.Patch.Acquisition;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabPatching : SettingsTab
{
    public override SettingsEntry[] Entries { get; } =
    {
        new SettingsEntry<DirectoryInfo>(Strings.PatchPathSetting, Strings.PatchPathSettingDescription, () => Program.Config.PatchPath, x => Program.Config.PatchPath = x)
        {
            CheckValidity = x =>
            {
                if (string.IsNullOrWhiteSpace(x?.FullName))
                    return Strings.PatchPathSettingNotSetValidation;

                return null;
            }
        },

        new SettingsEntry<AcquisitionMethod>(Strings.PatchDownloadMethodSetting, Strings.PatchDownloadMethodSettingDescription, () => Program.Config.PatchAcquisitionMethod ?? AcquisitionMethod.Aria,
            x => Program.Config.PatchAcquisitionMethod = x),
        new NumericSettingsEntry(Strings.MaximumSpeedSetting, Strings.MaximumSpeedSettingDescription, () => (int)Program.Config.PatchSpeedLimit,
            x => Program.Config.PatchSpeedLimit = x, 0, int.MaxValue, 1000),
        new SettingsEntry<bool>(Strings.KeepPatchesSetting, Strings.KeepPatchesSettingDescription, () => Program.Config.KeepPatches ?? false, x => Program.Config.KeepPatches = x),
    };

    public override string Title => Strings.PatchingTitle;
}
