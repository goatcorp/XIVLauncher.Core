using System.Diagnostics;
using System.Numerics;

using Hexa.NET.ImGui;

using Serilog;

using XIVLauncher.Common;
using XIVLauncher.Common.Game;
using XIVLauncher.Common.PlatformAbstractions;
using XIVLauncher.Common.Util;
using XIVLauncher.Core.Accounts;
using XIVLauncher.Core.Components;
using XIVLauncher.Core.Components.LoadingPage;
using XIVLauncher.Core.Components.MainPage;
using XIVLauncher.Core.Components.SettingsPage;
using XIVLauncher.Core.Configuration;
using XIVLauncher.Core.Resources.Localization;
using XIVLauncher.PlatformAbstractions;

namespace XIVLauncher.Core;

public class LauncherApp : Component
{
    public static bool IsDebug { get; private set; } = Debugger.IsAttached;

    #region Modal State

    private bool isModalDrawing = false;
    private bool modalOnNextFrame = false;
    private string modalText = string.Empty;
    private string modalTitle = string.Empty;
    private string modalButtonText = string.Empty;
    private Action modalButtonPressAction = null!;
    private readonly ManualResetEvent modalWaitHandle = new(false);

    #endregion

    public enum LauncherState
    {
        Main,
        Settings,
        Loading,
        OtpEntry,
        Fts,
        SteamDeckPrompt,
    }

    private LauncherState state = LauncherState.Main;

    public LauncherState State
    {
        get => this.state;
        set
        {
            // If we are coming from the settings, we should reload the news, as the client language might have changed
            if (this.state == LauncherState.Settings)
            {
                this.mainPage.ReloadNews();
            }

            this.state = value;

            switch (value)
            {
                case LauncherState.Settings:
                    this.setPage.OnShow();
                    break;

                case LauncherState.Main:
                    this.mainPage.OnShow();
                    break;

                case LauncherState.Loading:
                    this.LoadingPage.OnShow();
                    break;

                case LauncherState.OtpEntry:
                    this.otpEntryPage.OnShow();
                    break;

                case LauncherState.Fts:
                    this.ftsPage.OnShow();
                    break;

                case LauncherState.SteamDeckPrompt:
                    this.steamDeckPromptPage.OnShow();
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }

    public Page CurrentPage => this.state switch
    {
        LauncherState.Main => this.mainPage,
        LauncherState.Settings => this.setPage,
        LauncherState.Loading => this.LoadingPage,
        LauncherState.OtpEntry => this.otpEntryPage,
        LauncherState.Fts => this.ftsPage,
        LauncherState.SteamDeckPrompt => this.steamDeckPromptPage,
        _ => throw new ArgumentOutOfRangeException(nameof(this.state), this.state, null)
    };

    public ILauncherConfig Settings => Program.Config;
    public Launcher Launcher { get; private set; }
    public ISteam? Steam => Program.Steam;
    public Storage Storage { get; private set; }

    public LoadingPage LoadingPage { get; }

    public AccountManager Accounts;
    public CommonUniqueIdCache UniqueIdCache;

    private readonly MainPage mainPage;
    private readonly SettingsPage setPage;
    private readonly OtpEntryPage otpEntryPage;
    private readonly FtsPage ftsPage;
    private readonly SteamDeckPromptPage steamDeckPromptPage;

    private readonly Background background = new();

    public LauncherApp(Storage storage, string frontierUrl, string? cutOffBootver)
    {
        this.Storage = storage;

        this.Accounts = new AccountManager(this.Storage.GetFile("accounts.json"));
        this.UniqueIdCache = new CommonUniqueIdCache(this.Storage.GetFile("uidCache.json"));

        this.Launcher = new Launcher(Program.Steam, UniqueIdCache, frontierUrl, Program.Config.AcceptLanguage ?? ApiHelpers.GenerateAcceptLanguage());

        this.mainPage = new MainPage(this);
        this.setPage = new SettingsPage(this);
        this.otpEntryPage = new OtpEntryPage(this);
        this.LoadingPage = new LoadingPage(this);
        this.ftsPage = new FtsPage(this);
        this.steamDeckPromptPage = new SteamDeckPromptPage(this);

        if (!EnvironmentSettings.IsNoKillswitch && !string.IsNullOrEmpty(cutOffBootver))
        {
            var bootver = SeVersion.Parse(Repository.Boot.GetVer(Program.Config.GamePath));
            var cutoff = SeVersion.Parse(cutOffBootver);

            if (bootver > cutoff)
            {
                this.ShowMessage("XIVLauncher is unavailable at this time as there were changes to the login process during a recent patch." +
                                "\n\nWe need to adjust to these changes and verify that our adjustments are safe before we can re-enable the launcher." +
                                "\n\nYou can use the Official Launcher instead until XIVLauncher has been updated.",
                                "XIVLauncher", "Close Launcher", () => Environment.Exit(0));
                return;
            }
        }

        this.RunStartupTasks();

#if DEBUG
        IsDebug = true;
#endif
    }

    public void ShowMessage(string text, string title = "XIVLauncher", string modalButtonText = "OK", Action? modalPressedAction = default)
    {
        if (this.isModalDrawing)
            throw new InvalidOperationException("Cannot open modal while another modal is open");

        this.modalText = text;
        this.modalTitle = title;
        this.modalButtonText = modalButtonText;
        if (modalPressedAction is null)
        {
            this.modalButtonPressAction = () =>
            {
                ImGui.CloseCurrentPopup();
                this.isModalDrawing = false;
                this.modalWaitHandle.Set();
            };
        }
        else
        {
            this.modalButtonPressAction = modalPressedAction;
        }
        this.isModalDrawing = true;
        this.modalOnNextFrame = true;
    }

    public void ShowMessageBlocking(string text, string title = "XIVLauncher", bool canContinue = true)
    {
        if (!this.modalWaitHandle.WaitOne(0) && this.isModalDrawing)
            throw new InvalidOperationException("Cannot open modal while another modal is open");

        this.modalWaitHandle.Reset();
        this.ShowMessage(text, title);
        this.modalWaitHandle.WaitOne();
    }

    public void ShowExceptionBlocking(Exception exception, string context)
    {
        this.ShowMessageBlocking($"An error occurred ({context}).\n\n{exception}", Strings.XIVLauncherError);
    }

    public bool HandleContinuationBlocking(Task task)
    {
        if (task.IsFaulted)
        {
            Log.Error(task.Exception, "Task failed");
            this.ShowMessageBlocking(task.Exception?.InnerException?.Message ?? "Unknown error - please check logs.", "Error");
            return false;
        }
        else if (task.IsCanceled)
        {
            this.ShowMessageBlocking("Task was canceled.", "Error");
            return false;
        }

        return true;
    }

    public void AskForOtp()
    {
        this.otpEntryPage.Reset();
        this.State = LauncherState.OtpEntry;
    }

    public string? WaitForOtp()
    {
        while (this.otpEntryPage.Result == null && !this.otpEntryPage.Cancelled)
            Thread.Yield();

        var res = this.otpEntryPage.Result;
        this.otpEntryPage.StopOtpListener();
        return res;
    }

    public void StartLoading(string line1, string line2 = "", string line3 = "", bool isIndeterminate = true, bool canCancel = false, bool canDisableAutoLogin = false)
    {
        this.State = LauncherState.Loading;
        this.LoadingPage.Line1 = line1;
        this.LoadingPage.Line2 = line2;
        this.LoadingPage.Line3 = line3;
        this.LoadingPage.IsIndeterminate = isIndeterminate;
        this.LoadingPage.CanCancel = canCancel;
        this.LoadingPage.CanDisableAutoLogin = canDisableAutoLogin;

        this.LoadingPage.Reset();
    }

    public void StopLoading()
    {
        this.State = LauncherState.Main;
    }

    public void RunStartupTasks()
    {
#if FLATPAK
        this.ftsPage.OpenFtsIfNeeded();
#endif
        this.mainPage.DoAutoLoginIfApplicable();
    }

    public override void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));

        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(ImGuiHelpers.ViewportSize);
        if (ImGui.Begin("Background",
                        ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNavFocus
                        | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            this.background.Draw();

        }

        ImGui.End();

        ImGui.PopStyleVar(2);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, this.CurrentPage.Padding ?? ImGui.GetStyle().WindowPadding);

        ImGui.SetNextWindowPos(new Vector2(0, 0));
        ImGui.SetNextWindowSize(ImGuiHelpers.ViewportSize);
        ImGui.SetNextWindowBgAlpha(0.75f);

        ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGuiColors.BlueShade0);

        if (ImGui.Begin("XIVLauncher", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            this.CurrentPage.Draw();
            base.Draw();
        }

        ImGui.End();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor();

        this.DrawModal();
    }

    private void DrawModal()
    {
        ImGui.SetNextWindowSize(new Vector2(450, 300));

        if (ImGui.BeginPopupModal(this.modalTitle + "###xl_modal", ref this.isModalDrawing, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoTitleBar))
        {
            if (ImGui.BeginChild("###xl_modal_scrolling", new Vector2(0, -ImGui.GetTextLineHeightWithSpacing() * 2)))
            {
                ImGui.TextWrapped(this.modalText);
            }

            ImGui.EndChild();

            const float BUTTON_WIDTH = 120f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - BUTTON_WIDTH) / 2);

            if (ImGui.Button(modalButtonText, new Vector2(BUTTON_WIDTH, 40)))
            {
                modalButtonPressAction();
            }

            ImGui.EndPopup();
        }

        if (this.modalOnNextFrame)
        {
            ImGui.OpenPopup("###xl_modal");
            this.modalOnNextFrame = false;
            this.isModalDrawing = true;
        }
    }
}
