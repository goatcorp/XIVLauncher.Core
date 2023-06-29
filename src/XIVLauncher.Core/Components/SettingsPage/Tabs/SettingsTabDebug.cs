using System.Collections;
using ImGuiNET;
using XIVLauncher.Core.UnixCompatibility;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabDebug : SettingsTab
{
    public override SettingsEntry[] Entries => Array.Empty<SettingsEntry>();
    public override string Title => "Debug";

    public override void Draw()
    {
        ImGui.TextUnformatted("Generic Information");
        ImGui.Separator();
        if (Distro.IsLinux)
            ImGui.TextUnformatted($"Operating System: {Distro.Name} - {Environment.OSVersion}");
        else
            ImGui.TextUnformatted($"Operating System: {Environment.OSVersion}");
        ImGui.TextUnformatted($"Runtime Version: {Environment.Version}"); 
        
        if (Program.IsSteamDeckHardware)
            ImGui.Text("Steam Deck Hardware Detected");

        if (Program.IsSteamDeckGamingMode)
            ImGui.Text("Steam Deck Gaming Mode Detected");

        if (Distro.IsFlatpak)
            ImGui.Text("Running as a Flatpak");

        ImGui.Spacing();

        ImGui.TextUnformatted("Environment Information");
        ImGui.Separator();
            if(ImGui.BeginTable("EnvironmentTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Key", ImGuiTableColumnFlags.WidthStretch, 0.35f);
                ImGui.TableSetupColumn("Value");
                ImGui.TableHeadersRow();
                foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
                {   
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGuiHelpers.TextWrapped(entry.Key?.ToString() ?? "null");
                    ImGui.TableNextColumn();
                    ImGuiHelpers.TextWrapped(entry.Value?.ToString() ?? "null");
                }
                ImGui.EndTable();
            }

        base.Draw();
    }
}