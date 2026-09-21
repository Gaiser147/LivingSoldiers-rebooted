# Changelog

All notable changes to this working copy. Versions before 1.7.2 are the original
mod by Toni Macaroni and are not covered here.

## 1.19.0

- Flares: the smoke column is a separate object (`DeadTactiSmoke`) sitting next
  to the flare (`FlareLit`), not below it. Only the flare was being taken off the
  wreck, so the column vanished with the wreck and crash sites were invisible
  from a distance. Every flare and smoke object that actually renders something
  is now detached and kept.

## 1.18.0

- Flares kept dying after a few seconds: the particle systems are single-shot and
  the script driving them sat on the wreck, which the mod removes. They are now
  set to loop, and a watchdog on each unrecovered soldier re-lights them every
  three seconds until he is revived.

## 1.17.0

- `/lshelp` and `/lscommands`: command overview in chat, with `/lshelp <topic>`
  for soldiers, work, ranges, cleanup and measuring.
- Flare selection prefers `FlareLit` again; the previous "topmost object named
  Flare" picked the unlit prop.
- The object tree of the first crash site is written to the log once, with the
  components per object, so flare problems can be diagnosed instead of guessed.

## 1.16.0

- Flares are detached from the wreck instead of copied. A copy starts as a fresh,
  not yet running particle system — only the sound restarted by itself, which is
  why the flare was audible but invisible.
- Map markers appear only after a soldier has been revived. They used to be set
  up for every soldier, so all crash sites were on the map from the start.

## 1.15.0

- `/lsremove dupes` refuses to run while the mod has not read its own soldiers
  yet — right after a restart that would have classified every soldier as
  surplus and deleted all of them. `/lsremove dupes force` overrides it.

## 1.14.0

- `/lsprofil`: splits one server frame into its parts — the game's own AI world
  simulation with a stopwatch around each stage, the network layer's own
  timings, physics steps per frame, and the size of the base in build pieces.

## 1.13.0

- **Duplicate spawns fixed.** Soldiers far from every player exist only in the
  game's world simulation. The mod searched among real actors only, found none
  at server start and spawned a fresh set every time; the old ones stayed. Six
  restarts had produced 109 Kelvin-type actors instead of 18.
  The mod now checks both lists, spawns no replacement for a soldier that still
  exists, and adopts him again via `ConvertToRealActor` when the game turns him
  into a real character.
- `/lsremove` reads the world simulation, so it finds distant soldiers too.
  `/lsremove dupes` removes only the surplus. The story Kelvin is identified by
  the lowest actor id and never removed.

## 1.12.0

- `/lsperf` also reports how many AI actors are in the world and how many of them
  are Kelvin or soldiers.

## 1.11.0

- `/lsremove`: remove soldiers by name, by count (farthest from any player
  first), or all of them, with a confirmation step.

## 1.10.0

- Bulk orders for every soldier at once: `/lsbuild`, `/lsmaintain`, `/lsclear`,
  `/lsget`, `/lsfollow`, `/lsstay`.

## 1.9.0

- No breaks: work orders no longer expire, so a soldier only stops when there is
  nothing left to do. Energy, rest and stamina are kept up while he works.
  Explicit player orders are always respected.

## 1.8.0

- Dead soldiers are removed after a grace period and marked in the save so they
  are not respawned.

## 1.7.3

- Per-minute server diagnostics: frame rate and hitches, process CPU and RAM,
  network entity counts, per-player ping, bandwidth, send window and packet loss,
  plus the time spent inside this mod's own hooks. `/lsperf` shows the last
  measurement in chat.

## 1.7.2

- Dedicated server support: soldiers are spawned on the server and attached to
  the network layer, so the game replicates them to everyone. Players without the
  mod are unaffected.
- Search ranges for trees, materials and structures are configurable and take
  effect without a restart.
- Soldier positions and state persist in the savegame.
