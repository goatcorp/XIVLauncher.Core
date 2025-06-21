namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineStableRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"wine-xiv-staging-fsync-git-10.8.r0.g47f77594";
    public string DownloadUrl { get; } = $"https://github.com/goatcorp/wine-xiv-git/releases/download/10.8.r0.g47f77594/wine-xiv-staging-fsync-git-{wineDistroId}-10.8.r0.g47f77594.tar.xz";
    public string[] Checksums { get; } = [
        "fac545a5d8ee219bf622903adb7a5ca6f2f968d9a2307da3f0ec394dde08c4d4f956487f4b326ef1cfc92eaface463f270f590695ad8874d5c88028fac8f6250", // wine-xiv-staging-fsync-git-arch-10.8.r0.g47f77594.tar.xz
        "c6857467ea3da9d7071164c89c87838dd1ff6006406e2ba24516bef3ea0c701386b26f68752b20710464410d62846ceea1c69f78caf777dbafc611daa7fba6c9",  // wine-xiv-staging-fsync-git-fedora-10.8.r0.g47f77594.tar.xz
        "fb0bf85190ec9d001e39135537f27f82e8bd0ab1f222c008012128fbb6f6d8c8547f1315d09d909e7219fe2c221005a5b98927bfb95a24a36c5fca526ae0e95b" // wine-xiv-staging-fsync-git-ubuntu-10.8.r0.g47f77594.tar.xz
    ];
}
