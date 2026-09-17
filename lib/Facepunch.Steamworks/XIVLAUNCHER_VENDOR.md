# Facepunch.Steamworks vendoring notes

Upstream:
https://github.com/Facepunch/Facepunch.Steamworks

Purpose:
Linux ARM64 Steamworks support for XIVLauncher.Core.

Upstream base:
b56e56a4ebd65f537e68254fe57b2a6a13c0d604

Additional changes:
- 7f86699 - Fix interface bootstrap versions for Steamworks SDK 1.64
- b0b0c3d - Expose floating gamepad text input

Only the Facepunch.Steamworks runtime project is vendored here.
Tests, generator sources, GitHub metadata, Unity plugin files, and other
development-only content are intentionally omitted.

This snapshot is temporary while ARM64 support is developed. It should
eventually be replaced by an upstream package or fetchable pinned revision.
