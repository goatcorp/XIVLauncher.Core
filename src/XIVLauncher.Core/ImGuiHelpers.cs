using System.Numerics;

using ImGuiNET;

namespace XIVLauncher.Core;

public static class ImGuiHelpers
{
    public static Vector2 ViewportSize => ImGui.GetIO().DisplaySize;

    public static float GlobalScale {get; set; } = 1.0f;

    public static float GetScaled(int size)
    {
        return GlobalScale * (float)size;
    }

    public static float GetScaled(float size)
    {
        return GlobalScale * size;
    }

    public static Vector2 GetScaled(Vector2 size)
    {
        return size * GlobalScale;
    }

    public static void TextWrapped(string text)
    {
        ImGui.PushTextWrapPos();
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    public static void CenteredText(string text)
    {
        CenterCursorForText(text);
        ImGui.TextUnformatted(text);
    }

    public static void CenterCursorForText(string text)
    {
        var textWidth = ImGui.CalcTextSize(text).X;
        CenterCursorFor((int)textWidth);
    }

    public static void CenterCursorFor(int itemWidth)
    {
        var window = (int)ImGui.GetWindowWidth();
        ImGui.SetCursorPosX(window / 2 - itemWidth / 2);
    }

    public static void AddTooltip(string text)
    {
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.TextUnformatted(text);
            ImGui.EndTooltip();
        }
    }
}
