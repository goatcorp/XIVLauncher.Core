using Hexa.NET.ImGui;

namespace XIVLauncher.Core.Components.SettingsPage;

public class NumericSettingsEntry : SettingsEntry<int>
{
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public int Step { get; set; }
    private readonly Guid id = Guid.NewGuid();

    public NumericSettingsEntry(string name, string description, Func<int> load, Action<int> save, int minValue = 0, int maxValue = int.MaxValue, int step = 1)
        : base(name, description, load, save)
    {
        this.MinValue = minValue;
        this.MaxValue = maxValue;
        this.Step = step;
    }

    public override void Draw()
    {
        var nativeValue = this.Value;

        ImGuiHelpers.TextWrapped(this.Name);

        if (ImGui.InputInt($"###{this.id:N}", ref nativeValue, Step))
        {
            this.InternalValue = Math.Max(this.MinValue, Math.Min(this.MaxValue, nativeValue));
        }

        base.Draw();
    }
}
