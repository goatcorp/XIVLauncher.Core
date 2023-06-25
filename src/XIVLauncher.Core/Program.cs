using System.Numerics;
using System.IO;
using CheapLoc;
using Config.Net;
using ImGuiNET;
using XIVLauncher.Core.Style;
using Serilog;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using XIVLauncher.Common;
using XIVLauncher.Common.Dalamud;
using XIVLauncher.Common.Game.Patch.Acquisition;
using XIVLauncher.Common.PlatformAbstractions;
using XIVLauncher.Common.Support;
using XIVLauncher.Common.Windows;
using XIVLauncher.Common.Unix;
using XIVLauncher.Common.Unix.Compatibility;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.Accounts.Secrets;
using XIVLauncher.Core.Accounts.Secrets.Providers;
using XIVLauncher.Core.Components.LoadingPage;
using XIVLauncher.Core.Configuration;
using XIVLauncher.Core.Configuration.Parsers;

namespace XIVLauncher.Core;

class Program
{
    private static Sdl2Window window;
    private static CommandList cl;
    private static GraphicsDevice gd;
    private static ImGuiBindings bindings;

    public static GraphicsDevice GraphicsDevice => gd;
    public static ImGuiBindings ImGuiBindings => bindings;
    public static ILauncherConfig Config { get; private set; }
    public static CommonSettings CommonSettings => new(Config);
    public static ISteam? Steam { get; private set; }
    public static DalamudUpdater DalamudUpdater { get; private set; }
    public static DalamudOverlayInfoProxy DalamudLoadInfo { get; private set; }
    public static CompatibilityTools CompatibilityTools { get; private set; }
    public static ISecretProvider Secrets { get; private set; }

    private static readonly Vector3 clearColor = new(0.1f, 0.1f, 0.1f);
    private static bool showImGuiDemoWindow = true;
    public static string Distro { get; private set; }
    private static string DistroLong;
    public static bool IsFlatpak { get; private set; }
    private static LauncherApp launcherApp;
    public static Storage storage;
    public static DirectoryInfo DotnetRuntime => storage.GetFolder("runtime");

    // TODO: We don't have the steamworks api for this yet.
    public static bool IsSteamDeckHardware => CoreEnvironmentSettings.IsDeck.HasValue ?
        CoreEnvironmentSettings.IsDeck.Value :
        Directory.Exists("/home/deck") || (CoreEnvironmentSettings.IsDeckGameMode ?? false) || (CoreEnvironmentSettings.IsDeckFirstRun ?? false);
    public static bool IsSteamDeckGamingMode => CoreEnvironmentSettings.IsDeckGameMode.HasValue ?
        CoreEnvironmentSettings.IsDeckGameMode.Value :
        Steam != null && Steam.IsValid && Steam.IsRunningOnSteamDeck();

    private const string APP_NAME = "xlcore";

    private static string[] mainargs;

    private static uint invalidationFrames = 0;
    private static Vector2 lastMousePosition;

    public static void Invalidate(uint frames = 100)
    {
        invalidationFrames = frames;
    }

    private static void SetupLogging(string[] args)
    {
        LogInit.Setup(Path.Combine(storage.GetFolder("logs").FullName, "launcher.log"), args);

        Log.Information("========================================================");
        Log.Information("Starting a session(v{Version} - {Hash})", AppUtil.GetAssemblyVersion(), AppUtil.GetGitHash());
        Log.Information("Running on {DistroName}, wine distro set to {Distro}", DistroLong, Distro);   
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

        Config.IsDx11 ??= true;
        Config.IsEncryptArgs ??= true;
        Config.IsFt ??= false;
        Config.IsOtpServer ??= false;
        Config.IsIgnoringSteam = CoreEnvironmentSettings.UseSteam.HasValue ? !CoreEnvironmentSettings.UseSteam.Value : Config.IsIgnoringSteam ?? false;

        Config.PatchPath ??= storage.GetFolder("patch");
        Config.PatchAcquisitionMethod ??= AcquisitionMethod.Aria;

        Config.DalamudEnabled ??= true;
        Config.DalamudLoadMethod = !OperatingSystem.IsWindows() ? DalamudLoadMethod.DllInject : DalamudLoadMethod.EntryPoint;

        Config.GlobalScale ??= 1.0f;

        Config.GameModeEnabled ??= false;
        Config.DxvkVersion ??= DxvkVersion.v1_10_3;
        Config.WineD3DUseVK ??= false;
        Config.DxvkAsyncEnabled ??= true;
        Config.DxvkFrameRate ??= 0;
        Config.ESyncEnabled ??= true;
        Config.FSyncEnabled ??= false;
        Config.DxvkHudCustom ??= "fps,frametimes,gpuload,version";
        Config.DxvkMangoCustom ??= Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".config", "MangoHud", "MangoHud.conf");
        Config.SetWin7 ??= true;

        Config.WineType ??= WineType.Managed;
        Config.WineBinaryPath ??= "/usr/bin";
        Config.WineDebugVars ??= "-all";

        Config.FixLDP ??= false;
        Config.FixIM ??= false;
    }

    public const uint STEAM_APP_ID = 39210;
    public const uint STEAM_APP_ID_FT = 312060;

    private static void Main(string[] args)
    {
        mainargs = args;
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
        
        GetDistro();
        SetupLogging(mainargs);
        LoadConfig(storage);

        Secrets = GetSecretProvider(storage);

        Loc.SetupWithFallbacks();

        uint appId, altId;
        string appName, altName;
        if (Config.IsFt.Value)
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
            if (!Config.IsIgnoringSteam ?? true)
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

        DalamudLoadInfo = new DalamudOverlayInfoProxy();
        DalamudUpdater = new DalamudUpdater(storage.GetFolder("dalamud"), storage.GetFolder("runtime"), storage.GetFolder("dalamudAssets"), storage.Root, null, null)
        {
            Overlay = DalamudLoadInfo
        };
        DalamudUpdater.Run();

        CreateCompatToolsInstance();

        Log.Debug("Creating Veldrid devices...");

#if DEBUG
        var version = AppUtil.GetGitHash();
#else
        var version = $"{AppUtil.GetAssemblyVersion()} ({AppUtil.GetGitHash()})";
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

        if (Config.DoVersionCheck ?? false && IsFlatpak)
        {
            var versionCheckResult = UpdateCheck.CheckForUpdate().GetAwaiter().GetResult();

            if (versionCheckResult.Success)
                needUpdate = versionCheckResult.NeedUpdate;
        }
        
        needUpdate = CoreEnvironmentSettings.IsUpgrade ? true : needUpdate;

        launcherApp = new LauncherApp(storage, needUpdate);

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
            cl.ClearColorTarget(0, new RgbaFloat(clearColor.X, clearColor.Y, clearColor.Z, 1f));
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
        var toolsFolder = storage.GetFolder("compatibilitytool");
        var wine = WineManager.Initialize();
        var dxvk = DxvkManager.Initialize();
        var wineoverride = "msquic,mscoree=n,b;";
        var wineenv = new Dictionary<string, string>();
        if (dxvk.IsDxvk)
        {
            wineoverride += "d3d9,d3d11,d3d10core,dxgi=n,b";
        }
        else
        {
            wineoverride += "d3d9,d3d11,d3d10core,dxgi=b";
        }
        CompatibilityTools = new CompatibilityTools(wine, dxvk, wineenv, wineoverride, winePrefix, toolsFolder, wineLogFile);
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

    private static void GetDistro()
    {
        try
        {
            if (!File.Exists("/etc/os-release"))
            {
                Distro = "ubuntu";
                DistroLong = "Unknown distribution";
                IsFlatpak = false;
                return;
            }
            var osRelease = File.ReadAllLines("/etc/os-release");
            var name = "";
            var pretty = "";
            var distro = "";
            var flatpak = false;
            foreach (var line in osRelease)
            {
                var keyValue = line.Split("=", 2);
                if (keyValue.Length == 1) continue;
                if (keyValue[0] == "NAME")
                    name = keyValue[1];
                if (keyValue[0] == "PRETTY_NAME")
                    pretty = keyValue[1];
                if (keyValue[0] == "ID_LIKE")
                {
                    if (keyValue[1].Contains("arch"))
                        distro = "arch";
                    if (keyValue[1].Contains("fedora"))
                        distro = "fedora";
                    if (keyValue[1].Contains("ubuntu"))
                        distro = "ubuntu";
                    if (keyValue[1].Contains("debian"))
                        distro = "ubuntu";
                }
                if (keyValue[0] == "ID")
                {   
                    if (keyValue[1].Contains("tumbleweed"))
                        distro = "fedora";
                    if (keyValue[1] == "org.freedesktop.platform")
                        flatpak = true;
                }
            }
            Distro = (distro == "") ? "ubuntu" : distro;
            DistroLong = pretty == "" ? (name == "" ? "Unknown distribution" : name) : pretty;
            IsFlatpak = flatpak;
        }
        catch
        {
            // If there's any kind of error opening the file or even finding it, just go with default.
            Distro = "ubuntu";
            DistroLong = "Unknown distribution";
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
            DalamudUpdater = new DalamudUpdater(storage.GetFolder("dalamud"), storage.GetFolder("runtime"), storage.GetFolder("dalamudAssets"), storage.Root, null, null)
            {
                Overlay = DalamudLoadInfo
            };
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
        string[] logfiles = { "dalamud.boot.log", "dalamud.boot.old.log", "dalamud.log", "dalamud.injector.log"};
        foreach (string logfile in logfiles)
            if (storage.GetFile(logfile).Exists) storage.GetFile(logfile).Delete();
        if (tsbutton)
            SetupLogging(mainargs);
        
    }

    public static void ClearAll(bool tsbutton = false)
    {
        ClearSettings(tsbutton);
        ClearPrefix();
        ClearPlugins(tsbutton);
        ClearTools(tsbutton);
        ClearLogs(true);
    }
}