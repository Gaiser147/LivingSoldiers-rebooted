## Downloads

| File | For |
| --- | --- |
| `LivingSoldiers_v1.19.0.zip` | Single player and co-op host |
| `LivingSoldiers_v1.19.0_Server.zip` | Dedicated server |

Same DLL in both. The larger package adds the `character` asset bundle for the
soldiers' appearance, which is only needed where something is actually drawn.
Other players do not need the mod.

## What is new

**Crash sites are visible again.** The flare and the smoke column are two
separate objects — `FlareLit` and `DeadTactiSmoke` — and the column sits next to
the flare, not below it. Only the flare was being taken off the wreck, so the
column disappeared along with the wreck. Both are now detached, kept lit, and
re-lit by a watchdog if their particle systems stop.

**Map markers only after a rescue.** Every soldier used to be put on the map at
startup, which gave away all crash sites from the first minute.

**Duplicate spawns fixed** (since 1.13.0). Soldiers far from every player exist
only in the game's world simulation. The mod searched among real actors alone,
found none at server start and spawned a fresh set every time while the old ones
stayed — 109 Kelvin-type actors instead of 18 after a few restarts, which put the
server at roughly 13 FPS. Use `/lsremove dupes` to clean an affected save, at
least a minute after the server started.

**Administration and diagnostics.** `/lshelp` for the command overview,
`/lsremove` for cleanup, `/lsperf` and `/lsprofil` for server performance and a
per-frame breakdown.

Full history: [CHANGELOG.md](CHANGELOG.md)

## Credits

Original mod and character assets by **Toni Macaroni**, who also wrote
[RedLoader](https://github.com/ToniMacaroni/RedLoader). This is an unofficial
working copy, not affiliated with or endorsed by the original author.
