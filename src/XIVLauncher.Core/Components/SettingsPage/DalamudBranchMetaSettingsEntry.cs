using ImGuiNET;
using XIVLauncher.Common.Dalamud;

namespace XIVLauncher.Core.Components.SettingsPage;

public class DalamudBranchMetaSettingsEntry : SettingsEntry<string>
{
    public DalamudBranchMeta.Branch? SelectedBranch { get; set; }

    public List<DalamudBranchMeta.Branch> Branches { get; set; } = [];

    public DalamudBranchMetaSettingsEntry(string name, string description, Func<string> load, Action<string?> save)
        : base(name, description, load, save)
    {
        var branchesTask = DalamudBranchMeta.FetchBranchesAsync();
        branchesTask.Wait();
        branchesTask.Result.ToList().ForEach(b => Branches.Add(b));
    }

    public override void Draw()
    {
        ImGuiHelpers.TextWrapped(this.Name);
        var currentBranch = this.Branches.Find(b => b.Track == this.InternalValue as string);
        if (ImGui.BeginCombo($"###{Id.ToString()}", currentBranch?.DisplayName))
        {
            foreach (var b in this.Branches)
            {
                if (ImGui.Selectable(b.DisplayName, b.Track == currentBranch?.Track))
                {
                    this.SelectedBranch = b;
                    this.InternalValue = b.Track;
                }
            }
            ImGui.EndCombo();
        }
        
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        ImGuiHelpers.TextWrapped(this.Description);
        ImGui.PopStyleColor();
    }

    public override void Save()
    {
        Program.Config.DalamudBetaKey = this.SelectedBranch.Key;
        base.Save();
    }
}
