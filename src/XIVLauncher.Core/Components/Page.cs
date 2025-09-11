using System.Numerics;

namespace XIVLauncher.Core.Components;

public class Page : Component
{
    protected LauncherApp App { get; }

    public Vector2? Padding { get; init; } = null;

    protected Page(LauncherApp app)
    {
        this.App = app;
    }

    public virtual void OnShow()
    {
    }
}
