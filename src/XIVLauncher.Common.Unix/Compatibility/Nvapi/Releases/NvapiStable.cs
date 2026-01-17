namespace XIVLauncher.Common.Unix.Compatibility.Nvapi.Releases;

public sealed class NvapiStableRelease : INvapiRelease
{
    public string Name { get; } = "dxvk-nvapi-v0.9.0";
    public string DownloadUrl { get; } = "https://github.com/jp7677/dxvk-nvapi/releases/download/v0.9.0/dxvk-nvapi-v0.9.0.tar.gz";
    public string Checksum { get; } = "c69fefee7e9b4efc2521bd96de4d413a130a8c509b62572673acd70a884ec2c0799eb973d65c466e66ece0df10e03410ed94e35c3fc10a5f42ff2b9c392f18e0";
}