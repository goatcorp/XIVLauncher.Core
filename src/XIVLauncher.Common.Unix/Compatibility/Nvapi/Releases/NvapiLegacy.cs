namespace XIVLauncher.Common.Unix.Compatibility.Nvapi.Releases;

public sealed class NvapiLegacyRelease : INvapiRelease
{
    public string Name { get; } = "dxvk-nvapi-v0.6.4";
    public string DownloadUrl { get; } = "https://github.com/jp7677/dxvk-nvapi/releases/download/v0.6.4/dxvk-nvapi-v0.6.4.tar.gz";
    public string Checksum { get; } = "fc99c1c4dd43b1e3c2870766f32c644921c686a52068de0fe2403a46c5b546ca5408669e181e6ba877f0a788939a374508912514bd3e4309c1ce0a79cbd7b6a4";
}