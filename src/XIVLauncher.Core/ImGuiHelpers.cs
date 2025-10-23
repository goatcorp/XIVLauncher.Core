using System.Numerics;

#if HEXA
using Hexa.NET.ImGui;
#endif
#if VELDRID
using ImGuiNET;
#endif

namespace XIVLauncher.Core;

public static class ImGuiHelpers
{
    public static Vector2 ViewportSize => ImGui.GetIO().DisplaySize;
    public static float GlobalScale => 
#if VELDRID
        ImGui.GetIO().FontGlobalScale;
#endif
#if HEXA
        ImGui.GetStyle().FontScaleMain;
#endif

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
