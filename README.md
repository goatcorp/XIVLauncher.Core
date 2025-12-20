![xlcore_sized](https://user-images.githubusercontent.com/16760685/197423373-b6082cdb-dc1f-46db-8768-3f507f182ba8.png)

# XIVLauncher.Core  [![Discord Shield](https://discordapp.com/api/guilds/581875019861328007/widget.png?style=shield)](https://discord.gg/3NMcUV5)
Cross-platform version of XIVLauncher, optimized for Steam Deck. Comes with a version of [WINE tuned for FFXIV](https://github.com/goatcorp/wine-xiv-git).

## Using on Steam Deck
If you want to use XIVLauncher on your Steam Deck, feel free to [follow our guide in our FAQ](https://goatcorp.github.io/faq/steamdeck). If you're having trouble, you can [join our Discord server](https://discord.gg/3NMcUV5) - please don't use the GitHub issues for troubleshooting unless you're sure that your problem is an actual issue with XIVLauncher.

## Building & Contributing
1. Clone this repository with submodules
2. Make sure you have a recent(.NET 6.0.400+) version of the .NET SDK installed
2. Run `dotnet build` or `dotnet publish`

Common components that are shared with the Windows version of XIVLauncher are linked as a submodule in the "lib" folder. XIVLauncher Core can run on Windows, but is by far not as polished as the [original Windows version](https://github.com/goatcorp/FFXIVQuickLauncher). Windows users should not use this application unless for troubleshooting purposes or development work.

## Distribution
XIVLauncher Core has community packages for various Linux distributions. Please be aware that **only the Flathub version is official**, but the others are **packaged by trusted community members**.  The community packages may not always be up-to-date, or may have versions that are broken or contain features under testing (especially if labeled as unstable or git). We can't take any responsibility for their safety or reliability.

| Repo        | Status      |
| ----------- | ----------- |
| [**Flathub (official)**](https://flathub.org/apps/details/dev.goats.xivlauncher) | ![Flathub](https://img.shields.io/flathub/v/dev.goats.xivlauncher) |
| [AUR](https://aur.archlinux.org/packages/xivlauncher) | ![AUR version](https://img.shields.io/aur/version/xivlauncher) |
| [AUR (bin)](https://aur.archlinux.org/packages/xivlauncher-bin) | ![AUR version](https://img.shields.io/aur/version/xivlauncher-bin) |
| [AUR (git)](https://aur.archlinux.org/packages/xivlauncher-git) | ![AUR version](https://img.shields.io/aur/version/xivlauncher-git) |
| [Copr (Fedora+openSuse+EL9)](https://copr.fedorainfracloud.org/coprs/rankyn/xivlauncher/) | ![COPR version](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Frankynbass%2FXIVLauncher4rpm%2Fmain%2Fbadge.json)|
| [GURU (Gentoo)](https://gitweb.gentoo.org/repo/proj/guru.git/tree/games-util/xivlauncher) | ![GURU version](https://repology.org/badge/version-for-repo/gentoo_ovl_guru/xivlauncher.core.svg?header=guru) |
| [MPR (Debian+Ubuntu)](https://mpr.makedeb.org/packages/xivlauncher)  | ![MPR package](https://repology.org/badge/version-for-repo/mpr/xivlauncher.core.svg?header=MPR) |
| [MPR (git) (Debian+Ubuntu)](https://mpr.makedeb.org/packages/xivlauncher-git)  | ![MPR package](https://repology.org/badge/version-for-repo/mpr/xivlauncher.core.svg?header=MPR) |
| [nixpkgs stable](https://search.nixos.org/packages?channel=25.11&from=0&size=50&sort=relevance&type=packages&query=xivlauncher) | ![nixpkgs stable version](https://repology.org/badge/version-for-repo/nix_stable_25_11/xivlauncher.core.svg?header=nixpkgs%2025.11) |
| [nixpkgs unstable](https://search.nixos.org/packages?channel=unstable&from=0&size=50&sort=relevance&type=packages&query=xivlauncher) | ![nixpkgs unstable version](https://repology.org/badge/version-for-repo/nix_unstable/xivlauncher.core.svg?header=nixpkgs%20unstable) |
| [PPA (Ubuntu)](https://launchpad.net/~linneris/+archive/ubuntu/xivlauncher-core-stable) | ![PPA version](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Flaunchpad.net%2Fapi%2F1.0%2F~linneris%2F%2Barchive%2Fxivlauncher-core-stable%3Fws.op%3DgetPublishedBinaries%26status%3DPublished%26distro_arch_series%3Dhttps%3A%2F%2Flaunchpad.net%2Fapi%2F1.0%2Fubuntu%2Fnoble%2Famd64&query=%24.entries[0].binary_package_version&logo=ubuntu&label=PPA&color=dark-green) |
| [AppImage](https://github.com/rankynbass/XIVLauncher-AppImage/releases/latest) | ![v1.1.0-1](https://img.shields.io/endpoint?url=https%3A%2F%2Fraw.githubusercontent.com%2Frankynbass%2FXIVLauncher-AppImage%2Fmaster%2Fbadge.json) |
