# Licensing and third-party notices

Original project contributions are offered under the MIT license in `LICENSE`,
to the extent the project contributors hold rights to them. This grant does not
relicense third-party software, game content, trademarks or other rights held by
others. Super Crazy Rhythm Castle belongs to its respective rights holders.
The integration is unofficial; users supply their own game installation.

## Libraries distributed with the client

| Library | Version | License text |
| --- | --- | --- |
| Archipelago.MultiClient.Net | 6.7.1 | licenses/Archipelago.MultiClient.Net-MIT.txt |
| Newtonsoft.Json | File 11.0.1; product 11.0.1-beta2, bundled by MultiClient 6.7.1 | licenses/Newtonsoft.Json-MIT.txt |

The APWorld contains neither DLL. These notices accompany both package types so
the complete integration's dependencies remain documented. `DISTRIBUTION.json`
inside each corrected package records its payload hashes and source reference.

MultiClient source and notices:
https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/tree/0c57591db30f2497b0b4fef87164aa2bbe2e51b2

The Newtonsoft DLL is supplied within that exact NuGet package; its vendored
source and notice are at:
https://github.com/ArchipelagoMW/Archipelago.MultiClient.Net/tree/0c57591db30f2497b0b4fef87164aa2bbe2e51b2/Newtonsoft.Json

These packages preserve those DLLs unchanged. This records provenance of the
distributed binaries; it does not claim they were independently reproduced from
source. The packaging tool pins their reviewed SHA-256 hashes.

## Separately installed client runtime

The client uses BepInEx and Il2CppInterop under their LGPL terms. The recommended
BepInEx 6.0.0-be.785 build maps to commit
`6abdba47eeebe08552282e7a58ef0f4a9ab60b62` and pins Il2CppInterop 1.5.3.

- BepInEx: LGPL 2.1, text in `licenses/BepInEx-LGPL-2.1.txt`.
  Source: https://github.com/BepInEx/BepInEx/tree/6abdba47eeebe08552282e7a58ef0f4a9ab60b62
- Il2CppInterop: LGPL 3, including GPL 3 terms, in
  `licenses/Il2CppInterop-LGPL-3.0-and-GPL-3.0.txt`.
  Source: https://github.com/BepInEx/Il2CppInterop/tree/dbda1cb353b0f4253345dc45136d170b9e50a5a0
- HarmonyX and MonoMod.RuntimeDetour are also supplied by the separate runtime,
  not these client ZIPs. Their source projects are
  https://github.com/BepInEx/HarmonyX and https://github.com/MonoMod/MonoMod.

Obtain the runtime and its accompanying notices from its official distributor:
https://builds.bepinex.dev/projects/bepinex_be

This package does not redistribute the runtime, its generated interop files, or
retail game assemblies. The client loads runtime libraries from the user's
BepInEx installation; it does not embed or statically link those library binaries.
No project term prohibits replacing compatible libraries or reverse engineering
this client to debug modifications to those libraries. The client can be rebuilt
against the user's selected runtime using `client/build.ps1 -GameDir ... -SkipInstall`.
Compatibility with arbitrary replacement versions is not guaranteed. LGPL texts
and this explanation are not a claim that all possible configurations were tested.

## Archipelago and game-derived identifiers

The integration uses the Archipelago framework, installed separately:
https://github.com/ArchipelagoMW/Archipelago
Its components retain their applicable licenses.

The source includes names, identifiers and factual mappings researched from a
locally installed game for interoperability. The project license does not grant
rights to the game's underlying assets or proprietary code. Retail binaries,
media, asset bundles and generated game interop assemblies must not be packaged.
