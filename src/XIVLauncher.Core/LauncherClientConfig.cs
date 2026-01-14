
using System.Net.Http.Json;

using Serilog;

namespace XIVLauncher.Core;


/// <summary>
///     Represents a response from Kamori's GetLauncherClientConfig endpoint.
/// </summary>
public readonly struct LauncherClientConfig
{
    private const string LAUNCHER_CONFIG_URL = "https://kamori.goats.dev/Launcher/GetLauncherClientConfig";
    private const string FRONTIER_FALLBACK = "https://launcher.finalfantasyxiv.com/v710/index.html?rc_lang={0}&time={1}";

    public string frontierUrl { get; init; }
    public string? cutOffBootver { get; init; }
    public uint flags { get; init; }

    public static async Task<LauncherClientConfig> GetAsync()
    {
        try
        {
            return await Program.HttpClient.GetFromJsonAsync<LauncherClientConfig>(LAUNCHER_CONFIG_URL).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not obtain LauncherClientConfig");
            return new LauncherClientConfig()
            {
                frontierUrl = FRONTIER_FALLBACK,
            };
        }
    }
}

