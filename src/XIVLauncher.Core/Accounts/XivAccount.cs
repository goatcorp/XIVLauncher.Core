using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog;

namespace XIVLauncher.Core.Accounts;

public class XivAccount
{
    [JsonIgnore]
    public string Id => $"{this.UserName}-{this.UseOtp}-{this.UseSteamServiceAccount}";

    public override string ToString() => this.Id;

    public string UserName { get; private set; }

    [JsonIgnore]
    public string Password
    {
        get
        {
            if (string.IsNullOrEmpty(this.UserName))
                return string.Empty;

            var credentials = Program.Secrets.GetPassword(this.UserName);
            return credentials ?? string.Empty;
        }
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                Program.Secrets.SavePassword(this.UserName, value);
            }
        }
    }

    public bool SavePassword { get; set; }
    public bool UseSteamServiceAccount { get; set; }
    public bool UseOtp { get; set; }

    public string ChosenCharacterName = string.Empty;
    public string ChosenCharacterWorld = string.Empty;
    public string ThumbnailUrl = string.Empty;

    public string LastSuccessfulOtp = string.Empty;

    public XivAccount(string userName)
    {
        this.UserName = userName.ToLower();
    }

    public string? FindCharacterThumb()
    {
        if (string.IsNullOrEmpty(this.ChosenCharacterName) || string.IsNullOrEmpty(this.ChosenCharacterWorld))
            return null;

        try
        {
            dynamic searchResponse = GetCharacterSearch(this.ChosenCharacterName, this.ChosenCharacterWorld)
                                     .GetAwaiter().GetResult();

            if (searchResponse.Results.Count > 1) //If we get more than one match from XIVAPI
            {
                foreach (var accountInfo in searchResponse.Results)
                {
                    //We have to check with it all lower in case they type their character name LiKe ThIsLoL. The server XIVAPI returns also contains the DC name, so let's just do a contains on the server to make it easy.
                    if (accountInfo.Name.Value.ToLower() == this.ChosenCharacterName.ToLower() && accountInfo.Server.Value.ToLower().Contains(this.ChosenCharacterWorld.ToLower()))
                    {
                        return accountInfo.Avatar.Value;
                    }
                }
            }

            return searchResponse.Results.Count > 0 ? (string)searchResponse.Results[0].Avatar : null;
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Couldn't download character search");

            return null;
        }
    }

    private const string URL = "https://xivapi.com/";

    public static async Task<JObject> GetCharacterSearch(string name, string world)
    {
        return await Get("character/search" + $"?name={name}&server={world}").ConfigureAwait(false);
    }

    public static async Task<dynamic> Get(string endpoint)
    {
        var result = await Program.HttpClient.GetStringAsync(URL + endpoint).ConfigureAwait(false);
        var parsedObject = JObject.Parse(result);
        return parsedObject;
    }
}
