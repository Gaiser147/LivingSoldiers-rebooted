# LivingSoldiers Rebooted

Revive the injured tactical soldiers scattered around the island in
**Sons of the Forest**. Once recovered they work like Kelvin: fetch logs and
materials, finish buildings, maintain the base, clear areas.

One build covers every situation. The mod detects by itself whether it runs in
single player, on a co-op host or on a dedicated server. **Other players do not
need it.** The soldiers are replicated by the game like any other character.

## Credits

The original **Living Soldiers** mod was created by **Toni Macaroni**, who also
wrote [RedLoader](https://github.com/ToniMacaroni/RedLoader), the mod loader
this depends on. All of the original design and the character assets are theirs.

This repository is a private working copy. The source was reconstructed from the
released DLL in order to fix a duplicate-spawn bug and to add server-side
administration and diagnostics. It is not an official release, and it is not
affiliated with or endorsed by the original author.

## Install

**You need RedLoader first.** Download it from
[its repository](https://github.com/ToniMacaroni/RedLoader) and install it into
your game or server folder.

**Then pick your package:**

| You are playing | Download |
| --- | --- |
| Single player or hosting co-op | `LivingSoldiers_<version>.zip` |
| Running a dedicated server | `LivingSoldiers_<version>_Server.zip` |

**Then:**

1. Open the zip. It contains a folder called `Mods`.
2. Copy that `Mods` folder into your game folder, next to
   `SonsOfTheForest.exe` (on a server: next to `SonsOfTheForestDS.exe`).
   Say yes when Windows asks about overwriting.
3. Start the game. On a server, restart it from your host's panel.

That is all. Your friends do not need to install anything.

**If you run a server on Linux** (most hosting providers do), one extra step is
needed, otherwise the mod is silently missing with no error message. Set this
before the server starts:

```
WINEDLLOVERRIDES=winhttp=n,b
```

Many panels have a field for start parameters or environment variables. If
yours does not, ask your provider's support to add it for you.

**Not working?** Check that `doorstop_config.ini` in the game folder says
`enabled = true`, and that a file called `version.dll` (server: `winhttp.dll`)
sits next to the game executable. Both belong to RedLoader.

## Commands

Typed in chat by a server owner, meaning your Steam ID is listed in
`ownerswhitelist.txt`. `/lshelp` gives the same overview in game. Full
reference: [docs/COMMANDS.md](docs/COMMANDS.md).

| Command | What it does |
| --- | --- |
| `/lshelp [topic]` | Command overview, or the details of one area |
| `/lsinfo` | How many soldiers exist, how many are revived |
| `/lsspawn` | Spawn a soldier in front of you |
| `/lsjobs` | Auto jobs on/off and current settings |
| `/lsbuild` `/lsmaintain` `/lsclear` `/lsget` | Order everyone to work |
| `/lsfollow` `/lsstay` | Follow you, or stay put |
| `/lsbreaks on\|off` | No breaks, they keep working |
| `/lsrange` `/lsstruct` | Search radius for materials and structures |
| `/lsclean` | Remove the bodies of killed soldiers |
| `/lsremove` | Overview and removal, including leftover duplicates |
| `/lsperf` `/lsprofil` | Server performance and a per-frame breakdown |

## Why the server administration exists

Characters far from every player are not kept as real objects by the game, only
as entries in a world simulation. Earlier versions looked for their soldiers
among the real objects alone, found none at server start because nobody is near
them yet, and spawned a fresh set every time while the old ones stayed. After a
few restarts a server carried over a hundred Kelvin-type AI actors, which
dropped it to roughly 13 frames per second and cost connected players more than
half their frame rate.

Since 1.13.0 the mod checks both lists, spawns no duplicates, and picks an
existing soldier back up the moment the game turns him into a real character.
`/lsremove dupes` clears the leftovers from an affected save.

## Building from source

.NET 8 SDK. The project references the assemblies that RedLoader generates in
your own installation, so point `GRoot` in `src/LivingSoldiers.csproj` at your
`_Redloader` folder:

```
dotnet build src/LivingSoldiers.csproj -c Release
```

The output targets `net8.0` but is loaded by RedLoader's .NET 6 runtime. The
assembly declares the matching target framework attribute itself.
