using System.Runtime.InteropServices;
using ImGuiNET;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabAutoStart : SettingsTab
{
    public override SettingsEntry[] Entries { get; } = new SettingsEntry[]
    {
        new SettingsEntry<string>("Pre-game script", 
            "Set a script that should be executed before the game launches",
            () => Program.Config.BeforeScript, s => Program.Config.BeforeScript = s),
        new SettingsEntry<bool>("Wait for pre-game script", "Wait for the pre-game script before launching the game",
            () => Program.Config.WaitForBeforeScript ?? false, s => Program.Config.WaitForBeforeScript = s),
        new SettingsEntry<string>("Pre-game Wine script", 
            "Set a script that should be executed in the Wine prefix before the game launches",
            () => Program.Config.BeforeScriptWine, s => Program.Config.BeforeScriptWine = s)
        {
            CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        },
        new SettingsEntry<bool>("Wait for pre-game Wine script", "Wait for the pre-game Wine script before launching the game",
            () => Program.Config.WaitForBeforeScriptWine ?? false, s => Program.Config.WaitForBeforeScriptWine = s)
        {
            CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        },
        new SettingsEntry<string>("Post-game script", 
            "Set a script that should be executed after the game has launched",
            () => Program.Config.AfterScript, s => Program.Config.AfterScript = s),
        new SettingsEntry<string>("Post-game Wine script",
            "Set a script that should be executed in the Wine prefix after the game has launched",
            () => Program.Config.AfterScriptWine, s => Program.Config.AfterScriptWine = s)
        {
            CheckVisibility = () => RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
        }
    };
    public override string Title => "Auto-Start";

    public override void Draw()
    {
        base.Draw();
    }
}