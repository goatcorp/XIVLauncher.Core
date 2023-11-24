using System.Numerics;
using System.Runtime.InteropServices;
using System.IO;
using ImGuiNET;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Core;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabSteamTool : SettingsTab
{
    private SettingsEntry<string> steamPath;
    
    private SettingsEntry<string> steamFlatpakPath;

    private bool steamToolExists => File.Exists(Path.Combine(Program.Config.SteamPath ?? Path.Combine(CoreEnvironmentSettings.XDG_DATA_HOME, "Steam"), "compatibilitytools.d", "xlcore", "xlcore"));

    private bool steamFlatpakToolExists => File.Exists(Path.Combine(Program.Config.SteamFlatpakPath ?? Path.Combine(CoreEnvironmentSettings.HOME, ".var", "app", "com.valvesoftware.Steam", "data", "Steam" ), "compatibilitytools.d", "xlcore", "xlcore"));

    public SettingsTabSteamTool()
    {
        Entries = new SettingsEntry[]
        {
            steamPath = new SettingsEntry<string>("Steam Path (native install)", "Path to the native steam config files. Only change this if you have your steam config stored somewhere else.",
                () => Program.Config.SteamPath ?? Path.Combine(CoreEnvironmentSettings.XDG_DATA_HOME, "Steam"), s => Program.Config.SteamPath = s),

            steamFlatpakPath = new SettingsEntry<string>("Steam Path (flatpak install)", "Path to the flatpak steam config files. Only change this if you have your steam config stored somewhere else.",
                () => Program.Config.SteamFlatpakPath ?? Path.Combine(CoreEnvironmentSettings.HOME, ".var", "app", "com.valvesoftware.Steam", "data", "Steam" ), s => Program.Config.SteamFlatpakPath = s),           
        };
    }

    public override SettingsEntry[] Entries { get; }

    public override bool IsUnixExclusive => true;

    public override string Title => "Steam Tool";

    public override void Draw()
    {
        if (CoreEnvironmentSettings.IsSteamCompatTool)
        {
            ImGui.Dummy(new Vector2(10));
            ImGui.Text("You are currently running XIVLauncher.Core as a Steam compatibility tool.");
            ImGui.Dummy(new Vector2(10));
            ImGui.Text("If you are trying to upgrade, you must first update your local install of XIVLauncher.Core. Then launch the local version," +
                        "\nnavigate back to this tab, and re-install as a Steam compatibility tool.");
            return;
        }
        ImGui.Dummy(new Vector2(10));
        ImGui.Text("Use this tab to install XIVLauncher.Core as a Steam compatibility tool.");
        ImGui.Dummy(new Vector2(10));
        ImGui.Text("After you have installed XIVLauncher.Core as a Steam tool, close this program, and launch Steam. Select Final Fantasy XIV from the library,");
        ImGui.Text("and go to Compatibility. Force the use of a specific Steam Play compatibility tool, and choose XIVLauncher.Core as Compatibility Tool.");
        ImGui.Text("XIVLauncher.Core will now be used to launch Final Fantasy XIV. This feature can be used with Flatpak steam.");


        ImGui.Dummy(new Vector2(10));        
        ImGui.Separator();
        ImGui.Dummy(new Vector2(10));

        ImGui.Text($"Native Steam Tool status: {(steamToolExists ? "INSTALLED" : "Not Installed")}");
        ImGui.Dummy(new Vector2(10));
        if (ImGui.Button($"{(steamToolExists ? "Re-i" : "I")}nstall to native Steam"))
        {
            this.Save();
            SteamCompatibilityTool.CreateTool(Program.Config.SteamPath);
        }

        ImGui.Dummy(new Vector2(10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(10));

        ImGui.Text($"Flatpak Steam Tool status: {(steamFlatpakToolExists ? "INSTALLED" : "Not Installed")}");
#if !FLATPAK
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
        ImGui.Text("You are NOT running flatpak XIVLauncher.Core. You probably shouldn't install to Flatpak Steam. It may cause issues.");
        ImGui.PopStyleColor();
#endif
        ImGui.Dummy(new Vector2(10));
        if (ImGui.Button($"{(steamFlatpakToolExists ? "Re-i" : "I")}nstall to flatpak Steam"))
        {
            this.Save();
            SteamCompatibilityTool.CreateTool(Program.Config.SteamFlatpakPath);
        }

        ImGui.Dummy(new Vector2(10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(10));

        base.Draw();
    }

    public override void Save()
    {
        base.Save();
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Program.CreateCompatToolsInstance();
    }
}
