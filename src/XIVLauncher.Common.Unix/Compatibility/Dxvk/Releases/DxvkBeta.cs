namespace XIVLauncher.Common.Unix.Compatibility.Dxvk.Releases;

public sealed class DxvkBetaRelease : IDxvkRelease
{
    public string Name { get; } = "dxvk-gplasync-v2.7-1";
    public string DownloadUrl { get; } = "https://raw.githubusercontent.com/goatcorp/xlcore-distrib/refs/heads/main/dxvk-gplasync-v2.7-1.tar.gz";
    public string Checksum { get; } = "1fe59a91f3d3a09a132d1f9c3f7e94d47154e53e58f935daa090aa6dea5c8e6c64800334a6f3014751492a2f32002d8ce63e8086284df0b8d4cbace0353e0f3b";
}
