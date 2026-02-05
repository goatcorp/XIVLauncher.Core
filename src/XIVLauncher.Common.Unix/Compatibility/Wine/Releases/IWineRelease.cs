namespace XIVLauncher.Common.Unix.Compatibility.Wine.Releases;

public interface IWineRelease
{
    string Name { get; }
    string DownloadUrl { get; }
    string[] Checksums { get; }
}
