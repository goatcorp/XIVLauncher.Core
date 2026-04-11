{
  inputs.nixpkgs.url = "github:nixos/nixpkgs/nixos-unstable";

  outputs =
    { nixpkgs, ... }:
    let
      forAllSystems =
        function:
        nixpkgs.lib.genAttrs nixpkgs.lib.systems.flakeExposed (
          system: function nixpkgs.legacyPackages.${system}
        );
    in
    {
      devShells = nixpkgs.lib.genAttrs [ "x86_64-linux" ] (
        system:
        let
          pkgs = nixpkgs.legacyPackages.${system};
        in
        {
          default = pkgs.mkShell {
            packages = with pkgs; [
              dotnetCorePackages.sdk_10_0
              sdl3
              libdecor
              icu
              libxrandr
              libxscrnsaver
              wayland
              libxkbcommon
              vulkan-loader
              gamemode
            ];
            env = {
              LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath (
                with pkgs;
                [
                  sdl3
                  libdecor
                  icu
                  libxrandr
                  libxscrnsaver
                  wayland
                  libxkbcommon
                  vulkan-loader
                  gamemode
                  libXcursor
                  libXi
                  libXfixes
                ]
              );
            };
          };
        }
      );
    };
}
