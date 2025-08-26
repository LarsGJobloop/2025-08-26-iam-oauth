{
  description = "An backend project covering several advanced concepts";

  inputs = {
    nixpkgs.url = "github:nixos/nixpkgs?ref=nixos-unstable";
  };

  outputs =
    { self, nixpkgs }:
    let
      withSystem = nixpkgs.lib.genAttrs [
        "x86_64-linux" # TODO! NOT Verified
        "x86_64-darwin" # TODO! NOT Verified
        "aarch64-linux" # TODO! NOT Verified
        "aarch64-darwin" # Verified
      ];
      withPkgs = callback: withSystem (system: callback (import nixpkgs { inherit system; }));
    in
    {
      devShells = withPkgs (pkgs: {
        default = pkgs.mkShell {
          packages = with pkgs; [
            dotnetCorePackages.sdk_9_0
          ];
        };

        env = {
          DOTNEtDOTNET_ROOT = builtins.toString pkgs.dotnetCorePackages.sdk_9_0;
        };
      });

      formatter = withPkgs (pkgs: pkgs.nixfmt-rfc-style);
    };
}
