namespace XIVLauncher.Common.Unix.Compatibility.Nvapi.Releases;

public sealed class NvapiStableRelease : INvapiRelease
{
    public string Name { get; } = "dxvk-nvapi-v0.9.1";
    public string DownloadUrl { get; } = "https://github.com/jp7677/dxvk-nvapi/releases/download/v0.9.1/dxvk-nvapi-v0.9.1.tar.gz";
    public string Checksum { get; } = "98477fd3a8aa24b74bb127adbef8e8a63a98a846f615f1f0bda1ed21dc69704866b6cd77d4f760f0ae519eb79546882af131a58adb033966a361ab075977043a";
}