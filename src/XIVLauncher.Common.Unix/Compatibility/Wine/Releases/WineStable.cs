namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

// Change paths as appropriate for a new stable wine release.
public sealed class WineStableRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"wine-xiv-staging-fsync-git-10.5.r0.g835c92a2";
    public string DownloadUrl { get; } = $"https://github.com/rankynbass/unofficial-wine-xiv-git/releases/download/10.5.r0.g835c92a2/wine-xiv-staging-fsync-git-{wineDistroId}-10.5.r0.g835c92a2.tar.xz";
}
