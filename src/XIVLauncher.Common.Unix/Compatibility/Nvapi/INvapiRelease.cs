namespace XIVLauncher.Common.Unix.Compatibility.Nvapi;

public interface INvapiRelease
{
    string Name { get; }
    string DownloadUrl { get; }
    string Checksum { get; }
}