using XIVLauncher.Common;

namespace XIVLauncher.Core.Components.SettingsPage.Tabs;

public class SettingsTabGame : SettingsTab
{
    public override SettingsEntry[] Entries { get; } =
    {
        new SettingsEntry<DirectoryInfo>("Game Path", "Where the game is or will be installed.", () => Program.Config.GamePath, x => Program.Config.GamePath = x)
        {
            CheckValidity = x =>
            {
                if (string.IsNullOrWhiteSpace(x?.FullName))
                    return "Game path is not set.";

                if (x.Name == "game" || x.Name == "boot")
                    return "Please select the path containing the folders \"game\" and \"boot\", not the folders itself.";

                return null;
            }
        },

        new SettingsEntry<DirectoryInfo>("Game Configuration Path", "Where your user config files will be stored.", () => Program.Config.GameConfigPath, x => Program.Config.GameConfigPath = x)
        {
            CheckValidity = x => string.IsNullOrWhiteSpace(x?.FullName) ? "Game Config Path is not set." : null,

            // TODO: We should also support this on Windows
            CheckVisibility = () => Environment.OSVersion.Platform == PlatformID.Unix,
        },

        new SettingsEntry<bool>("Use DirectX11", "Use the modern DirectX11 version of the game.", () => Program.Config.IsDx11 ?? true, x => Program.Config.IsDx11 = x)
        {
            CheckWarning = x => !x ? "DirectX 9 is no longer supported by the game or Dalamud. Things may not work." : null
        },

        new SettingsEntry<string>("Additional Game Arguments", "Follows Steam conventions: VAR1=value VAR2=value %command% -arg1 -arg2.\nCan't pass programs (like gamescope -- %command%). Does not accept flatpak args (--parent-pid=1, etc.)", () => Program.Config.AdditionalArgs, x => Program.Config.AdditionalArgs = x),
        new SettingsEntry<ClientLanguage>("Game Language", "Select the game's language.", () => Program.Config.ClientLanguage ?? ClientLanguage.English, x => Program.Config.ClientLanguage = x),
        new SettingsEntry<DpiAwareness>("Game DPI Awareness", "Select the game's DPI Awareness. Change this if the game's scaling looks wrong.", () => Program.Config.DpiAwareness ?? DpiAwareness.Unaware, x => Program.Config.DpiAwareness = x),
        new SettingsEntry<bool>("Free Trial Account", "Check this if you are using a free trial account.", () => Program.Config.IsFt ?? false, x => Program.Config.IsFt = x),
        new SettingsEntry<bool>("Use XIVLauncher authenticator/OTP macros", "Check this if you want to use the XIVLauncher authenticator app or macros.", () => Program.Config.IsOtpServer ?? false, x => Program.Config.IsOtpServer = x),
        new SettingsEntry<bool>("Ignore Steam", "Check this if you do not want XIVLauncher to communicate with Steam (Requires Restart).", () => Program.Config.IsIgnoringSteam ?? false, x => Program.Config.IsIgnoringSteam = x)
        {
            CheckVisibility = () => !CoreEnvironmentSettings.IsSteamCompatTool,
        },
        new SettingsEntry<bool>("Use Experimental UID Cache", "Tries to save your login token for the next start. Can result in launching with expired sessions.\nDisable if receiving FFXIV error 1012 or 500X.", () => Program.Config.IsUidCacheEnabled ?? false, x => Program.Config.IsUidCacheEnabled = x),
    };

    public override string Title => "Game";

    public override void Draw()
    {
        base.Draw();
    }
}