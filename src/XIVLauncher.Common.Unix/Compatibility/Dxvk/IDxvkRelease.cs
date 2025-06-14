namespace XIVLauncher.Common.Unix.Compatibility.Dxvk;

public interface IDxvkRelease
{
    string Name { get; }
    string DownloadUrl { get; }
    string Checksum { get; }
}
