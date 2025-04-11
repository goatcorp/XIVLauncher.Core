namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineLegacyRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"wine-xiv-staging-fsync-git-8.5.r4.g4211bac7";
    public string DownloadUrl { get; } = $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{wineDistroId}-8.5.r4.g4211bac7.tar.xz";
}
