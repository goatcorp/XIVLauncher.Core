using Hexa.NET.ImGui;

using Serilog;

using XIVLauncher.Common.Dalamud;

namespace XIVLauncher.Core.Components.SettingsPage;

public class DalamudBranchMetaSettingsEntry : SettingsEntry<string>
{
    private DalamudBranchMeta.Branch? SelectedBranch { get; set; }

    private Task<IEnumerable<DalamudBranchMeta.Branch>> BranchTask { get; init; }

    private List<DalamudBranchMeta.Branch> Branches { get; set; } = [];

    private bool manualKeyToggle;
    private string ManualBranchTrack = "";
    private string ManualBranchKey = "";

    public DalamudBranchMetaSettingsEntry(string name, string description, Func<string> load, Action<string?> save)
        : base(name, description, load, save)
    {
        this.BranchTask = this.LoadBranchesAsync();
    }

    private async Task<IEnumerable<DalamudBranchMeta.Branch>> LoadBranchesAsync()
    {
        var branches = await DalamudBranchMeta.FetchBranchesAsync(Program.HttpClient).ConfigureAwait(false);
        this.Branches = [.. branches];
        return branches;
    }

    public override void Draw()
    {
        ImGuiHelpers.TextWrapped(this.Name);
        ImGui.BeginDisabled(!this.BranchTask.IsCompletedSuccessfully);
        var currentBranch = this.Branches.Find(b => b.Track == this.InternalValue as string);
        if (ImGui.BeginCombo($"###{this.Id}", currentBranch?.DisplayName))
        {
            foreach (var branch in this.Branches)
            {
                if (branch.Hidden) continue;
                if (ImGui.Selectable(branch.DisplayName, branch.Track == currentBranch?.Track))
                {
                    this.SelectedBranch = branch;
                    this.InternalValue = branch.Track;
                }
            }
            ImGui.EndCombo();
        }
        ImGui.EndDisabled();
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudGrey);
        ImGuiHelpers.TextWrapped(this.Description);
        ImGui.PopStyleColor();
        ImGui.Checkbox($"Have a beta key?###{this.Id}manual", ref this.manualKeyToggle);

        if (this.manualKeyToggle)
        {
            ImGui.InputText($"Track###{this.Id}manualtrack", ref this.ManualBranchTrack, 10000);
            ImGui.InputText($"Key###{this.Id}manualkey", ref this.ManualBranchKey, 10000);
        }
    }

    public override void Save()
    {
        if (this.SelectedBranch is not null)
        {
            Program.Config.DalamudBetaKind = SelectedBranch.Track;
            Program.Config.DalamudBetaKey = SelectedBranch.Key;
        }
        if (this.ManualBranchTrack != "" && this.ManualBranchKey != "")
        {
            Program.Config.DalamudBetaKind = this.ManualBranchTrack;
            Program.Config.DalamudBetaKey = this.ManualBranchKey;
        }
        Program.DalamudUpdater.Run(Program.Config.DalamudBetaKind, Program.Config.DalamudBetaKey);
    }
}
