using System.Numerics;

using ImGuiNET;

namespace XIVLauncher.Core.Components;

public class Background : Component
{
    private TextureWrap bgTexture;

    public Background()
    {
        this.bgTexture = TextureWrap.Load(AppUtil.GetEmbeddedResourceBytes("bg_logo.png"));
    }

    public override void Draw()
    {
        ImGui.SetCursorPos(new Vector2(0, ImGuiHelpers.ViewportSize.Y - bgTexture.Height));
        ImGui.Image(bgTexture.ImGuiHandle, new Vector2(bgTexture.Width, bgTexture.Height));
        base.Draw();
    }
}
