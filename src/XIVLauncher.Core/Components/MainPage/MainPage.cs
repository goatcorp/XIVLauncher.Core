using System.Diagnostics;
using System.Numerics;

using Hexa.NET.ImGui;
using Hexa.NET.SDL3;

using Serilog;

using XIVLauncher.Common;
using XIVLauncher.Common.Addon;
using XIVLauncher.Common.Dalamud;
using XIVLauncher.Common.Game;
using XIVLauncher.Common.Game.Exceptions;
using XIVLauncher.Common.Game.Patch;
using XIVLauncher.Common.Game.Patch.Acquisition.Aria;
using XIVLauncher.Common.Game.Patch.PatchList;
using XIVLauncher.Common.PlatformAbstractions;
using XIVLauncher.Common.Unix;
using XIVLauncher.Common.Unix.Compatibility.Wine;
using XIVLauncher.Common.Util;
using XIVLauncher.Common.Windows;
using XIVLauncher.Core.Accounts;
using XIVLauncher.Core.Resources.Localization;
using XIVLauncher.Core.Support;

namespace XIVLauncher.Core.Components.MainPage;

public class MainPage : Page
{
    private readonly LoginFrame loginFrame;
    private readonly NewsFrame newsFrame;
    private readonly ActionButtons actionButtons;

    public bool IsLoggingIn { get; private set; }

    public MainPage(LauncherApp app)
        : base(app)
    {
        this.loginFrame = new LoginFrame(this);
        this.newsFrame = new NewsFrame(app);

        this.actionButtons = new ActionButtons();

        this.AccountSwitcher = new AccountSwitcher(app.Accounts);
        this.AccountSwitcher.AccountChanged += this.AccountSwitcherOnAccountChanged;

        this.loginFrame.OnLogin += this.ProcessLogin;
        this.actionButtons.OnSettingsButtonClicked += () => this.App.State = LauncherApp.LauncherState.Settings;
        this.actionButtons.OnStatusButtonClicked += () => AppUtil.OpenBrowser("https://is.xivup.com/");
        this.actionButtons.OnAccountButtonClicked += () => AppUtil.OpenBrowser("https://sqex.to/Msp");

        this.Padding = new Vector2(32f, 32f);

        var savedAccount = App.Accounts.CurrentAccount;

        if (savedAccount != null) this.SwitchAccount(savedAccount, false);

        if (PlatformHelpers.IsElevated())
            App.ShowMessage(Strings.XLElevatedWarning, "XIVLauncher");

        Troubleshooting.LogTroubleshooting();
    }

    public AccountSwitcher AccountSwitcher { get; private set; }

    public void DoAutoLoginIfApplicable()
    {
        Debug.Assert(App.State == LauncherApp.LauncherState.Main);

        if ((App.Settings.IsAutologin ?? false) && !string.IsNullOrEmpty(this.loginFrame.Username) && !string.IsNullOrEmpty(this.loginFrame.Password))
            ProcessLogin(LoginAction.Game);
    }

    public override void Draw()
    {
        base.Draw();

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(32f, 32f));
        this.newsFrame.Draw();

        ImGui.SameLine();

        this.loginFrame.Draw();
        this.AccountSwitcher.Draw();

        this.actionButtons.Draw();
        ImGui.PopStyleVar();
    }

    public void ReloadNews() => this.newsFrame.ReloadNews();

    private void SwitchAccount(XivAccount account, bool saveAsCurrent)
    {
        this.loginFrame.Username = account.UserName;
        this.loginFrame.IsOtp = account.UseOtp;
        this.loginFrame.IsFreeTrial = account.IsFreeTrial;
        this.loginFrame.IsSteam = account.UseSteamServiceAccount;
        this.loginFrame.IsAutoLogin = App.Settings.IsAutologin ?? false;

        if (account.SavePassword)
            this.loginFrame.Password = account.Password;

        if (saveAsCurrent)
        {
            App.Accounts.CurrentAccount = account;
        }
    }

    private void AccountSwitcherOnAccountChanged(object? sender, XivAccount e)
    {
        SwitchAccount(e, true);
    }

    private void ProcessLogin(LoginAction action)
    {
        if (this.IsLoggingIn)
            return;

        this.App.StartLoading(Strings.LoggingIn, canDisableAutoLogin: true);

        // if (Program.UsesFallbackSteamAppId && this.loginFrame.IsSteam)
        //     throw new Exception("Doesn't own Steam AppId on this account.");

        Task.Run(async () =>
        {
            if (GameHelpers.CheckIsGameOpen() && action == LoginAction.Repair)
            {
                App.ShowMessageBlocking(Strings.RepairOfficialLauncherOpenError, Strings.XIVLauncherError);

                Reactivate();
                return;
            }

            if (Repository.Ffxiv.GetVer(App.Settings.GamePath) == Constants.BASE_GAME_VERSION &&
                App.Settings.IsUidCacheEnabled == true)
            {
                App.ShowMessageBlocking(
                    Strings.ReinstallUIDCacheError,
                    Strings.XIVLauncherError);

                this.Reactivate();
                return;
            }

            IsLoggingIn = true;

            App.Settings.IsAutologin = this.loginFrame.IsAutoLogin;

            var result = await Login(loginFrame.Username, loginFrame.Password, loginFrame.IsOtp, loginFrame.IsSteam, loginFrame.IsFreeTrial, false, action).ConfigureAwait(false);

            if (result)
            {
                var sdlEvent = new SDLEvent
                {
                    Type = (int)SDLEventType.Quit
                };
                if (SDL.PushEvent(ref sdlEvent))
                {
                    Log.Error($"Failed to push event to SDL queue: {SDL.GetErrorS()}");
                }
            }
            else
            {
                Log.Verbose("Reactivated after Login() != true");
                this.Reactivate();
            }
        }).ContinueWith(t =>
        {
            if (!App.HandleContinuationBlocking(t))
                this.Reactivate();
        });
    }

    public async Task<bool> Login(string username, string password, bool isOtp, bool isSteam, bool isFreeTrial, bool doingAutoLogin, LoginAction action)
    {
        if (action == LoginAction.Fake)
        {
            IGameRunner gameRunner;
            // FIXME: Should we really be passing a null DalamudLauncher to both of these?
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                gameRunner = new WindowsGameRunner(null, false);
            else
                gameRunner = new UnixGameRunner(Program.CompatibilityTools, null, false);

            App.Launcher.LaunchGame(gameRunner, "0", 1, 2, false, "", App.Settings.GamePath!, ClientLanguage.Japanese, true, DpiAwareness.Unaware);

            return false;
        }

        var bootRes = await HandleBootCheck().ConfigureAwait(false);

        if (!bootRes)
            return false;

        var otp = string.Empty;

        if (isOtp && App.UniqueIdCache.HasValidCache(username) && App.Settings.IsUidCacheEnabled == false)
            Program.ResetUIDCache();

        if (isOtp && !App.UniqueIdCache.HasValidCache(username))
        {
            App.AskForOtp();
            otp = App.WaitForOtp();

            // Make sure we are loading again
            App.State = LauncherApp.LauncherState.Loading;
        }

        if (otp == null)
            return false;

        PersistAccount(username, password, isOtp, isSteam, isFreeTrial);

        var loginResult = await TryLoginToGame(username, password, otp, isSteam, isFreeTrial, action).ConfigureAwait(false);

        return await TryProcessLoginResult(loginResult, isSteam, action).ConfigureAwait(false);
    }

    private async Task<Launcher.LoginResult> TryLoginToGame(string username, string password, string otp, bool isSteam, bool isFreeTrial, LoginAction action)
    {
#if !DEBUG
        bool? gateStatus = null;
        try
        {
            // TODO: Also apply the login status fix here
            var gate = await App.Launcher.GetGateStatus(App.Settings.ClientLanguage ?? ClientLanguage.English).ConfigureAwait(false);
            gateStatus = gate.Status;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not obtain gate status");
        }

        if (gateStatus == null)
        {
            App.ShowMessageBlocking("Login servers could not be reached or maintenance is in progress. This might be a problem with your connection.");
            return null!;
        }
#endif

        try
        {
            var enableUidCache = App.Settings.IsUidCacheEnabled ?? false;
            var gamePath = App.Settings.GamePath!;
            var language = App.Settings.ClientLanguage ?? ClientLanguage.English;

            if (action == LoginAction.Repair)
                return await App.Launcher.Login(username, password, otp, isSteam, false, gamePath, true, isFreeTrial, language).ConfigureAwait(false);
            else
                return await App.Launcher.Login(username, password, otp, isSteam, enableUidCache, gamePath, false, isFreeTrial, language).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not login to game");
            throw;
        }
    }

    private async Task<bool> TryProcessLoginResult(Launcher.LoginResult loginResult, bool isSteam, LoginAction action)
    {
        // Format error message in the way OauthLoginException expects.
        var preErrorMsg = "window.external.user(\"login=auth,ng,err,";
        var postErrorMsg = "\");";

        if (loginResult.State == Launcher.LoginState.NoService)
        {
            throw new OauthLoginException(preErrorMsg + Strings.LoginNoServiceAccountError + postErrorMsg);
        }

        if (loginResult.State == Launcher.LoginState.NoTerms)
        {
            throw new OauthLoginException(preErrorMsg + Strings.LoginTermsNotAcceptedError + postErrorMsg);
        }

        /*
         * The server requested us to patch Boot, even though in order to get to this code, we just checked for boot patches.
         *
         * This means that something or someone modified boot binaries without our involvement.
         * We have no way to go back to a "known" good state other than to do a full reinstall.
         *
         * This has happened multiple times with users that have viruses that infect other EXEs and change their hashes, causing the update
         * server to reject our boot hashes.
         *
         * In the future we may be able to just delete /boot and run boot patches again, but this doesn't happen often enough to warrant the
         * complexity and if boot is fucked game probably is too.
         */
        if (loginResult.State == Launcher.LoginState.NeedsPatchBoot)
        {
            throw new OauthLoginException(preErrorMsg + "Boot conflict, need reinstall" + postErrorMsg);
        }

        if (action == LoginAction.Repair)
        {
            try
            {
                if (loginResult.State == Launcher.LoginState.NeedsPatchGame)
                {
                    if (!await RepairGame(loginResult).ConfigureAwait(false))
                        return false;

                    loginResult.State = Launcher.LoginState.Ok;
                }
                else
                {
                    throw new OauthLoginException(preErrorMsg + "Repair login state not NeedsPatchGame" + postErrorMsg);
                }
            }
            catch (Exception)
            {
                /*
                 * We should never reach here.
                 * If server responds badly, then it should not even have reached this point, as error cases should have been handled before.
                 * If RepairGame was unsuccessful, then it should have handled all of its possible errors, instead of propagating it upwards.
                 */
                //CustomMessageBox.Builder.NewFrom(ex, "TryProcessLoginResult/Repair").WithParentWindow(_window).Show();
                throw;
            }
        }

        if (loginResult.State == Launcher.LoginState.NeedsPatchGame)
        {
            if (!await InstallGamePatch(loginResult).ConfigureAwait(false))
            {
                Log.Error("patchSuccess != true");
                return false;
            }

            loginResult.State = Launcher.LoginState.Ok;
        }

        if (action == LoginAction.GameNoLaunch)
        {
            App.ShowMessageBlocking(Strings.UpdateCheckFinished, "XIVLauncher");

            return false;
        }

#if !DEBUG
        bool? gateStatus = null;
        try
        {
            // TODO: Also apply the login status fix here
            var gate = await App.Launcher.GetGateStatus(App.Settings.ClientLanguage ?? ClientLanguage.English).ConfigureAwait(false);
            gateStatus = gate.Status;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not obtain gate status");
        }

        switch (gateStatus)
        {
            case null:
                App.ShowMessageBlocking("Login servers could not be reached or maintenance is in progress. This might be a problem with your connection.");
                return false;
            case false:
                App.ShowMessageBlocking("Maintenance is in progress.");
                return false;
        }
#endif

        Debug.Assert(loginResult.State == Launcher.LoginState.Ok);

        while (true)
        {
            try
            {
                using var process = await StartGameAndAddon(loginResult, isSteam, action == LoginAction.GameNoDalamud, action == LoginAction.GameNoPlugins, action == LoginAction.GameNoThirdparty).ConfigureAwait(false);

                if (process is null)
                    throw new InvalidOperationException("Could not obtain Process Handle");

                if (process.ExitCode != 0 && (App.Settings.TreatNonZeroExitCodeAsFailure ?? false))
                {
                    throw new InvalidOperationException("Game exited with non-zero exit code");
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "StartGameAndError resulted in an exception.");
                throw;
            }

            //NOTE(goat): This HAS to handle all possible exceptions from StartGameAndAddon!!!!!
            /*
            List<string> summaries = new();
            List<string> actionables = new();
            List<string> descriptions = new();

            foreach (var exception in exceptions)
            {
                switch (exception)
                {
                    case GameExitedException:
                        var count = 0;

                        foreach (var processName in new string[] { "ffxiv_dx11", "ffxiv" })
                        {
                            foreach (var process in Process.GetProcessesByName(processName))
                            {
                                count++;
                                process.Dispose();
                            }
                        }

                        if (count >= 2)
                        {
                            summaries.Add(Loc.Localize("MultiboxDeniedWarningSummary",
                                "You can't launch more than two instances of the game by default."));
                            actionables.Add(string.Format(
                                Loc.Localize("MultiboxDeniedWarningActionable",
                                    "Please check if there is an instance of the game that did not close correctly. (Detected: {0})"),
                                count));
                            descriptions.Add(null);

                            builder.WithButtons(MessageBoxButton.YesNoCancel)
                                   .WithDefaultResult(MessageBoxResult.Yes)
                                   .WithCancelButtonText(Loc.Localize("LaunchGameKillThenRetry", "_Kill then try again"));
                        }
                        else
                        {
                            summaries.Add(Loc.Localize("GameExitedPrematurelyErrorSummary",
                                "XIVLauncher could not detect that the game started correctly."));
                            actionables.Add(Loc.Localize("GameExitedPrematurelyErrorActionable",
                                "This may be a temporary issue. Please try restarting your PC. It is possible that your game installation is not valid."));
                            descriptions.Add(null);
                        }

                        break;

                    case BinaryNotPresentException:
                        summaries.Add(Loc.Localize("BinaryNotPresentErrorSummary",
                            "Could not find the game executable."));
                        actionables.Add(Loc.Localize("BinaryNotPresentErrorActionable",
                            "This might be caused by your antivirus. You may have to reinstall the game."));
                        descriptions.Add(null);
                        break;

                    case IOException:
                        summaries.Add(Loc.Localize("LoginIoErrorSummary",
                            "Could not locate game data files."));
                        summaries.Add(Loc.Localize("LoginIoErrorActionable",
                            "This may mean that the game path set in XIVLauncher isn't preset, e.g. on a disconnected drive or network storage. Please check the game path in the XIVLauncher settings."));
                        descriptions.Add(exception.ToString());
                        break;

                    case Win32Exception win32Exception:
                        summaries.Add(string.Format(
                            Loc.Localize("UnexpectedErrorSummary",
                                "Unexpected error has occurred. ({0})"),
                            $"0x{(uint)win32Exception.HResult:X8}: {win32Exception.Message}"));
                        actionables.Add(Loc.Localize("UnexpectedErrorActionable",
                            "Please report this error."));
                        descriptions.Add(exception.ToString());
                        break;

                    default:
                        summaries.Add(string.Format(
                            Loc.Localize("UnexpectedErrorSummary",
                                "Unexpected error has occurred. ({0})"),
                            exception.Message));
                        actionables.Add(Loc.Localize("UnexpectedErrorActionable",
                            "Please report this error."));
                        descriptions.Add(exception.ToString());
                        break;
                }
            }

            if (exceptions.Count == 1)
            {
                builder.WithText($"{summaries[0]}\n\n{actionables[0]}")
                       .WithDescription(descriptions[0]);
            }
            else
            {
                builder.WithText(Loc.Localize("MultipleErrors", "Multiple errors have occurred."));

                for (var i = 0; i < summaries.Count; i++)
                {
                    builder.WithAppendText($"\n{i + 1}. {summaries[i]}\n    => {actionables[i]}");
                    if (string.IsNullOrWhiteSpace(descriptions[i]))
                        continue;
                    builder.WithAppendDescription($"########## Exception {i + 1} ##########\n{descriptions[i]}\n\n");
                }
            }

            if (descriptions.Any(x => x != null))
                builder.WithAppendSettingsDescription("Login");


            switch (builder.Show())
            {
                case MessageBoxResult.Yes:
                    continue;

                case MessageBoxResult.No:
                    return false;

                case MessageBoxResult.Cancel:
                    for (var pass = 0; pass < 8; pass++)
                    {
                        var allKilled = true;

                        foreach (var processName in new string[] { "ffxiv_dx11", "ffxiv" })
                        {
                            foreach (var process in Process.GetProcessesByName(processName))
                            {
                                allKilled = false;

                                try
                                {
                                    process.Kill();
                                }
                                catch (Exception ex2)
                                {
                                    Log.Warning(ex2, "Could not kill process (PID={0}, name={1})", process.Id, process.ProcessName);
                                }
                                finally
                                {
                                    process.Dispose();
                                }
                            }
                        }

                        if (allKilled)
                            break;
                    }

                    Task.Delay(1000).Wait();
                    continue;
            }
            */
        }
    }

    public async Task<Process> StartGameAndAddon(Launcher.LoginResult loginResult, bool isSteam, bool forceNoDalamud, bool noPlugins, bool noThird)
    {
        var dalamudOk = false;

        IDalamudRunner dalamudRunner;
        IDalamudCompatibilityCheck dalamudCompatCheck;

        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
                dalamudRunner = new WindowsDalamudRunner(Program.DalamudUpdater.Runtime);
                dalamudCompatCheck = new WindowsDalamudCompatibilityCheck();
                break;

            case PlatformID.Unix:
                dalamudRunner = new UnixDalamudRunner(Program.CompatibilityTools, Program.DotnetRuntime);
                dalamudCompatCheck = new UnixDalamudCompatibilityCheck();
                break;

            default:
                throw new NotImplementedException();
        }

        Troubleshooting.LogTroubleshooting();

        var dalamudLauncher = new DalamudLauncher(dalamudRunner, Program.DalamudUpdater,
            App.Settings.DalamudLoadMethod.GetValueOrDefault(DalamudLoadMethod.DllInject), App.Settings.GamePath,
            App.Storage.Root, App.Storage.GetFolder("logs"), App.Settings.ClientLanguage ?? ClientLanguage.English,
            App.Settings.DalamudLoadDelay, false, noPlugins, noThird, Troubleshooting.GetTroubleshootingJson());

        try
        {
            dalamudCompatCheck.EnsureCompatibility();
        }
        catch (IDalamudCompatibilityCheck.NoRedistsException ex)
        {
            Log.Error(ex, "No Dalamud Redists found");
            throw;
        }
        catch (IDalamudCompatibilityCheck.ArchitectureNotSupportedException ex)
        {
            Log.Error(ex, "Architecture not supported");
            throw;
        }

        if (App.Settings.DalamudEnabled.GetValueOrDefault(true) && !forceNoDalamud)
        {
            try
            {
                App.StartLoading(Strings.WaitingForDalamud, Strings.PleaseBePatient);
                dalamudOk = dalamudLauncher.HoldForUpdate(App.Settings.GamePath) == DalamudLauncher.DalamudInstallState.Ok;
            }
            catch (DalamudRunnerException ex)
            {
                Log.Error(ex, "Couldn't ensure Dalamud runner");
                throw;
            }
        }

        IGameRunner runner;

        // Set LD_PRELOAD to value of XL_PRELOAD if we're running as a steam compatibility tool.
        // This check must be done before the FixLDP check so that it will still work.
        if (CoreEnvironmentSettings.IsSteamCompatTool)
        {
            var ldpreload = System.Environment.GetEnvironmentVariable("LD_PRELOAD") ?? "";
            var xlpreload = System.Environment.GetEnvironmentVariable("XL_PRELOAD") ?? "";
            ldpreload = (ldpreload + ":" + xlpreload).Trim(':');
            if (!string.IsNullOrEmpty(ldpreload))
                System.Environment.SetEnvironmentVariable("LD_PRELOAD", ldpreload);
        }

        // Hack: Force C.utf8 to fix incorrect unicode paths
        if (App.Settings.FixLocale == true && !string.IsNullOrEmpty(Program.CType))
        {
            System.Environment.SetEnvironmentVariable("LC_ALL", Program.CType);
            System.Environment.SetEnvironmentVariable("LC_CTYPE", Program.CType);
        }

        // Hack: Strip out gameoverlayrenderer.so entries from LD_PRELOAD
        if (App.Settings.FixLDP == true)
        {
            var ldpreload = CoreEnvironmentSettings.GetCleanEnvironmentVariable("LD_PRELOAD", "gameoverlayrenderer.so");
            System.Environment.SetEnvironmentVariable("LD_PRELOAD", ldpreload);
        }

        // Hack: XMODIFIERS=@im=null
        if (App.Settings.FixIM == true)
        {
            System.Environment.SetEnvironmentVariable("XMODIFIERS", "@im=null");
        }

        // Hack: Fix libicuuc dalamud crashes
        if (App.Settings.FixError127 == true)
        {
            System.Environment.SetEnvironmentVariable("DOTNET_SYSTEM_GLOBALIZATION_USENLS", "true");
        }

        // The Timezone environment on Unix platforms tends to cause issues with in-game time display.
        // For now the best workaround is to unset it, although it can be specified with AdditionalArgs
        // again if the user really wants to.
        if (Environment.OSVersion.Platform == PlatformID.Unix && App.Settings.DontUseSystemTz == true)
        {
            System.Environment.SetEnvironmentVariable("TZ", string.Empty);
        }

        // Deal with "Additional Arguments". VAR=value %command% -args
        var launchOptions = (App.Settings.AdditionalArgs ?? string.Empty).Split("%command%", 2);
        var launchEnv = "";
        var gameArgs = "";

        // If there's only one launch option (no %command%) figure out whether it's args or env variables.
        if (launchOptions.Length == 1)
        {
            if (launchOptions[0].StartsWith('-'))
                gameArgs = launchOptions[0];
            else
                launchEnv = launchOptions[0];
        }
        else
        {
            launchEnv = launchOptions[0] ?? "";
            gameArgs = launchOptions[1] ?? "";
        }

        if (!string.IsNullOrEmpty(launchEnv))
        {
            foreach (var envvar in launchEnv.Split(null))
            {
                if (!envvar.Contains('=')) continue;    // ignore entries without an '='
                var kvp = envvar.Split('=', 2);
                if (kvp[0].EndsWith('+'))               // if key ends with +, then it's actually key+=value
                {
                    kvp[0] = kvp[0].TrimEnd('+');
                    kvp[1] = (System.Environment.GetEnvironmentVariable(kvp[0]) ?? "") + kvp[1];
                }
                System.Environment.SetEnvironmentVariable(kvp[0], kvp[1]);
            }
        }

        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            runner = new WindowsGameRunner(dalamudLauncher, dalamudOk);
        }
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            if (App.Settings.WineStartupType == WineStartupType.Custom)
            {
                if (App.Settings.WineBinaryPath == null)
                    throw new InvalidOperationException("Custom wine binary path wasn't set.");
                else if (!Directory.Exists(App.Settings.WineBinaryPath))
                    throw new InvalidOperationException("Custom wine binary path is invalid: no such directory.\n" +
                                                        "Check path carefully for typos: " + App.Settings.WineBinaryPath);
                else if (!File.Exists(Path.Combine(App.Settings.WineBinaryPath, "wine64")))
                    throw new InvalidOperationException("Custom wine binary path is invalid: no wine64 found at that location.\n" +
                                                        "Check path carefully for typos: " + App.Settings.WineBinaryPath);

                Log.Information("Using Custom Wine: " + App.Settings.WineBinaryPath);
            }
            else
            {
                Log.Information("Using Managed Wine: " + App.Settings.WineManagedVersion.ToString());
            }
            Log.Information("Using Dxvk Version: " + App.Settings.DxvkVersion.ToString());

            var signal = new ManualResetEvent(false);
            var isFailed = false;

            var _ = Task.Run(async () =>
            {
                var tempPath = App.Storage.GetFolder("temp");
                await Program.CompatibilityTools.EnsureTool(tempPath).ConfigureAwait(false);
            }).ContinueWith(t =>
            {
                isFailed = t.IsFaulted || t.IsCanceled;

                if (isFailed)
                    Log.Error(t.Exception, "Couldn't ensure compatibility tool");

                signal.Set();
            });

            App.StartLoading(Strings.PreparingForCompatTool, Strings.PleaseBePatient);
            signal.WaitOne();
            signal.Dispose();

            if (isFailed)
                return null!;

            App.StartLoading(Strings.StartingGame, Strings.HaveFun);

            runner = new UnixGameRunner(Program.CompatibilityTools, dalamudLauncher, dalamudOk);

            // SE has its own way of encoding spaces when encrypting arguments, which interferes 
            // with quoting, but they are necessary when passing paths unencrypted
            var userPath = Program.CompatibilityTools.UnixToWinePath(App.Settings.GameConfigPath!.FullName);
            if (App.Settings.IsEncryptArgs.GetValueOrDefault(true))
                gameArgs += $" UserPath={userPath}";
            else
                gameArgs += $" UserPath=\"{userPath}\"";

            gameArgs = gameArgs.Trim();
        }
        else
        {
            throw new NotImplementedException();
        }

        // We won't do any sanity checks here anymore, since that should be handled in StartLogin
        var launchedProcess = App.Launcher.LaunchGame(runner,
            loginResult.UniqueId,
            loginResult.OauthLogin.Region,
            loginResult.OauthLogin.MaxExpansion,
            isSteam,
            gameArgs,
            App.Settings.GamePath,
            App.Settings.ClientLanguage.GetValueOrDefault(ClientLanguage.English),
            App.Settings.IsEncryptArgs.GetValueOrDefault(true),
            App.Settings.DpiAwareness.GetValueOrDefault(DpiAwareness.Unaware));

        // Hide the launcher if not Steam Deck or if using as a compatibility tool (XLM)
        // Show the Steam Deck prompt if on steam deck and not using as a compatibility tool
        if (!Program.IsSteamDeckHardware || CoreEnvironmentSettings.IsSteamCompatTool)
        {
            Hide();
        }
        else
        {
            App.State = LauncherApp.LauncherState.SteamDeckPrompt;
        }

        if (launchedProcess == null)
        {
            Log.Information("GameProcess was null...");
            IsLoggingIn = false;
            return null!;
        }

        var addonMgr = new AddonManager();

        try
        {
            App.Settings.Addons ??= new List<AddonEntry>();

            var addons = App.Settings.Addons.Where(x => x.IsEnabled).Select(x => x.Addon).Cast<IAddon>().ToList();

            addonMgr.RunAddons(launchedProcess.Id, addons);
        }
        catch (Exception)
        {
            IsLoggingIn = false;
            addonMgr.StopAddons();
            throw;
        }

        Log.Debug("Waiting for game to exit");

        await Task.Run(() => launchedProcess!.WaitForExit()).ConfigureAwait(false);

        Log.Verbose("Game has exited");

        if (addonMgr.IsRunning)
            addonMgr.StopAddons();

        try
        {
            if (App.Steam?.IsValid == true)
            {
                App.Steam.Shutdown();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not shut down Steam");
        }

        return launchedProcess!;
    }

    private void PersistAccount(string username, string password, bool isOtp, bool isSteam, bool isFreeTrial)
    {
        // Update account password.
        if (App.Accounts.CurrentAccount != null && App.Accounts.CurrentAccount.UserName.Equals(username, StringComparison.Ordinal) &&
            App.Accounts.CurrentAccount.Password != password &&
            App.Accounts.CurrentAccount.SavePassword)
            App.Accounts.UpdatePassword(App.Accounts.CurrentAccount, password);

        // Update account free trial status.
        if (App.Accounts.CurrentAccount != null && App.Accounts.CurrentAccount.UserName.Equals(username, StringComparison.OrdinalIgnoreCase) &&
            App.Accounts.CurrentAccount.IsFreeTrial != isFreeTrial)
            App.Accounts.UpdateFreeTrial(App.Accounts.CurrentAccount, isFreeTrial);

        if (App.Accounts.CurrentAccount is null || App.Accounts.CurrentAccount.Id != $"{username}-{isOtp}-{isSteam}")
        {
            var accountToSave = new XivAccount(username)
            {
                Password = password,
                SavePassword = true,
                UseOtp = isOtp,
                IsFreeTrial = isFreeTrial,
                UseSteamServiceAccount = isSteam
            };
            App.Accounts.AddAccount(accountToSave);
            App.Accounts.CurrentAccount = accountToSave;
        }
    }

    private async Task<bool> HandleBootCheck()
    {
        try
        {
            if (App.Settings.PatchPath is { Exists: false })
            {
                App.Settings.PatchPath = null;
            }

            App.Settings.PatchPath ??= new DirectoryInfo(Path.Combine(Paths.RoamingPath, "patches"));

            PatchListEntry[] bootPatches;

            try
            {
                bootPatches = await App.Launcher.CheckBootVersion(App.Settings.GamePath!).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to check boot version");
                App.ShowMessage(Strings.CannotGetBootVerError, "XIVLauncher");

                return false;
            }

            if (bootPatches.Length == 0)
                return true;

            return await TryHandlePatchAsync(Repository.Boot, bootPatches, "").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            App.ShowExceptionBlocking(ex, "PatchBoot");
            Environment.Exit(0);

            return false;
        }
    }

    private Task<bool> InstallGamePatch(Launcher.LoginResult loginResult)
    {
        Debug.Assert(loginResult.State == Launcher.LoginState.NeedsPatchGame,
            "loginResult.State == Launcher.LoginState.NeedsPatchGame ASSERTION FAILED");

        Debug.Assert(loginResult.PendingPatches != null, "loginResult.PendingPatches != null ASSERTION FAILED");

        return TryHandlePatchAsync(Repository.Ffxiv, loginResult.PendingPatches, loginResult.UniqueId);
    }

    private async Task<bool> TryHandlePatchAsync(Repository repository, PatchListEntry[] pendingPatches, string sid)
    {
        // BUG(goat): This check only behaves correctly on Windows - the mutex doesn't seem to disappear on Linux, .NET issue?
#if WIN32
        using var mutex = new Mutex(false, "XivLauncherIsPatching");

        if (!mutex.WaitOne(0, false))
        {
            App.ShowMessageBlocking( "XIVLauncher is already patching your game in another instance. Please check if XIVLauncher is still open.", "XIVLauncher");
            Environment.Exit(0);
            return false; // This line will not be run.
        }
#endif

        if (GameHelpers.CheckIsGameOpen())
        {
            App.ShowMessageBlocking(Strings.CannotPatchGameOpenError, "XIVLauncher");

            return false;
        }

        using var installer = new PatchInstaller(App.Settings.GamePath, App.Settings.KeepPatches ?? false);
        using var acquisition = new AriaPatchAcquisition(new FileInfo(Path.Combine(App.Storage.GetFolder("logs").FullName, "aria2.log")));
        Program.Patcher = new PatchManager(acquisition, App.Settings.PatchSpeedLimit, repository, pendingPatches, App.Settings.GamePath,
                                           App.Settings.PatchPath, installer, App.Launcher, sid);
        Program.Patcher.OnFail += PatcherOnFail;
        installer.OnFail += this.InstallerOnFail;

        /*
        Hide();

        PatchDownloadDialog progressDialog = _window.Dispatcher.Invoke(() =>
        {
            var d = new PatchDownloadDialog(patcher);
            if (_window.IsVisible)
                d.Owner = _window;
            d.Show();
            d.Activate();
            return d;
        });
        */

        this.App.StartLoading(string.Format(Strings.NowPatching, repository.ToString().ToLowerInvariant()), canCancel: false, isIndeterminate: false);

        try
        {
            var token = new CancellationTokenSource();
            var statusThread = new Thread(UpdatePatchStatus);

            statusThread.Start();

            void UpdatePatchStatus()
            {
                while (!token.IsCancellationRequested)
                {
                    Thread.Sleep(30);

                    App.LoadingPage.Line2 = string.Format(Strings.WorkingOnStatus, Program.Patcher.CurrentInstallIndex, Program.Patcher.Downloads.Count);
                    App.LoadingPage.Line3 = string.Format(Strings.LeftToDownloadStatus, MathHelpers.BytesToString(Program.Patcher.AllDownloadsLength < 0 ? 0 : Program.Patcher.AllDownloadsLength),
                        MathHelpers.BytesToString(Program.Patcher.Speeds.Sum()));

                    App.LoadingPage.Progress = Program.Patcher.CurrentInstallIndex / (float)Program.Patcher.Downloads.Count;
                }
            }

            try
            {
                await Program.Patcher.PatchAsync(false).ConfigureAwait(false);
            }
            finally
            {
                token.Cancel();
                statusThread.Join(TimeSpan.FromMilliseconds(1000));
            }

            return true;
        }
        catch (PatchInstallerException ex)
        {
            App.ShowMessageBlocking(string.Format(Strings.PatchInstallerStartFailError, ex.Message), Strings.XIVLauncherError);
        }
        catch (NotEnoughSpaceException sex)
        {
            switch (sex.Kind)
            {
                case NotEnoughSpaceException.SpaceKind.Patches:
                    App.ShowMessageBlocking(
                        string.Format(Strings.NotEnoughSpacePatchesError,
                            MathHelpers.BytesToString(sex.BytesRequired), MathHelpers.BytesToString(sex.BytesFree)), Strings.XIVLauncherError);
                    break;

                case NotEnoughSpaceException.SpaceKind.AllPatches:
                    App.ShowMessageBlocking(
                        string.Format(Strings.NotEnoughSpaceAllPatchesError,
                            MathHelpers.BytesToString(sex.BytesRequired), MathHelpers.BytesToString(sex.BytesFree)), Strings.XIVLauncherError);
                    break;

                case NotEnoughSpaceException.SpaceKind.Game:
                    App.ShowMessageBlocking(
                        string.Format(Strings.NotEnoughSpaceGameError,
                            MathHelpers.BytesToString(sex.BytesRequired), MathHelpers.BytesToString(sex.BytesFree)), Strings.XIVLauncherError);
                    break;

                default:
                    Debug.Assert(false, "HandlePatchAsync:Invalid NotEnoughSpaceException.SpaceKind value.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during patching");
            App.ShowExceptionBlocking(ex, "HandlePatchAsync");
        }
        finally
        {
            App.State = LauncherApp.LauncherState.Main;
        }

        return false;
    }

    private void PatcherOnFail(PatchListEntry patch, string context)
    {
        App.ShowMessageBlocking(string.Format(Strings.CannotVerifyGameFilesError, context, patch.VersionId), Strings.XIVLauncherError);
        Environment.Exit(0);
    }

    private void InstallerOnFail()
    {
        App.ShowMessageBlocking(Strings.PatchInstallerGenericError, Strings.XIVLauncherError);
        Environment.Exit(0);
    }

    private async Task<bool> RepairGame(Launcher.LoginResult loginResult)
    {
        var doLogin = false;

        // BUG(goat): This check only behaves correctly on Windows - the mutex doesn't seem to disappear on Linux, .NET issue?
#if WIN32
        using var mutex = new Mutex(false, "XivLauncherIsPatching");

        if (!mutex.WaitOne(0, false))
        {
            App.ShowMessageBlocking("XIVLauncher is already patching your game in another instance. Please check if XIVLauncher is still open.", "XIVLauncher");
            Environment.Exit(0);
            return false; // This line will not be run.
        }
#endif

        Debug.Assert(loginResult.PendingPatches != null, "loginResult.PendingPatches != null ASSERTION FAILED");
        Debug.Assert(loginResult.PendingPatches.Length != 0, "loginResult.PendingPatches.Length != 0 ASSERTION FAILED");

        Log.Information("STARTING REPAIR");

        // TODO: bundle the PatchInstaller with xl-core on Windows and run this remotely
        using var verify = new PatchVerifier(Program.Config.GamePath!, Program.Config.PatchPath!, loginResult, TimeSpan.FromMilliseconds(100), loginResult.OauthLogin.MaxExpansion, false);

        for (var doVerify = true; doVerify;)
        {
            this.App.StartLoading(Strings.NowRepairingFiles, canCancel: false, isIndeterminate: false);

            verify.Start();

            var timer = new Timer(new TimerCallback((object? obj) =>
            {
                switch (verify.State)
                {
                    // TODO: show more progress info here
                    case PatchVerifier.VerifyState.DownloadMeta:
                        this.App.LoadingPage.Line2 = $"{verify.CurrentFile}";
                        this.App.LoadingPage.Line3 = $"{Math.Min(verify.PatchSetIndex + 1, verify.PatchSetCount)}/{verify.PatchSetCount} - {MathHelpers.BytesToString(verify.Progress)}/{MathHelpers.BytesToString(verify.Total)}";
                        this.App.LoadingPage.Progress = (float)(verify.Total != 0 ? (float)verify.Progress / (float)verify.Total : 0.0);
                        break;

                    case PatchVerifier.VerifyState.VerifyAndRepair:
                        this.App.LoadingPage.Line2 = $"{verify.CurrentFile}";
                        this.App.LoadingPage.Line3 = $"{Math.Min(verify.PatchSetIndex + 1, verify.PatchSetCount)}/{verify.PatchSetCount} - {Math.Min(verify.TaskIndex + 1, verify.TaskCount)}/{verify.TaskCount} - {MathHelpers.BytesToString(verify.Progress)}/{MathHelpers.BytesToString(verify.Total)}";
                        this.App.LoadingPage.Progress = (float)(verify.Total != 0 ? (float)verify.Progress / (float)verify.Total : 0);
                        break;

                    default:
                        this.App.LoadingPage.Line2 = "";
                        this.App.LoadingPage.Line3 = $"{Math.Min(verify.TaskIndex + 1, verify.TaskCount)}/{verify.TaskCount}";
                        this.App.LoadingPage.Progress = (float)(verify.State == PatchVerifier.VerifyState.Done ? 1.0 : 0);
                        break;
                }
            }
            ));
            timer.Change(0, 250);

            await verify.WaitForCompletion().ConfigureAwait(false);
            timer.Dispose();
            this.App.StopLoading();

            switch (verify.State)
            {
                case PatchVerifier.VerifyState.Done:
                    // TODO: ask the user if they want to login or rerun after repair

                    var mainText = verify.NumBrokenFiles switch
                    {
                        0 => Strings.RepairAllFilesValid,
                        1 => Strings.RepairFixedSingularFile,
                        _ => string.Format(Strings.RepairFixedPluralFiles, verify.NumBrokenFiles),
                    };

                    var additionalText = verify.MovedFiles.Count switch
                    {
                        0 => "",
                        1 => "\n\n" + string.Format(Strings.RepairSingleFileMoved, verify.MovedFileToDir),
                        _ => "\n\n" + string.Format(Strings.RepairPluralFilesMoved, verify.MovedFiles.Count, verify.MovedFileToDir),
                    };

                    App.ShowMessageBlocking(mainText + additionalText);

                    doVerify = false;
                    break;

                case PatchVerifier.VerifyState.Error:
                    doLogin = false;

                    if (verify.LastException is NoVersionReferenceException)
                    {
                        App.ShowMessageBlocking(Strings.RepairGameVerUnsupportedError);
                    }
                    else
                    {
                        App.ShowMessageBlocking(verify.LastException + "\n\n" + Strings.RepairFailureError);
                    }

                    doVerify = false;
                    break;

                case PatchVerifier.VerifyState.Cancelled:
                    doLogin = doVerify = false;
                    break;
            }
        }


        return doLogin;
    }

    private void Hide()
    {
        Program.HideWindow();
    }

    private void Reactivate()
    {
        IsLoggingIn = false;
        this.App.State = LauncherApp.LauncherState.Main;

        Program.ShowWindow();
    }
}
