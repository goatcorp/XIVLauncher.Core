using System.Numerics;

using ImGuiNET;

using Serilog;

using XIVLauncher.Common.Http;
using XIVLauncher.Core.Resources.Localization;

namespace XIVLauncher.Core.Components;

public class OtpEntryPage : Page
{
    private string otp = string.Empty;
    private bool appearing = false;
    private OtpListener? otpListener;

    public string? Result { get; private set; }
    public bool Cancelled { get; private set; }

    public OtpEntryPage(LauncherApp app) : base(app)
    {
        if (Program.Steam is not null) Program.Steam.OnGamepadTextInputDismissed += this.SteamOnOnGamepadTextInputDismissed;
    }

    private void SteamOnOnGamepadTextInputDismissed(bool success)
    {
        if (success)
        {
            if (Program.Steam is not null) this.Result = Program.Steam.GetEnteredGamepadText();
        }
    }

    public void Reset()
    {
        this.otp = string.Empty;
        this.appearing = true;
        this.Result = null;
        this.Cancelled = false;

        // TODO(goat): This doesn't work if you call it right after starting the app... Steam probably takes a little while to initialize. Might be annoying for autologin.
        // BUG: We have to turn this off when using OTP server, because there's no way to dismiss open keyboards
        if (Program.Steam != null && Program.Steam.IsValid && Program.IsSteamDeckHardware && App.Settings.IsOtpServer is false)
        {
            var success = Program.Steam.ShowGamepadTextInput(false, false, Strings.EnterYourOTP, 6, string.Empty);
            Log.Verbose("ShowGamepadTextInput: {Success}", success);
        }

        if (App.Settings.IsOtpServer ?? false)
        {
            try
            {
                // Start Listen
                this.StartOtpListener();
                Log.Debug("OTP server started...");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not start OTP HTTP listener");
            }
        }
    }

    private void TryAcceptOtp(string otp)
    {
        if (string.IsNullOrEmpty(otp) || otp.Length != 6)
        {
            Log.Error("Invalid OTP: {Otp}", otp);
            return;
        }

        Log.Verbose("Received OTP: {Otp}", otp);
        this.Result = otp;
    }

    public void StartOtpListener()
    {
        if (this.otpListener is null)
        {
            this.otpListener = new OtpListener("core-" + AppUtil.GetAssemblyVersion());
            this.otpListener.OnOtpReceived += this.TryAcceptOtp;
            Task.Run(this.otpListener.Start);
        }
    }

    public void StopOtpListener()
    {
        if (this.otpListener is not null)
        {
            this.otpListener.Stop();
            this.otpListener.OnOtpReceived -= this.TryAcceptOtp;
        }
        this.otpListener = null;
    }

    public override void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, 7f);

        var childSize = new Vector2(300, 200);
        var vpSize = ImGuiHelpers.ViewportSize;

        ImGui.SetNextWindowPos(new Vector2(vpSize.X / 2 - childSize.X / 2, vpSize.Y / 2 - childSize.Y / 2), ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.4f);

        if (ImGui.BeginChild("###otp", childSize, true, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Dummy(new Vector2(40));

            // center text in window
            ImGuiHelpers.CenteredText(Strings.EnterYourOTP);

            const int INPUT_WIDTH = 150;
            ImGui.SetNextItemWidth(INPUT_WIDTH);
            ImGuiHelpers.CenterCursorFor(INPUT_WIDTH);

            if (this.appearing)
            {
                ImGui.SetKeyboardFocusHere(0);
                this.appearing = false;
            }

            var doEnter = ImGui.InputText("###otpInput", ref this.otp, 6, ImGuiInputTextFlags.CharsDecimal | ImGuiInputTextFlags.EnterReturnsTrue);

            var buttonSize = new Vector2(INPUT_WIDTH / 2 - 4, 30);
            ImGuiHelpers.CenterCursorFor(INPUT_WIDTH);

            if (ImGui.Button(Strings.OKLabel, buttonSize) || doEnter)
            {
                TryAcceptOtp(this.otp);
            }

            ImGui.SameLine();

            if (ImGui.Button(Strings.CancelLabel, buttonSize))
            {
                this.Cancelled = true;
            }
        }

        ImGui.EndChild();

        ImGui.PopStyleVar();

        base.Draw();
    }
}
