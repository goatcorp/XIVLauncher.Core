using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Common.Unix.Compatibility;

public static class Dxvk
{
    private const string DXVK_CURRENT_NAME = "dxvk-gplasync-v2.6-1";
    private const string DXVK_CURRENT_URL = "https://gitlab.com/Ph42oN/dxvk-gplasync/-/raw/main/releases/dxvk-gplasync-v2.6-1.tar.gz";
    private const string DXVK_LEGACY_NAME = "dxvk-async-1.10.3";
    private const string DXVK_LEGACY_URL = "https://github.com/Sporif/dxvk-async/releases/download/1.10.3/dxvk-async-1.10.3.tar.gz";


    public static async Task InstallDxvk(DirectoryInfo prefix, DirectoryInfo installDirectory, DxvkVersion version)
    {
        string name;
        string url;
        Console.WriteLine("Installing DXVK");
        switch (version)
        {
            case DxvkVersion.Current:
                name = DXVK_CURRENT_NAME;
                url = DXVK_CURRENT_URL;
                break;
            
            case DxvkVersion.Legacy:
                name = DXVK_LEGACY_NAME;
                url = DXVK_LEGACY_URL;
                break;

            case DxvkVersion.Disabled:
                return;

            default:
                throw new ArgumentOutOfRangeException("Invalid Dxvk.DxvkVersion. Value does not exist.");
        }
        Console.WriteLine($"name = {name}, url = {url}");
        var dxvkPath = Path.Combine(installDirectory.FullName, name, "x64");
        Console.WriteLine(dxvkPath);

        if (!Directory.Exists(dxvkPath))
        {
            Log.Information("DXVK does not exist, downloading");
            await DownloadDxvk(installDirectory, url).ConfigureAwait(false);
        }

        var system32 = Path.Combine(prefix.FullName, "drive_c", "windows", "system32");
        var files = Directory.GetFiles(dxvkPath);

        foreach (string fileName in files)
        {
            File.Copy(fileName, Path.Combine(system32, Path.GetFileName(fileName)), true);
        }
    }

    private static async Task DownloadDxvk(DirectoryInfo installDirectory, string url)
    {
        Console.WriteLine($"Downloading Dxvk from {url}");
        using var client = new HttpClient();
        var tempPath = PlatformHelpers.GetTempFileName();

        File.WriteAllBytes(tempPath, await client.GetByteArrayAsync(url));
        PlatformHelpers.Untar(tempPath, installDirectory.FullName);

        File.Delete(tempPath);
    }
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

public enum DxvkVersion
{
    [SettingsDescription("GPLAsync 2.6", "Dxvk 2.6 with GPLAsync patches. For most graphics cards.")]
    Current,

    [SettingsDescription("Async 1.10.3", "Dxvk 1.10.3 with Async patches. For older graphics cards.")]
    Legacy,

    [SettingsDescription("Disabled", "Use OpenGL/WineD3D instead. Slow, and might not work with Dalamud.")]
    Disabled,
}
