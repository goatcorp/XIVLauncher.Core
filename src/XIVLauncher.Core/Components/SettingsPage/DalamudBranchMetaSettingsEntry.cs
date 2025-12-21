using ImGuiNET;
using XIVLauncher.Common.Dalamud;

namespace XIVLauncher.Core.Components.SettingsPage;

public class DalamudBranchMetaSettingsEntry : SettingsEntry<string>
{
    private DalamudBranchMeta.Branch? SelectedBranch { get; set; }
    private Task<IEnumerable<DalamudBranchMeta.Branch>> BranchTask { get; set; }

    private List<DalamudBranchMeta.Branch> Branches { get; set; } = [];

    public DalamudBranchMetaSettingsEntry(string name, string description, Func<string> load, Action<string?> save)
        : base(name, description, load, save)
    {
        var branchesTask = DalamudBranchMeta.FetchBranchesAsync();
        this.BranchTask = branchesTask;
        branchesTask.Result.ToList().ForEach(b => Branches.Add(b));
    }

    public override void Draw()
    {
        ImGuiHelpers.TextWrapped(Name);
        if (!this.BranchTask.IsCompletedSuccessfully) ImGui.BeginDisabled();
        var currentBranch = Branches.Find(b => b.Track == InternalValue as string);
        if (ImGui.BeginCombo($"###{Id.ToString()}", currentBranch?.DisplayName))
        {
            foreach (var b in Branches)
            {
                if (ImGui.Selectable(b.DisplayName, b.Track == currentBranch?.Track))
                {
                    SelectedBranch = b;
                    InternalValue = b.Track;
                }
            }
            ImGui.EndCombo();
        }
        if (!this.BranchTask.IsCompletedSuccessfully) ImGui.EndDisabled();
        
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        ImGuiHelpers.TextWrapped(Description);
        ImGui.PopStyleColor();
    }

    public override void Save()
    {
        Program.Config.DalamudBetaKind = SelectedBranch?.Track;
        Program.Config.DalamudBetaKey = SelectedBranch?.Key;
        Program.DalamudUpdater.Run(Program.Config.DalamudBetaKind,  Program.Config.DalamudBetaKey);
    }
}
