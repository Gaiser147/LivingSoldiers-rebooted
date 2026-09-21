using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RedLoader;
using Sons.Ai.Vail;
using Sons.Crafting.Structures;
using UnityEngine;

namespace CharacterReplacement
{
    /// <summary>
    /// Splits the server frame into its parts. The perf monitor only ever measured this mod's own hooks,
    /// which says nothing about where the other 48 ms per frame go. This puts a stopwatch around the game's
    /// own per-frame work (AI world simulation) and reads the timings the network layer already keeps,
    /// so the biggest consumer can be named instead of guessed.
    /// </summary>
    internal static class Profiler
    {
        private struct Slot { public string Name; public long Ticks; public int Calls; }

        private static bool _patched;
        private static readonly Dictionary<MethodBase, int> Index = new();
        private static Slot[] _slots = Array.Empty<Slot>();
        private static int _frames;
        private static float _windowStart;
        private static float _lastFixedTime;
        private static int _fixedSteps;
        private static string _report = "Noch keine Messung. /lsprofil nach ein paar Minuten Spielzeit.";

        // Bolt-Zeiten, pro Frame aufsummiert (ms)
        private static double _bPoll, _bSend, _bScope, _bStepRemote, _bSimLocal, _bDispatch, _bScene, _bAdjust;

        private static readonly (Type type, string method)[] Targets =
        {
            (typeof(VailWorldSimulation), "Update"),
            (typeof(VailWorldSimulation), "UpdateServer"),
            (typeof(VailWorldSimulation), "TickWorldActors"),
            (typeof(VailWorldSimulation), "UpdateRelevantActors"),
            (typeof(VailWorldSimulation), "UpdateSimpleActorZones"),
            (typeof(VailWorldSimulation), "UpdateRelevantSpawnZones"),
            (typeof(VailWorldSimulation), "TickSpawners"),
            (typeof(VailWorldSimulation), "TickSimpleAnimals"),
            (typeof(VailWorldSimulation), "TickVillagesAndCaves"),
        };

        public static void Apply()
        {
            if (_patched) return;
            _patched = true;
            try
            {
                var h = new HarmonyLib.Harmony("livingsoldiers.profiler");
                var slots = new List<Slot>();
                foreach (var (type, name) in Targets)
                {
                    MethodInfo m = null;
                    try { m = AccessTools.Method(type, name); } catch { }
                    if (m == null) { RLog.Msg($"{CharacterReplacement.Tag} [Profil] {type.Name}.{name} nicht gefunden, wird uebersprungen"); continue; }
                    try
                    {
                        h.Patch(m,
                            prefix: new HarmonyMethod(typeof(Profiler), nameof(Pre)),
                            postfix: new HarmonyMethod(typeof(Profiler), nameof(Post)));
                        Index[m] = slots.Count;
                        slots.Add(new Slot { Name = name });
                    }
                    catch (Exception e) { RLog.Warning($"{CharacterReplacement.Tag} [Profil] {name}: {e.Message}"); }
                }
                _slots = slots.ToArray();
                _windowStart = Time.realtimeSinceStartup;
                RLog.Msg($"{CharacterReplacement.Tag} [Profil] {_slots.Length} Messpunkte aktiv");
            }
            catch (Exception e)
            {
                RLog.Error($"{CharacterReplacement.Tag} [Profil] Start fehlgeschlagen: {e.Message}");
            }
        }

        private static void Pre(MethodBase __originalMethod, out long __state) => __state = Stopwatch.GetTimestamp();

        private static void Post(MethodBase __originalMethod, long __state)
        {
            if (!Index.TryGetValue(__originalMethod, out int i)) return;
            _slots[i].Ticks += Stopwatch.GetTimestamp() - __state;
            _slots[i].Calls++;
        }

        /// <summary>Called once per frame from the mod's update.</summary>
        public static void Tick()
        {
            _frames++;
            try
            {
                float ft = Time.fixedTime;
                if (_lastFixedTime > 0f && Time.fixedDeltaTime > 0f)
                    _fixedSteps += Mathf.RoundToInt((ft - _lastFixedTime) / Time.fixedDeltaTime);
                _lastFixedTime = ft;
            }
            catch { }
            try
            {
                if (BoltNetwork.isRunning)
                {
                    _bPoll += BoltCore.PollNetworkTime.TotalMilliseconds;
                    _bSend += BoltCore.SendTime.TotalMilliseconds;
                    _bScope += BoltCore.AutoscopeTime.TotalMilliseconds;
                    _bStepRemote += BoltCore.StepNonControlledRemoteEntitiesTime.TotalMilliseconds;
                    _bSimLocal += BoltCore.SimulateLocalAndControlledEntitiesTime.TotalMilliseconds;
                    _bDispatch += BoltCore.DispatchAllEventsTime.TotalMilliseconds;
                    _bScene += BoltCore.InvokeRemoteSceneCallbacksTime.TotalMilliseconds;
                    _bAdjust += BoltCore.AdjustEstimatedRemoteFramesTime.TotalMilliseconds;
                }
            }
            catch { }

            if (Time.realtimeSinceStartup - _windowStart >= 60f) Build();
        }

        private static void Build()
        {
            float window = Time.realtimeSinceStartup - _windowStart;
            _windowStart = Time.realtimeSinceStartup;
            int frames = Math.Max(_frames, 1);
            float fps = _frames / Math.Max(window, 0.001f);
            double msPerFrame = 1000.0 / Math.Max(fps, 0.001f);

            var parts = new List<(string name, double ms, int calls)>();
            for (int i = 0; i < _slots.Length; i++)
            {
                double ms = _slots[i].Ticks * 1000.0 / Stopwatch.Frequency / frames;
                parts.Add((_slots[i].Name, ms, _slots[i].Calls));
                _slots[i].Ticks = 0; _slots[i].Calls = 0;
            }
            parts.Sort((a, b) => b.ms.CompareTo(a.ms));

            var sb = new StringBuilder();
            sb.Append($"Frame {msPerFrame:0.0} ms ({fps:0} fps). Davon: ");
            int shown = 0;
            foreach (var p in parts)
            {
                if (p.ms < 0.05 || shown >= 5) continue;
                shown++;
                sb.Append($"{p.name} {p.ms:0.0} ms ({p.ms / msPerFrame * 100:0}%, {(double)p.calls / frames:0.0}x/Frame); ");
            }
            if (shown == 0) sb.Append("nichts Messbares in der Weltsimulation; ");
            string worldSim = $"KI-Tick laut Spiel: Welt {Safe(() => VailWorldSimulation.Instance().GetWorldTickMs().ToString("0.0"))} ms, einfache Tiere {Safe(() => VailWorldSimulation.Instance().GetSimpleTickMs().ToString("0.0"))} ms";

            double f = frames;
            string bolt = $"Netzwerk pro Frame: lesen {_bPoll / f:0.00} | senden {_bSend / f:0.00} | Sichtbarkeit {_bScope / f:0.00} | fremde Objekte {_bStepRemote / f:0.00} | eigene {_bSimLocal / f:0.00} | Events {_bDispatch / f:0.00} ms";
            _bPoll = _bSend = _bScope = _bStepRemote = _bSimLocal = _bDispatch = _bScene = _bAdjust = 0;

            string phys = $"Physik/Fixschritte: {(double)_fixedSteps / f:0.0} pro Frame (fixedDeltaTime {Safe(() => (Time.fixedDeltaTime * 1000f).ToString("0.0"))} ms, Bolt-Rate {Safe(() => BoltCore.framesPerSecond.ToString())}/s)";
            _fixedSteps = 0;

            string world = $"Basis: {Safe(StructureCount)} Bauteile, Bolt-Objekte {Safe(() => BoltCore._entitiesOK.count.ToString())}";

            _report = sb.ToString();
            RLog.Msg($"{CharacterReplacement.Tag} [Profil] {_report}");
            RLog.Msg($"{CharacterReplacement.Tag} [Profil] {worldSim} | {phys}");
            RLog.Msg($"{CharacterReplacement.Tag} [Profil] {bolt}");
            RLog.Msg($"{CharacterReplacement.Tag} [Profil] {world}");
            _lines.Clear();
            _lines.Add(_report);
            _lines.Add(worldSim);
            _lines.Add(phys);
            _lines.Add(bolt);
            _lines.Add(world);
            _frames = 0;
        }

        private static readonly List<string> _lines = new() { "Noch keine Messung. /lsprofil nach einer Minute Spielzeit erneut." };

        private static ScrewStructureManager _structMgr;

        private static string StructureCount()
        {
            if (!_structMgr) _structMgr = UnityEngine.Object.FindObjectOfType<ScrewStructureManager>();
            return _structMgr && _structMgr._structures != null ? _structMgr._structures.Count.ToString() : "?";
        }

        private static string Safe(Func<string> f) { try { return f(); } catch { return "?"; } }

        public static IEnumerable<string> ChatLines() => _lines;
    }
}
