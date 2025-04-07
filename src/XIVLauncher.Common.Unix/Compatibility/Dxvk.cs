using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Common.Unix.Compatibility;

public static class Dxvk
{
    private const string DXVK_DOWNLOAD = "https://gitlab.com/Ph42oN/dxvk-gplasync/-/raw/447db06ecff8a64f900b12741dbd8d1c8d8eae22/releases/dxvk-gplasync-v2.6-1.tar.gz";
    private const string DXVK_NAME = "dxvk-gplasync-v2.6-1";

    public static async Task InstallDxvk(DirectoryInfo prefix, DirectoryInfo installDirectory)
    {
        var dxvkPath = Path.Combine(installDirectory.FullName, DXVK_NAME, "x64");

        if (!Directory.Exists(dxvkPath))
        {
            Log.Information("DXVK does not exist, downloading");
            await DownloadDxvk(installDirectory).ConfigureAwait(false);
        }

        var system32 = Path.Combine(prefix.FullName, "drive_c", "windows", "system32");
        var files = Directory.GetFiles(dxvkPath);

        foreach (string fileName in files)
        {
            File.Copy(fileName, Path.Combine(system32, Path.GetFileName(fileName)), true);
        }
    }

    private static async Task DownloadDxvk(DirectoryInfo installDirectory)
    {
        using var client = new HttpClient();
        var tempPath = PlatformHelpers.GetTempFileName();

        File.WriteAllBytes(tempPath, await client.GetByteArrayAsync(DXVK_DOWNLOAD));
        PlatformHelpers.Untar(tempPath, installDirectory.FullName);

        File.Delete(tempPath);
    }

    public enum DxvkHudType
    {
        [SettingsDescription("None", "Show nothing")]
        None,

        [SettingsDescription("FPS", "Only show FPS")]
        Fps,

        [SettingsDescription("Full", "Show everything")]
        Full,
    }
}
