namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineStableRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"wine-xiv-staging-fsync-git-10.8.r0.a2ca9e4-nolsc";
    public string DownloadUrl { get; } = $"https://github.com/goatcorp/wine-xiv-git/releases/download/10.8.r0.a2ca9e4/wine-xiv-staging-fsync-git-{wineDistroId}-10.8.r0.a2ca9e4-nolsc.tar.xz";
    public string[] Checksums { get; } = [
        "a4ff42d8e7a8057f794b3f3f6a686b21411f091c8264acd419869c726a1d21b594b2e024b7b995fea4fb99f3b4538fdda78eb0ab7a44d0905bc1272102eecf6e", // wine-xiv-staging-fsync-git-arch-10.8.r0.a2ca9e4-nolsc.tar.xz
        "1693253de16618db15ee51e1a96af02330eccccba4dc3f0710a63c920e33d5063c67c6b7d4950b61be61105f14eb301ef861278a057e6b4d31ccf8c58f126e60",  // wine-xiv-staging-fsync-git-fedora-10.8.r0.a2ca9e4-nolsc.tar.xz
        "3c306bd5e153563ba11befee529d34cef2e9138e23d0d6576f5723719ce8676970ff7196aee122348aef82b3afab8987fafe73ef11601c0e07b297a9aa2fe69d" // wine-xiv-staging-fsync-git-ubuntu-10.8.r0.a2ca9e4-nolsc.tar.xz
    ];
}
