using System.Numerics;

using ImGuiNET;

namespace XIVLauncher.Core.Components.MainPage;

public class ActionButtons : Component
{
    public event Action? OnStatusButtonClicked;
    public event Action? OnSettingsButtonClicked;

    public override void Draw()
    {
        var btnSize = new Vector2(80) * ImGuiHelpers.GlobalScale;

        ImGui.PushFont(FontManager.IconFont);
        ImGui.BeginDisabled(this.OnStatusButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.Globe.ToIconString(), btnSize))
        {
            this.OnStatusButtonClicked?.Invoke();
        }
        ImGui.PushFont(FontManager.TextFont);
        ImGuiHelpers.AddTooltip("Service Status");
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.BeginDisabled(this.OnSettingsButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString(), btnSize))
        {
            this.OnSettingsButtonClicked?.Invoke();
        }
        ImGui.PushFont(FontManager.TextFont);
        ImGuiHelpers.AddTooltip("Settings");
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.PopFont();

        base.Draw();
    }
}
