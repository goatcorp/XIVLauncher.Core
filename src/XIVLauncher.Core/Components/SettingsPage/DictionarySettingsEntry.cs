using ImGuiNET;
using System.Collections;
using System.Linq;

namespace XIVLauncher.Core.Components.SettingsPage;

public class DictionarySettingsEntry : SettingsEntry<string>
{
    public Dictionary<string, string> Pairs;

    public DictionarySettingsEntry(string name, string description, Dictionary<string, string> pairs, Func<string> load, Action<string> save)
        : base(name, description, load, save)
    { 
        this.Pairs = pairs;
    }


    public override void Draw()
    {
        var nativeValue = this.Value;
        string idx = (string)(this.InternalValue ?? "Proton 7.0");

        ImGuiHelpers.TextWrapped(this.Name);

        Dictionary<string, string>.KeyCollection keys = Pairs.Keys;

        if (ImGui.BeginCombo($"###{Id.ToString()}", idx + ": " + Pairs[idx]))
        {
            foreach ( string key in keys )
            {
                if (ImGui.Selectable(key + ": " + Pairs[key], idx == key))
                {
                    this.InternalValue = key;
                }
            }
            ImGui.EndCombo();
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        ImGuiHelpers.TextWrapped(this.Description);
        ImGui.PopStyleColor();

        if (this.CheckValidity != null)
        {
            var validityMsg = this.CheckValidity.Invoke(this.Value);
            this.IsValid = string.IsNullOrEmpty(validityMsg);

            if (!this.IsValid)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                ImGui.Text(validityMsg);
                ImGui.PopStyleColor();
            }
        }
        else
        {
            this.IsValid = true;
        }

        var warningMessage = this.CheckWarning?.Invoke(this.Value);

        if (warningMessage != null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.Text(warningMessage);
            ImGui.PopStyleColor();
        }
        
    }
}