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

    // In order for the nvngx dlls to work properly with wine-staging, they need to be in the game directory.
    // The prefix system32 folder will not work. If the dlls are only in system32, the game will hang on startup.
    public static void CopyNvngx(DirectoryInfo gameDirectory, DirectoryInfo prefix, bool installIntoPrefix = true)
    {
        var game = Path.Combine(gameDirectory.FullName, "game");
        var system32 = Path.Combine(prefix.FullName, "drive_c", "windows", "system32");
        var installedGame = false;
        var installedPrefix = false;
        if (File.Exists(Path.Combine(game, "nvngx.dll")) && File.Exists(Path.Combine(game, "_nvngx.dll")))
        {
            // Already installed. Don't bother copying.
            Log.Verbose($"nvngx.dll installed in {game}: True");
            installedGame = true;
        }
        else
            Log.Verbose($"nvngx.dll installed in {game}: False");
        if (File.Exists(Path.Combine(prefix.FullName, "nvngx.dll")) && File.Exists(Path.Combine(prefix.FullName, "_nvngx.dll")))
        {
            // Already installed. Don't bother copying.
            Log.Verbose($"nvngx.dll installed in {system32}: True");
            installedPrefix = true;
        }
        else
            Log.Verbose($"nvngx.dll installed in {system32}: False");

        if (installedGame && installedPrefix) return;

        var nvngxPath = NvidiaWineDLLPath();
        if (string.IsNullOrEmpty(nvngxPath))
        {
            Log.Information("No nvngx.dll or _nvngx.dll found. Try copying them to ~/.xlcore/compatibilitytool");
            Log.Information("If using AMD or intel graphics, ignore this message");
            return;
        }

        var files = Directory.GetFiles(nvngxPath);

        // Only nvngx.dll and _nvngx.dll are needed for dlss to function, but there is also nvngx_dlssg.dll
        // which may be needed in the future. So just copy all the files.
        foreach (var file in files)
        {
            if (!installedGame)
                File.Copy(file, Path.Combine(game, Path.GetFileName(file)), true);
            if (!installedPrefix && installIntoPrefix)
                File.Copy(file, Path.Combine(system32, Path.GetFileName(file)), true);
        }
    }

    private static string NvidiaWineDLLPath()
    {
        string nvngxPath = "";
        string HOME = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string PATH = Environment.GetEnvironmentVariable("XL_NVNGXPATH");      

        var targets = new List<string>
        { 
            Path.Combine(HOME, ".xlcore", "compatibilitytool"),
            Path.Combine("/", "app", "lib"),                            // flatpak
            Path.Combine("/", "usr", "lib", "extensions"),              // flatpak
            Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"),        // flatpak, debuntu
            Path.Combine("/", "usr", "lib64"),                          // fedora, opensuse
            Path.Combine("/", "usr", "lib"),                            // arch
            Path.Combine("/", "run", "host", "lib", "x86_64-linux-gnu"), // distrobox container on debuntu
            Path.Combine("/", "run", "host", "lib64"),                  // distrobox container on fedora, opensuse
            Path.Combine("/", "run", "host", "lib"),                    // distrobox container on arch
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
