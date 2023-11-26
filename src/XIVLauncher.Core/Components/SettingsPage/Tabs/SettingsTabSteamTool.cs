using System.Numerics;
using System.Runtime.InteropServices;
using System.IO;
using ImGuiNET;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.UnixCompatibility;

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
#if FLATPAK
            steamFlatpakPath = new SettingsEntry<string>("Steam Path (flatpak install)", "Path to the flatpak steam config files. Only change this if you have your steam config stored somewhere else.",
                () => Program.Config.SteamFlatpakPath ?? Path.Combine(CoreEnvironmentSettings.HOME, ".var", "app", "com.valvesoftware.Steam", "data", "Steam" ), s => Program.Config.SteamFlatpakPath = s),           
#endif
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
            ImGui.Text("If you are trying to upgrade, you must first update your local install of XIVLauncher.Core. Then launch the local" +
                        "\nversion, navigate back to this tab, and re-install as a Steam compatibility tool.");
            ImGui.Text("\nIf you are trying to uninstall, you should likewise launch the native version of XIVLauncher, and click the appropriate" +
                        "\nuninstall button.");
            return;
        }
        ImGui.Text("\nUse this tab to install XIVLauncher.Core as a Steam compatibility tool.");
        ImGui.Text("\nAfter you have installed XIVLauncher as a Steam tool, close this program, and launch Steam. Select Final Fantasy XIV from the library,");
        ImGui.Text("and go to Compatibility. Force the use of a specific Steam Play compatibility tool, and choose XIVLauncher.Core as Compatibility Tool.");
        ImGui.Text("XIVLauncher.Core will now be used to launch Final Fantasy XIV. This feature can be used with Flatpak steam.");
        ImGui.Text("\nIf you wish to install into Flatpak Steam, you must use Flatseal to give XIVLauncher access to Steam's flatpak path. This is probably something like:");
        ImGui.Text($"{CoreEnvironmentSettings.HOME}/.var/app/com.valvesoftware.Steam. If you do not give this permission, installation will fail. You will probably also want to");
        ImGui.Text($"give Steam permission to {CoreEnvironmentSettings.HOME}/.xlcore, so that you can continue to use your current xlcore folder.");

        ImGui.Dummy(new Vector2(10));        
        ImGui.Separator();
        ImGui.Dummy(new Vector2(10));

        if (!SteamCompatibilityTool.IsSteamInstalled)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.Text("\nWARNING! Steam install not detected. If it is installed, make sure you run it at least once.\n");
            ImGui.PopStyleColor();
        }

        ImGui.Text($"Native Steam Tool status: {(steamToolExists ? "INSTALLED" : "Not Installed")}");
        ImGui.Dummy(new Vector2(10));
        if (ImGui.Button($"{(steamToolExists ? "Re-i" : "I")}nstall to native Steam"))
        {
            this.Save();
            SteamCompatibilityTool.CreateTool(Program.Config.SteamPath);
        }
        ImGui.SameLine();
        if (!steamToolExists)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Uninstall from native Steam"))
        {
            this.Save();
            SteamCompatibilityTool.DeleteTool(Program.Config.SteamPath);
        }
        if (!steamToolExists)
        {
            ImGui.EndDisabled();
        }

#if FLATPAK
        ImGui.Dummy(new Vector2(10));
        ImGui.Separator();
        ImGui.Dummy(new Vector2(10));

        if (!SteamCompatibilityTool.IsFlatpakSteamInstalled)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.Text("\nWARNING! Flatpak Steam install not detected. If it is installed, make sure you run it at least once.\n" + 
                "This may also appear if you have not given your Flatpak XIVLauncher file permissions to ~/.var/app/com.valvesoftware.Steam\n");
            ImGui.PopStyleColor();
        }

        ImGui.Text($"Flatpak Steam Tool status: {(steamFlatpakToolExists ? "INSTALLED" : "Not Installed")}");
        ImGui.Dummy(new Vector2(10));
        if (ImGui.Button($"{(steamFlatpakToolExists ? "Re-i" : "I")}nstall to flatpak Steam"))
        {
            this.Save();
            SteamCompatibilityTool.CreateTool(Program.Config.SteamFlatpakPath);
        }
        ImGui.SameLine();
        if (!steamFlatpakToolExists)
        {
            ImGui.BeginDisabled();
        }
        if (ImGui.Button("Uninstall from Flatpak Steam"))
        {
            this.Save();
            SteamCompatibilityTool.DeleteTool(Program.Config.SteamFlatpakPath);
        }
        if (!steamFlatpakToolExists)
        {
            ImGui.EndDisabled();
        }

#endif
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
