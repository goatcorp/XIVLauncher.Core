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
        Package = LinuxDistro.ubuntu;
        Container = LinuxContainer.none;
        try
        {
            if (!File.Exists("/etc/os-release"))
            {
                Package = LinuxDistro.ubuntu;
                Container = LinuxContainer.none;
                Name = "Unknown distribution";
                LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"));
                LibraryPaths.Add(Path.Combine("/", "usr", "lib64"));
                LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
                LibraryPaths.Add(Path.Combine("/", "lib64"));
                LibraryPaths.Add(Path.Combine("/", "lib"));
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
                    LibraryPaths.Add(Path.Combine("/", "app", "lib"));
                    LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x84_64-linux-gnu"));
                    LibraryPaths.Add(Path.Combine("/", "usr", "lib", "extensions"));
                    Container = LinuxContainer.flatpak;
                }
                else if (osInfo.ContainsKey("HOME_URL"))
                {
                    if (osInfo["ID"] == "ubuntu-core" && osInfo["HOME_URL"] == "https://snapcraft.io")
                    {
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"));
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
                        // nvidia host path, needed for dlss on steam snap. These paths look on the host distro.
                        LibraryPaths.Add(Path.Combine("/", "var", "lib", "snapd", "hostfs", "usr", "lib", "x86_64-linux-gnu", "nvidia"));
                        LibraryPaths.Add(Path.Combine("/", "var", "lib", "snapd", "hostfs", "usr", "lib64", "nvidia"));
                        LibraryPaths.Add(Path.Combine("/", "var", "lib", "snapd", "hostfs", "usr", "lib", "nvidia"));
                        Container = LinuxContainer.snap;
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
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib64"));
                        Package = LinuxDistro.fedora;
                        break;
                    }
                    if (kvp.Value.ToLower().Contains("tumbleweed"))
                    {
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib64"));
                        Package = LinuxDistro.fedora;
                        break;
                    }
                    if (kvp.Value.ToLower().Contains("arch"))
                    {
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
                        Package = LinuxDistro.arch;
                        break;
                    }
                    if (kvp.Value.ToLower().Contains("ubuntu") || kvp.Value.ToLower().Contains("debian"))
                    {
                        LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"));
                        break;
                    }
                    // Unknown distro, add extra library search paths
                    LibraryPaths.Add(Path.Combine("/", "usr", "lib64"));
                    LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
                    LibraryPaths.Add(Path.Combine("/", "lib64"));
                    LibraryPaths.Add(Path.Combine("/", "lib"));
                }
            }
        }
        catch
        {
            // If there's any kind of error opening the file or even finding it, just go with default.
            Package = LinuxDistro.ubuntu;
            Name = "Unknown distribution";
            LibraryPaths.Add(Path.Combine("/", "usr", "lib", "x86_64-linux-gnu"));
            LibraryPaths.Add(Path.Combine("/", "usr", "lib64"));
            LibraryPaths.Add(Path.Combine("/", "usr", "lib"));
            LibraryPaths.Add(Path.Combine("/", "lib64"));
            LibraryPaths.Add(Path.Combine("/", "lib"));
        }
    }

    public static List<string> FileFind(string searchPath, string file, bool followSymlinks = false)
    {
        if (!IsLinux)
            return new List<string>();
        
        var found = new List<string>();
        var psi = new ProcessStartInfo("find");
        psi.Arguments = $"{(followSymlinks ? "-L " : "")}{searchPath} -name \"{file}\"";
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;
        var findCmd = new Process();
        findCmd.StartInfo = psi;
        try
        {
            findCmd.Start();
            var output = findCmd.StandardOutput.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(output))
            {
                var outputArray = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (string foundfile in outputArray)
                    found.Add(foundfile);
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Console.WriteLine("Error: could not execute \"find\" command. Is it installed?");
            Console.WriteLine(ex.Message);
        }
        finally
        {
            findCmd.Dispose();
        }
        return found;
    }

    public static List<string> FileFind(IEnumerable<string> searchPaths, string file, bool followSymlinks = false)
    {
        if (!IsLinux)
            return new List<string>();

        var found = new List<string>();
        foreach (string searchPath in searchPaths)
        {
            found.AddRange(FileFind(searchPath, file, followSymlinks));
        }
        return found;
    }

    public static bool IsFileFound(string searchPath, string file, bool followSymlinks = false)
    {
        if (!IsLinux)
            return false;

        if (FileFind(searchPath, file, followSymlinks).Count > 0)
            return true;
        return false;
    }

    public static bool IsFileFound(IEnumerable<string> searchPaths, string file, bool followSymlinks = false)
    {
        if (!IsLinux)
            return false;

        foreach (string searchPath in searchPaths)
        {
            if (IsFileFound(searchPath, file, followSymlinks))
                return true;
        }
        return false;
    }
}