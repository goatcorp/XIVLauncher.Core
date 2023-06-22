using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using XIVLauncher.Common.Util;
using XIVLauncher.Common.Unix.Compatibility;

namespace XIVLauncher.Core.Runners;

public class WineRunner : Runner
{
    public override string RunnerType => "Wine";

    public override string RunCommand { get; }

    public override string RunArguments { get; }

    public override string Server { get; }

    public override string PathArguments => "winepath --windows";

    private DirectoryInfo ToolFolder => new DirectoryInfo(Path.Combine(Program.storage.Root.FullName, "compatibilitytool", "wine"));

    private string WineServer => Path.Combine(ToolFolder.FullName, "wine", Folder, "bin", "wineserver");

    public WineRunner(string winepath, string wineargs, string folder, string url, DirectoryInfo prefix, Dictionary<string, string> env = null)
        : base(folder, url, prefix, env)
    {
        if (string.IsNullOrEmpty(winepath))
        {
            RunCommand = Path.Combine(ToolFolder.FullName, Folder, "bin", "wine64");
            Server = Path.Combine(ToolFolder.FullName, Folder, "bin", "wineserver");
        }
        else
        {
            RunCommand = Path.Combine(winepath, "wine64");
            Server = Path.Combine(winepath, "wineserver");
        }
        RunArguments = wineargs;
    }

    public override async Task Install()
    {
        if (IsDirectoryEmpty(Path.Combine(ToolFolder.FullName, Folder)))
        {
            Log.Information($"Downloading Tool to {ToolFolder.FullName}");
            if (string.IsNullOrEmpty(DownloadUrl))
            {
                Log.Error($"Attempted to download runner {RunnerType} without a download Url.");
                throw new InvalidOperationException($"{RunnerType} runner does not exist, and no download URL was provided for it.");
            }
            Log.Information($"{Folder} does not exist. Downloading...");
            using var client = new HttpClient();
            var tempPath = Path.GetTempFileName();

            File.WriteAllBytes(tempPath, await client.GetByteArrayAsync(DownloadUrl));
            PlatformHelpers.Untar(tempPath, ToolFolder.FullName);

            File.Delete(tempPath);
        }
        else
            Log.Information("Did not try to download Wine.");
    }
}