# Command reference

Typed in chat by a server owner, your Steam ID has to be listed in
`ownerswhitelist.txt`. `/lshelp` prints the same overview in game.

## Soldiers

| Command | What it does |
| --- | --- |
| `/lsinfo` | Status: how many soldiers exist, how many are revived |
| `/lsspawn` | Spawn a soldier directly in front of you |
| `/lslook` | Re-roll the appearance of all soldiers |

## Work

| Command | What it does |
| --- | --- |
| `/lsjobs` | Auto jobs on/off and current settings |
| `/lsbuild` | Tell everyone to finish buildings |
| `/lsmaintain` | Tell everyone to maintain the base |
| `/lsclear` | Tell everyone to clear the area |
| `/lsget` | Tell everyone to fetch materials |
| `/lsfollow` | Everyone follows you |
| `/lsstay` | Everyone stays where they are |
| `/lsbreaks on\|off` | No breaks, they keep working until there is nothing left |

`/lsfollow` and `/lsstay` put the auto jobs on hold. They resume once you give a
work command again.

## Search ranges

| Command | What it does |
| --- | --- |
| `/lsrange <number>` | Radius for trees and materials lying around |
| `/lsstruct <number>` | Radius for build sites, holders and repairs |

`1` is the game's own value, `2` doubles it. Both take effect immediately, no
restart needed. Large values cost server performance, because every search scans
a bigger area every time.

## Cleanup

| Command | What it does |
| --- | --- |
| `/lsclean on\|off\|now` | Remove the bodies of killed soldiers |
| `/lsclean <seconds>` | How long a body stays before it is removed |
| `/lsremove` | Overview: total, managed by the mod, surplus |
| `/lsremove dupes` | Remove the surplus ones only |
| `/lsremove <number>` | Remove that many, farthest from any player first |
| `/lsremove <name>` | Remove one specific soldier |
| `/lsremove alle` | Remove every soldier |

Removal is permanent and survives a restart. The story Kelvin is never removed. He is
identified by the lowest actor id, since he exists from the moment the world is
created.

Run `/lsremove dupes` at least a minute after the server started. Before that the
mod has not read its own soldiers yet, and every soldier would count as surplus.
It refuses in that case and says so; `/lsremove dupes force` overrides it.

## Measuring

| Command | What it does |
| --- | --- |
| `/lsperf` | Server FPS, CPU, RAM, network, number of AI actors |
| `/lsprofil` | Breakdown of one server frame in milliseconds |

Both measure in one-minute windows, so right after a restart they have nothing to
report yet.

`/lsprofil` covers the game's own AI world simulation stage by stage, the network
layer's internal timings (reading, sending, visibility, entity simulation,
events), how many physics steps run per frame, and how many build pieces the base
consists of.
