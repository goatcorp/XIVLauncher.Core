![xlcore_sized](https://user-images.githubusercontent.com/16760685/197423373-b6082cdb-dc1f-46db-8768-3f507f182ba8.png)

# XIVLauncher.Core
Cross-platform version of XIVLauncher, optimized for Steam Deck. Comes with a version of [WINE tuned for FFXIV](https://github.com/goatcorp/wine-xiv-git).

## Building & Contributing
1. Clone this repository with submodules
2. Make sure you have a recent(.NET6+) version of the .NET SDK installed
2. Run `dotnet build` or `dotnet publish`

Common components that are shared with the Windows version of XIVLauncher are linked as a submodule in the "lib" folder. XIVLauncher Core can run on Windows, but is by far not as polished as the original Windows version, as such we are not distributing it.

## Distribution
XIVLauncher Core is packaged for various Linux distributions.

| Repo        | Status      |
| ----------- | ----------- |
| Flathub     | ![Flathub](https://img.shields.io/flathub/v/dev.goats.xivlauncher)       |
| AUR   | ![AUR version](https://img.shields.io/aur/version/xivlauncher)        |
| AUR(git) | ![AUR version](https://img.shields.io/aur/version/xivlauncher-git) |
