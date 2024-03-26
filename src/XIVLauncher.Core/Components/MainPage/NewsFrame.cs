using System.Numerics;

using ImGuiNET;

using XIVLauncher.Common;
using XIVLauncher.Common.Game;

namespace XIVLauncher.Core.Components.MainPage;

public class NewsFrame : Component
{
    private const int BANNER_TIME = 8000;

    private readonly LauncherApp app;
    private readonly Timer bannerTimer;

    private Headlines? headlines;
    private TextureWrap[] banners = { };

    private IReadOnlyList<Banner> bannerList = new List<Banner>();

    private int currentBanner = 0;

    private bool newsLoaded = false;

    public NewsFrame(LauncherApp app)
    {
        this.app = app;
        this.ReloadNews();

        this.bannerTimer = new Timer(TimerElapsed);
        bannerTimer.Change(0, BANNER_TIME);
    }

    private void TimerElapsed(object? state)
    {
        if (!this.newsLoaded)
            return;

        this.currentBanner = (this.currentBanner + 1) % this.banners.Length;
        Program.Invalidate(10);
    }

    public void ReloadNews()
    {
        Task.Run(async () =>
        {
            this.newsLoaded = false;

            await Headlines.GetWorlds(this.app.Launcher, this.app.Settings.ClientLanguage ?? ClientLanguage.English).ConfigureAwait(false);

            bannerList = await Headlines.GetBanners(this.app.Launcher, this.app.Settings.ClientLanguage ?? ClientLanguage.English).ConfigureAwait(false);

            await Headlines.GetMessage(this.app.Launcher, this.app.Settings.ClientLanguage ?? ClientLanguage.English).ConfigureAwait(false);

            headlines = await Headlines.GetNews(this.app.Launcher, this.app.Settings.ClientLanguage ?? ClientLanguage.English).ConfigureAwait(false);

            this.banners = new TextureWrap[bannerList.Count];

            var client = new HttpClient();

            for (var i = 0; i < bannerList.Count; i++)
            {
                var textureBytes = await client.GetByteArrayAsync(this.bannerList[i].LsbBanner).ConfigureAwait(false);
                this.banners[i] = TextureWrap.Load(textureBytes);
            }

            this.newsLoaded = true;
        });
    }

    private Vector2 GetSize()
    {
        var vp = ImGuiHelpers.ViewportSize;
        var calculatedSize = vp.X >= 1280 ? vp.X * 0.7f : vp.X * 0.5f;
        return new Vector2(calculatedSize, vp.Y - 128f);
    }

    public override void Draw()
    {
        if (ImGui.BeginChild("###newsFrame", this.GetSize()))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(32f, 32f));

            if (this.newsLoaded)
            {
                var banner = this.banners[this.currentBanner];
                ImGui.Image(banner.ImGuiHandle, banner.Size);

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                    AppUtil.OpenBrowser(this.bannerList[this.currentBanner].Link.ToString());

                ImGui.Dummy(new Vector2(15));

                void ShowNewsEntry(News newsEntry)
                {
                    ImGui.Text(newsEntry.Title);

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Left) && !string.IsNullOrEmpty(newsEntry.Url))
                    {
                        AppUtil.OpenBrowser(newsEntry.Url);
                    }
                }

                if (this.headlines is not null)
                {
                    ImGui.TextDisabled("News");

                    foreach (News newsEntry in this.headlines.News)
                    {
                        ShowNewsEntry(newsEntry);
                    }

                    ImGui.Spacing();

                    ImGui.TextDisabled("Topics");
                    foreach (News topic in this.headlines.Topics)
                    {
                        ShowNewsEntry(topic);
                    }
                }
            }
            else
            {
                ImGui.Text("News is loading...");
            }

            ImGui.PopStyleVar();
        }

        ImGui.EndChild();

        base.Draw();
    }
}
