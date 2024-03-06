using XIVLauncher.Common.Dalamud;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabDalamud : SettingsTab
{
    public override string Title => "Dalamud";
    public override SettingsEntry[] Entries { get; }
    private SettingsEntry<bool> enableManualInjection;

    public SettingsTabDalamud()
    {
        this.Entries = new SettingsEntry[]
        {

            new SettingsEntry<bool>("Enable Dalamud", "Enable the Dalamud plugin system", () => Program.Config.DalamudEnabled ?? true, b => Program.Config.DalamudEnabled = b),

            new SettingsEntry<DalamudLoadMethod>("Load Method", "Choose how Dalamud is loaded.", () => Program.Config.DalamudLoadMethod ?? DalamudLoadMethod.DllInject, method => Program.Config.DalamudLoadMethod = method),

            new NumericSettingsEntry("Injection Delay (ms)", "Choose how long to wait after the game has loaded before injecting.", () => Program.Config.DalamudLoadDelay, delay => Program.Config.DalamudLoadDelay = delay, 0, int.MaxValue, 1000),

            enableManualInjection = new SettingsEntry<bool>("Enable Manual Injection", "Use a local build of Dalamud instead of the automatically provided one (For developers only!)", () => Program.Config.DalamudManualInjectionEnabled ?? false, (enabled) =>
             {
                 Program.Config.DalamudManualInjectionEnabled = enabled;

                 if (!enabled)
                 {
                     Program.DalamudUpdater.RunnerOverride = null;
                     return;
                 }

                 if (Directory.Exists(Program.Config.DalamudManualInjectPath) && Directory.GetFiles(Program.Config.DalamudManualInjectPath).FirstOrDefault(x => x == "Dalamud.Injector.exe") is not null)
                 {
                     Program.DalamudUpdater.RunnerOverride = new FileInfo(Path.Combine(Program.Config.DalamudManualInjectPath, Program.DALAMUD_INJECTOR_NAME));
                 }
             }),

            new SettingsEntry<string>("Manual Injection Path", "The path to the local version of Dalamud where Dalamud.Injector.exe is located", () => Program.Config.DalamudManualInjectPath, (input) =>
            {
                Program.Config.DalamudManualInjectPath = input;
                Program.DalamudUpdater.RunnerOverride = new FileInfo(Path.Combine(input, Program.DALAMUD_INJECTOR_NAME));
            })
            {
                CheckVisibility = () => enableManualInjection.Value == true,
                CheckValidity = input =>
                {
                    if (!Directory.Exists(input))
                    {
                        return "There is no directory at that path.";
                    }
                    if (Directory.GetFiles(input).FirstOrDefault(x => x == Program.DALAMUD_INJECTOR_NAME) is not null)
                    {
                        return "Dalamud.Injector.exe was not found inside of the provided directory.";
                    }
                    return null;
                },
            },
        };
    }
}