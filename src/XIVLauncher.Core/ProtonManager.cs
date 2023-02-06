using System;
using System.IO;
using System.Linq;

namespace XIVLauncher.Core;

public static class ProtonManager
{
    public static Dictionary<string, string> Versions { get; private set; }

    public static void GetVersions(string steamRoot)
    {
        Versions = new Dictionary<string, string>();
        var commonDir = new DirectoryInfo(Path.Combine(steamRoot, "steamapps","common"));
        var compatDir = new DirectoryInfo(Path.Combine(steamRoot, "compatibilitytools.d"));

        Console.WriteLine($"Common Directory: {commonDir.FullName}");
        Console.WriteLine($"Compat DIrectory: {compatDir.FullName}");
        var commonDirs = commonDir.GetDirectories("Proton*");
        var compatDirs = compatDir.GetDirectories("*Proton*");

        foreach (var dir in commonDirs)
            if (File.Exists(Path.Combine(dir.FullName,"proton"))) Versions.Add(dir.Name, dir.FullName);

        foreach (var dir in compatDirs)
            if (File.Exists(Path.Combine(dir.FullName,"proton"))) Versions.Add(dir.Name, dir.FullName);
    }

    public static string GetPath(string name)
    {
        if (Versions.ContainsKey(name))
            return Versions[name];
        return Versions[GetDefaultVersion()];
    }

    public static string GetDefaultVersion()
    {
        if (CheckVersion("Proton 7.0")) return "Proton 7.0";
        return Versions.First().Key;
    }

    public static bool CheckVersion(string name)
    {
        return (Versions.ContainsKey(name)) ? true : false;
    }
}