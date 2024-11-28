using System.Numerics;

using ImGuiNET;

using Serilog;

namespace XIVLauncher.Core.Components.Common;

public class Input : Component
{
    private string inputBacking = string.Empty;

    private volatile bool isSteamDeckInputActive = false;

    public string Label { get; }

    public string Hint { get; }

    public uint MaxLength { get; }

    public ImGuiInputTextFlags Flags { get; }

    public bool IsEnabled { get; set; } = true;
    public Vector2 Spacing { get; }

    public bool HasSteamDeckInput { get; set; }

    public string SteamDeckPrompt { get; set; }

    public bool TakeKeyboardFocus { get; set; }

    /** Executed on detection of the enter key **/
    public event Action? Enter;

    public string Value
    {
        get => this.inputBacking;
        set => this.inputBacking = value;
    }

    public Input(
        string label,
        string hint,
        Vector2? spacing,
        uint maxLength = 255,
        bool isEnabled = true,
        ImGuiInputTextFlags flags = ImGuiInputTextFlags.None)
    {
        this.Label = label;
        this.Hint = hint;
        this.MaxLength = maxLength;
        this.Flags = flags;
        this.IsEnabled = isEnabled;
        this.Spacing = spacing ?? Vector2.Zero;

        this.SteamDeckPrompt = hint;

        if (Program.Steam != null)
        {
            Program.Steam.OnGamepadTextInputDismissed += this.SteamOnOnGamepadTextInputDismissed;
            this.HasSteamDeckInput = Program.IsSteamDeckHardware;
        }
    }

    private void SteamOnOnGamepadTextInputDismissed(bool success)
    {
        if (success && this.isSteamDeckInputActive)
            this.inputBacking = Program.Steam!.GetEnteredGamepadText();

        this.isSteamDeckInputActive = false;
    }

    public override void Draw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12f, 10f));
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0);
        ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.BlueShade1);
        ImGui.PushStyleColor(ImGuiCol.FrameBgActive, ImGuiColors.BlueShade2);
        ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, ImGuiColors.BlueShade2);
        ImGui.PushStyleColor(ImGuiCol.TextDisabled, ImGuiColors.TextDisabled);
        ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.Text);

        if (this.TakeKeyboardFocus && ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        ImGui.Text(this.Label);

        if (!this.IsEnabled || this.isSteamDeckInputActive)
            ImGui.BeginDisabled();

        var ww = ImGui.GetWindowWidth();
        ImGui.SetNextItemWidth(ww);

        ImGui.PopStyleColor();

        ImGui.InputTextWithHint($"###{this.Id}", this.Hint, ref this.inputBacking, this.MaxLength, this.Flags);

        if (ImGui.IsItemFocused() && ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            Enter?.Invoke();
        }

        if (ImGui.IsItemActivated() && this.HasSteamDeckInput && Program.Steam != null && Program.Steam.IsValid)
        {
            this.isSteamDeckInputActive = Program.Steam?.ShowGamepadTextInput(this.Flags.HasFlag(ImGuiInputTextFlags.Password), false, this.SteamDeckPrompt, (int)this.MaxLength, this.inputBacking) ?? false;
            Log.Information("SteamDeck Input Active({Name}): {IsActive}", this.Label, this.isSteamDeckInputActive);
        }

        ImGui.Dummy(this.Spacing);

        if (!this.IsEnabled || this.isSteamDeckInputActive)
            ImGui.EndDisabled();

        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(4);
    }
}
