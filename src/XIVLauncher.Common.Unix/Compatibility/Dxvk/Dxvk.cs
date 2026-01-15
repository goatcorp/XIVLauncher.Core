using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Serilog;

using XIVLauncher.Common.Unix.Compatibility.Dxvk.Releases;
using XIVLauncher.Common.Unix.Compatibility.Wine;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Common.Unix.Compatibility.Dxvk;

public enum DxvkVersion
{

    [SettingsDescription("Stable", "DXVK 2.6.1 with GPLAsync patches. Requires Mesa 24.0 or Nvidia 535 drivers. Ideal for graphics cards.")]
    Stable,

    [SettingsDescription("Beta", "DXVK 2.7 with GPLAsync patches. Requires Mesa 25.0 or Nvidia 550 drivers. Ideal for modern graphics cards.")]
    Beta,

    [SettingsDescription("Legacy", "DXVK 1.10.3 with Async patches. Requires Mesa 22.0 or Nvidia 470 drivers. Ideal for older graphics card.")]
    Legacy,

    [SettingsDescription("Disabled", "Use OpenGL/WineD3D instead. Slow, and might not work with Dalamud.")]
    Disabled,
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

public static class Dxvk
{
    public static async Task InstallDxvk(DirectoryInfo prefix, DirectoryInfo installDirectory, DxvkVersion version)
    {
        if (version is DxvkVersion.Disabled)
        {
            return;
        }
        IDxvkRelease release = version switch
        {
            DxvkVersion.Stable => new DxvkStableRelease(),
            DxvkVersion.Beta => new DxvkBetaRelease(),
            DxvkVersion.Legacy => new DxvkLegacyRelease(),
            _ => throw new NotImplementedException(),
        };

        var dxvkPath = Path.Combine(installDirectory.FullName, release.Name, "x64");
        if (!Directory.Exists(dxvkPath))
        {
            Log.Information("DXVK does not exist, downloading");
            await DownloadDxvk(installDirectory, release.DownloadUrl, release.Checksum).ConfigureAwait(false);
        }

        var system32 = Path.Combine(prefix.FullName, "drive_c", "windows", "system32");
        var files = Directory.GetFiles(dxvkPath);

        foreach (var fileName in files)
        {
            File.Copy(fileName, Path.Combine(system32, Path.GetFileName(fileName)), true);
        }
    }

    private static async Task DownloadDxvk(DirectoryInfo installDirectory, string url, string checksum)
    {
        using var client = HappyEyeballsHttp.CreateHttpClient();
        var tempPath = PlatformHelpers.GetTempFileName();

        File.WriteAllBytes(tempPath, await client.GetByteArrayAsync(url).ConfigureAwait(false));

        if (!CompatUtil.EnsureChecksumMatch(tempPath, [checksum]))
        {
            throw new InvalidDataException("SHA512 checksum verification failed");
        }

        PlatformHelpers.Untar(tempPath, installDirectory.FullName);

        File.Delete(tempPath);
    }
}

