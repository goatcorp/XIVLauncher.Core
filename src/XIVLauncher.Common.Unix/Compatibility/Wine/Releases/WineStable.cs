namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineStableRelease(WineReleaseDistro wineDistroId) : IWineRelease
{
    public string Name { get; } = $"unofficial-wine-xiv-staging-10.8";
    public string DownloadUrl { get; } = $"https://github.com/rankynbass/unofficial-wine-xiv-git/releases/download/v10.8/unofficial-wine-xiv-staging-{wineDistroId}-10.8.tar.xz";
    public string[] Checksums { get; } = [
        "fbd78d16d94c9d5a0a43ffa8c8d0107c2f69f6956aab19e2449a5e8de762e9e9c6eb60fd82bfc5c85969a603773ff368abe13cd8c923d9ab4302fa3bdb1a8406", // unofficial-wine-xiv-staging-arch-10.8.tar.xz
        "3ea54e93990643273cb1fc866324cf8f7fcce8bb8b0f1bf3e45d6e3fb4ba38c765dd79312bde9586bd6c345d76d8382076876bb05408893cb1bd1f19640f1683", // unofficial-wine-xiv-staging-fedora-10.8.tar.xz
        "578a1883d1820209dca70d4775544d7354aa3a05d762d6e044e5de23d43892f6800424e295da36c1589d0566ebb553b3e48b2f099559660033a69a5e544a6165"  // unofficial-wine-xiv-staging-ubuntu-10.8.tar.xza
    ];
}
