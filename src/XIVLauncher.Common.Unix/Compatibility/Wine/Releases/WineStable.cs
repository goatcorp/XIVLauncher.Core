namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineStableRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"wine-xiv-staging-fsync-git-10.8.r0.g47f77594-nolsc";
    public string DownloadUrl { get; } = $"https://github.com/goatcorp/wine-xiv-git/releases/download/10.8.r0.g47f77594/wine-xiv-staging-fsync-git-{wineDistroId}-10.8.r0.g47f77594-nolsc.tar.xz";
    public string[] Checksums { get; } = [
        "e7803fff77cec837f604eef15af8434b4d74acd0e3adf1885049b31143bdd6b69f03f56b14f078e501f42576b3b4434deca547294b2ded0c471720ef7e412367", // wine-xiv-staging-fsync-git-arch-10.8.r0.g47f77594-nolsc.tar.xz
        "7475788ba4cd448743fa44acba475eac796c9fe1ec8a2b37e0fdb7123cf3feac0c97f0a4e43ea023bf1e70853e7916a5a27e835fc5f651ac5c08040251bc4522",  // wine-xiv-staging-fsync-git-fedora-10.8.r0.g47f77594-nolsc.tar.xz
        "9d06e403b0b879a7b1f6394d69a6d23ee929c27f1f7a3abbf0f34fab3cbaff0b8154849d406f3ed15ee62ec0444379173070da208607fadabbf65186ed0cbf95" // wine-xiv-staging-fsync-git-ubuntu-10.8.r0.g47f77594-nolsc.tar.xz
    ];
}
