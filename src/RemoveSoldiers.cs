using System;
using System.Collections.Generic;
using System.Linq;
using RedLoader;
using Sons.Ai.Vail;
using Sons.Multiplayer;
using SonsSdk.Networking;
using TheForest.Utils;
using UnityEngine;

namespace CharacterReplacement
{
    /// <summary>
    /// Removing living soldiers on purpose (performance): every soldier is a full AI actor plus a network
    /// entity, so fewer of them means less work per server frame. Removed soldiers are marked in the save
    /// and never come back - this is not reversible, which is why the command asks for a confirmation
    /// before wiping everything.
    /// </summary>
    internal static class RemoveSoldiers
    {
        private static float _confirmUntil;
        private static string _confirmKey = "";

        /// <summary>Positions of all connected players (used for "farthest away first").</summary>
        private static List<(ulong sid, Vector3 pos)> Players()
        {
            var list = new List<(ulong, Vector3)>();
            try
            {
                var pe = MultiplayerUtilities._playerEntities;
                if (pe != null)
                {
                    for (int i = 0; i < pe.Count; i++)
                    {
                        var e = pe[i];
                        if (e && e.isAttached) list.Add((MultiplayerUtilities.GetSteamId(e), e.transform.position));
                    }
                }
            }
            catch { }
            if (list.Count == 0)
            {
                try { if (LocalPlayer.Transform) list.Add((0UL, LocalPlayer.Transform.position)); } catch { }
            }
            return list;
        }

        private static Vector3 PosOf(SoldierSpots.Spot s)
        {
            try { if (s.Actor) return s.Actor.transform.position; } catch { }
            return s.LastPosition;
        }

        private static float MinDistToPlayers(SoldierSpots.Spot s, List<(ulong sid, Vector3 pos)> players)
        {
            if (players.Count == 0) return 0f;
            Vector3 p = PosOf(s);
            float best = float.MaxValue;
            foreach (var pl in players)
            {
                float d = Vector3.Distance(p, pl.pos);
                if (d < best) best = d;
            }
            return best;
        }

        /// <summary>Soldiers that currently exist in the world (the ones that cost performance).</summary>
        private static List<SoldierSpots.Spot> Living()
        {
            var list = new List<SoldierSpots.Spot>();
            foreach (var s in SoldierSpots.All())
            {
                try { if (s.Actor && !s.Dead) list.Add(s); } catch { }
            }
            return list;
        }

        public static IEnumerable<string> List(ulong callerSteamId)
        {
            var (total, known, surplus) = WorldCleanup.Count();
            yield return $"Kelvin-Actors in der Welt: {total} gesamt - davon Story-Kelvin 1, von der Mod verwaltet {known}, ueberzaehlig {surplus}";
            if (SoldierAdopt.PendingCount > 0)
                yield return $"{SoldierAdopt.PendingCount} Soldaten sind gerade zu weit weg und werden uebernommen, sobald jemand in ihre Naehe kommt.";

            var players = Players();
            var living = Living();
            if (living.Count > 0)
            {
                Vector3 me = Vector3.zero;
                bool haveMe = false;
                foreach (var pl in players) if (pl.sid == callerSteamId) { me = pl.pos; haveMe = true; }
                var sorted = haveMe
                    ? living.OrderBy(s => Vector3.Distance(PosOf(s), me)).ToList()
                    : living.OrderBy(s => s.Name).ToList();
                yield return $"Davon gerade als echte Figur geladen ({living.Count}):";
                int shown = 0;
                foreach (var s in sorted)
                {
                    if (shown++ >= 12) { yield return $"... und {sorted.Count - 12} weitere"; break; }
                    string dist = haveMe ? $"{Vector3.Distance(PosOf(s), me):0} m" : "?";
                    yield return $"  {s.Name} - {dist}";
                }
            }
            yield return "/lsremove dupes = nur die ueberzaehligen | /lsremove alle = alle Soldaten | /lsremove <anzahl> | /lsremove <name>";
        }

        /// <summary>Removes one soldier by name. Returns false if there is no such soldier in the world.</summary>
        public static bool RemoveByName(string name, out string message)
        {
            foreach (var s in Living())
            {
                if (!string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)) continue;
                DeadCleanup.RemoveSpot(s, "auf Befehl entfernt");
                message = $"{s.Name} entfernt.";
                return true;
            }
            message = $"Kein Soldat mit dem Namen '{name}' in der Welt. /lsremove zeigt die Liste.";
            return false;
        }

        /// <summary>Removes the given number of soldiers, farthest away from any player first.</summary>
        public static string RemoveCount(int count)
        {
            var players = Players();
            var living = Living();
            if (living.Count == 0) return "Es sind gerade keine Soldaten in der Welt.";
            count = Math.Clamp(count, 1, living.Count);
            var order = living.OrderByDescending(s => MinDistToPlayers(s, players)).Take(count).ToList();
            foreach (var s in order) DeadCleanup.RemoveSpot(s, "auf Befehl entfernt (weit entfernt)");
            string names = string.Join(", ", order.Select(s => s.Name));
            return $"{order.Count} Soldaten entfernt: {names}. Verbleibend: {Living().Count}";
        }

        /// <summary>Removes only the duplicates - needs a second call within 30 seconds to confirm.</summary>
        public static string RemoveSurplus(ulong callerSteamId, bool force = false)
        {
            var (total, known, surplus) = WorldCleanup.Count();
            if (surplus == 0) return $"Keine ueberzaehligen Kelvins gefunden ({total} gesamt, davon von der Mod verwaltet {known}).";
            // Schutz: solange die Mod ihre eigenen Soldaten noch nicht kennt, waere JEDER Kelvin "ueberzaehlig"
            // und wuerde geloescht. Nach einem Neustart dauert das Einrichten rund 45 Sekunden.
            if (!force && (!CharacterReplacement.SetupDone || known == 0))
            {
                return "Abgebrochen: die Mod hat ihre eigenen Soldaten noch nicht eingelesen - jetzt zu loeschen wuerde ALLE treffen. " +
                       "Bitte eine Minute warten und /lsremove erneut eingeben; sobald dort verwaltete Soldaten stehen, ist es sicher. " +
                       "(Wenn wirklich keine eigenen Soldaten mehr da sind: /lsremove dupes force)";
            }
            if (!Confirmed(callerSteamId, "dupes"))
                return $"Das entfernt {surplus} ueberzaehlige Kelvins. Story-Kelvin und die {known} verwalteten Soldaten bleiben. " +
                       "Zum Bestaetigen innerhalb von 30 Sekunden noch einmal /lsremove dupes eingeben.";
            int n = WorldCleanup.RemoveSurplus();
            return $"{n} ueberzaehlige Kelvins entfernt. Uebrig: {WorldCleanup.Count().total}";
        }

        /// <summary>Removes everything - needs a second call within 30 seconds to confirm.</summary>
        public static string RemoveAll(ulong callerSteamId)
        {
            var (total, _, _) = WorldCleanup.Count();
            if (total <= 1) return "Es sind ausser dem Story-Kelvin keine Soldaten da.";
            if (!Confirmed(callerSteamId, "alle"))
                return $"Das entfernt ALLE Soldaten endgueltig ({total - 1} Stueck) - auch nach einem Neustart kommen sie nicht zurueck. " +
                       "Der Story-Kelvin bleibt. Zum Bestaetigen innerhalb von 30 Sekunden noch einmal /lsremove alle eingeben.";
            int n = WorldCleanup.RemoveAllCompanions();
            SoldierAdopt.Clear();
            return $"{n} Soldaten entfernt. Der Story-Kelvin ist nicht betroffen.";
        }

        private static bool Confirmed(ulong steamId, string what)
        {
            float now = Time.realtimeSinceStartup;
            string key = steamId + ":" + what;
            if (_confirmKey != key || now > _confirmUntil)
            {
                _confirmKey = key;
                _confirmUntil = now + 30f;
                return false;
            }
            _confirmUntil = 0f;
            return true;
        }
    }
}
