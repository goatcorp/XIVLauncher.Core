using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Serilog;

using XIVLauncher.Common.Unix.Compatibility.Dxvk;
using XIVLauncher.Common.Unix.Compatibility.Wine;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Common.Unix.Compatibility;

public class CompatibilityTools
{
    private const string WINEDLLOVERRIDES = "msquic=,mscoree=n,b;d3d9,d3d11,d3d10core,dxgi=";
    private const uint DXVK_CLEANUP_THRESHHOLD = 5;
    private const uint WINE_CLEANUP_THRESHHOLD = 5;

    private readonly DirectoryInfo wineDirectory;
    private readonly DirectoryInfo dxvkDirectory;
    private readonly StreamWriter logWriter;

    private string WineBinPath => Settings.StartupType == WineStartupType.Managed ?
                                    Path.Combine(wineDirectory.FullName, Settings.WineRelease.Name, "bin") :
                                    Settings.CustomBinPath;
    private string Wine64Path => Path.Combine(WineBinPath, "wine64");
    private string WineServerPath => Path.Combine(WineBinPath, "wineserver");

    private readonly DxvkVersion dxvkVersion;
    private readonly DxvkHudType hudType;
    private readonly bool gamemodeOn;
    private readonly string dxvkAsyncOn;

    public bool IsToolReady { get; private set; }
    public WineSettings Settings { get; private set; }
    public bool IsToolDownloaded => File.Exists(Wine64Path) && Settings.Prefix.Exists;

    public CompatibilityTools(WineSettings wineSettings, DxvkVersion dxvkVersion, DxvkHudType hudType, bool gamemodeOn, bool dxvkAsyncOn, DirectoryInfo toolsFolder)
    {
        this.Settings = wineSettings;
        this.dxvkVersion = dxvkVersion;
        this.hudType = hudType;
        this.gamemodeOn = gamemodeOn;
        this.dxvkAsyncOn = dxvkAsyncOn ? "1" : "0";

        this.wineDirectory = new DirectoryInfo(Path.Combine(toolsFolder.FullName, "wine"));
        this.dxvkDirectory = new DirectoryInfo(Path.Combine(toolsFolder.FullName, "dxvk"));

        // TODO: Replace these with a nicer way of preventing a pileup of compat tools,
        // This implementation is just a hack.
        if (Directory.GetFiles(dxvkDirectory.FullName).Length >= DXVK_CLEANUP_THRESHHOLD)
        {
            Directory.Delete(dxvkDirectory.FullName, true);
            Directory.CreateDirectory(dxvkDirectory.FullName);
        }
        if (Directory.GetFiles(wineDirectory.FullName).Length >= WINE_CLEANUP_THRESHHOLD)
        {
            Directory.Delete(wineDirectory.FullName, true);
            Directory.CreateDirectory(wineDirectory.FullName);
        }

        this.logWriter = new StreamWriter(wineSettings.LogFile.FullName);

        if (wineSettings.StartupType == WineStartupType.Managed)
        {
            if (!this.wineDirectory.Exists)
                this.wineDirectory.Create();
            if (!this.dxvkDirectory.Exists)
                this.dxvkDirectory.Create();
        }

        if (!wineSettings.Prefix.Exists)
            wineSettings.Prefix.Create();
    }

    public async Task EnsureTool(DirectoryInfo tempPath)
    {
        if (!File.Exists(Wine64Path))
        {
            Log.Information($"Compatibility tool does not exist, downloading {Settings.WineRelease.DownloadUrl}");
            await DownloadTool(tempPath).ConfigureAwait(false);
        }

        EnsurePrefix();
        await Dxvk.Dxvk.InstallDxvk(Settings.Prefix, dxvkDirectory, dxvkVersion).ConfigureAwait(false);

        IsToolReady = true;
    }

    private async Task DownloadTool(DirectoryInfo tempPath)
    {
        using var client = new HttpClient();
        var tempFilePath = Path.Combine(tempPath.FullName, $"{Guid.NewGuid()}");

        await File.WriteAllBytesAsync(tempFilePath, await client.GetByteArrayAsync(Settings.WineRelease.DownloadUrl).ConfigureAwait(false)).ConfigureAwait(false);

        PlatformHelpers.Untar(tempFilePath, this.wineDirectory.FullName);

        Log.Information("Compatibility tool successfully extracted to {Path}", this.wineDirectory.FullName);

        File.Delete(tempFilePath);
    }

    public void EnsurePrefix()
    {
        RunInPrefix("cmd /c dir %userprofile%/Documents > nul").WaitForExit();
    }

    public Process RunInPrefix(string command, string workingDirectory = "", IDictionary<string, string> environment = null, bool redirectOutput = false, bool writeLog = false, bool wineD3D = false)
    {
        var psi = new ProcessStartInfo(Wine64Path);
        psi.Arguments = command;

        Log.Verbose("Running in prefix: {FileName} {Arguments}", psi.FileName, command);
        return RunInPrefix(psi, workingDirectory, environment, redirectOutput, writeLog, wineD3D);
    }

    public Process RunInPrefix(string[] args, string workingDirectory = "", IDictionary<string, string> environment = null, bool redirectOutput = false, bool writeLog = false, bool wineD3D = false)
    {
        var psi = new ProcessStartInfo(Wine64Path);
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        Log.Verbose("Running in prefix: {FileName} {Arguments}", psi.FileName, psi.ArgumentList.Aggregate(string.Empty, (a, b) => a + " " + b));
        return RunInPrefix(psi, workingDirectory, environment, redirectOutput, writeLog, wineD3D);
    }

    private void MergeDictionaries(StringDictionary a, IDictionary<string, string> b)
    {
        if (b is null)
            return;

        foreach (var keyValuePair in b)
        {
            if (a.ContainsKey(keyValuePair.Key))
                a[keyValuePair.Key] = keyValuePair.Value;
            else
                a.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    private Process RunInPrefix(ProcessStartInfo psi, string workingDirectory, IDictionary<string, string> environment, bool redirectOutput, bool writeLog, bool wineD3D)
    {
        psi.RedirectStandardOutput = redirectOutput;
        psi.RedirectStandardError = writeLog;
        psi.UseShellExecute = false;
        psi.WorkingDirectory = workingDirectory;

        var ogl = wineD3D || this.dxvkVersion == DxvkVersion.Disabled;

        var wineEnviromentVariables = new Dictionary<string, string>
        {
            { "WINEPREFIX", Settings.Prefix.FullName },
            { "WINEDLLOVERRIDES", $"{WINEDLLOVERRIDES}{(ogl ? "b" : "n,b")}" }
        };

        if (!string.IsNullOrEmpty(Settings.DebugVars))
        {
            wineEnviromentVariables.Add("WINEDEBUG", Settings.DebugVars);
        }

        wineEnviromentVariables.Add("XL_WINEONLINUX", "true");
        string ldPreload = Environment.GetEnvironmentVariable("LD_PRELOAD") ?? "";

        string dxvkHud = hudType switch
        {
            DxvkHudType.None => "0",
            DxvkHudType.Fps => "fps",
            DxvkHudType.Full => "full",
            _ => throw new ArgumentOutOfRangeException()
        };

        if (this.gamemodeOn == true && !ldPreload.Contains("libgamemodeauto.so.0"))
        {
            ldPreload = ldPreload.Equals("", StringComparison.OrdinalIgnoreCase) ? "libgamemodeauto.so.0" : ldPreload + ":libgamemodeauto.so.0";
        }

        wineEnviromentVariables.Add("DXVK_HUD", dxvkHud);
        wineEnviromentVariables.Add("DXVK_ASYNC", dxvkAsyncOn);
        wineEnviromentVariables.Add("WINEESYNC", Settings.EsyncOn);
        wineEnviromentVariables.Add("WINEFSYNC", Settings.FsyncOn);

        wineEnviromentVariables.Add("LD_PRELOAD", ldPreload);

        MergeDictionaries(psi.EnvironmentVariables, wineEnviromentVariables);
        MergeDictionaries(psi.EnvironmentVariables, environment);

        Process helperProcess = new();
        helperProcess.StartInfo = psi;
        helperProcess.ErrorDataReceived += new DataReceivedEventHandler((_, errLine) =>
        {
            if (string.IsNullOrEmpty(errLine.Data))
                return;

            try
            {
                logWriter.WriteLine(errLine.Data);
                Console.Error.WriteLine(errLine.Data);
            }
            catch (Exception ex) when (ex is ArgumentOutOfRangeException ||
                                       ex is OverflowException ||
                                       ex is IndexOutOfRangeException)
            {
                // very long wine log lines get chopped off after a (seemingly) arbitrary limit resulting in strings that are not null terminated
                //logWriter.WriteLine("Error writing Wine log line:");
                //logWriter.WriteLine(ex.Message);
            }
        });

        helperProcess.Start();
        if (writeLog)
            helperProcess.BeginErrorReadLine();

        return helperProcess;
    }

    public int[] GetProcessIds(string executableName)
    {
        var wineDbg = RunInPrefix("winedbg --command \"info proc\"", redirectOutput: true);
        var output = wineDbg.StandardOutput.ReadToEnd();
        var matchingLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Where(l => l.Contains(executableName));
        return matchingLines.Select(l => int.Parse(l.Substring(1, 8), System.Globalization.NumberStyles.HexNumber)).ToArray();
    }

    public int GetProcessId(string executableName)
    {
        return GetProcessIds(executableName).FirstOrDefault();
    }

    public Int32 GetUnixProcessId(Int32 winePid)
    {
        var environment = new Dictionary<string, string>();
        var wineDbg = RunInPrefix("winedbg --command \"info procmap\"", redirectOutput: true, environment: environment);
        var output = wineDbg.StandardOutput.ReadToEnd();
        if (output.Contains("syntax error\n") || output.Contains("Exception c0000005")) // valve wine changed the error message
        {
            var processName = GetProcessName(winePid);
            return GetUnixProcessIdByName(processName);
        }
        var matchingLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1).Where(
            l => int.Parse(l.Substring(1, 8), System.Globalization.NumberStyles.HexNumber) == winePid);
        var unixPids = matchingLines.Select(l => int.Parse(l.Substring(10, 8), System.Globalization.NumberStyles.HexNumber)).ToArray();
        return unixPids.FirstOrDefault();
    }

    private string GetProcessName(Int32 winePid)
    {
        var environment = new Dictionary<string, string>();
        var wineDbg = RunInPrefix("winedbg --command \"info proc\"", redirectOutput: true, environment: environment);
        var output = wineDbg.StandardOutput.ReadToEnd();
        var matchingLines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1).Where(
            l => int.Parse(l.Substring(1, 8), System.Globalization.NumberStyles.HexNumber) == winePid);
        var processNames = matchingLines.Select(l => l.Substring(20).Trim('\'')).ToArray();
        return processNames.FirstOrDefault();
    }

    private Int32 GetUnixProcessIdByName(string executableName)
    {
        int closest = 0;
        int early = 0;
        var currentProcess = Process.GetCurrentProcess(); // Gets XIVLauncher.Core's process
        bool nonunique = false;
        foreach (var process in Process.GetProcessesByName(executableName))
        {
            if (process.Id < currentProcess.Id)
            {
                early = process.Id;
                continue;  // Process was launched before XIVLauncher.Core
            }
            // Assume that the closest PID to XIVLauncher.Core's is the correct one. But log an error if more than one is found.
            if ((closest - currentProcess.Id) > (process.Id - currentProcess.Id) || closest == 0)
            {
                if (closest != 0) nonunique = true;
                closest = process.Id;
            }
            if (nonunique) Log.Error($"More than one {executableName} found! Selecting the most likely match with process id {closest}.");
        }
        // Deal with rare edge-case where pid rollover causes the ffxiv pid to be lower than XLCore's.
        if (closest == 0 && early != 0) closest = early;
        if (closest != 0) Log.Verbose($"Process for {executableName} found using fallback method: {closest}. XLCore pid: {currentProcess.Id}");
        return closest;
    }

    public string UnixToWinePath(string unixPath)
    {
        var launchArguments = new string[] { "winepath", "--windows", unixPath };
        var winePath = RunInPrefix(launchArguments, redirectOutput: true);
        var output = winePath.StandardOutput.ReadToEnd();
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
    }

    public void AddRegistryKey(string key, string value, string data)
    {
        var args = new string[] { "reg", "add", key, "/v", value, "/d", data, "/f" };
        var wineProcess = RunInPrefix(args);
        wineProcess.WaitForExit();
    }

    public void Kill()
    {
        var psi = new ProcessStartInfo(WineServerPath)
        {
            Arguments = "-k"
        };
        psi.EnvironmentVariables.Add("WINEPREFIX", Settings.Prefix.FullName);

        Process.Start(psi);
    }
}
