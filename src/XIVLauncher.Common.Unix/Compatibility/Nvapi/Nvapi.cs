using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Serilog;

using XIVLauncher.Common.Unix.Compatibility.Nvapi.Releases;
using XIVLauncher.Common.Unix.Compatibility.Wine;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Common.Unix.Compatibility.Nvapi;

public enum NvapiVersion
{
    [SettingsDescription("Stable", "Dxvk-Nvapi v0.9.0. For DLSS with nVidia cards and Stable Dxvk release")]
    Stable,

    [SettingsDescription("Disabled", "Do not use Dxvk-Nvapi. For GPUs without DLSS support")]
    Disabled,
}

public static class Nvapi
{
    public static async Task InstallNvapi(DirectoryInfo prefix, DirectoryInfo installDirectory, NvapiVersion version)
    {
        if (version is NvapiVersion.Disabled)
        {
            return;
        }
        INvapiRelease release = version switch
        {
            NvapiVersion.Stable => new NvapiStableRelease(),
            _ => throw new NotImplementedException(),
        };

        var nvapiPath = Path.Combine(installDirectory.FullName, release.Name, "x64");
        if (!Directory.Exists(nvapiPath))
        {
            var installPath = new DirectoryInfo(Path.Combine(installDirectory.FullName, release.Name));
            if (!installPath.Exists)
                installPath.Create();
            Log.Information("Dxvk-nvapi does not exist, downloading");
            await DownloadNvapi(installPath, release.DownloadUrl, release.Checksum).ConfigureAwait(false);
        }

        var system32 = Path.Combine(prefix.FullName, "drive_c", "windows", "system32");
        var files = Directory.GetFiles(nvapiPath);

        foreach (var fileName in files)
        {
            File.Copy(fileName, Path.Combine(system32, Path.GetFileName(fileName)), true);
        }
    }

    public static void CopyNvngx(DirectoryInfo prefix, DirectoryInfo gameDirectory)
    {
        var system32 = Path.Combine(prefix.FullName, "drive_c", "windows", "system32");
        var game = Path.Combine(gameDirectory.FullName, "game");
        var system32Installed = File.Exists(Path.Combine(system32, "nvngx.dll")) && File.Exists(Path.Combine(system32, "_nvngx.dll"));
        var gameInstalled = File.Exists(Path.Combine(game, "nvngx.dll")) && File.Exists(Path.Combine(game, "_nvngx.dll"));
        Log.Verbose($"nvngx.dll installed in {system32}: {system32Installed.ToString()}");
        Log.Verbose($"nvngx.dll installed in {game}: {gameInstalled.ToString()}");
        if (system32Installed && gameInstalled)
        {
            // Already installed. Don't bother copying.
            return;
        }
        if (gameInstalled)
        {
            File.Copy(Path.Combine(game, "nvngx.dll"), Path.Combine(system32, "nvngx.dll"), true);
            File.Copy(Path.Combine(game, "_nvngx.dll"), Path.Combine(system32, "_nvngx.dll"), true);
            return;
        }
        if (system32Installed)
        {
            File.Copy(Path.Combine(system32, "nvngx.dll"), Path.Combine(game, "nvngx.dll"), true);
            File.Copy(Path.Combine(system32, "_nvngx.dll"), Path.Combine(game, "_nvngx.dll"), true);
            return;
        }

        var nvngxPath = NvidiaWineDLLPath();
        if (string.IsNullOrEmpty(nvngxPath))
        {
            Log.Information("No nvngx.dll or _nvngx.dll found. Try copying them to ~/.xlcore/compatibilitytool");
            Log.Information("If using AMD or intel graphics, ignore this message");
            return;
        }

        File.Copy(Path.Combine(nvngxPath, "nvngx.dll"), Path.Combine(system32, "nvngx.dll"), true);
        File.Copy(Path.Combine(nvngxPath, "nvngx.dll"), Path.Combine(game, "nvngx.dll"), true);
        File.Copy(Path.Combine(nvngxPath, "_nvngx.dll"), Path.Combine(system32, "_nvngx.dll"), true);
        File.Copy(Path.Combine(nvngxPath, "_nvngx.dll"), Path.Combine(game, "_nvngx.dll"), true);
    }

    private static string NvidiaWineDLLPath()
    {
        string nvngxPath = "";
        string HOME = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string PATH = Environment.GetEnvironmentVariable("XL_NVNGXPATH");      

        var targets = new List<string>
        { 
            Path.Combine(HOME, ".xlcore", "compatibilitytool"),
            Path.Combine("/", "app", "lib"),                        // flatpak
            Path.Combine("/", "usr", "lib", "extensions"),          // flatpak
            Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"),    // flatpak, debuntu
            Path.Combine("/", "usr", "lib64"),                      // fedora, opensuse
            Path.Combine("/", "usr", "lib"),                        // arch
        };

        if (!string.IsNullOrEmpty(PATH))
        {
            var firstcheck = new DirectoryInfo(PATH);
            Log.Verbose("XL_NVNGXPATH: " + firstcheck.FullName);
            targets.Insert(0, firstcheck.FullName);
        }
        
        var options = new EnumerationOptions();
        options.RecurseSubdirectories = true;
        options.MaxRecursionDepth = 10;

        foreach (var target in targets)
        {
            if (!Directory.Exists(target))
            {
                Log.Verbose($"DLSS: {target} directory does not exist");
                continue;
            }
            Log.Verbose($"DLSS: {target} directory exists... Searching...");

            var found = Directory.GetFiles(target, "nvngx.dll", options);
            if (found.Length > 0)
            {
                if (File.Exists(found[0]))
                {
                    nvngxPath = new FileInfo(found[0]).DirectoryName;
                }
                break;
            }
            Log.Verbose($"DLSS: No nvngx.dll found at {target}");
        }
        return nvngxPath;
    }

    private static async Task DownloadNvapi(DirectoryInfo installDirectory, string url, string checksum)
    {
        using var client = new HttpClient();
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
