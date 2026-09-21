using System;
using System.Collections.Generic;

namespace CharacterReplacement
{
    /// <summary>Chat help: /lshelp for the overview, /lshelp &lt;topic&gt; for the details of one area.</summary>
    internal static class Help
    {
        private static readonly (string key, string title, string[] lines)[] Topics =
        {
            ("soldaten", "Soldaten", new[]
            {
                "/lsinfo              Status: wie viele Soldaten, wie viele wiederbelebt",
                "/lsspawn             einen Soldaten direkt vor dir erzeugen",
                "/lslook              Aussehen aller Soldaten neu wuerfeln",
                "/lsremove            Uebersicht und Loeschbefehle (siehe /lshelp aufraeumen)",
            }),
            ("arbeit", "Arbeit und Befehle", new[]
            {
                "/lsjobs              Auto-Jobs an/aus und Einstellungen anzeigen",
                "/lsbuild             allen sagen: Gebaeude fertigstellen",
                "/lsmaintain          allen sagen: Basis pflegen",
                "/lsclear             allen sagen: Gebiet roden",
                "/lsget               allen sagen: Material holen",
                "/lsfollow            allen sagen: folgen",
                "/lsstay              allen sagen: hier bleiben",
                "/lsbreaks on|off     Pausen verhindern - sie arbeiten durch",
                "Hinweis: /lsfollow und /lsstay halten die Auto-Jobs an, bis du einen",
                "Arbeitsbefehl gibst. Danach uebernehmen die Auto-Jobs wieder.",
            }),
            ("reichweite", "Suchradien", new[]
            {
                "/lsrange <zahl>      Radius fuer Baeume und herumliegendes Material",
                "/lsstruct <zahl>     Radius fuer Bauplaetze, Behaelter und Reparaturen",
                "Beide wirken sofort, ohne Neustart. 1 ist der Wert des Spiels,",
                "2 verdoppelt den Radius. Sehr grosse Werte kosten Serverleistung.",
            }),
            ("aufraeumen", "Aufraeumen", new[]
            {
                "/lsclean on|off|now  Leichen getoeteter Soldaten entfernen",
                "/lsclean <sekunden>  wie lange eine Leiche liegen bleibt",
                "/lsremove            Uebersicht: gesamt / verwaltet / ueberzaehlig",
                "/lsremove dupes      nur die ueberzaehligen entfernen",
                "/lsremove <anzahl>   so viele entfernen, die entferntesten zuerst",
                "/lsremove <name>     einen bestimmten entfernen",
                "/lsremove alle       alle Soldaten entfernen",
                "Loeschen ist endgueltig - auch nach einem Neustart sind sie weg.",
                "Der Story-Kelvin wird nie geloescht.",
            }),
            ("messen", "Leistung messen", new[]
            {
                "/lsperf              Server-FPS, CPU, RAM, Netzwerk, Anzahl KI-Figuren",
                "/lsprofil            Aufschluesselung eines Server-Frames in Millisekunden",
                "Beide messen in Fenstern von einer Minute - direkt nach dem Start",
                "steht da noch nichts.",
            }),
        };

        public static IEnumerable<string> Lines(string topic)
        {
            topic = (topic ?? "").Trim().ToLowerInvariant();

            if (topic.Length > 0)
            {
                foreach (var t in Topics)
                {
                    if (t.key != topic) continue;
                    yield return $"--- {t.title} ---";
                    foreach (var l in t.lines) yield return l;
                    yield break;
                }
                yield return $"Kein Hilfethema '{topic}'.";
            }

            yield return "LivingSoldiers - Befehle. Details mit /lshelp <thema>:";
            foreach (var t in Topics) yield return $"  /lshelp {t.key,-12} {t.title}";
            yield return "Am haeufigsten gebraucht: /lsinfo, /lsjobs, /lsbuild, /lsremove, /lsperf";
        }
    }
}
