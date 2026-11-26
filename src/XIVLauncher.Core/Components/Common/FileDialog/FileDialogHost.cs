using XIVLauncher.Core.ImGuiFileDialog;

namespace XIVLauncher.Core.Components.Common;

/// <summary>
/// Singleton host that owns the shared folder-picker dialog and draws it each frame.
/// </summary>
public static class FileDialogHost
{
    private static FileDialog? dialog;
    private static Action<string>? onSelected;

    /// <summary>
    /// Opens a folder-mode picker (directory only, hidden files visible, single selection).
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="startPath">Starting directory; defaults to the launcher storage folder.</param>
    /// <param name="onSelect">Invoked with the chosen directory path when the user presses Ok.</param>
    public static void OpenFolder(string title, string? startPath, Action<string> onSelect)
    {
        var start = string.IsNullOrEmpty(startPath) ? Program.storage.Root.FullName : startPath;

        dialog = new FileDialog(
            "XIVLauncher_FilePicker",
            title,
            string.Empty, // empty filters => directory mode
            start,
            string.Empty,
            string.Empty,
            1,
            false,
            ImGuiFileDialogFlags.SelectOnly);
        dialog.Show();
        onSelected = onSelect;
    }

    /// <summary>
    /// Opens a file-mode picker with the given filter string.
    /// </summary>
    /// <param name="title">Window title.</param>
    /// <param name="filters">Filter string; ".*" shows all files.</param>
    /// <param name="startPath">Starting directory; defaults to the launcher storage folder.</param>
    /// <param name="onSelect">Invoked with the chosen file path when the user presses Ok.</param>
    public static void OpenFile(string title, string filters, string? startPath, Action<string> onSelect)
    {
        var start = string.IsNullOrEmpty(startPath) ? Program.storage.Root.FullName : startPath;

        dialog = new FileDialog(
            "XIVLauncher_FilePicker",
            title,
            filters,
            start,
            string.Empty,
            string.Empty,
            1,
            false,
            ImGuiFileDialogFlags.SelectOnly);
        dialog.Show();
        onSelected = onSelect;
    }

    /// <summary>Draws the dialog once per frame; call from the launcher draw loop.</summary>
    public static void Draw()
    {
        if (dialog is null)
            return;

        if (dialog.Draw())
        {
            if (dialog.GetIsOk())
            {
                var results = dialog.GetResults();
                if (results.Count > 0 && !string.IsNullOrEmpty(results[0]))
                    onSelected?.Invoke(results[0]);
            }

            dialog = null;
            onSelected = null;
        }
    }
}
