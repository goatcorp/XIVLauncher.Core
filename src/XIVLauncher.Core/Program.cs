using System.Numerics;

using CheapLoc;

using Config.Net;

using ImGuiNET;

using Serilog;

using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

using XIVLauncher.Common;
using XIVLauncher.Common.Dalamud;
using XIVLauncher.Common.Game.Patch.Acquisition;
using XIVLauncher.Common.PlatformAbstractions;
using XIVLauncher.Common.Support;
using XIVLauncher.Common.Unix;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Common.Windows;
using XIVLauncher.Core.Accounts.Secrets;
using XIVLauncher.Core.Accounts.Secrets.Providers;
using XIVLauncher.Core.Components.LoadingPage;
using XIVLauncher.Core.Configuration;
using XIVLauncher.Core.Configuration.Parsers;
using XIVLauncher.Core.UnixCompatibility;
using XIVLauncher.Core.Style;

namespace XIVLauncher.Core;

class Program
{
    private static Sdl2Window window = null!;
    private static CommandList cl = null!;
    private static GraphicsDevice gd = null!;
    private static ImGuiBindings bindings = null!;

    public static GraphicsDevice GraphicsDevice => gd;
    public static ImGuiBindings ImGuiBindings => bindings;
    public static ILauncherConfig Config { get; private set; } = null!;
    public static CommonSettings CommonSettings => new(Config);
    public static ISteam? Steam { get; private set; }
    public static DalamudUpdater DalamudUpdater { get; private set; } = null!;
    public static DalamudOverlayInfoProxy DalamudLoadInfo { get; private set; } = null!;
    public static CompatibilityTools CompatibilityTools { get; private set; } = null!;
    public static ISecretProvider Secrets { get; private set; } = null!;

    private static readonly Vector3 ClearColor = new(0.1f, 0.1f, 0.1f);

    private static LauncherApp launcherApp = null!;
    public static Storage storage = null!;
    public static DirectoryInfo DotnetRuntime => storage.GetFolder("runtime");

    // TODO: We don't have the steamworks api for this yet.
    // SteamDeck=1 on Steam Deck by default. SteamGamepadUI=1 in Big Picture / Gaming Mode.
    public static bool IsSteamDeckHardware => CoreEnvironmentSettings.IsDeck.HasValue ?
        CoreEnvironmentSettings.IsDeck.Value :
        CoreEnvironmentSettings.IsSteamDeckVar || (CoreEnvironmentSettings.IsDeckGameMode ?? false) || (CoreEnvironmentSettings.IsDeckFirstRun ?? false);
    public static bool IsSteamDeckGamingMode => CoreEnvironmentSettings.IsDeckGameMode.HasValue ?
        CoreEnvironmentSettings.IsDeckGameMode.Value :
        Steam != null && Steam.IsValid && Steam.IsRunningOnSteamDeck() && CoreEnvironmentSettings.IsSteamGamepadUIVar;

    private const string APP_NAME = "xlcore";

    private static string[] mainArgs = { };

    private static uint invalidationFrames = 0;
    private static Vector2 lastMousePosition = Vector2.Zero;


    public static string CType = CoreEnvironmentSettings.GetCType();

    public static Version CoreVersion { get; } = Version.Parse(AppUtil.GetAssemblyVersion());

    public const string CoreRelease = "Official";

    public static string CoreHash = AppUtil.GetGitHash() ?? "";

    public static void Invalidate(uint frames = 100)
    {
        invalidationFrames = frames;
    }

    private static void SetupLogging(string[] args)
    {
        LogInit.Setup(Path.Combine(storage.GetFolder("logs").FullName, "launcher.log"), args);

        Log.Information("========================================================");
        Log.Information("Starting a session(v{Version} - {Hash})", AppUtil.GetAssemblyVersion(), AppUtil.GetGitHash());
    }

    private static void LoadConfig(Storage storage)
    {
        Config = new ConfigurationBuilder<ILauncherConfig>()
                 .UseCommandLineArgs()
                 .UseIniFile(storage.GetFile("launcher.ini").FullName)
                 .UseTypeParser(new DirectoryInfoParser())
                 .UseTypeParser(new AddonListParser())
                 .Build();

        if (string.IsNullOrEmpty(Config.AcceptLanguage))
        {
            Config.AcceptLanguage = ApiHelpers.GenerateAcceptLanguage();
        }

        Config.GamePath ??= storage.GetFolder("ffxiv");
        Config.GameConfigPath ??= storage.GetFolder("ffxivConfig");
        Config.ClientLanguage ??= ClientLanguage.English;
        Config.DpiAwareness ??= DpiAwareness.Unaware;
        Config.IsAutologin ??= false;
        Config.CompletedFts ??= false;
        Config.DoVersionCheck ??= true;
        Config.FontPxSize ??= 22.0f;

        Config.IsEncryptArgs ??= true;
        Config.IsFt ??= false;
        Config.IsOtpServer ??= false;
        Config.IsIgnoringSteam = CoreEnvironmentSettings.UseSteam.HasValue ? !CoreEnvironmentSettings.UseSteam.Value : Config.IsIgnoringSteam ?? false;

        Config.PatchPath ??= storage.GetFolder("patch");
        Config.PatchAcquisitionMethod ??= AcquisitionMethod.Aria;

        Config.DalamudEnabled ??= true;
        Config.DalamudLoadMethod ??= DalamudLoadMethod.EntryPoint;

        Config.GlobalScale ??= 1.0f;

        Config.GameModeEnabled ??= false;
        Config.DxvkAsyncEnabled ??= true;
        Config.ESyncEnabled ??= true;
        Config.FSyncEnabled ??= false;
        Config.SetWin7 ??= true;

        Config.WineStartupType ??= WineStartupType.Managed;
        Config.WineBinaryPath ??= "/usr/bin";
        Config.WineDebugVars ??= "-all";

        Config.FixLDP ??= false;
        Config.FixIM ??= false;
        Config.FixLocale ??= false;

        Config.SteamPath ??= Path.Combine(CoreEnvironmentSettings.HOME, ".local", "share");
        Config.SteamFlatpakPath ??= Path.Combine(CoreEnvironmentSettings.HOME, ".var", "app", "com.valvesoftware.Steam", "data", "Steam" );
        Config.SteamToolInstalled ??= false;
        Config.SteamFlatpakToolInstalled ??= false;
    }

    public const uint STEAM_APP_ID = 39210;
    public const uint STEAM_APP_ID_FT = 312060;

    /// <summary>
    ///     The name of the Dalamud injector executable file.
    /// </summary>
    // TODO: move this somewhere better.
    public const string DALAMUD_INJECTOR_NAME = "Dalamud.Injector.exe";

    /// <summary>
    ///     Creates a new instance of the Dalamud updater.
    /// </summary>
    /// <remarks>
    ///     If <see cref="ILauncherConfig.DalamudManualInjectionEnabled"/> is true and there is an injector at <see cref="ILauncherConfig.DalamudManualInjectPath"/> then
    ///     manual injection will be used instead of a Dalamud branch.
    /// </remarks>
    /// <returns>A <see cref="DalamudUpdater"/> instance.</returns>
    private static DalamudUpdater CreateDalamudUpdater()
    {
        if (Config.DalamudManualInjectPath is not null &&
           Config.DalamudManualInjectPath.Exists &&
           Config.DalamudManualInjectPath.GetFiles().FirstOrDefault(x => x.Name == DALAMUD_INJECTOR_NAME) is not null)
        {
            return new DalamudUpdater(Config.DalamudManualInjectPath, storage.GetFolder("runtime"), storage.GetFolder("dalamudAssets"), storage.Root, null, null)
            {
                Overlay = DalamudLoadInfo,
                RunnerOverride = new FileInfo(Path.Combine(Config.DalamudManualInjectPath.FullName, DALAMUD_INJECTOR_NAME))
            };
        }
        return new DalamudUpdater(storage.GetFolder("dalamud"), storage.GetFolder("runtime"), storage.GetFolder("dalamudAssets"), storage.Root, null, null)
        {
            Overlay = DalamudLoadInfo,
        };
    }

    private static void Main(string[] args)
    {
        mainArgs = args;
        storage = new Storage(APP_NAME);

        if (CoreEnvironmentSettings.ClearAll)
        {
            ClearAll();
        }
        else
        {
            if (CoreEnvironmentSettings.ClearSettings) ClearSettings();
            if (CoreEnvironmentSettings.ClearPrefix) ClearPrefix();
            if (CoreEnvironmentSettings.ClearPlugins) ClearPlugins();
            if (CoreEnvironmentSettings.ClearTools) ClearTools();
            if (CoreEnvironmentSettings.ClearLogs) ClearLogs();
        }

        SetupLogging(mainArgs);
        LoadConfig(storage);

        Secrets = GetSecretProvider(storage);

        Loc.SetupWithFallbacks();

        if (System.OperatingSystem.IsLinux())
        {
            if (mainArgs.Length > 0)
                Log.Information("Command Line option selected: {args}", string.Join(' ', mainArgs));
            var exitValue = LinuxCommandLineOptions();
            if (exitValue)
            {
                Log.Information("Exiting...");
                return;
            }
            SteamCompatibilityTool.UpdateSteamTools();
        }

        uint appId, altId;
        string appName, altName;
        if (Config.IsFt == true)
        {
            appId = STEAM_APP_ID_FT;
            altId = STEAM_APP_ID;
            appName = "FFXIV Free Trial";
            altName = "FFXIV Retail";
        }
        else
        {
            appId = STEAM_APP_ID;
            altId = STEAM_APP_ID_FT;
            appName = "FFXIV Retail";
            altName = "FFXIV Free Trial";
        }
        try
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    Steam = new WindowsSteam();
                    break;

                case PlatformID.Unix:
                    Steam = new UnixSteam();
                    break;

                default:
                    throw new PlatformNotSupportedException();
            }
            if (CoreEnvironmentSettings.IsSteamCompatTool || (!Config.IsIgnoringSteam ?? true))
            {
                try
                {
                    Steam.Initialize(appId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Couldn't init Steam with AppId={appId} ({appName}), trying AppId={altId} ({altName})");
                    Steam.Initialize(altId);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Steam couldn't load");
        }

        // Manual or auto injection setup.
        DalamudLoadInfo = new DalamudOverlayInfoProxy();
        DalamudUpdater = CreateDalamudUpdater();
        DalamudUpdater.Run();

        CreateCompatToolsInstance();

        Log.Debug("Creating Veldrid devices...");

#if DEBUG
        var version = CoreHash;
#else
        var version = $"{CoreVersion} ({CoreHash})";
#endif

        // Create window, GraphicsDevice, and all resources necessary for the demo.
        VeldridStartup.CreateWindowAndGraphicsDevice(
            new WindowCreateInfo(50, 50, 1280, 800, WindowState.Normal, $"XIVLauncher {version}"),
            new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true),
            out window,
            out gd);

        window.Resized += () =>
        {
            gd.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
            bindings.WindowResized(window.Width, window.Height);
            Invalidate();
        };
        cl = gd.ResourceFactory.CreateCommandList();
        Log.Debug("Veldrid OK!");

        bindings = new ImGuiBindings(gd, gd.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height, storage.GetFile("launcherUI.ini"), Config.FontPxSize ?? 21.0f);
        Log.Debug("ImGui OK!");

        StyleModelV1.DalamudStandard.Apply();
        ImGui.GetIO().FontGlobalScale = Config.GlobalScale ?? 1.0f;

        var needUpdate = false;

#if FLATPAK
        if (Config.DoVersionCheck ?? false)
        {
            var versionCheckResult = UpdateCheck.CheckForUpdate().GetAwaiter().GetResult();

            if (versionCheckResult.Success)
                needUpdate = versionCheckResult.NeedUpdate;
        }   
#endif

        needUpdate = CoreEnvironmentSettings.IsUpgrade ? true : needUpdate;

        var launcherClientConfig = LauncherClientConfig.Fetch().GetAwaiter().GetResult();
        launcherApp = new LauncherApp(storage, needUpdate, launcherClientConfig.frontierUrl, launcherClientConfig.cutOffBootver);

        Invalidate(20);

        // Main application loop
        while (window.Exists)
        {
            Thread.Sleep(50);

            InputSnapshot snapshot = window.PumpEvents();

            if (!window.Exists)
                break;

            var overlayNeedsPresent = false;

            if (Steam != null && Steam.IsValid)
                overlayNeedsPresent = Steam.BOverlayNeedsPresent;

            if (!snapshot.KeyEvents.Any() && !snapshot.MouseEvents.Any() && !snapshot.KeyCharPresses.Any() && invalidationFrames == 0 && lastMousePosition == snapshot.MousePosition
                && !overlayNeedsPresent)
            {
                continue;
            }

            if (invalidationFrames == 0)
            {
                invalidationFrames = 10;
            }

            if (invalidationFrames > 0)
            {
                invalidationFrames--;
            }

            lastMousePosition = snapshot.MousePosition;

            bindings.Update(1f / 60f, snapshot);

            launcherApp.Draw();

            cl.Begin();
            cl.SetFramebuffer(gd.MainSwapchain.Framebuffer);
            cl.ClearColorTarget(0, new RgbaFloat(ClearColor.X, ClearColor.Y, ClearColor.Z, 1f));
            bindings.Render(gd, cl);
            cl.End();
            gd.SubmitCommands(cl);
            gd.SwapBuffers(gd.MainSwapchain);
        }

        // Clean up Veldrid resources
        gd.WaitForIdle();
        bindings.Dispose();
        cl.Dispose();
        gd.Dispose();
    }

    public static void CreateCompatToolsInstance()
    {
        var wineLogFile = new FileInfo(Path.Combine(storage.GetFolder("logs").FullName, "wine.log"));
        var winePrefix = storage.GetFolder("wineprefix");
        var wineSettings = new WineSettings(Config.WineStartupType, Config.WineBinaryPath, Config.WineDebugVars, wineLogFile, winePrefix, Config.ESyncEnabled, Config.FSyncEnabled);
        var toolsFolder = storage.GetFolder("compatibilitytool");
        Directory.CreateDirectory(Path.Combine(toolsFolder.FullName, "dxvk"));
        Directory.CreateDirectory(Path.Combine(toolsFolder.FullName, "beta"));
        CompatibilityTools = new CompatibilityTools(wineSettings, Config.DxvkHudType, Config.GameModeEnabled, Config.DxvkAsyncEnabled, toolsFolder);
    }

    public static void ShowWindow()
    {
        window.Visible = true;
    }

    public static void HideWindow()
    {
        window.Visible = false;
    }

    private static ISecretProvider GetSecretProvider(Storage storage)
    {
        var secretsFilePath = Environment.GetEnvironmentVariable("XL_SECRETS_FILE_PATH") ?? "secrets.json";

        var envVar = Environment.GetEnvironmentVariable("XL_SECRET_PROVIDER") ?? "KEYRING";
        envVar = envVar.ToUpper();

        switch (envVar)
        {
            case "FILE":
                return new FileSecretProvider(storage.GetFile(secretsFilePath));

            case "KEYRING":
                {
                    var keyChain = new KeychainSecretProvider();

                    if (!keyChain.IsAvailable)
                    {
                        Log.Error("An org.freedesktop.secrets provider is not available - no secrets will be stored");
                        return new DummySecretProvider();
                    }

                    return keyChain;
                }

            case "NONE":
                return new DummySecretProvider();

            default:
                throw new ArgumentException($"Invalid secret provider: {envVar}");
        }
    }

    public static void ClearSettings(bool tsbutton = false)
    {
        if (storage.GetFile("launcher.ini").Exists) storage.GetFile("launcher.ini").Delete();
        if (tsbutton)
        {
            LoadConfig(storage);
            launcherApp.State = LauncherApp.LauncherState.Settings;
        }
    }

    public static void ClearPrefix()
    {
        storage.GetFolder("wineprefix").Delete(true);
        storage.GetFolder("wineprefix");
    }

    public static void ClearPlugins(bool tsbutton = false)
    {
        storage.GetFolder("dalamud").Delete(true);
        storage.GetFolder("dalamudAssets").Delete(true);
        storage.GetFolder("installedPlugins").Delete(true);
        storage.GetFolder("runtime").Delete(true);
        if (storage.GetFile("dalamudUI.ini").Exists) storage.GetFile("dalamudUI.ini").Delete();
        if (storage.GetFile("dalamudConfig.json").Exists) storage.GetFile("dalamudConfig.json").Delete();
        storage.GetFolder("dalamud");
        storage.GetFolder("dalamudAssets");
        storage.GetFolder("installedPlugins");
        storage.GetFolder("runtime");
        if (tsbutton)
        {
            DalamudLoadInfo = new DalamudOverlayInfoProxy();
            DalamudUpdater = CreateDalamudUpdater();
            DalamudUpdater.Run();
        }
    }

    public static void ClearTools(bool tsbutton = false)
    {
        storage.GetFolder("compatibilitytool").Delete(true);
        storage.GetFolder("compatibilitytool/beta");
        storage.GetFolder("compatibilitytool/dxvk");
        if (tsbutton) CreateCompatToolsInstance();
    }

    public static void ClearLogs(bool tsbutton = false)
    {
        storage.GetFolder("logs").Delete(true);
        storage.GetFolder("logs");
        string[] logfiles = { "dalamud.boot.log", "dalamud.boot.old.log", "dalamud.log", "dalamud.injector.log" };
        foreach (string logfile in logfiles)
            if (storage.GetFile(logfile).Exists) storage.GetFile(logfile).Delete();
        if (tsbutton)
            SetupLogging(mainArgs);

    }
    public static void ClearAll(bool tsbutton = false)
    {
        ClearSettings(tsbutton);
        ClearPrefix();
        ClearPlugins(tsbutton);
        ClearTools(tsbutton);
        ClearLogs(true);
    }

    private static bool LinuxCommandLineOptions()
    {
        bool exit = false;

        if (mainArgs.Contains("-v"))
        {
            Console.WriteLine("Verbose Logging enabled...");
        }

        if (mainArgs.Contains("--update-tools") || mainArgs.Contains("-u"))
        {
            SteamCompatibilityTool.UpdateSteamTools(true);
            exit = true;
        }

        if (mainArgs.Contains("--info"))
        {
            Console.WriteLine($"This program: XIVLauncher.Core {CoreVersion.ToString()} - {CoreRelease}");
            Console.WriteLine($"Steam compatibility tool {(SteamCompatibilityTool.IsSteamToolInstalled ? "is installed: " + SteamCompatibilityTool.CheckVersion(isFlatpak: false).Replace(",", " ") : "is not installed.")}");
            Console.WriteLine($"Steam (flatpak) compatibility tool {(SteamCompatibilityTool.IsSteamFlatpakToolInstalled ? "is installed: " + SteamCompatibilityTool.CheckVersion(isFlatpak: true).Replace(",", " ") : "is not installed.")}");
            exit = true;
        }

        if (mainArgs.Contains("--deck-install") && mainArgs.Contains("--deck-remove"))
        {
            SteamCompatibilityTool.DeleteTool(isFlatpak: false);
            Console.WriteLine("Using both --deck-install and --deck-remove. Doing --deck-remove first");
            Console.WriteLine($"Removed XIVLauncher.Core as a Steam compatibility tool from {Program.Config.SteamPath ?? ""}");
            SteamCompatibilityTool.CreateTool(isFlatpak: false);
            Console.WriteLine($"Installed as Steam compatibility tool to {Program.Config.SteamPath ?? ""}");
            exit = true;
        }
        else if (mainArgs.Contains("--deck-install"))
        {
            SteamCompatibilityTool.CreateTool(isFlatpak: false);
            Console.WriteLine($"Installed as Steam compatibility tool to {Program.Config.SteamPath ?? ""}");
            exit = true;
        }
        else if (mainArgs.Contains("--deck-remove"))
        {
            SteamCompatibilityTool.DeleteTool(isFlatpak: false);
            Console.WriteLine($"Removed XIVLauncher.Core as a Steam compatibility tool from {Program.Config.SteamPath ?? ""}");
            exit = true;
        }

        if (mainArgs.Contains("--flatpak-install") && mainArgs.Contains("--flatpak-remove"))
        {
            Console.WriteLine("Using both --flatpak-install and --flatpak-remove. Doing --deck-remove first");
            SteamCompatibilityTool.DeleteTool(isFlatpak: true);
            Console.WriteLine($"Removed XIVLauncher.Core as a Steam compatibility tool from {Program.Config.SteamFlatpakPath ?? ""}");
            SteamCompatibilityTool.CreateTool(isFlatpak: true);
            Console.WriteLine($"Installed as Steam compatibility tool to {Program.Config.SteamFlatpakPath ?? ""}");
            exit = true;
        }
        else if (mainArgs.Contains("--flatpak-install"))
        {
            SteamCompatibilityTool.CreateTool(isFlatpak: true);
            Console.WriteLine($"Installed as Steam compatibility tool to {Program.Config.SteamFlatpakPath ?? ""}");
            exit = true;
        }
        else if (mainArgs.Contains("--flatpak-remove"))
        {
            SteamCompatibilityTool.DeleteTool(isFlatpak: true);
            Console.WriteLine($"Removed XIVLauncher.Core as a Steam compatibility tool from {Program.Config.SteamFlatpakPath ?? ""}");
            exit = true;
        }

        if (mainArgs.Contains("--version"))
        {
            Console.WriteLine($"XIVLauncher.Core {CoreVersion.ToString()} - {CoreRelease}");
            Console.WriteLine("Copyright (C) 2024 goatcorp.\nLicense GPLv3+: GNU GPL version 3 or later <https://gnu.org/licenses/gpl.html>.");
            exit = true;
        }

        if (mainArgs.Contains("-V"))
        {
            Console.WriteLine(CoreVersion.ToString());
            Console.WriteLine(CoreRelease);
            exit = true;
        }

        if (mainArgs.Contains("--help") || mainArgs.Contains("-h"))
        {
            Console.WriteLine($"XIVLaunher.Core {CoreVersion.ToString()}\nA third-party launcher for Final Fantasy XIV.\n\nOptions (use only one):");
            Console.WriteLine("  -v                 Turn on verbose logging, then run the launcher.");
            Console.WriteLine("  -h, --help         Display this message.");
            Console.WriteLine("  -V                 Display brief version info.");
            Console.WriteLine("  --version          Display version and copywrite info.");
            Console.WriteLine("  --info             Display Steam compatibility tool install status");
            Console.WriteLine("  -u, --update-tools Try to update any installed xlcore steam compatibility tools.");
            Console.WriteLine("\nFor Steam Deck and native Steam");
            Console.WriteLine("  --deck-install     Install as a compatibility tool in the default location.");
            Console.WriteLine($"                     Path: {Program.Config.SteamPath ?? ""}");
            Console.WriteLine("  --deck-remove      Remove compatibility tool install from the defualt location.");
            Console.WriteLine("\nFor Flatpak Steam");
            Console.WriteLine("  --flatpak-install  Install as a compatibility tool to flatpak Steam.");
            Console.WriteLine($"                     Path: {Program.Config.SteamFlatpakPath ?? ""}");
            Console.WriteLine("  --flatpak-remove   Remove compatibility tool from flatpak Steam.");
            Console.WriteLine("");
            exit = true;
        }
        
        return exit;
    }
}
