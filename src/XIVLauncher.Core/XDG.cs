using System.IO;

using XIVLauncher.Common;

namespace XIVLauncher.Core;

public static class XDG
{
    private static Platform platform;

    static XDG()
    {
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            platform = Platform.Win32;
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            platform = Platform.Linux;
        }
        else if (Environment.OSVersion.Platform == PlatformID.MacOSX)
        {
            platform = Platform.Mac;
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }
    }
    public static string? GetStoragePath(string appName, string? overridePath = null)
    {
        if (!string.IsNullOrEmpty(overridePath))
        {
            return overridePath;
        }
        if (platform == Platform.Win32)
        {
            return null; // Let Storage class handle it. Windows works fine.
        }
        else if (platform == Platform.Linux  || platform == Platform.Mac)
        {
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName)))
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName); // Use XDG Base Directory spec path if it exists.
                                                                                                                         // This is ~/.local/share/xlcore on Linux and ~/Library/Application Support/xlcore on Mac.
                                                                                                                         // This can be overridden with the XL_USERDIR environment variable, which will take precedence over both the old path and the XDG path.
            }
            if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), $".{appName}")))
            {
                return null; // Let Storage class handle it and use the old ~/.xlcore path if it exists, and the XDG_DATA_HOME/xlcore directory does not exist.
            }
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName); // Use XDG Base Directory spec path for new installs on Linux and Mac.
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported platform");
        }
    }
}