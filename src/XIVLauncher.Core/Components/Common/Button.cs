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

    public Button(string label, bool isEnabled = true, Vector4? color = null, Vector4? hoverColor = null, Vector4? textColor = null)
    {
        this.Label = label;
        this.IsEnabled = isEnabled;
        this.Color = color ?? ImGuiColors.Blue;
        this.HoverColor = hoverColor ?? ImGuiColors.BlueShade3;
        this.TextColor = textColor ?? ImGuiColors.DalamudWhite;
    }

    public override void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(16f, 16f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
        ImGui.PushStyleColor(ImGuiCol.Button, this.Color);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, this.HoverColor);
        ImGui.PushStyleColor(ImGuiCol.Text, this.TextColor);

        if (ImGui.Button(this.Label, new Vector2(this.Width ?? -1, 0)) || (ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter)))
        {
            this.Click?.Invoke();
        }

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(3);
    }
}
