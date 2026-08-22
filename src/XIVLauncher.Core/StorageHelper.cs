using System.IO;
using System.Diagnostics;

using XIVLauncher.Common;

namespace XIVLauncher.Core;

public static class StorageHelper
{
    private static Platform platform;

    private static string xdgPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Program.APP_NAME);

    private static string oldPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".xlcore");

    public static string? GetStoragePath()
    {
        if (!string.IsNullOrEmpty(CoreEnvironmentSettings.UserDir))
        {
            return CoreEnvironmentSettings.UserDir;
        }

        if (OperatingSystem.IsWindows())
        {
            return null; // Use default storage on Windows.
        }

        var xdgStoragePath = new DirectoryInfo(xdgPath);
        var oldStoragePath = new DirectoryInfo(oldPath);
       
        if (xdgStoragePath.Exists)
        {
            return null; 
        }

        if (oldStoragePath.Exists)
        {
            try
            {
                oldStoragePath.MoveTo(xdgStoragePath.FullName); // Move ~/.xlcore to new XDG path.
                return null;
            }
            catch (System.Exception)
            {
                Console.WriteLine($"Warning: Failed to move directory from {oldStoragePath.FullName} to {xdgStoragePath.FullName}");
                return oldStoragePath.FullName; // Return the old storage path ~/.xlcore if the move failed.
            }
        }
        return null; // If no storage directory exists, return null to use the default path.
    }

    public static void MakeSymlink(string storagePath)
    {
        if (OperatingSystem.IsWindows() || !string.IsNullOrEmpty(CoreEnvironmentSettings.UserDir))
        {
            return; // Do not create symlink on Windows, or if a custom user directory is set.
        }

        // This should only happen if XDG_DATA_HOME is on a separate volume from HOME.
        // Make a symlink at XDG_DATA_HOME/dev.goats.xivlauncher pointing to ~/.xlcore
        if (Path.Combine(storagePath) == Path.Combine(oldPath))
        {
            var oldTarget = Directory.ResolveLinkTarget(oldPath, true)?.FullName ?? oldPath;
            try
            {
                Directory.CreateSymbolicLink(xdgPath, oldTarget);
            }
            catch (System.Exception)
            {
                Console.WriteLine($"Warning: Failed to create symlink at {xdgPath} to {oldTarget}");
            }
            return;
        }

        // Catch broken or incorrect symlinks at ~/.xlcore
        if (Directory.Exists(oldPath) || File.Exists(oldPath))
        {
            var isDirectory = File.GetAttributes(oldPath).HasFlag(FileAttributes.Directory);

            var oldTarget = isDirectory ? Directory.ResolveLinkTarget(oldPath, true) : File.ResolveLinkTarget(oldPath, true);
            if (oldTarget is null)
            {
                Console.WriteLine($"Warning: {oldPath} exists but is not a symlink. Please remove or rename it and restart the launcher to create a symlink at {oldPath}.");
                return; // ~/.xlcore is not a symlink. Don't do anything.
            }
            if (Path.Combine(oldTarget.FullName) != Path.Combine(storagePath))
            {
                try
                {
                    if (isDirectory)
                    {
                        Directory.Delete(oldPath);
                    }
                    else
                    {
                        File.Delete(oldPath);
                    }
                    Directory.CreateSymbolicLink(oldPath, storagePath);
                }
                catch (System.Exception ex)
                {                    
                    Console.WriteLine(ex.Message);
                    Console.WriteLine($"Warning: Failed to update symlink at {oldPath} to point to {storagePath}. Please manually update the symlink or remove {oldPath} and restart the launcher to use the new XDG storage location.");
                }
            }
            return;
        }

        // Make a symlink at ~/.xlcore pointing to the storage path
        var storageTarget = Directory.ResolveLinkTarget(storagePath, true)?.FullName ?? storagePath;
        try
        {
            Directory.CreateSymbolicLink(oldPath, storageTarget);
        }
        catch (System.Exception)
        {
            Console.WriteLine($"Warning:Failed to create symlink at {oldPath} to {storageTarget}");
        }
    }

    public static string? FixConfigPath(string configPath)
    {
        if (OperatingSystem.IsWindows() || !string.IsNullOrEmpty(CoreEnvironmentSettings.UserDir) || configPath == null)
            return configPath; // Do not modify the path on Windows, or if a custom user directory is set.

        if (Path.Combine(Program.storage.Root.FullName) == Path.Combine(oldPath))
            return configPath; // If the storage path is still the old path, do not modify the config ini path.

        if (configPath.StartsWith(oldPath))
            return Path.Combine(ReplaceFirst(configPath, oldPath, Program.storage.Root.FullName)); // Return the modified path.

        return configPath; // Return the original path if it does not need to be modified.
    }

    public static DirectoryInfo? FixConfigPath(DirectoryInfo configPath)
    {
        if (OperatingSystem.IsWindows() || !string.IsNullOrEmpty(CoreEnvironmentSettings.UserDir) || configPath == null)
            return configPath; // Do not modify the path on Windows, or if a custom user directory is set.

        if (Path.Combine(Program.storage.Root.FullName) == Path.Combine(oldPath))
            return configPath; // If the storage path is still the old path, do not modify the config ini path.

        if (configPath.FullName.StartsWith(oldPath))
            return new DirectoryInfo(Path.Combine(ReplaceFirst(configPath.FullName, oldPath, Program.storage.Root.FullName))); // Return the modified path.

        return configPath; // Return the original path if it does not need to be modified.
    }

    private static string ReplaceFirst(string text, string search, string replace)
    {
      int pos = text.IndexOf(search);
      if (pos < 0)
      {
        return text;
      }
      return text[..pos] + replace + text[(pos + search.Length)..];
    }
}
