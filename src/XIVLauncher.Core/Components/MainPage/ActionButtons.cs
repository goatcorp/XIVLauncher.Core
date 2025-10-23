using System.Numerics;

#if HEXA
using Hexa.NET.ImGui;
#endif
#if VELDRID
using ImGuiNET;
#endif

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
        
#if HEXA
        ImGui.PushFont(FontManager.IconFont, 0.0f);
#endif
#if VELDRID
        ImGui.PushFont(FontManager.IconFont);
#endif
        ImGui.BeginDisabled(this.OnAccountButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.User.ToIconString(), btnSize))
        {
            this.OnAccountButtonClicked?.Invoke();
        }
#if HEXA
        ImGui.PushFont(FontManager.TextFont, 0.0f);
#endif
#if VELDRID
        ImGui.PushFont(FontManager.TextFont);
#endif
        ImGuiHelpers.AddTooltip(Strings.MyAccount);
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.BeginDisabled(this.OnStatusButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.Heartbeat.ToIconString(), btnSize))
        {
            this.OnStatusButtonClicked?.Invoke();
        }
#if HEXA
        ImGui.PushFont(FontManager.TextFont, 0.0f);
#endif
#if VELDRID
        ImGui.PushFont(FontManager.TextFont);
#endif
        ImGuiHelpers.AddTooltip(Strings.ServiceStatus);
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.SameLine();

        ImGui.BeginDisabled(this.OnSettingsButtonClicked == null);
        if (ImGui.Button(FontAwesomeIcon.Cog.ToIconString(), btnSize))
        {
            this.OnSettingsButtonClicked?.Invoke();
        }
#if HEXA
        ImGui.PushFont(FontManager.TextFont, 0.0f);
#endif
#if VELDRID
        ImGui.PushFont(FontManager.TextFont);
#endif
        ImGuiHelpers.AddTooltip(Strings.LauncherSettings);
        ImGui.PopFont();
        ImGui.EndDisabled();

        ImGui.PopFont();

        base.Draw();
    }
}
