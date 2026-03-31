namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineBetaRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"wine-xiv-staging-fsync-git-10.8.r0.a2ca9e4";
    public string DownloadUrl { get; } = $"https://github.com/goatcorp/wine-xiv-git/releases/download/10.8.r0.a2ca9e4/wine-xiv-staging-fsync-git-{wineDistroId}-10.8.r0.a2ca9e4.tar.xz";
    public string[] Checksums { get; } = [
        "f5ca302bab5d4fc321800333da76daff6d18230dc24c36fc2f7eda4d918407c3ebbb9c5684c43c1d1110ff28b93503fbdc1b2bc9ad7109462884dcdc415dd5cc", // wine-xiv-staging-fsync-git-arch-10.8.r0.a2ca9e4.tar.xz
        "eed98e8eeb176626689f77683620d323ace87662a79ef206394508742be6f1a1a581bcdaae08b1c19c1b8da2b077ec5b2dcb0b4937dd384d1d34b2f573040f8f",  // wine-xiv-staging-fsync-git-fedora-10.8.r0.a2ca9e4.tar.xz
        "675a694f8a66c33222da3b713772f22801dff4e39d803df37e2116c6fe2d519083d307cbcfcc54e062e667bd811b50b5328f6d42b8e2d85c5efec7666c4bf5e2" // wine-xiv-staging-fsync-git-ubuntu-10.8.r0.a2ca9e4.tar.xz
    ];
}
