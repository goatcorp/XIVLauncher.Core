using System;
using System.Diagnostics;
using System.Globalization;

namespace XIVLauncher.Core;

public static class CoreEnvironmentSettings
{
    public static bool? IsDeck => CheckEnvBoolOrNull("XL_DECK");
    public static bool? IsDeckGameMode => CheckEnvBoolOrNull("XL_GAMEMODE");
    public static bool? IsDeckFirstRun => CheckEnvBoolOrNull("XL_FIRSTRUN");
    public static bool IsUpgrade => CheckEnvBool("XL_SHOW_UPGRADE");
    public static bool ClearSettings => CheckEnvBool("XL_CLEAR_SETTINGS");
    public static bool ClearPrefix => CheckEnvBool("XL_CLEAR_PREFIX");
    public static bool ClearDalamud => CheckEnvBool("XL_CLEAR_DALAMUD");
    public static bool ClearPlugins => CheckEnvBool("XL_CLEAR_PLUGINS");
    public static bool ClearTools => CheckEnvBool("XL_CLEAR_TOOLS");
    public static bool ClearLogs => CheckEnvBool("XL_CLEAR_LOGS");
    public static bool ClearAll => CheckEnvBool("XL_CLEAR_ALL");
    public static bool? UseSteam => CheckEnvBoolOrNull("XL_USE_STEAM"); // Fix for Steam Deck users who lock themselves out

    private static bool CheckEnvBool(string key)
    {
        string val = (System.Environment.GetEnvironmentVariable(key) ?? string.Empty).ToLower();
        if (val == "1" || val == "true" || val == "yes" || val == "y" || val == "on") return true;
        return false;
    }

    private static bool? CheckEnvBoolOrNull(string key)
    {
        string val = (System.Environment.GetEnvironmentVariable(key) ?? string.Empty).ToLower();
        if (val == "1" || val == "true" || val == "yes" || val == "y" || val == "on") return true;
        if (val == "0" || val == "false" || val == "no" || val == "n" || val == "off") return false;
        return null;
    }

    public static string GetCleanEnvironmentVariable(string envvar, string badstring = "", string separator = ":")
    {
        string dirty = Environment.GetEnvironmentVariable(envvar) ?? "";
        if (badstring.Equals("")) return dirty;
        return string.Join(separator, Array.FindAll<string>(dirty.Split(separator, StringSplitOptions.RemoveEmptyEntries), s => !s.Contains(badstring)));
    }

    public static string GetCType()
    {
        if (System.OperatingSystem.IsWindows())
            return "";
        var psi = new ProcessStartInfo("sh");
        psi.Arguments = "-c \"locale -a 2>/dev/null | grep -i utf\"";
        psi.RedirectStandardOutput = true;

        var proc = new Process();
        proc.StartInfo = psi;
        proc.Start();
        var output = proc.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return Array.Find(output, s => s.ToUpper().StartsWith("C."));
    }
}