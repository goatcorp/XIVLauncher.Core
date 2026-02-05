using XIVLauncher.Common;

namespace XIVLauncher.Core.Configuration;

internal class CommonSettings
{
    private readonly ILauncherConfig config;

    public CommonSettings(ILauncherConfig config)
    {
        this.config = config;
    }

    public string AcceptLanguage => this.config.AcceptLanguage!;
    public ClientLanguage? ClientLanguage => this.config.ClientLanguage;
    public bool? KeepPatches => false;
    public DirectoryInfo PatchPath => this.config.PatchPath!;
    public DirectoryInfo GamePath => this.config.GamePath!;
    public long SpeedLimitBytes => 0;
    public int DalamudInjectionDelayMs => 0;
}
