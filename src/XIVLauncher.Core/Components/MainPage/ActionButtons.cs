using System.Numerics;

using ImGuiNET;

using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.MainPage;

public class ActionButtons : Component
{
    public event Action? OnAccountButtonClicked;
    public event Action? OnStatusButtonClicked;
    public event Action? OnSettingsButtonClicked;

    public override void Draw()
    {
        var btnSize = new Vector2(80) * ImGuiHelpers.GlobalScale;

        ImGui.PushFont(FontManager.IconFont);
        ImGui.BeginDisabled(this.OnAccountButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.User.ToIconString(), btnSize))
        {
            this.OnAccountButtonClicked?.Invoke();
        }
        ImGui.PushFont(FontManager.TextFont);
        ImGuiHelpers.AddTooltip(Strings.MyAccount);
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.BeginDisabled(this.OnStatusButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.Heartbeat.ToIconString(), btnSize))
        {
            this.OnStatusButtonClicked?.Invoke();
        }
        ImGui.PushFont(FontManager.TextFont);
        ImGuiHelpers.AddTooltip(Strings.ServiceStatus);
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.BeginDisabled(this.OnSettingsButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString(), btnSize))
        {
            this.OnSettingsButtonClicked?.Invoke();
        }
        ImGui.PushFont(FontManager.TextFont);
        ImGuiHelpers.AddTooltip(Strings.LauncherSettings);
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.PopFont();

        base.Draw();
    }
}
