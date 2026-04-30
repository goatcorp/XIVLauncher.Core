using Hexa.NET.ImGui;

using XIVLauncher.Common.Dalamud;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.SettingsPage;

public class DalamudBranchMetaSettingsEntry : SettingsEntry<string>
{
    private DalamudBranchMeta.Branch? SelectedBranch { get; set; }

    private Task<IEnumerable<DalamudBranchMeta.Branch>> BranchTask { get; init; }

    private List<DalamudBranchMeta.Branch> Branches { get; set; } = [];
    
    private string manualBranchTrack = "";
    private string manualBranchKey = "";

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
                if (branch.Hidden && !(branch.Track == this.manualBranchTrack && branch.Key == this.manualBranchKey)) continue;
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
        if (ImGui.Button(Strings.DalamudBranchSwitcherBetaKeyButton)) ImGui.OpenPopup(Strings.DalamudBranchSwitcherBetaKeyTitle);
        if (ImGui.BeginPopupModal(Strings.DalamudBranchSwitcherBetaKeyTitle))
        {
            ImGui.PushItemWidth(200);
            ImGui.InputText($"{Strings.DalamudBranchSwitcherBetaKeyTrack}###{this.Id}-manual-track", ref this.manualBranchTrack, 10000);
            ImGui.InputText($"{Strings.DalamudBranchSwitcherBetaKeyKey}###{this.Id}-manual-key", ref this.manualBranchKey, 10000);
            ImGui.PopItemWidth();
            if (ImGui.Button(Strings.ClearLabel))
            {
                this.manualBranchTrack = "";
                this.manualBranchKey = "";
            }
            ImGui.SameLine();
            if (ImGui.Button(Strings.CloseLabel)) ImGui.CloseCurrentPopup();
            ImGui.EndPopup();
        }
    }

    public override void Save()
    {
        if (this.SelectedBranch is null) return;
        Program.Config.DalamudBetaKind = SelectedBranch.Track;
        Program.Config.DalamudBetaKey = SelectedBranch.Key;
        Program.DalamudUpdater.Run(Program.Config.DalamudBetaKind, Program.Config.DalamudBetaKey);
    }
}
