using System;
using System.Runtime.InteropServices;

namespace XIVLauncher.Common.Unix.Compatibility.Wine;

public enum FsyncSupport
{
    Supported,
    UnsupportedPlatform,
    OutdatedKernel,
}

public static class WineUtility
{
    public static FsyncSupport SystemFsyncSupport()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return FsyncSupport.UnsupportedPlatform;
        if (Environment.OSVersion.Version.Major < 5 && (Environment.OSVersion.Version.Minor < 16 || Environment.OSVersion.Version.Major < 6))
            return FsyncSupport.OutdatedKernel;
        return FsyncSupport.Supported;
    }
}
