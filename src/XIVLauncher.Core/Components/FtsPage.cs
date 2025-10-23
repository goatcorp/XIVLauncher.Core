using System.Numerics;

#if HEXA
using Hexa.NET.ImGui;
#endif
#if VELDRID
using ImGuiNET;
#endif

namespace XIVLauncher.Core.Components;

public class FtsPage : Page
{
    private readonly TextureWrap steamdeckFtsTexture;
    private readonly TextureWrap steamdeckAppIdErrorTexture;

    private bool isSteamDeckAppIdError = false;

    public FtsPage(LauncherApp app)
        : base(app)
    {
        this.steamdeckFtsTexture = TextureWrap.Load(AppUtil.GetEmbeddedResourceBytes("steamdeck_fts.png"));
        this.steamdeckAppIdErrorTexture = TextureWrap.Load(AppUtil.GetEmbeddedResourceBytes("steamdeck_fterror.png"));
    }

    public void OpenFtsIfNeeded()
    {
        if (CoreEnvironmentSettings.IsDeckFirstRun.HasValue)
        {
            if (CoreEnvironmentSettings.IsDeckFirstRun.Value)
            {
                App.State = LauncherApp.LauncherState.Fts;
                return;
            }
            else
                return;
        }

        if (!(App.Settings.CompletedFts ?? false) && Program.IsSteamDeckHardware)
        {
            App.State = LauncherApp.LauncherState.Fts;
            return;
        }

        if (Program.IsSteamDeckHardware && (Program.Steam == null || !Program.Steam.IsValid))
        {
            // If IsIgnoringSteam == true, skip the error screen. This fixes a bug with Steam Deck always showing the Fts Error screen.
            if (App.Settings.IsIgnoringSteam ?? false) return;
            App.State = LauncherApp.LauncherState.Fts;
            this.isSteamDeckAppIdError = true;
        }
    }

    private void FinishFts(bool save)
    {
        App.State = LauncherApp.LauncherState.Main;

        if (save)
            App.Settings.CompletedFts = true;
    }

    public override void Draw()
    {
        ImGui.SetCursorPos(new Vector2(0));

        ImGui.Image(this.isSteamDeckAppIdError ? this.steamdeckAppIdErrorTexture.ImGuiHandle : this.steamdeckFtsTexture.ImGuiHandle, new Vector2(1280, 800));

        ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, Vector4.Zero);

        ImGui.SetCursorPos(new Vector2(316, 481));

        if (ImGui.Button("###openGuideButton", new Vector2(649, 101)))
        {
            if (!this.isSteamDeckAppIdError)
            {
                AppUtil.OpenBrowser("https://goatcorp.github.io/faq/steamdeck");
            }
            else
            {
                Environment.Exit(0);
            }
        }

        ImGui.SetCursorPos(new Vector2(316, 598));

        if (ImGui.Button("###finishFtsButton", new Vector2(649, 101)) && !this.isSteamDeckAppIdError)
        {
            this.FinishFts(true);
        }

        ImGui.PopStyleColor(3);

        base.Draw();
    }
}
