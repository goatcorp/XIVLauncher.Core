using System;
using System.IO;
using System.Linq;
using Serilog;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Core;

public static class ProtonManager
{
    public static Dictionary<string, string> Versions { get; private set; }

    public static void GetVersions(string steamRoot)
    {
        Versions = new Dictionary<string, string>();
        var commonDir = new DirectoryInfo(Path.Combine(steamRoot, "steamapps","common"));
        var compatDir = new DirectoryInfo(Path.Combine(steamRoot, "compatibilitytools.d"));
        var steamRootExists = true;

        try
        {
            if (Directory.Exists(steamRoot))
            {
                Log.Information($"Steam Root is {steamRoot}");
                Log.Verbose($"Steam Common Directory is {commonDir.FullName}");
                Log.Verbose($"Steam Compatibility Tools Directory is {compatDir.FullName}");
            }
            else
            {
                throw new DirectoryNotFoundException($"Steam Root directory \"{steamRoot}\" does not exist or is not a directory.");
            }
        }
        catch (DirectoryNotFoundException ex)
        {
            Log.Error(ex, "No Steam directory found. Proton disabled.");
            steamRootExists = false;
        }
        
        if (steamRootExists)
        {
            try
            {

                var commonDirs = commonDir.GetDirectories("Proton*");
                foreach (var dir in commonDirs)
                    if (File.Exists(Path.Combine(dir.FullName,"proton"))) Versions.Add(dir.Name, dir.FullName);
            }
            catch (DirectoryNotFoundException ex)
            {
                Log.Error(ex, $"Couldn't find any Proton versions in {commonDir}. Check launcher.ini and make sure that SteamPath points to your local Steam root. This is probably something like /home/deck/.steam/root or /home/deck/.local/share/Steam.");
            }
            try {
                var compatDirs = compatDir.GetDirectories("*Proton*");
                foreach (var dir in compatDirs)
                    if (File.Exists(Path.Combine(dir.FullName,"proton"))) Versions.Add(dir.Name, dir.FullName);
            }
            catch (DirectoryNotFoundException ex)
            {
                Log.Error(ex, $"Couldn't find any Proton versions in {compatDir}. Check launcher.ini and make sure that SteamPath points to your local Steam root. This is probably something like /home/deck/.steam/root or /home/deck/.local/share/Steam.");
            }
        }

        if (Versions.Count == 0)
            Versions.Add("DISABLED", "No valid Proton verions found. Bad SteamPath or Steam not installed.");
    }

    public static string GetPath(string name)
    {
        if (Versions.ContainsKey(name))
            return Versions[name];
        return Versions[GetDefaultVersion()];
    }

    public static string GetDefaultVersion()
    {
        if (VersionExists("Proton 7.0")) return "Proton 7.0";
        return Versions.First().Key;
    }

    public static bool VersionExists(string name)
    {
        return (Versions.ContainsKey(name)) ? true : false;
    }

    public static bool IsValid()
    {
        return Versions.ContainsKey("DISABLED") ? false : true;
    }
}