using System.Numerics;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Serilog;
using XIVLauncher.Core;

namespace XIVLauncher.Core.UnixCompatibility;

public static class SteamCompatibilityTool
{
    // This is here to prevent auto-updating with different releases of XLCore. So XIVLauncher-RB will not overwrite official, vice versa. 
    private const string RELEASE = "Official";
    public static bool IsSteamInstalled => Directory.Exists(Program.Config.SteamPath);

    public static bool IsSteamFlatpakInstalled => Directory.Exists(Program.Config.SteamFlatpakPath);

    public static bool IsSteamToolInstalled => Directory.Exists(Path.Combine(Program.Config.SteamPath, "compatibilitytools.d", "xlcore"));

    public static bool IsSteamFlatpakToolInstalled => Directory.Exists(Path.Combine(Program.Config.SteamFlatpakPath, "compatibilitytools.d", "xlcore"));

    private static string findXIVLauncherFiles()
    {
        return System.AppDomain.CurrentDomain.BaseDirectory;
    }

    private static void SetConfigValues(bool isFlatpak)
    {
        if (isFlatpak)
            Program.Config.SteamFlatpakToolInstalled = IsSteamFlatpakToolInstalled;
        else
            Program.Config.SteamToolInstalled = IsSteamToolInstalled;
    }

    public static void CreateTool(bool isFlatpak)
    {
        var path = isFlatpak ? Program.Config.SteamFlatpakPath : Program.Config.SteamPath;
        var compatfolder = new DirectoryInfo(Path.Combine(path, "compatibilitytools.d"));
        compatfolder.Create();
        var destination = new DirectoryInfo(Path.Combine(compatfolder.FullName, "xlcore"));
        if (File.Exists(destination.FullName))
            File.Delete(destination.FullName);
        if (destination.Exists)
            destination.Delete(true);
        
        destination.Create();
        destination.CreateSubdirectory("XIVLauncher");
        destination.CreateSubdirectory("bin");
        destination.CreateSubdirectory("lib");

        var xlcore = new FileInfo(Path.Combine(destination.FullName, "xlcore"));
        var compatibilitytool_vdf = new FileInfo(Path.Combine(destination.FullName, "compatibilitytool.vdf"));
        var toolmanifest_vdf = new FileInfo(Path.Combine(destination.FullName, "toolmanifest.vdf"));
        var openssl_fix = new FileInfo(Path.Combine(destination.FullName, "openssl_fix.cnf"));
        var version = new FileInfo(Path.Combine(destination.FullName, "version"));
        
        xlcore.Delete();
        compatibilitytool_vdf.Delete();
        toolmanifest_vdf.Delete();
        openssl_fix.Delete();
        version.Delete();

        using (var fs = xlcore.Create())
        {
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("xlcore");
            resource.CopyTo(fs);
            fs.Close();
        }

        // File.SetUnixFileMode() doesn't exist in .NET 6, so just run chmod
        var psi = new ProcessStartInfo("/bin/chmod");
        psi.ArgumentList.Add("+x");
        psi.ArgumentList.Add(xlcore.FullName);
        using (Process proc = new Process())
        {
            proc.StartInfo = psi;
            proc.Start();
            proc.WaitForExit();
        }

        using (var fs = compatibilitytool_vdf.Create())
        {
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("compatibilitytool.vdf");
            resource.CopyTo(fs);
            fs.Close();
        }

        using (var fs = toolmanifest_vdf.Create())
        {
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("toolmanifest.vdf");
            resource.CopyTo(fs);
            fs.Close();
        }

        using (var fs = openssl_fix.Create())
        {
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("openssl_fix.cnf");
            resource.CopyTo(fs);
            fs.Close();
        }
        using (var fs = version.Create())
        {
            byte[] resource = new System.Text.UTF8Encoding(true).GetBytes(Program.CoreVersion.ToString() + "\n" + RELEASE);
            fs.Write(resource, 0, resource.Length);
            fs.Close();
        }

        // Copy XIVLauncher files
        var XIVLauncherFiles = new DirectoryInfo(findXIVLauncherFiles());
        foreach (var file in XIVLauncherFiles.GetFiles())
        {
            file.CopyTo(Path.Combine(destination.FullName, "XIVLauncher", file.Name), true);
        }

        var aria2c = new FileInfo("/app/bin/aria2c");
        var libsecret = new FileInfo("/app/lib/libsecret-1.so.0.0.0");

        if (aria2c.Exists)
            aria2c.CopyTo(Path.Combine(destination.FullName, "bin", "aria2c"));
        
        if (libsecret.Exists)
        {
            var libPath = Path.Combine(destination.FullName, "lib");
            libsecret.CopyTo(Path.Combine(libPath, "libsecret-1.so.0.0.0"));
            File.CreateSymbolicLink(Path.Combine(libPath, "libsecret-1.so"), "libsecret-1.so.0.0.0");
        }
        
        Log.Verbose($"[SCT] XIVLauncher installed as Steam compatibility tool to folder {destination.FullName}");
        SetConfigValues(isFlatpak);
    }

    public static void DeleteTool(bool isFlatpak)
    {
        var path = isFlatpak ? Program.Config.SteamFlatpakPath : Program.Config.SteamPath;
        var steamToolFolder = new DirectoryInfo(Path.Combine(path, "compatibilitytools.d", "xlcore"));
        if (!steamToolFolder.Exists) return;
        steamToolFolder.Delete(true);
        Log.Verbose($"[SCT] Deleted Steam compatibility tool at folder {steamToolFolder.FullName}");
        SetConfigValues(isFlatpak);
    }

    public static void UpdateSteamTools(bool console = false)
    {
        if (CoreEnvironmentSettings.IsSteamCompatTool)
            return;

        var message = UpdateTool(false);
        if (console)
            Console.WriteLine(message);
        
        message = UpdateTool(true);
        if (console)
            Console.WriteLine(message);
    }

    public static string CheckVersion(bool isFlatpak)
    {
        var path = isFlatpak ? Program.Config.SteamFlatpakPath : Program.Config.SteamPath;
        var versionFile = new FileInfo(Path.Combine(path, "compatibilitytools.d", "xlcore", "version"));
        
        if (!versionFile.Exists)
            return "";
        
        var version = "";
        try
        {
            using (var sr = new StreamReader(versionFile.FullName))
            {
                var toolVersion = sr.ReadLine() ?? "";
                var release = sr.ReadLine() ?? "";
                sr.Close();
                if (string.IsNullOrEmpty(toolVersion))
                    toolVersion = "Unknown";
                if (string.IsNullOrEmpty(release))
                    release = "Unknown";
                version = toolVersion + ',' + release; 
            }
        }
        catch (Exception e)
        {
            Log.Error(e, $"Could not get the Steam {(isFlatpak ? "(flatpak) " : "")}compatibility tool version at {path}.");
            version = "Unknown,Unknown";
        }
        return version;
    }

    private static string UpdateTool(bool isFlatpak)
    {
        var message = string.Empty;

        if ((isFlatpak && !IsSteamFlatpakToolInstalled) || (!isFlatpak && !IsSteamToolInstalled))
        {
            message = $"Steam {(isFlatpak ? "(flatpak) " : "")}compatibility tool is not installed. Nothing to update.";
            Log.Information(message);
            return message;
        }
        var path = isFlatpak ? Program.Config.SteamFlatpakPath : Program.Config.SteamPath;
        var versionFile = new FileInfo(Path.Combine(path, "compatibilitytools.d", "xlcore", "version"));

        if (!versionFile.Exists)
        {
            CreateTool(isFlatpak);
            message = $"Updating Steam {(isFlatpak ? "(flatpak) " : "")}compatibility tool at {path}/compatibilitytools.d/xlcore to version {Program.CoreVersion.ToString()}";
            Log.Information(message);
            return message;
        }

        var toolInfo = CheckVersion(isFlatpak).Split(',', 2);
        var toolVersion = toolInfo[0];
        var release = toolInfo[1];

        try
        {
            if (release != RELEASE)
            {
                message = $"Steam {(isFlatpak ? "(flatpak) " : "")}compatibility Tool mismatch! \"{release}\" release is installed, but this is \"{RELEASE}\" release. Not installing update.";
                Log.Warning(message);
                return message;
            }
            if (Version.Parse(toolVersion) < Program.CoreVersion)
            {
                CreateTool(isFlatpak);
                message = $"Updating Steam {(isFlatpak ? "(flatpak) " : "")}compatibility tool at {path}/compatibilitytools.d/xlcore to version {Program.CoreVersion.ToString()}";
                Log.Information(message);
            }
            else
            {
                message = $"Steam {(isFlatpak ? "(flatpak) " : "")}compatibility tool version at {path} is {toolVersion} >= {Program.CoreVersion.ToString()}, nothing to do.";
                Log.Information(message);
            }
        }
        catch (Exception e)
        {
            message = $"Could not get the Steam {(isFlatpak ? "(flatpak) " : "")}compatibility tool version at {path}. Not installing update.";
            Log.Error(e, message);
        }
        return message;
    }
}