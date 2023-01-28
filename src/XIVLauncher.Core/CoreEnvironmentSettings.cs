namespace XIVLauncher.Core;

public static class CoreEnvironmentSettings
{
    public static bool? IsDeck => CheckEnvBoolOrNull("XLC_DECK");
    public static bool? IsDeckGameMode => CheckEnvBoolOrNull("XLC_GAMEMODE");
    public static bool? IsDeckFirstRun => CheckEnvBoolOrNull("XLC_FIRSTRUN");
    public static bool IsUpgrade => CheckEnvBool("XLC_SHOW_UPGRADE");
    public static bool ClearSettings => CheckEnvBool("XLC_CLEAR_SETTINGS");
    public static bool ClearPrefix => CheckEnvBool("XLC_CLEAR_PREFIX");
    public static bool ClearPlugins => CheckEnvBool("XLC_CLEAR_PLUGINS");
    public static bool ClearTools => CheckEnvBool("XLC_CLEAR_TOOLS");
    public static bool ClearLogs => CheckEnvBool("XLC_CLEAR_LOGS");
    public static bool ClearAll => CheckEnvBool("XLC_CLEAR_ALL");
    public static bool? UseSteam => CheckEnvBoolOrNull("XLC_USE_STEAM"); // Fix for Steam Deck users who lock themselves out

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
}