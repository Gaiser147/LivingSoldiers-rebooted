using System;
using System.Collections;
using System.Reflection;
using RedLoader;
using Sons.Ai.Vail;
using Sons.Gameplay;
using Sons.Gameplay.GPS;
using Sons.Save;
using GameState = Sons.Save.GameState;
using SonsSdk;
using UnityEngine;
using Color = System.Drawing.Color;
using Object = UnityEngine.Object;

namespace CharacterReplacement
{
    public static class Config
    {
        public static ConfigCategory Category { get; private set; }
        public static ConfigEntry<bool> AddJianyu { get; private set; }
        public static ConfigEntry<bool> SolderGlasses { get; private set; }
        public static ConfigEntry<bool> UsePlayerModel { get; private set; }
        public static ConfigEntry<bool> RemoveDeadSoldiers { get; private set; }
        public static ConfigEntry<bool> NoBreaks { get; private set; }
        public static ConfigEntry<float> RemoveDeadAfterSeconds { get; private set; }
        public static ConfigEntry<bool> RandomizeLook { get; private set; }
        public static ConfigEntry<bool> HostCustomModel { get; private set; }
        public static ConfigEntry<float> OrderRangeMultiplier { get; private set; }
        public static ConfigEntry<float> ClearRadiusMultiplier { get; private set; }
        public static ConfigEntry<float> HolderRangeMultiplier { get; private set; }
        public static ConfigEntry<bool> AutoJobsEnabled { get; private set; }
        public static ConfigEntry<string> AutoJobs { get; private set; }
        public static ConfigEntry<string> AutoJobsFor { get; private set; }
        public static ConfigEntry<float> AutoJobsIdleSeconds { get; private set; }
        public static ConfigEntry<float> AutoJobsHolderRadius { get; private set; }
        public static ConfigEntry<float> AutoJobsPlayerRadius { get; private set; }

        public static void Init()
        {
            Category = ConfigSystem.CreateFileCategory("CharacterReplacement", "CharacterReplacement", "CharacterReplacement.cfg");
            AddJianyu = Category.CreateEntry("add_jianyu", true, "Add Jianyu", "If Jianyu should be part of the squad");
            SolderGlasses = Category.CreateEntry("soldier_glasses", true, "Soldier Glasses", "If soldiers should wear sun glasses");
            UsePlayerModel = Category.CreateEntry("use_player_model", true, "Player look for co-op", "Spawn soldiers as player-looking companions (PlayerRobby) so players without the mod see soldiers instead of Kelvin clones");
            RandomizeLook = Category.CreateEntry("randomize_look", true, "Random faces", "Give every soldier a random player head/skin (only with player look)");
            OrderRangeMultiplier = Category.CreateEntry("order_range_multiplier", 2f, "Order search range", "Multiplier for how far Kelvin and the soldiers search for things to collect (logs, sticks, stones ...). 1 = game default");
            ClearRadiusMultiplier = Category.CreateEntry("clear_radius_multiplier", 1f, "Clear area radius", "Multiplier for the radius of 'clear area' orders. 1 = game default");
            HolderRangeMultiplier = Category.CreateEntry("holder_range_multiplier", 2f, "Structure search range", "Multiplier for how far Kelvin and the soldiers look for structures: holders/containers, log sleds, build sites, structures to repair/maintain. 1 = game default");
            AutoJobsEnabled = Category.CreateEntry("auto_jobs_enabled", false, "Auto jobs enabled", "Let idle companions collect items into holders on their own (off by default)");
            AutoJobs = Category.CreateEntry("auto_jobs", "Log,Stick,Rock", "Auto jobs", "Items idle companions collect on their own into holders (comma separated: Log, Stick, Rock, Stone, Berries, Fish ...). Empty = off");
            AutoJobsFor = Category.CreateEntry("auto_jobs_for", "soldiers", "Auto jobs for", "Who works on their own: soldiers, kelvin, all, none");
            AutoJobsIdleSeconds = Category.CreateEntry("auto_jobs_idle_seconds", 60f, "Idle time before auto job", "Seconds a companion must be idle before starting a job on its own");
            AutoJobsHolderRadius = Category.CreateEntry("auto_jobs_holder_radius", 40f, "Auto jobs: holder radius", "Only start an auto job if a matching holder is within this many meters of the companion");
            NoBreaks = Category.CreateEntry("no_breaks", true, "Keep working (no breaks)", "Kelvin and the soldiers keep working until there is nothing left to do instead of taking a break after a few minutes. Player orders (follow, stay, take a break) are still obeyed");
            RemoveDeadSoldiers = Category.CreateEntry("remove_dead_soldiers", true, "Remove dead soldiers", "Remove the bodies of killed soldiers (saves performance). Never touches the story Kelvin/Virginia");
            RemoveDeadAfterSeconds = Category.CreateEntry("remove_dead_after_seconds", 120f, "Remove dead after (s)", "Seconds a dead soldier stays before the body is removed (time to pick up dropped items)");
            AutoJobsPlayerRadius = Category.CreateEntry("auto_jobs_player_radius", 150f, "Auto jobs: player radius", "Only start an auto job if a player is within this many meters (keeps them at the base)");
            HostCustomModel = Category.CreateEntry("host_custom_model", true, "Custom model for host", "Only when player look is off: the host sees the mod's own soldier model on the Kelvin clones. Players without the mod always see the game's model.");
        }

        public static void OnSettingsUiClosed()
        {
        }
    }

    public static class SkinInitializer
    {
        private static readonly int SubsurfaceMaskMapId = Shader.PropertyToID("_SubsurfaceMaskMap");
        private static readonly int ThicknessMapId = Shader.PropertyToID("_ThicknessMap");

        public static void InitializeSkins(GameObject root)
        {
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat.GetTexture(SubsurfaceMaskMapId) || mat.GetTexture(ThicknessMapId))
                    {
                        MaterialTools.SetDiffusionProfile(mat, MaterialTools.EDiffusionProfile.HumanSkinA);
                    }
                }
            }
        }
    }

    [RegisterTypeInIl2Cpp]
    public class SoldierController : MonoBehaviour
    {
        public static System.Collections.Generic.List<SoldierController> Soldiers = new();

        private VailActor _actor;
        private Robby _robby;
        private string _dyingSoldierName;
        private NamedIntData _isRevivedState;
        private System.Collections.Generic.List<GameObject> _injuredObjects;

        public bool IsRevived => _isRevivedState.SaveValueBool;

        private void Awake()
        {
            _actor = GetComponent<VailActor>();
            _robby = GetComponentInChildren<Robby>();
            InitializationCoro().RunCoro();
            Soldiers.Add(this);
            _actor.PreActionCallbacks += (Il2CppSystem.Action<VailActor, Group, IStimuli, Thought, Priority>)
                new Action<VailActor, Group, IStimuli, Thought, Priority>(PreAction);
        }

        private void PreAction(VailActor actor, Group group, IStimuli stimuli, Thought thought, Priority priority)
        {
            if (group._name != "Robby Recover Injured") return;
            RLog.Msg(Color.Orange, "Recovered " + _dyingSoldierName);
            _isRevivedState.SetValue(true);
            // Only now does he belong on the map.
            CharacterReplacement.SetLocatorVisible(_actor, true);
            if (_injuredObjects == null) return;
            foreach (GameObject go in _injuredObjects)
            {
                if (go) go.SetActive(false);
            }
        }

        public void Init(string soldierName, System.Collections.Generic.List<GameObject> injuredObjects = null)
        {
            if (!string.IsNullOrEmpty(_dyingSoldierName)) return;
            _dyingSoldierName = soldierName;
            _isRevivedState = GameState.GetOrCreate(_dyingSoldierName + "_revived", false, true);
            _injuredObjects = injuredObjects;
        }

        private void OnDestroy()
        {
            RLog.Msg($"[LivingSoldiers] Soldier object {_dyingSoldierName} destroyed (revived={(_isRevivedState != null && _isRevivedState.SaveValueBool)})");
            Soldiers.Remove(this);
        }

        private IEnumerator InitializationCoro()
        {
            float until = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < until) yield return null;
            bool revived = _isRevivedState.SaveValueBool;
            if (!revived)
            {
                _robby.SetInjuredState((Robby.InjuredState)1);
            }
            CharacterReplacement.SetLocatorVisible(_actor, revived);
            if (_injuredObjects == null) yield break;
            foreach (GameObject go in _injuredObjects)
            {
                if (!go) continue;
                bool shouldBeOn = !revived;
                if (go.activeSelf != shouldBeOn) go.SetActive(shouldBeOn);
                // A particle system that was switched off and on again does not necessarily start
                // running by itself - that is why the sound played but the red smoke stayed away.
                if (shouldBeOn) CharacterReplacement.LightFlare(go);
            }
            if (!revived) FlareKeepAlive().RunCoro();
            yield break;
        }

        /// <summary>
        /// Keeps the flare burning. Something on the crash site used to drive it, and that object is gone
        /// once the mod has taken the soldier over - without this the smoke dies after a few seconds.
        /// </summary>
        private IEnumerator FlareKeepAlive()
        {
            while (this && _injuredObjects != null && !_isRevivedState.SaveValueBool)
            {
                foreach (GameObject go in _injuredObjects)
                {
                    if (!go) continue;
                    if (!go.activeSelf) go.SetActive(true);
                    bool dead = true;
                    foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        if (ps.isPlaying) { dead = false; break; }
                    }
                    if (dead) CharacterReplacement.LightFlare(go);
                }
                float until = Time.realtimeSinceStartup + 3f;
                while (Time.realtimeSinceStartup < until) yield return null;
            }
        }
    }
}

namespace BundleData
{
    public static class CharacterBundle
    {
        private static GameObject _jianyu;
        private static GPSLocatorIcons _soldierlocatoricons;
        private static GameObject _tacticalsoldier;

        public static AssetBundle Bundle { get; private set; }

        public static GameObject Jianyu
        {
            get
            {
                if (!_jianyu)
                {
                    _jianyu = Bundle.LoadAsset("jianyu").Cast<GameObject>();
                    _jianyu.hideFlags = (HideFlags)61;
                }
                return _jianyu;
            }
        }

        public static GPSLocatorIcons Soldierlocatoricons
        {
            get
            {
                if (!_soldierlocatoricons)
                {
                    _soldierlocatoricons = Bundle.LoadAsset("soldierlocatoricons").Cast<GPSLocatorIcons>();
                    _soldierlocatoricons.hideFlags = (HideFlags)61;
                }
                return _soldierlocatoricons;
            }
        }

        public static GameObject Tacticalsoldier
        {
            get
            {
                if (!_tacticalsoldier)
                {
                    _tacticalsoldier = Bundle.LoadAsset("tacticalsoldier").Cast<GameObject>();
                    _tacticalsoldier.hideFlags = (HideFlags)61;
                }
                return _tacticalsoldier;
            }
        }

        public static void LoadFromFile(string path)
        {
            Bundle = AssetBundle.LoadFromFile(path);
        }

        public static GameObject InstantiateJianyu() => Object.Instantiate(Jianyu);
        public static GameObject InstantiateTacticalsoldier() => Object.Instantiate(Tacticalsoldier);
    }
}
