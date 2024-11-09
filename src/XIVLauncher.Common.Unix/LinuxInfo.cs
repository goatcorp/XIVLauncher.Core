using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace XIVLauncher.Common.Unix;

public static class LinuxInfo
{
    public static LinuxDistro Package { get; private set; }

    public static LinuxContainer Container { get; private set; }

    public static List<string> LibraryPaths { get; private set; }

    public static string Name { get; private set; }

    public static bool IsLinux { get; private set; }

    static LinuxInfo()
    {
        LibraryPaths = new List<string>();
        var os = System.Environment.OSVersion;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Package = LinuxDistro.none;
            Container = LinuxContainer.none;
            Name = os.VersionString;
            IsLinux = false;
            return;
        }

        IsLinux = true;
        Package = LinuxDistro.none;
        Container = LinuxContainer.none;
        try
        {
            if (!File.Exists("/etc/os-release"))
            {
                Package = LinuxDistro.ubuntu;
                Container = LinuxContainer.none;
                Name = "Unknown distribution";
                addLibraryPaths();
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

            // Check for flatpak or snap
            if (osInfo.ContainsKey("ID"))
            {
                if (osInfo["ID"] == "org.freedesktop.platform")
                {
                    Container = LinuxContainer.flatpak;
                    Package = LinuxDistro.ubuntu;
                }
                else if (osInfo.ContainsKey("HOME_URL"))
                {
                    if (osInfo["ID"] == "ubuntu-core" && osInfo["HOME_URL"] == "https://snapcraft.io")
                    {
                        Container = LinuxContainer.snap;
                        Package = LinuxDistro.ubuntu;
                    }
                }
            }

            // Check distro package if not a container
            if (Container == LinuxContainer.none)
            {
                foreach (var kvp in osInfo)
                {
                    if (kvp.Value.ToLower().Contains("fedora"))
                    {
                        Package = LinuxDistro.fedora;
                        break;
                    }
                    else if (kvp.Value.ToLower().Contains("tumbleweed"))
                    {
                        Package = LinuxDistro.fedora;
                        break;
                    }
                    else if (kvp.Value.ToLower().Contains("arch"))
                    {
                        Package = LinuxDistro.arch;
                        break;
                    }
                    else if (kvp.Value.ToLower().Contains("ubuntu") || kvp.Value.ToLower().Contains("debian"))
                    {
                        Package = LinuxDistro.ubuntu;
                        break;
                    }
                }
                if (Package == LinuxDistro.none)
                {
                    Package = LinuxDistro.ubuntu;
                }
            }
            addLibraryPaths();
            foreach (var path in LibraryPaths)
                Console.Write(path + ":");
        }
        catch
        {
            // If there's any kind of error opening the file or even finding it, just go with default.
            Package = LinuxDistro.ubuntu;
            Name = "Unknown distribution";
            addLibraryPaths();
        }
    }

    private static void addLibraryPaths()
    {
        switch (Container)
        {
            case LinuxContainer.flatpak:
                LibraryPaths.Add(Path.Combine("/", "app", "lib"));
                LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x84_64-linux-gnu"));
                LibraryPaths.Add(Path.Combine("/", "usr", "lib", "extensions"));
                break;

            case LinuxContainer.snap:
                LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"));
                LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
                // nvidia host path, needed for dlss on steam snap. These paths look on the host distro.
                LibraryPaths.Add(Path.Combine("/", "var", "lib", "snapd", "hostfs", "usr", "lib", "x86_64-linux-gnu", "nvidia"));
                LibraryPaths.Add(Path.Combine("/", "var", "lib", "snapd", "hostfs", "usr", "lib64", "nvidia"));
                LibraryPaths.Add(Path.Combine("/", "var", "lib", "snapd", "hostfs", "usr", "lib", "nvidia"));
                break;

            case LinuxContainer.none:
                if (Package == LinuxDistro.none && IsLinux)
                    Package = LinuxDistro.ubuntu;
                switch (Package)
                {
                    case LinuxDistro.arch:
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
                        break;

                    case LinuxDistro.fedora:
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib64"));
                        break;

                    case LinuxDistro.ubuntu:
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"));
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib64"));
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
                        LibraryPaths.Add(Path.Combine("/", "lib64"));
                        LibraryPaths.Add(Path.Combine("/", "lib"));
                        break;

                    case LinuxDistro.none:
                        break;
                }
                break;
        }

    }
}