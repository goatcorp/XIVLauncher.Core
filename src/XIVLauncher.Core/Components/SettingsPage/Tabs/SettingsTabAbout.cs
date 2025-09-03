using System.Numerics;

using ImGuiNET;

using XIVLauncher.Common.Util;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabAbout : SettingsTab
{
    private readonly TextureWrap logoTexture;

    public override SettingsEntry[] Entries => Array.Empty<SettingsEntry>();

    public override string Title => Strings.AboutTitle;

    public SettingsTabAbout()
    {
        this.logoTexture = TextureWrap.Load(AppUtil.GetEmbeddedResourceBytes("logo.png"));
    }

    public override void Draw()
    {
        ImGui.Image(this.logoTexture.ImGuiHandle, new Vector2(256) * ImGuiHelpers.GlobalScale);

        ImGui.Text($"XIVLauncher Core v{AppUtil.GetAssemblyVersion()}({AppUtil.GetGitHash()})");

        var contribText = string.Format(Strings.XLCoreCreatedBy, "goaaats, Blooym, rankynbass");
        if(ImGui.Selectable(contribText, default, default, ImGui.CalcTextSize(contribText)))
            AppUtil.OpenBrowser("https://github.com/goatcorp/XIVLauncher.Core/graphs/contributors");

        ImGui.Dummy(new Vector2(20));

        if (ImGui.Button(Strings.OpenRepositoryButton))
        {
            AppUtil.OpenBrowser("https://github.com/goatcorp/XIVLauncher.Core");
        }
        ImGui.SameLine();
        if (ImGui.Button(Strings.JoinDiscordButton))
        {
            AppUtil.OpenBrowser("https://discord.gg/3NMcUV5");
        }
        ImGui.SameLine();
        if (ImGui.Button(Strings.SeeSoftwareLicensesButton))
        {
            PlatformHelpers.OpenBrowser(Path.Combine(AppContext.BaseDirectory, "license.txt"));
        }

        base.Draw();
    }
}
