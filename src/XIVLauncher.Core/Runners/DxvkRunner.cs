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

public class DxvkRunner : Runner
{
    public override string RunnerType => "Dxvk";

    private DirectoryInfo ToolFolder => new DirectoryInfo(Path.Combine(Program.storage.Root.FullName, "compatibilitytool", "dxvk"));

    public DxvkRunner(string folder, string url, DirectoryInfo prefix, Dictionary<string, string> env = null)
        : base(folder, url, prefix, env)
    { }

    public override async Task Install()
    {
        if (IsDirectoryEmpty(Path.Combine(ToolFolder.FullName, Folder)))
        {
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

        var prefixinstall = new DirectoryInfo(Path.Combine(Prefix.FullName, "drive_c", "windows", "system32"));
        var files = new DirectoryInfo(Path.Combine(ToolFolder.FullName, Folder, "x64")).GetFiles();

        foreach (FileInfo fileName in files)
        {
            fileName.CopyTo(Path.Combine(prefixinstall.FullName, fileName.Name), true);
        }
    }
}