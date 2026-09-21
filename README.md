# LivingSoldiers

Revive the injured tactical soldiers scattered around the island in
**Sons of the Forest**. Once recovered they work like Kelvin: fetch logs and
materials, finish buildings, maintain the base, clear areas.

One build covers every situation. The mod detects by itself whether it runs in
single player, on a co-op host or on a dedicated server. **Other players do not
need it** — the soldiers are replicated by the game like any other character.

---

## Credits

The original **Living Soldiers** mod was created by **Toni Macaroni**, who also
wrote [RedLoader](https://github.com/ToniMacaroni/RedLoader), the mod loader
this depends on. All of the original design and the character assets are theirs.

This repository is a private working copy: the source was reconstructed from the
released DLL in order to fix a duplicate-spawn bug and to add server-side
administration and diagnostics. It is not an official release, and it is not
affiliated with or endorsed by the original author.

---

## Packages

| Package | Size | For |
| --- | --- | --- |
| `LivingSoldiers_<version>.zip` | ~60 MB | Single player and co-op host |
| `LivingSoldiers_<version>_Server.zip` | ~50 KB | Dedicated server |

Same DLL in both. The only difference is the `character` asset bundle, which
holds the soldiers' own look — needed only where something is actually drawn,
so the server package leaves it out.

## Installing

Requires [RedLoader](https://github.com/ToniMacaroni/RedLoader): `version.dll`
(server: `winhttp.dll`) and the `_Redloader` folder must sit in the game or
server directory, and `doorstop_config.ini` must say `enabled = true`.

1. Copy the `Mods` folder from the package into the game or server directory,
   overwriting existing files.
2. Start the game. On a server, restart it from the panel — a loaded mod cannot
   be swapped while it is running.

**Linux server under Wine**, set before launching:

```
WINEDLLOVERRIDES=winhttp=n,b
```

Without it Wine uses its own `winhttp.dll`, RedLoader never starts, and the mod
is silently missing with no error message.

## Commands

Typed in chat by a server owner (Steam ID in `ownerswhitelist.txt`). `/lshelp`
gives the same overview in game. Full reference: [docs/COMMANDS.md](docs/COMMANDS.md).

| Command | What it does |
| --- | --- |
| `/lshelp [topic]` | Command overview, or the details of one area |
| `/lsinfo` | How many soldiers exist, how many are revived |
| `/lsspawn` | Spawn a soldier in front of you |
| `/lsjobs` | Auto jobs on/off and current settings |
| `/lsbuild` `/lsmaintain` `/lsclear` `/lsget` | Order everyone to work |
| `/lsfollow` `/lsstay` | Follow you / stay put |
| `/lsbreaks on\|off` | No breaks — they keep working |
| `/lsrange` `/lsstruct` | Search radius for materials / structures |
| `/lsclean` | Remove the bodies of killed soldiers |
| `/lsremove` | Overview and removal, including leftover duplicates |
| `/lsperf` `/lsprofil` | Server performance and a per-frame breakdown |

## Why the server administration exists

Characters far from every player are not kept as real objects by the game, only
as entries in a world simulation. Earlier versions looked for their soldiers
among the real objects alone, found none at server start (nobody is near them
yet) and spawned a fresh set every time while the old ones stayed. After a few
restarts a server carried over a hundred Kelvin-type AI actors, which dropped it
to roughly 13 frames per second and cost connected players more than half their
frame rate.

Since 1.13.0 the mod checks both lists, spawns no duplicates, and picks an
existing soldier back up the moment the game turns him into a real character.
`/lsremove dupes` clears the leftovers from an affected save.

## Building

.NET 8 SDK. The project references the assemblies that RedLoader generates in
your own installation, so point `GRoot` in `src/LivingSoldiers.csproj` at your
`_Redloader` folder:

```
dotnet build src/LivingSoldiers.csproj -c Release
```

The output targets `net8.0` but is loaded by RedLoader's .NET 6 runtime; the
assembly declares the matching target framework attribute itself.
