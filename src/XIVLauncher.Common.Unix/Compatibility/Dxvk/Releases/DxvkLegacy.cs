namespace XIVLauncher.Common.Unix.Compatibility.Dxvk.Releases;

public sealed class DxvkLegacyRelease : IDxvkRelease
{
    public string Name { get; } = "dxvk-async-1.10.3";
    public string DownloadUrl { get; } = "https://github.com/Sporif/dxvk-async/releases/download/1.10.3/dxvk-async-1.10.3.tar.gz";
    public string Checksum { get; } = "afc856b859f1c36d919055e471ae1dd1900424ea42139ab8c1ae231fe9617234d1dfa53f6bf0e5d183575a224f2b8bc950f258108607f39cc419823d68f06ff2";
}
