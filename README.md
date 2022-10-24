![xlcore_sized](https://user-images.githubusercontent.com/16760685/197423373-b6082cdb-dc1f-46db-8768-3f507f182ba8.png)

# XIVLauncher.Core  [![Discord Shield](https://discordapp.com/api/guilds/581875019861328007/widget.png?style=shield)](https://discord.gg/3NMcUV5)
Cross-platform version of XIVLauncher, optimized for Steam Deck. Comes with a version of [WINE tuned for FFXIV](https://github.com/goatcorp/wine-xiv-git).

## Using on Steam Deck
If you want to use XIVLauncher on your Steam Deck, feel free to [follow our guide in our FAQ](https://goatcorp.github.io/faq/steamdeck). If you're having trouble, you can [join our Discord server](https://discord.gg/3NMcUV5) - please don't use the GitHub issues for troubleshooting unless you're sure that your problem is an actual issue with XIVLauncher.

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
| MPR   | ![MPR package](https://repology.org/badge/version-for-repo/mpr/xivlauncher.svg?header=MPR) |
| RPM | [![COPR version](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Frankynbass%2FXIVLauncher4rpm%2Fmain%2Fbadge.json)](https://copr.fedorainfracloud.org/coprs/rankyn/xivlauncher/ "For Fedora and openSUSE")|
