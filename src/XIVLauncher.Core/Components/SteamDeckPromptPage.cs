using System.Numerics;

using ImGuiNET;

namespace XIVLauncher.Core.Components;

public class SteamDeckPromptPage : Page
{
    private readonly TextureWrap switchPromptTexture;

    public SteamDeckPromptPage(LauncherApp app)
        : base(app)
    {
        this.switchPromptTexture = TextureWrap.Load(AppUtil.GetEmbeddedResourceBytes("steamdeck_switchprompt.png"));
    }

    public override void Draw()
    {
        ImGui.SetCursorPos(new Vector2(0));

        ImGui.Image(this.switchPromptTexture.ImGuiHandle, new Vector2(1280, 800));

        base.Draw();
    }
}
