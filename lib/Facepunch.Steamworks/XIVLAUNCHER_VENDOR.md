# Vendored Facepunch.Steamworks runtime

Upstream:
https://github.com/Facepunch/Facepunch.Steamworks

Base revision:
b56e56a4ebd65f537e68254fe57b2a6a13c0d604

This runtime-only snapshot is vendored for XIVLauncher Linux ARM64 support.

XIVLauncher continues to use `goaaats.Steamworks` for its existing platforms.
That path is intentionally unchanged. Linux ARM64 uses this newer POSIX
Facepunch.Steamworks implementation because it requires newer Steamworks SDK
interfaces that are not available through the existing package.

Compared with the upstream base revision, this snapshot contains six
intentional source differences:

- `Generated/Interfaces/ISteamApps.cs`
- `Generated/Interfaces/ISteamFriends.cs`
- `Generated/Interfaces/ISteamRemotePlay.cs`
- `Generated/Interfaces/ISteamUGC.cs`

  Updated interface bootstrap versions for Steamworks SDK 1.64, corresponding
  to upstream Facepunch.Steamworks PR #861:
  https://github.com/Facepunch/Facepunch.Steamworks/pull/861

- `SteamUtils.cs`

  Exposes `ShowFloatingGamepadTextInput`, which exists in the generated
  `ISteamUtils` binding but is not currently exposed by the upstream public
  `SteamUtils` wrapper. This corresponds to upstream issue #714:
  https://github.com/Facepunch/Facepunch.Steamworks/issues/714

- `Facepunch.Steamworks.Posix.csproj`

  Keeps the upstream multi-target framework list for normal builds, but
  selects `net6.0` only when XIVLauncher is targeting Linux ARM64. This avoids
  restoring unused .NET Framework and .NET Standard reference packages in
  hermetic Flatpak builds while leaving non-ARM builds unchanged.

Only the runtime project is vendored. Tests, generator tooling, GitHub
metadata, Unity packaging, and other development-only content are excluded.

This snapshot should be replaced with an upstream package or revision once
Facepunch.Steamworks provides the required Steamworks SDK 1.64 interface
versions and floating gamepad text input API in a suitable published release.
