using Serilog;

namespace XIVLauncher.Core;

public static class UpdateCheck
{
    private const string UPDATE_URL = "https://raw.githubusercontent.com/goatcorp/xlcore-distrib/main/version.txt";

    public static bool IsUpdateCheckComplete { get; private set; } = false;

    public static bool IsUpdateAvailable { get; private set; } = false;

    public static async Task<VersionCheckResult> CheckForUpdate(bool doVersionCheck)
    {
        if (!doVersionCheck)
        {
            IsUpdateCheckComplete = true;
            return new VersionCheckResult
            {
                Success = true,
                WantVersion = AppUtil.GetAssemblyVersion(),
                NeedUpdate = false,
            };
        }

        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(15);

            var response = await client.GetStringAsync(UPDATE_URL).ConfigureAwait(false);
            var remoteVersion = Version.Parse(response);

            var localVersion = Version.Parse(AppUtil.GetAssemblyVersion());

            // This order is important! If you swap these lines, MainPage.ProcessLogin will
            // not cancel login attempt when update warning shows.
            IsUpdateAvailable = remoteVersion > localVersion;
            IsUpdateCheckComplete = true;

            return new VersionCheckResult
            {
                Success = true,
                WantVersion = response,
                NeedUpdate = remoteVersion > localVersion,
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not check version");

            IsUpdateCheckComplete = true;

            return new VersionCheckResult
            {
                Success = false,
            };
        }
    }

    public class VersionCheckResult
    {
        public bool Success { get; set; }
        public bool NeedUpdate { get; init; }
        public string? WantVersion { get; init; }
    }
}