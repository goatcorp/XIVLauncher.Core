using XIVLauncher.Common.Dalamud;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabDalamud : SettingsTab
{
    public override string Title => Strings.DalamudTitle;
    public override SettingsEntry[] Entries { get; }
    private SettingsEntry<bool> enableManualInjection;

    public SettingsTabDalamud()
    {
        this.Entries = new SettingsEntry[]
        {

            new SettingsEntry<bool>(Strings.EnableDalamudSetting, Strings.EnableDalamudSettingDescription, () => Program.Config.DalamudEnabled ?? true, b => Program.Config.DalamudEnabled = b),

            new SettingsEntry<DalamudLoadMethod>(Strings.LoadMethodSetting, Strings.LoadMethodSettingDescription, () => Program.Config.DalamudLoadMethod ?? DalamudLoadMethod.DllInject, method => Program.Config.DalamudLoadMethod = method),

            new NumericSettingsEntry(Strings.DalamudInjectionDelaySetting, Strings.DalamudInjectionDelaySettingDescription, () => Program.Config.DalamudLoadDelay, delay => Program.Config.DalamudLoadDelay = delay, 0, int.MaxValue, 1000),

            enableManualInjection = new SettingsEntry<bool>(Strings.DalamudManualInjectionEnableSetting, Strings.DalamudManualInjectionEnableSettingDescription, () => Program.Config.DalamudManualInjectionEnabled ?? false, (enabled) =>
            {
                Program.Config.DalamudManualInjectionEnabled = enabled;

                if (!enabled)
                {
                    Program.DalamudUpdater.RunnerOverride = null;
                    return;
                }

                if (Program.Config.DalamudManualInjectPath is not null &&
                    Program.Config.DalamudManualInjectPath.Exists &&
                    Program.Config.DalamudManualInjectPath.GetFiles().FirstOrDefault(x => x.Name == Program.DALAMUD_INJECTOR_NAME) is not null)
                {
                    Program.DalamudUpdater.RunnerOverride = new FileInfo(Path.Combine(Program.Config.DalamudManualInjectPath.FullName, Program.DALAMUD_INJECTOR_NAME));
                }
            }),

            new SettingsEntry<DirectoryInfo>(Strings.DalamudLocalInjectionPathSetting, Strings.DalamudLocalInjectionPathSettingDescription, () => Program.Config.DalamudManualInjectPath, (input) =>
            {
                if (enableManualInjection.Value == false) return;
                if (input is null) return;
                Program.Config.DalamudManualInjectPath = input;
                Program.DalamudUpdater.RunnerOverride = new FileInfo(Path.Combine(input.FullName, Program.DALAMUD_INJECTOR_NAME));
            })
            {
                CheckVisibility = () => enableManualInjection.Value,
                CheckValidity = input =>
                {
                    if (input is null || !input.Exists)
                    {
                        return Strings.DalamudLocalInjectionPathSettingNoDirValidation;
                    }
                    else if (input.GetFiles().FirstOrDefault(x => x.Name == Program.DALAMUD_INJECTOR_NAME) is null)
                    {
                        return Strings.DalamudLocalInjectionPathSettingNoInjValidation;
                    }
                    return null;
                },
            },
        };
    }
}
