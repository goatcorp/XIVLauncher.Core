using System;

namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public sealed class WineArm64StableRelease : IWineRelease
{
    public string Name { get; } =
        "wine-xiv-arm64ec-staging-11.17.r0.g83acf91a-r1";

    // Development override until the ARM64 runtime has a permanent
    // release location.
    public string DownloadUrl { get; } =
        Environment.GetEnvironmentVariable("XIVLAUNCHER_ARM64_WINE_URL")
        ?? throw new InvalidOperationException(
            "The ARM64 managed Wine runtime has not been published yet.");

    public string[] Checksums { get; } =
    [
        "f2e806e4265a15887866de995e6982d9eef4d593fc89cbb00a915ce0432b2d64a9bfbf42231cbd420e21124b79a906987c19ac44d4b4d22478d968addcfd197d"
    ];
}
