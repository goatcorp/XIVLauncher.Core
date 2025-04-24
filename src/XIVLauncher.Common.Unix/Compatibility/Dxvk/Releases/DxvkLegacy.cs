namespace XIVLauncher.Common.Unix.Compatibility.Dxvk.Releases;

public sealed class DxvkLegacyRelease : IDxvkRelease
{
    public string Name { get; } = "dxvk-async-1.10.3";
    public string DownloadUrl { get; } = "https://github.com/Sporif/dxvk-async/releases/download/1.10.3/dxvk-async-1.10.3.tar.gz";
}
