using System.Globalization;
using System.Numerics;
using System.Resources;
using System.Runtime.InteropServices;

using Config.Net;

using ImGuiNET;

using Serilog;

using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

using XIVLauncher.Common;
using XIVLauncher.Common.Dalamud;
using XIVLauncher.Common.Game.Patch;
using XIVLauncher.Common.Game.Patch.Acquisition;
using XIVLauncher.Common.Game.Patch.Acquisition.Aria;
using XIVLauncher.Common.PlatformAbstractions;
using XIVLauncher.Common.Support;
using XIVLauncher.Common.Unix;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Unix.Compatibility.Dxvk;
using XIVLauncher.Common.Unix.Compatibility.Nvapi;
using XIVLauncher.Common.Unix.Compatibility.Wine;
using XIVLauncher.Common.Util;
using XIVLauncher.Common.Windows;
using XIVLauncher.Core.Accounts.Secrets;
using XIVLauncher.Core.Accounts.Secrets.Providers;
using XIVLauncher.Core.Components.LoadingPage;
using XIVLauncher.Core.Configuration;
using XIVLauncher.Core.Configuration.Parsers;
using XIVLauncher.Core.Resources.Localization;
using XIVLauncher.Core.Style;

namespace XIVLauncher.Core;

sealed class Program
{
    private const string APP_NAME = "xlcore";
    private static readonly Vector3 ClearColor = new(0.1f, 0.1f, 0.1f);
    private static string[] mainArgs = [];
    private static LauncherApp launcherApp = null!;
    private static Sdl2Window window = null!;
    private static CommandList commandList = null!;
    private static GraphicsDevice graphicsDevice = null!;
    private static ImGuiBindings guiBindings = null!;

    public static GraphicsDevice GraphicsDevice => graphicsDevice;
    public static ImGuiBindings ImGuiBindings => guiBindings;
    public static ILauncherConfig Config { get; private set; } = null!;
    public static CommonSettings CommonSettings => new(Config);
    public static ISteam? Steam { get; private set; }
    public static DalamudUpdater DalamudUpdater { get; private set; } = null!;
    public static DalamudOverlayInfoProxy DalamudLoadInfo { get; private set; } = null!;
    public static CompatibilityTools CompatibilityTools { get; private set; } = null!;
    public static ISecretProvider Secrets { get; private set; } = null!;
    public static HttpClient HttpClient { get; private set; } = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };
    public static PatchManager Patcher { get; set; } = null!;
    public static Storage storage = null!;
    public static DirectoryInfo DotnetRuntime => storage.GetFolder("runtime");
    public static string CType = CoreEnvironmentSettings.GetCType();

    // TODO: We don't have the steamworks api for this yet.
    public static bool IsSteamDeckHardware => CoreEnvironmentSettings.IsDeck.HasValue ?
        CoreEnvironmentSettings.IsDeck.Value :
        Directory.Exists("/home/deck") || (CoreEnvironmentSettings.IsDeckGameMode ?? false) || (CoreEnvironmentSettings.IsDeckFirstRun ?? false);


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

        if (Config.GamePath == null)
        {
            var compatGamePath = Environment.GetEnvironmentVariable("STEAM_COMPAT_INSTALL_PATH"); // auto-set when using compat tool
            var compatGameId = Environment.GetEnvironmentVariable("STEAM_COMPAT_APP_ID"); // auto-set when using compat tool
            // Set the game path to the game's installation directory if its either the full game or free trial.
            Config.GamePath = !string.IsNullOrWhiteSpace(compatGamePath) &&
                              int.TryParse(compatGameId, out int compatGameIdParsed) &&
                              (compatGameIdParsed == STEAM_APP_ID || compatGameIdParsed == STEAM_APP_ID_FT)
                ? new DirectoryInfo(compatGamePath)
                : storage.GetFolder("ffxiv");
        }

        Config.GameConfigPath ??= storage.GetFolder("ffxivConfig");
        Config.ClientLanguage ??= ClientLanguage.English;
        Config.DpiAwareness ??= DpiAwareness.Unaware;
        Config.IsAutologin ??= false;
        Config.CompletedFts ??= false;
        Config.FontPxSize ??= 22.0f;

        Config.IsEncryptArgs ??= true;
        Config.IsOtpServer ??= false;
        Config.IsIgnoringSteam = CoreEnvironmentSettings.UseSteam.HasValue ? !CoreEnvironmentSettings.UseSteam.Value : Config.IsIgnoringSteam ?? false;

        Config.PatchPath ??= storage.GetFolder("patch");
        Config.PatchAcquisitionMethod ??= AcquisitionMethod.Aria;

        Config.DalamudEnabled ??= true;
        Config.DalamudLoadMethod ??= DalamudLoadMethod.EntryPoint;

        Config.GlobalScale ??= 1.0f;

        Config.GameModeEnabled ??= false;
        Config.DxvkVersion ??= DxvkVersion.Stable;
        Config.DxvkAsyncEnabled ??= true;
        Config.NvapiVersion ??= NvapiVersion.Stable;
        Config.ESyncEnabled ??= true;
        Config.FSyncEnabled ??= false;
        Config.SetWin7 ??= true;

        Config.WineStartupType ??= WineStartupType.Managed;
        Config.WineManagedVersion ??= WineManagedVersion.Stable;
        Config.WineBinaryPath ??= "/usr/bin";
        Config.WineDebugVars ??= "-all";

        Config.FixLDP ??= false;
        Config.FixIM ??= false;
        Config.FixLocale ??= false;
        Config.FixError127 ??= false;
        Config.DontUseSystemTz ??= false;
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
        FileInfo? runnerOverride = null;
        if (Config.DalamudManualInjectPath is not null &&
            Config.DalamudManualInjectionEnabled == true &&
            Config.DalamudManualInjectPath.Exists &&
            Config.DalamudManualInjectPath.GetFiles().FirstOrDefault(x => x.Name == DALAMUD_INJECTOR_NAME) is not null)
        {
            runnerOverride = new FileInfo(Path.Combine(Config.DalamudManualInjectPath.FullName, DALAMUD_INJECTOR_NAME));
        }
        return new DalamudUpdater(storage.GetFolder("dalamud"), storage.GetFolder("runtime"), storage.GetFolder("dalamudAssets"), null, null)
        {
            Overlay = DalamudLoadInfo,
            RunnerOverride = runnerOverride
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

        Dictionary<uint, string> apps = [];
        if (CoreEnvironmentSettings.SteamAppId != 0)
        {
            apps.Add(CoreEnvironmentSettings.SteamAppId, "XLM");
        }
        if (CoreEnvironmentSettings.AltAppID != 0)
        {
            apps.Add(CoreEnvironmentSettings.AltAppID, "XL_APPID");
        }
        if (!apps.ContainsKey(STEAM_APP_ID))
        {
            apps.Add(STEAM_APP_ID, "FFXIV Retail");
        }
        if (!apps.ContainsKey(STEAM_APP_ID_FT))
        {
            apps.Add(STEAM_APP_ID_FT, "FFXIV Free Trial");
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
            if (Config.IsIgnoringSteam != true || CoreEnvironmentSettings.IsSteamCompatTool)
            {
                foreach (var app in apps)
                {
                    try
                    {
                        Steam.Initialize(app.Key);
                        Log.Information($"Successfully initialized Steam entry {app.Key} - {app.Value}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Failed to initialize Steam Steam entry {app.Key} - {app.Value}");
                    }
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
        DalamudUpdater.Run(Config.DalamudBetaKind, Config.DalamudBetaKey);

        CreateCompatToolsInstance();

        Log.Debug("Creating Veldrid devices...");

#if DEBUG
        var version = AppUtil.GetGitHash();
#else
        var version = $"{AppUtil.GetAssemblyVersion()} ({AppUtil.GetGitHash()})";
#endif

        // Initialise SDL, as that's needed to figure out where to spawn the window.
        Sdl2Native.SDL_Init(SDLInitFlags.Video);

        // For now, just spawn the window on the primary display, which in SDL2 has displayIndex 0.
        // Maybe we may want to save the window location or the preferred display in the config at some point?
        if (!GetDisplayBounds(displayIndex: 0, out var bounds))
            Log.Warning("Couldn't figure out the bounds of the primary display, falling back to previous assumption that (0,0) is the top left corner of the left-most monitor.");

        // Create the window and graphics device separately, because Veldrid would have reinitialised SDL if done with their combined method.
        window = VeldridStartup.CreateWindow(new WindowCreateInfo(50 + bounds.X, 50 + bounds.Y, 1280, 800, WindowState.Normal, $"XIVLauncher {version}"));
        graphicsDevice = VeldridStartup.CreateGraphicsDevice(window, new GraphicsDeviceOptions(false, null, true, ResourceBindingModel.Improved, true, true));

        window.Resized += () =>
        {
            graphicsDevice.MainSwapchain.Resize((uint)window.Width, (uint)window.Height);
            guiBindings.WindowResized(window.Width, window.Height);
        };
        commandList = graphicsDevice.ResourceFactory.CreateCommandList();
        Log.Debug("Veldrid OK!");

        guiBindings = new ImGuiBindings(graphicsDevice, graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, window.Width, window.Height, storage.GetFile("launcherUI.ini"), Config.FontPxSize ?? 21.0f);
        Log.Debug("ImGui OK!");

        StyleModelV1.DalamudStandard.Apply();
        ImGui.GetIO().FontGlobalScale = Config.GlobalScale ?? 1.0f;

        var launcherClientConfig = LauncherClientConfig.GetAsync().GetAwaiter().GetResult();
        launcherApp = new LauncherApp(storage, launcherClientConfig.frontierUrl, launcherClientConfig.cutOffBootver);

        // Main application loop
        while (window.Exists)
        {
            Thread.Sleep(30);
            var snapshot = window.PumpEvents();
            if (!window.Exists)
                break;

            guiBindings.Update(1 / 60f, snapshot);
            launcherApp.Draw();
            commandList.Begin();
            commandList.SetFramebuffer(graphicsDevice.MainSwapchain.Framebuffer);
            commandList.ClearColorTarget(0, new RgbaFloat(ClearColor.X, ClearColor.Y, ClearColor.Z, 1f));
            guiBindings.Render(graphicsDevice, commandList);
            commandList.End();
            graphicsDevice.SubmitCommands(commandList);
            graphicsDevice.SwapBuffers(graphicsDevice.MainSwapchain);
        }

        // Clean up Veldrid resources
        // FIXME: Veldrid doesn't clean up after SDL though, so some leakage may have been happening for all this time.
        graphicsDevice.WaitForIdle();
        guiBindings.Dispose();
        commandList.Dispose();
        graphicsDevice.Dispose();
        HttpClient.Dispose();
        if (Patcher is not null)
        {
            Patcher.StartCancellation();
            // PatchManager.UnInitializeAcquisition() is private but the function bellow is the only call that is in the method and is public accessible
            try
            {
                AriaHttpPatchAcquisition.UnInitializeAsync().ConfigureAwait(false);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not uninitialize patch acquisition.");
            }
        }
    }

    public static void CreateCompatToolsInstance()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        var wineLogFile = new FileInfo(Path.Combine(storage.GetFolder("logs").FullName, "wine.log"));
        var winePrefix = storage.GetFolder("wineprefix");
        var wineSettings = new WineSettings(Config.WineStartupType ?? WineStartupType.Custom, Config.WineManagedVersion ?? WineManagedVersion.Stable, Config.WineBinaryPath, Config.WineDebugVars, wineLogFile, winePrefix, Config.ESyncEnabled ?? true, Config.FSyncEnabled ?? false);
        var toolsFolder = storage.GetFolder("compatibilitytool");
        Directory.CreateDirectory(Path.Combine(toolsFolder.FullName, "dxvk"));
        Directory.CreateDirectory(Path.Combine(toolsFolder.FullName, "nvapi"));
        Directory.CreateDirectory(Path.Combine(toolsFolder.FullName, "wine"));
        CompatibilityTools = new CompatibilityTools(wineSettings, Config.DxvkVersion ?? DxvkVersion.Stable, Config.DxvkHudType, Config.NvapiVersion ?? NvapiVersion.Stable, Config.GameModeEnabled ?? false, Config.DxvkAsyncEnabled ?? true, toolsFolder, Config.GamePath);
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
            DalamudUpdater.Run(Config.DalamudBetaKind, Config.DalamudBetaKey);
        }
    }

    public static void ClearTools(bool tsbutton = false)
    {
        storage.GetFolder("compatibilitytool").Delete(true);
        storage.GetFolder("compatibilitytool/wine");
        storage.GetFolder("compatibilitytool/dxvk");
        storage.GetFolder("compatibilitytool/nvapi");
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

    public static void ResetUIDCache(bool tsbutton = false) => launcherApp.UniqueIdCache.Reset();

    private static unsafe bool GetDisplayBounds(int displayIndex, out Rectangle bounds)
    {
        bounds = new Rectangle();
        fixed (Rectangle* rectangle = &bounds)
        {
            if (Sdl2Native.SDL_GetDisplayBounds(displayIndex, rectangle) != 0)
            {
                return false;
            }
        }
        return true;
    }
}
