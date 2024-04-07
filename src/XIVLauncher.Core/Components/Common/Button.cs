using System.Numerics;

using ImGuiNET;

namespace XIVLauncher.Core.Components.Common;

public class Button : Component
{
    public bool IsEnabled { get; set; } = true;

    public string Label { get; set; }
    public Vector4 Color { get; set; }
    public Vector4 HoverColor { get; set; }
    public Vector4 TextColor { get; set; }

    public event Action? Click;

    public int? Width { get; set; }

    public int? Height { get; set; }

    public Button(string label, bool isEnabled = true, Vector4? color = null, Vector4? hoverColor = null, Vector4? textColor = null)
    {
        Label = label;
        IsEnabled = isEnabled;
        Color = color ?? ImGuiColors.Blue;
        HoverColor = hoverColor ?? ImGuiColors.BlueShade3;
        TextColor = textColor ?? ImGuiColors.DalamudWhite;
    }

    public override void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(Width is not null ? 0f : 16f, Height is not null ? 0f : 16f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
        ImGui.PushStyleColor(ImGuiCol.Button, Color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, HoverColor);
        ImGui.PushStyleColor(ImGuiCol.Text, TextColor);

        if (ImGui.Button(Label, new Vector2(Width ?? -1, Height ?? 0)) || (ImGui.IsItemFocused() && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))))
        {
            this.Click?.Invoke();
        }

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
    }
}
