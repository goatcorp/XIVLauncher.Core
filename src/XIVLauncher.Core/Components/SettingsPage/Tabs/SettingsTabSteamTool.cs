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

    private bool steamInstalled = SteamCompatibilityTool.IsSteamInstalled;

    private bool steamToolInstalled = SteamCompatibilityTool.IsSteamToolInstalled;
    
    private bool steamFlatpakInstalled = SteamCompatibilityTool.IsSteamFlatpakInstalled;
    
    private bool steamFlatpakToolInstalled = SteamCompatibilityTool.IsSteamFlatpakToolInstalled;

    public SettingsTabSteamTool()
    {
        Entries = new SettingsEntry[]
        {
            steamPath = new SettingsEntry<string>("Steam Path (native)", "Path to the native steam install. Only change this if you have Steam installed in a non-default location.",
                () => Program.Config.SteamPath ?? Path.Combine(CoreEnvironmentSettings.HOME, ".local", "share", "Steam"), s => Program.Config.SteamPath = s),
            steamFlatpakPath = new SettingsEntry<string>("Steam Path (flatpak)", "Path to the flatpak Steam installation. Only change this if you have your flatpak Steam installed to a non-default location.",
                () => Program.Config.SteamFlatpakPath ?? Path.Combine(CoreEnvironmentSettings.HOME, ".var", "app", "com.valvesoftware.Steam", "data", "Steam" ), s => Program.Config.SteamFlatpakPath = s)
            {    
                CheckVisibility = () => Program.IsSteamDeckHardware != true && SteamCompatibilityTool.IsSteamFlatpakInstalled,
            },
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
            ImGui.Text("If you are trying to upgrade, you must first update your flatpak install of XIVLauncher.Core. Then launch the flatpak" +
                        "\nversion, navigate back to this tab, and re-install as a Steam compatibility tool.");
            ImGui.Text("\nIf you are trying to uninstall, you should likewise launch the flatpak version of XIVLauncher, and click the appropriate" +
                        "\nuninstall button.");
            return;
        }
        ImGui.Text("\nUse this tab to install XIVLauncher.Core as a Steam compatibility tool.");
        ImGui.Text("\nAfter you have installed XIVLauncher as a Steam compatibility tool please close XIVLauncher and launch or restart Steam. Find 'Final Fantasy XIV Online'");
        ImGui.Text("in your steam library and open the 'Properties' menu and navigate to the 'Compatibility' tab. Enable 'Force the use of a specific Steam Play compatibility tool'");
        ImGui.Text("and from the dropdown menu select 'XIVLauncher.Core'. If this option does not show up then restart Steam and try again. After finishing these steps,");
        ImGui.Text("XIVLauncher will now be used when launching FINAL FANTASY XIV from steam.");
        // Steam deck should never have flatpak steam
        if (Program.IsSteamDeckHardware != true)
        {
            ImGui.Text("\nIf you wish to install into Flatpak Steam, you must use Flatseal to give XIVLauncher access to Steam's flatpak path. This is commonly found at:");
            ImGui.Text($"~/.var/app/com.valvesoftware.Steam. If you do not give this permission, the install option will not even appear. You will also need to give Steam");
            ImGui.Text($"access to ~/.xlcore, so that you can continue to use your current xlcore folder.");
            ImGui.Text("\nDO NOT use native XIVLauncher to install to flatpak Steam. Use flatpak XIVLauncher instead.");
        }

        ImGui.Dummy(new Vector2(10));        
        ImGui.Separator();
        ImGui.Dummy(new Vector2(10));

        ImGui.Text($"Steam settings directory: {(steamInstalled ? "PRESENT" : "Not Present")}. Native Steam Tool: {(steamToolInstalled ? "INSTALLED" : "Not Installed")}.");
        ImGui.Dummy(new Vector2(10));
        if (!steamInstalled) ImGui.BeginDisabled();
        if (ImGui.Button($"{(steamToolInstalled ? "Re-i" : "I")}nstall to native Steam"))
        {
            this.Save();
            SteamCompatibilityTool.CreateTool(isFlatpak: false);
            steamToolInstalled = SteamCompatibilityTool.IsSteamToolInstalled;
        }
        if (!steamInstalled) ImGui.EndDisabled();
        ImGui.SameLine();
        if (!steamToolInstalled) ImGui.BeginDisabled();
        if (ImGui.Button("Uninstall from native Steam"))
        {
            this.Save();
            SteamCompatibilityTool.DeleteTool(isFlatpak: false);
            steamToolInstalled = SteamCompatibilityTool.IsSteamToolInstalled;
        }
        if (!steamToolInstalled) ImGui.EndDisabled();

        if (!Program.IsSteamDeckHardware && steamFlatpakInstalled)
        {
            ImGui.Dummy(new Vector2(10));
            ImGui.Separator();
            ImGui.Dummy(new Vector2(10));

            ImGui.Text($"Flatpak Steam settings directory: PRESENT. Flatpak Steam Tool: {(steamFlatpakToolInstalled ? "INSTALLED" : "Not Installed")}");
            ImGui.Dummy(new Vector2(10));
            if (!steamFlatpakInstalled) ImGui.BeginDisabled();
            if (ImGui.Button($"{(steamFlatpakToolInstalled ? "Re-i" : "I")}nstall to flatpak Steam"))
            {
                this.Save();
                SteamCompatibilityTool.CreateTool(isFlatpak: true);
                steamFlatpakToolInstalled = SteamCompatibilityTool.IsSteamFlatpakToolInstalled;
            }
            if (!steamFlatpakInstalled) ImGui.EndDisabled();
            ImGui.SameLine();
            if (!steamFlatpakToolInstalled)
            {
                ImGui.BeginDisabled();
            }
            if (ImGui.Button("Uninstall from Flatpak Steam"))
            {
                this.Save();
                SteamCompatibilityTool.DeleteTool(isFlatpak: true);
                steamFlatpakToolInstalled = SteamCompatibilityTool.IsSteamFlatpakToolInstalled;
            }
            if (!steamFlatpakToolInstalled)
            {
                ImGui.EndDisabled();
            }
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
