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
        ImGui.SetCursorPos(new Vector2(0, ImGuiHelpers.ViewportSize.Y - this.bgTexture.Height));
        ImGui.Image(this.bgTexture.ImGuiHandle, new Vector2(this.bgTexture.Width, this.bgTexture.Height));
        base.Draw();
    }
}
