using ImGuiNET;
using XIVLauncher.Core.UnixCompatibility;

namespace XIVLauncher.Core.Components.SettingsPage;

public class DictionarySettingsEntry : SettingsEntry<string>
{
    public Dictionary<string, Dictionary<string, string>> Pairs;

    public string DefaultValue;

    public bool ShowDescription;

    public bool ShowItemDescription;

    public DictionarySettingsEntry(string name, string description, Dictionary<string, Dictionary<string, string>> pairs, Func<string> load, Action<string?> save, string defaultValue, bool showSelectedDesc = false, bool showItemDesc = true)
        : base(name, description, load, save)
    { 
        this.Pairs = pairs;
        this.DefaultValue = defaultValue;
        this.ShowDescription = showSelectedDesc;
        this.ShowItemDescription = showItemDesc;
    }


    public override void Draw()
    {
        var nativeValue = this.Value;
        string idx = (string)(this.InternalValue ?? DefaultValue);

        ImGuiHelpers.TextWrapped(this.Name);

        Dictionary<string, Dictionary<string, string>>.KeyCollection keys = Pairs.Keys;
        var label = Pairs[idx].ContainsKey("label") ? $"[{Pairs[idx]["label"]}] " : "";
        var name = Pairs[idx].ContainsKey("name") ? Pairs[idx]["name"] : idx;
        var desc = ShowDescription && Pairs[idx].ContainsKey("desc") ? $" - {Pairs[idx]["desc"]}" : "";
        var mark = Pairs[idx].ContainsKey("mark") ? $" *{Pairs[idx]["mark"]}*" : "";

        if (ImGui.BeginCombo($"###{Id.ToString()}", $"{label}{name}{desc}{mark}"))
        {
            foreach ( string key in keys )
            {
                var itemlabel = Pairs[key].ContainsKey("label") ? $"[{Pairs[key]["label"]}] " : "";
                var itemname = Pairs[key].ContainsKey("name") ? Pairs[key]["name"] : key;
                var itemdesc = ShowItemDescription && Pairs[key].ContainsKey("desc") ? $" - {Pairs[key]["desc"]}" : "";
                var itemmark = Pairs[key].ContainsKey("mark") ? $" *{Pairs[key]["mark"]}*" : "";
                if (ImGui.Selectable($"{itemlabel}{itemname}{itemdesc}{itemmark}", idx == key))
                {
                    this.InternalValue = key;
                }
            }
            ImGui.EndCombo();
        }

        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        if (!string.IsNullOrEmpty(this.Description)) ImGuiHelpers.TextWrapped(this.Description);
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