using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace XIVLauncher.Common.Unix.Compatibility.Wine;

public enum WineReleaseDistro
{
    ubuntu,
    fedora,
    arch,
}

public static class CompatUtil
{
    private const string OS_RELEASE_PATH = "/etc/os-release";

    public static WineReleaseDistro GetWineIdForDistro()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            throw new InvalidOperationException("GetWineIdForDistro can only be called on the Linux platform");
        }

        try
        {
            if (!File.Exists(OS_RELEASE_PATH))
            {
                return WineReleaseDistro.ubuntu;
            }
            var osRelease = File.ReadAllLines(OS_RELEASE_PATH);
            var osInfo = new Dictionary<string, string>();
            foreach (var line in osRelease)
            {
                var keyValue = line.Split('=', 2);
                if (keyValue.Length == 1)
                    osInfo.Add(keyValue[0], "");
                else
                    osInfo.Add(keyValue[0], keyValue[1]);
            }

            var name = (osInfo.ContainsKey("NAME") ? osInfo["NAME"] : "").Trim('"');
            var pretty = (osInfo.ContainsKey("PRETTY_NAME") ? osInfo["PRETTY_NAME"] : "").Trim('"');
            name = pretty == "" ? (name == "" ? "Unknown distribution" : name) : pretty;

            // Check for flatpak or snap
            if (osInfo.ContainsKey("ID"))
            {
                if (osInfo["ID"] == "org.freedesktop.platform")
                {
                    return WineReleaseDistro.ubuntu;
                }
                else if (osInfo.ContainsKey("HOME_URL"))
                {
                    if (osInfo["ID"] == "ubuntu-core" && osInfo["HOME_URL"] == "https://snapcraft.io")
                    {
                        return WineReleaseDistro.ubuntu;
                    }
                }
            }

            // Check for values in osInfo.
            foreach (var kvp in osInfo)
            {
                if (kvp.Value.ToLower().Contains("fedora") || kvp.Value.ToLower().Contains("tumbleweed"))
                {
                    return WineReleaseDistro.fedora;
                }

                if (kvp.Value.ToLower().Contains("arch"))
                {
                    return WineReleaseDistro.arch;
                }

                if (kvp.Value.ToLower().Contains("ubuntu") || kvp.Value.ToLower().Contains("debian"))
                {
                    return WineReleaseDistro.ubuntu;
                }
            }

            return WineReleaseDistro.ubuntu;
        }
        catch
        {
            return WineReleaseDistro.ubuntu;
        }
    }

    public static bool EnsureChecksumMatch(string filePath, string[] checksums)
    {
        if (checksums.Length == 0)
        {
            return false;
        }
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);
        var computedHash = Convert.ToHexString(sha512.ComputeHash(stream)).ToLowerInvariant();
        return checksums.Any(checksum => string.Equals(checksum, computedHash, StringComparison.OrdinalIgnoreCase));
    }
}
