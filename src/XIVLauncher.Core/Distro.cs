using System.Numerics;
using System.IO;
using System.Collections.Generic;

namespace XIVLauncher.Core.UnixCompatibility;

public enum DistroPackage
{
    ubuntu,

    fedora,

    arch,

    none,
}

public static class Distro
{
    public static DistroPackage Package { get; private set; }

    public static string Name { get; private set; }

    public static bool IsFlatpak { get; private set; }

    public static bool IsLinux { get; private set; }

    public static void UseWindows()
    {
        Package = DistroPackage.none;
        OperatingSystem os = System.Environment.OSVersion;
        Name = os.VersionString;
        IsFlatpak = false;
        IsLinux = false;

    }

    public static void UseUnix()
    {
        IsLinux = true;
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
            var distro = DistroPackage.ubuntu;
            var flatpak = false;
            var OSInfo = new Dictionary<string, string>();
            foreach (var line in osRelease)
            {
                var keyValue = line.Split('=', 2);
                if (keyValue.Length == 1)
                    OSInfo.Add(keyValue[0], "");
                else
                    OSInfo.Add(keyValue[0], keyValue[1]);
            }

            var name = (OSInfo.ContainsKey("NAME") ? OSInfo["NAME"] : "").Trim('"');
            var pretty = (OSInfo.ContainsKey("PRETTY_NAME") ? OSInfo["PRETTY_NAME"] : "").Trim('"');
            var idLike = OSInfo.ContainsKey("ID_LIKE") ? OSInfo["ID_LIKE"] : "";
            if (idLike.Contains("arch"))
                distro = DistroPackage.arch;
            else if (idLike.Contains("fedora"))
                distro = DistroPackage.fedora;
            else
                distro = DistroPackage.ubuntu;

            var id = OSInfo.ContainsKey("ID") ? OSInfo["ID"] : "";
            if (id.Contains("tumbleweed") || id.Contains("fedora"))
                distro = DistroPackage.fedora;
            if (id == "org.freedesktop.platform")
                flatpak = true;

            Package = distro;
            Name = pretty == "" ? (name == "" ? "Unknown distribution" : name) : pretty;
            IsFlatpak = flatpak;
        }
        catch
        {
            // If there's any kind of error opening the file or even finding it, just go with default.
            Package = DistroPackage.ubuntu;
            Name = "Unknown distribution";
            IsFlatpak = false;
        }
    }    
}