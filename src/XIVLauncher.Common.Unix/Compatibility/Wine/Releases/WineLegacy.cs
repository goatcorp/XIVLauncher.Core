namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineLegacyRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"wine-xiv-staging-fsync-git-8.5.r4.g4211bac7";
    public string DownloadUrl { get; } = $"https://github.com/goatcorp/wine-xiv-git/releases/download/8.5.r4.g4211bac7/wine-xiv-staging-fsync-git-{wineDistroId}-8.5.r4.g4211bac7.tar.xz";
    public string[] Checksums { get; } = [
        "832de4d834bdbd6e1e069f13efcb56fa1508c9d7ba0609e1161a52d814f2c6f7c89c8e2d1bcff05da7f0b5cab0662f7e5d57865ab7a5c9d144e6bd55051adee5", // wine-xiv-staging-fsync-git-fedora-8.5.r4.g4211bac7.tar.xz
        "5158108788f21f03c895216265824eba0080c6aceacd901a1f60e242ed161db2d6cb65304bbc0187310b08513df2a5c40c62a41838ac1765a422f85387109930", // wine-xiv-staging-fsync-git-ubuntu-8.5.r4.g4211bac7.tar.xz
        "ff77e19d35c598bc5602222d4bb4c0b85ae375f99f9ae0000f847a904ef80c120d89e59da921ee05fe54b0bd583e9cf1fb7f142b95f3ad2d3aba9891b6605f08"  // wine-xiv-staging-fsync-git-arch-8.5.r4.g4211bac7.tar.xz
    ];
}
