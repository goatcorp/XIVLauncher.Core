using System.Numerics;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Serilog;
using XIVLauncher.Core;

namespace XIVLauncher.Core.UnixCompatibility;

public static class SteamCompatibilityTool
{
    public static bool IsSteamInstalled => Directory.Exists(Program.Config.SteamPath);

    public static bool IsSteamFlatpakInstalled => Directory.Exists(Program.Config.SteamFlatpakPath);

    public static bool IsSteamToolInstalled => Directory.Exists(Path.Combine(Program.Config.SteamPath, "compatibilitytools.d", "xlcore"));

    public static bool IsSteamFlatpakToolInstalled => Directory.Exists(Path.Combine(Program.Config.SteamFlatpakPath, "compatibilitytools.d", "xlcore"));

    private static string findXIVLauncherFiles()
    {
        return System.AppDomain.CurrentDomain.BaseDirectory;
    }

    public static void CreateTool(string path)
    {
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

        xlcore.Delete();
        compatibilitytool_vdf.Delete();
        toolmanifest_vdf.Delete();
        openssl_fix.Delete();

        using (var fs = xlcore.Create())
        {
            var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("xlcore");
            resource.CopyTo(fs);
            fs.Close();
        }

        // File.SetUnixFileMode() doesn't exist, for some reason, so just run chmod
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
    }

    public static void DeleteTool(string path)
    {
        var steamToolFolder = new DirectoryInfo(Path.Combine(path, "compatibilitytools.d", "xlcore"));
        if (!steamToolFolder.Exists) return;
        steamToolFolder.Delete(true);
        Log.Verbose($"[SCT] Deleted Steam compatibility tool at folder {steamToolFolder.FullName}");
    }
}