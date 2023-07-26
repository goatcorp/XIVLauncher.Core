using System.Numerics;
using System.IO;
using System.Collections.Generic;
using XIVLauncher.Common;
using System.Runtime.InteropServices;

namespace XIVLauncher.Core;

public enum DistroPackage
{
    ubuntu,

    fedora,

    arch,

    none,
}

public static class OSInfo
{
    public static DistroPackage Package { get; private set; }

    public static string Name { get; private set; }

    public static bool IsFlatpak { get; private set; }

    public static Platform Platform { get; private set; }

    static OSInfo()
    {
        var os = System.Environment.OSVersion;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Package = DistroPackage.none;
            Name = os.VersionString;
            IsFlatpak = false;
            Platform = Platform.Win32;
            return;
        }

        // There's no wine releases for MacOS or FreeBSD, and I'm not sure this will even compile on either
        // platform, but here's some code just in case. Can modify this as needed if it's useful in the future.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Platform = Platform.Mac;
            Name = os.VersionString;
            IsFlatpak = false;
            Package = DistroPackage.none;
            return;
        }
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            Platform = Platform.Mac;  // Don't have an option for this atm.
            Name = os.VersionString;
            IsFlatpak = false;
            Package = DistroPackage.none;
            return;            
        }

        Platform = Platform.Linux;
        try
        {
            if (!File.Exists("/etc/os-release"))
            {
                Package = DistroPackage.ubuntu;
                Name = "Unknown distribution";
                IsFlatpak = false;
                return;
            }
            var osRelease = File.ReadAllLines("/etc/os-release");
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
            Name = pretty == "" ? (name == "" ? "Unknown distribution" : name) : pretty;

            if (CheckFlatpak(osInfo))
            {
                IsFlatpak = true;
                Package = DistroPackage.ubuntu;
                return;
            }

            Package = CheckDistro(osInfo);
            IsFlatpak = false;
            return;
        }
        catch
        {
            // If there's any kind of error opening the file or even finding it, just go with default.
            Package = DistroPackage.ubuntu;
            Name = "Unknown distribution";
            IsFlatpak = false;
        }
    }

    private static bool CheckFlatpak(Dictionary<string, string> osInfo)
    {
        if (osInfo.ContainsKey("ID"))
            if (osInfo["ID"] == "org.freedesktop.platform")
                return true;
        return false;
    }

    private static DistroPackage CheckDistro(Dictionary<string, string> osInfo)
    {
        foreach (var kvp in osInfo)
        {
            if (kvp.Value.ToLower().Contains("fedora"))
                return DistroPackage.fedora;
            if (kvp.Value.ToLower().Contains("tumbleweed"))
                return DistroPackage.fedora;
            if (kvp.Value.ToLower().Contains("ubuntu"))
                return DistroPackage.ubuntu;
            if (kvp.Value.ToLower().Contains("debian"))
                return DistroPackage.ubuntu;
            if (kvp.Value.ToLower().Contains("arch"))
                return DistroPackage.arch;
        }
        return DistroPackage.ubuntu;
    }
}