namespace XIVLauncher.Core.ImGuiFileDialog;

/// <summary>
/// Replacements for the Dalamud.Utility extensions the dialog relies on.
/// </summary>
internal static class FileDialogShims
{
    /// <summary>Enters a Monitor scope on the lock, disposed on using-scope exit.</summary>
    public static IDisposable EnterScope(this Lock l)
    {
        System.Threading.Monitor.Enter(l);
        return new MonitorScope(l);
    }

    /// <summary>Invokes a delegate inside a lock, swallowing nothing (parity with Dalamud.InvokeSafely minus logging).</summary>
    public static void InvokeSafely(this Delegate d, object arg)
    {
        if (d.Target is null)
            return;
        lock (d.Target)
        {
            d.DynamicInvoke(arg);
        }
    }

    private sealed class MonitorScope(Lock l) : IDisposable
    {
        public void Dispose() => System.Threading.Monitor.Exit(l);
    }
}
