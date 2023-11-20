using System.Net.Http.Headers;
using Newtonsoft.Json;
using Serilog;
using XIVLauncher.Common.Util;

namespace XIVLauncher.Core;

internal class Frontier
{
    private const string LEASE_META_URL = "https://kamori.goats.dev/Launcher/GetLease";

    private const string FRONTIER_FALLBACK_URL = "https://launcher.finalfantasyxiv.com/v650/index.html?rc_lang={0}&time={1}";

    [Flags]
    public enum LeaseFeatureFlags
    {
        None = 0,
        GlobalDisableDalamud = 1,
        GlobalDisableLogin = 1 << 1,
    }

    #pragma warning disable CS8618
    private class Lease
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public string? CutOffBootver { get; set; }

        public string FrontierUrl { get; set; }

        public LeaseFeatureFlags Flags { get; set; }

        public string ReleasesList { get; set; }

        public DateTime? ValidUntil { get; set; }
    }
    #pragma warning restore CS8618

    public class LeaseAcquisitionException : Exception
    {
        public LeaseAcquisitionException(string message)
            : base($"Couldn't acquire lease: {message}")
        {
        }
    }

    public static async Task<string> GetFrontierUrl()
    {
        using var client = new HttpClient
        {
            DefaultRequestHeaders =
            {
                UserAgent = { new ProductInfoHeaderValue("XIVLauncher", AppUtil.GetGitHash()) }
            }
        };
        client.Timeout = TimeSpan.FromSeconds(15);
        client.DefaultRequestHeaders.AddWithoutValidation("X-XL-Track", "Release");
        client.DefaultRequestHeaders.AddWithoutValidation("X-XL-LV", "0");
        client.DefaultRequestHeaders.AddWithoutValidation("X-XL-HaveVersion", AppUtil.GetAssemblyVersion());
        client.DefaultRequestHeaders.AddWithoutValidation("X-XL-HaveAddon", "no");
        client.DefaultRequestHeaders.AddWithoutValidation("X-XL-FirstStart", "no");
        client.DefaultRequestHeaders.AddWithoutValidation("X-XL-HaveWine", "no");

        var result = FRONTIER_FALLBACK_URL;

        HttpResponseMessage? response;
        try
        {
            response = await client.GetAsync(LEASE_META_URL).ConfigureAwait(false);   
            response.EnsureSuccessStatusCode();
            if (response.Headers.TryGetValues("X-XL-Canary", out var values) &&
                values.FirstOrDefault() == "yes")
            {
                Log.Information("Updates: Received canary track lease!");
            }

            var leaseData = JsonConvert.DeserializeObject<Lease>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));

            if (!leaseData.Success)
                throw new LeaseAcquisitionException(leaseData.Message!);

            result = leaseData.FrontierUrl;
        }
        catch (TaskCanceledException tcex)
        {
            Log.Error(tcex, "Could not get frontier url from server. Request timed out.");
        }
        catch (LeaseAcquisitionException leex)
        {
            Log.Error(leex, leex.Message);
        }
        catch (HttpRequestException hrex)
        {
            Log.Error(hrex, "Could not get frontier url from server. Could not contact kamori.goats.dev.");
        }
        catch (JsonSerializationException jsex)
        {
            Log.Error(jsex, "The server returned invalid data.");
        }
        return result;
    }
}