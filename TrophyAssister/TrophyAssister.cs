// TrophyAssister - client-side trophy drop helper for Valheim 1.0 (Unity 6, BepInEx 5.4.23+)
//
// Nothing runs on the server, so it works on vanilla servers. The only network
// traffic is one message per kill between players: drops are rolled by the game
// that owns the creature, and when that isn't the killer's game, the owner sends
// the result to the killer, who rolls their own pity (see TrophyNet).
//
// Pity system: every kill of a creature that does NOT drop its trophy adds a
// bonus to that creature's next trophy chance. When the trophy drops, the
// counter for that creature resets. Counters are saved per character.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TrophyAssister
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class TrophyAssisterPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.koendertjh.trophyassister";
        public const string ModName = "TrophyAssister";
        public const string ModVersion = PluginInfo.Version;

        internal static ManualLogSource Log;

        // General
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> NotifyOnDrop;
        internal static ConfigEntry<bool> ShowPityProgress;
        internal static ConfigEntry<bool> LogKills;

        // Base boost
        internal static ConfigEntry<float> ChanceMultiplier;
        internal static ConfigEntry<bool> OnlyUncollected;
        internal static ConfigEntry<float> StarChanceMultiplier;

        // Pity
        internal static ConfigEntry<bool> PityEnabled;
        internal static ConfigEntry<float> PityBonusPerKill;
        internal static ConfigEntry<PityMode> PityModeSetting;
        internal static ConfigEntry<float> PityGrowthPerKill;
        internal static ConfigEntry<float> RareThreshold;
        internal static ConfigEntry<float> RareGrowthPerKill;
        internal static ConfigEntry<int> GuaranteeAfterKills;
        internal static ConfigEntry<bool> OnlyCountMyKills;

        // Trophy list
        internal static ConfigEntry<bool> ShowChancesInList;
        internal static ConfigEntry<bool> ShowMissingTrophies;
        internal static ConfigEntry<bool> SpoilerFree;
        internal static ConfigEntry<string> HiddenTrophies;

        // Wishbone tracker
        internal static ConfigEntry<bool> TrackerEnabled;
        internal static ConfigEntry<float> TrackerRange;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;

            Enabled = Config.Bind("1 - General", "Enabled", true, "Master switch.");
            NotifyOnDrop = Config.Bind("1 - General", "NotifyOnDrop", true,
                "Show a message when a trophy drops.");
            ShowPityProgress = Config.Bind("1 - General", "ShowPityProgress", true,
                "After a kill without a trophy, show the dry kills and new chance below the top-left messages.");
            LogKills = Config.Bind("1 - General", "LogKills", false,
                "Write a line to BepInEx/LogOutput.log for every kill of a creature with a trophy: " +
                "who landed the deciding hit, whether it counted for pity, and the result. For troubleshooting.");

            ChanceMultiplier = Config.Bind("2 - Base boost", "ChanceMultiplier", 1f,
                new ConfigDescription("Multiplies the normal trophy chance before pity is added. 1 = vanilla.",
                    new AcceptableValueRange<float>(1f, 100f)));
            OnlyUncollected = Config.Bind("2 - Base boost", "OnlyUncollected", false,
                "Only boost (and count pity for) trophies this character has never picked up.");
            StarChanceMultiplier = Config.Bind("2 - Base boost", "StarChanceMultiplier", 2f,
                new ConfigDescription("Each star multiplies the trophy chance by this. 2 = vanilla " +
                    "(1 star = 2x, 2 stars = 4x). Higher makes starred creatures drop trophies more often; " +
                    "the chance can't go below vanilla.",
                    new AcceptableValueRange<float>(2f, 10f)));

            PityEnabled = Config.Bind("3 - Pity", "Enabled", true,
                "Increase the trophy chance with every kill that didn't drop one.");
            PityModeSetting = Config.Bind("3 - Pity", "Mode", PityMode.Compound,
                "Compound: every dry kill multiplies the current chance by (1 + GrowthPerKill). " +
                "10% trophy -> 11% -> 12.1% -> 13.31% ...  Rare trophies grow slower in absolute terms.\n" +
                "Flat: every dry kill adds FlatBonusPerKill percentage points (0.05 = +5% each kill).");
            PityGrowthPerKill = Config.Bind("3 - Pity", "GrowthPerKill", 0.10f,
                new ConfigDescription("Compound mode: growth per dry kill. 0.10 = current chance grows by 10% each kill.",
                    new AcceptableValueRange<float>(0f, 2f)));
            RareThreshold = Config.Bind("3 - Pity", "RareThreshold", 0.05f,
                new ConfigDescription("Compound mode: trophies with a vanilla chance at or below this (0.05 = 5%) count as rare.",
                    new AcceptableValueRange<float>(0f, 1f)));
            RareGrowthPerKill = Config.Bind("3 - Pity", "RareGrowthPerKill", 0.20f,
                new ConfigDescription("Compound mode: growth per dry kill for rare trophies. 0.20 = +20% of current chance each kill.",
                    new AcceptableValueRange<float>(0f, 2f)));
            PityBonusPerKill = Config.Bind("3 - Pity", "FlatBonusPerKill", 0.05f,
                new ConfigDescription("Flat mode: chance added per dry kill. 0.05 = +5 percentage points.",
                    new AcceptableValueRange<float>(0f, 1f)));
            GuaranteeAfterKills = Config.Bind("3 - Pity", "GuaranteeAfterKills", 0,
                new ConfigDescription("Hard pity: guaranteed trophy after this many dry kills. 0 = off.",
                    new AcceptableValueRange<int>(0, 1000)));
            OnlyCountMyKills = Config.Bind("3 - Pity", "OnlyCountMyKills", true,
                "Only your kills add pity: your last hit, a burn/poison tick after your hit, or a kill by your " +
                "summons and tamed creatures following you. Kills by other players always count for them. " +
                "Turn off to also count kills by creatures and traps on creatures your game owns.");

            ShowChancesInList = Config.Bind("4 - Trophy list", "ShowChances", true,
                "Show the current drop chance and dry-kill count on each trophy in the Trophies tab.");
            ShowMissingTrophies = Config.Bind("4 - Trophy list", "ShowMissing", true,
                "Add trophies you haven't collected yet to the list, greyed out.");
            SpoilerFree = Config.Bind("4 - Trophy list", "SpoilerFree", true,
                "Missing trophies of creatures you've never killed show a '?' icon and '??????' as name. " +
                "After your first kill they show greyed out with their name. Off = all missing trophies show greyed out.");
            HiddenTrophies = Config.Bind("4 - Trophy list", "HiddenTrophies", "TrophyDeerWhite",
                "Trophy prefab names (comma separated) never added to the list as missing. " +
                "TrophyDeerWhite is unfinished game content that has no name in the game's text files.");

            TrackerEnabled = Config.Bind("5 - Wishbone tracker", "Enabled", true,
                "Show a small wishbone on each trophy in the Trophies tab. Click it to track that trophy: while the " +
                "wishbone is equipped, it also pings (in red) for living creatures that drop a tracked trophy.");
            TrackerRange = Config.Bind("5 - Wishbone tracker", "Range", 0f,
                new ConfigDescription("How close (in meters) a creature with a tracked trophy must be for the wishbone to find it. " +
                    "0 = any creature the game has loaded around you (follows the game's simulation distance setting).",
                    new AcceptableValueRange<float>(0f, 500f)));

            PityStore.Load();
            TrackedStore.Load();

            _harmony = new Harmony(ModGuid);
            Patcher.SafePatch(_harmony, typeof(KillCredit));
            Patcher.SafePatch(_harmony, typeof(GenerateDropListPatch));
            Patcher.SafePatch(_harmony, typeof(TrophyListPatch));
            Patcher.SafePatch(_harmony, typeof(TrophyNet));
            Patcher.SafePatch(_harmony, typeof(SaveOnLogout));
            Patcher.SafePatch(_harmony, typeof(WishboneIconPatch));
            Commands.Register();

            Logger.LogInfo($"{ModName} {ModVersion} loaded (client-side).");
        }

        private void Update()
        {
            PityLabel.Tick();
            TrophyFinder.Tick();
            PityStore.SaveIfDue();
        }

        private void OnDestroy()
        {
            PityStore.Save();
            _harmony?.UnpatchSelf();
        }
    }

    // ---------------------------------------------------------------- patch

    // CharacterDrop.GenerateDropList rolls each drop's chance on the client that
    // owns the creature when it dies. We leave the game's own roll alone: for
    // chances of 30% or less it uses an anti-bad-luck counter that restarts
    // whenever the chance changes, so raising m_chance would break it. Instead,
    // when the game's roll misses the trophy, we roll the pity bonus on top so
    // the combined chance equals the boosted chance, and add the trophy ourselves.
    [HarmonyPatch]
    internal static class GenerateDropListPatch
    {
        internal class Entry
        {
            public CharacterDrop.Drop Drop;
            public float BaseChance; // drop.m_chance, 0-star
            public float Vanilla;    // what the game rolls (with its 2x-per-star multiplier)
            public float Target;     // boosted chance we want in total
        }

        internal class State
        {
            public string CharKey;
            public string CreatureKey; // null = this kill doesn't count for pity
            public string CreatureName;
            public int LevelMultiplier;
            public readonly List<Entry> Trophies = new List<Entry>();

            // Another player's kill: we only send them what the game dropped.
            public long HandOffTo;
            public string HandOffCreature;
            public int HandOffLevel;
        }

        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(CharacterDrop), "GenerateDropList");

        private static void Prefix(CharacterDrop __instance, out State __state)
        {
            __state = null;
            try
            {
                if (!TrophyAssisterPlugin.Enabled.Value || __instance == null || __instance.m_drops == null) return;

                if (Player.m_localPlayer == null) return;

                var character = __instance.GetComponent<Character>();
                string creatureKey = GameUtil.PrefabName(__instance.gameObject);
                int level = character != null ? character.GetLevel() : 1;
                long killer = KillCredit.Killer(character);

                if (killer != 0 && killer != ZDOMan.GetSessionID())
                {
                    if (TrophyAssisterPlugin.LogKills.Value)
                        TrophyAssisterPlugin.Log.LogInfo(
                            $"Kill: {creatureKey} ({level - 1} star), last hit by {KillCredit.LastAttackerName(character)}, " +
                            "another player's kill: sent to them");
                    __state = new State { HandOffTo = killer, HandOffCreature = creatureKey, HandOffLevel = level };
                    return;
                }

                bool countPity = TrophyAssisterPlugin.PityEnabled.Value &&
                                 (!TrophyAssisterPlugin.OnlyCountMyKills.Value || killer != 0);
                if (TrophyAssisterPlugin.LogKills.Value)
                    TrophyAssisterPlugin.Log.LogInfo(
                        $"Kill: {creatureKey} ({level - 1} star), last hit by {KillCredit.LastAttackerName(character)}, " +
                        $"counts for pity: {countPity}");

                string name = character != null ? GameUtil.Localize(character.m_name) : creatureKey;
                __state = Build(__instance.m_drops, creatureKey, name, level, countPity);
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogWarning($"Prefix error (drop left vanilla): {e.Message}");
            }
        }

        private static void Postfix(CharacterDrop __instance, State __state, List<KeyValuePair<GameObject, int>> __result)
        {
            if (__state == null || __result == null) return;

            try
            {
                var dropped = new HashSet<string>(__result.Where(kv => kv.Key != null).Select(kv => kv.Key.name));
                if (__state.HandOffTo != 0)
                {
                    TrophyNet.SendKill(__state.HandOffTo, __state.HandOffCreature, __state.HandOffLevel,
                        TrophyNet.DropCenter(__instance), dropped);
                    return;
                }

                var extra = Resolve(__state, dropped);
                if (extra.Key != null) __result.Add(extra);
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogWarning($"Postfix error: {e.Message}");
            }
        }

        // The trophies in this drop list we boost or count pity for, with their chances
        // for the local character. Null when there's nothing to do.
        internal static State Build(List<CharacterDrop.Drop> drops, string creatureKey, string creatureName,
            int level, bool countPity)
        {
            var player = Player.m_localPlayer;
            string charKey = GameUtil.CharKey(player);
            if (player == null || charKey == null || drops == null) return null;

            int levelMult = Mathf.Max(1, (int)Mathf.Pow(2f, level - 1)); // same as the game
            float starMult = Mathf.Max(levelMult, Mathf.Pow(TrophyAssisterPlugin.StarChanceMultiplier.Value, level - 1));
            HashSet<string> owned = null;
            State state = null;

            foreach (var drop in drops)
            {
                if (drop == null || !GameUtil.IsTrophy(drop.m_prefab, out _)) continue;
                if (TrophyAssisterPlugin.OnlyUncollected.Value)
                {
                    owned = owned ?? new HashSet<string>(player.GetTrophies());
                    if (owned.Contains(drop.m_prefab.name)) continue;
                }

                int dry = countPity ? PityStore.Get(charKey, creatureKey) : 0;
                float vanilla = Mathf.Clamp01(drop.m_chance * (drop.m_levelMultiplier ? levelMult : 1));
                float target = Mathf.Clamp01(Chance.Next(drop.m_chance, dry) * (drop.m_levelMultiplier ? starMult : 1));
                if (target <= vanilla && !countPity) continue;

                if (state == null)
                    state = new State
                    {
                        CharKey = charKey,
                        CreatureKey = countPity ? creatureKey : null,
                        CreatureName = creatureName,
                        LevelMultiplier = levelMult
                    };

                state.Trophies.Add(new Entry { Drop = drop, BaseChance = drop.m_chance, Vanilla = vanilla, Target = target });
            }
            return state;
        }

        // After the game's roll (`dropped` = prefab names it dropped): rolls the pity bonus,
        // updates the counter and shows the messages. Returns the trophy to add, if any.
        internal static KeyValuePair<GameObject, int> Resolve(State state, ICollection<string> dropped)
        {
            var extra = default(KeyValuePair<GameObject, int>);
            bool gotTrophy = state.Trophies.Any(t => dropped.Contains(t.Drop.m_prefab.name));
            bool byPity = false;

            // The game's roll missed: roll the pity bonus. P(game) + P(miss) * bonus = target.
            if (!gotTrophy)
            {
                foreach (var t in state.Trophies)
                {
                    if (t.Target <= t.Vanilla || t.Vanilla >= 1f) continue;
                    float bonus = (t.Target - t.Vanilla) / (1f - t.Vanilla);
                    if (UnityEngine.Random.value >= bonus) continue;

                    extra = new KeyValuePair<GameObject, int>(t.Drop.m_prefab, Amount(t.Drop, state.LevelMultiplier));
                    gotTrophy = byPity = true;
                    break;
                }
            }

            if (gotTrophy)
            {
                string how = byPity ? "pity roll" : null;
                if (state.CreatureKey != null)
                {
                    int tries = PityStore.Get(state.CharKey, state.CreatureKey);
                    PityStore.Reset(state.CharKey, state.CreatureKey);
                    if (tries > 0) how = (how != null ? how + ", " : "") + $"after {tries} dry kill{(tries == 1 ? "" : "s")}";
                }
                if (TrophyAssisterPlugin.NotifyOnDrop.Value)
                    Hud.Center($"Trophy dropped: {state.CreatureName}!" + (how != null ? $"  ({how})" : ""));
                if (TrophyAssisterPlugin.LogKills.Value)
                    TrophyAssisterPlugin.Log.LogInfo($"  -> trophy dropped ({(byPity ? "pity roll" : "game roll")})");
            }
            else if (state.CreatureKey != null)
            {
                int dry = PityStore.Increment(state.CharKey, state.CreatureKey);
                float next = Chance.Next(state.Trophies[0].BaseChance, dry);
                if (TrophyAssisterPlugin.LogKills.Value)
                    TrophyAssisterPlugin.Log.LogInfo($"  -> no trophy, dry kills = {dry}, next chance {next:P0} (0 star)");
                if (TrophyAssisterPlugin.ShowPityProgress.Value)
                    PityLabel.Show($"{state.CreatureName} trophy: {dry} dry kill{(dry == 1 ? "" : "s")}, next chance {next:P0}");
            }
            return extra;
        }

        // Same amount the game would have dropped for this entry.
        private static int Amount(CharacterDrop.Drop drop, int levelMult)
        {
            int n = drop.m_dontScale
                ? UnityEngine.Random.Range(drop.m_amountMin, drop.m_amountMax)
                : Game.instance.ScaleDrops(drop.m_prefab, drop.m_amountMin, drop.m_amountMax);
            if (drop.m_levelMultiplier && !drop.m_levelMultiplierNotAmount) n *= levelMult;
            if (drop.m_onePerPlayer && ZNet.instance != null) n = ZNet.instance.GetNrOfPlayers();
            return Mathf.Clamp(n, 1, 100);
        }
    }

    // Who gets credit for a kill. Burning/poison ticks carry no attacker, so the
    // killing blow alone can't tell us. Instead we remember, per creature, which
    // player (or player's summon) landed the last damaging hit that DID have an attacker.
    // Character.ApplyDamage runs on the creature's owner, the same game that rolls
    // its drops.
    [HarmonyPatch]
    internal static class KillCredit
    {
        private class Credit { public long Peer; public string Who; }
        private static readonly ConditionalWeakTable<Character, Credit> LastAttacker =
            new ConditionalWeakTable<Character, Credit>();

        private static bool Prepare() => TargetMethod() != null;

        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(Character), "ApplyDamage");

        private static void Prefix(Character __instance, HitData hit)
        {
            try
            {
                if (__instance == null || hit == null || !hit.HaveAttacker() || hit.GetTotalDamage() <= 0.1f) return;
                var attacker = hit.GetAttacker();
                if (attacker == null) return; // attacker gone (e.g. unsummoned) - keep the previous credit
                var credit = LastAttacker.GetOrCreateValue(__instance);
                credit.Peer = GameUtil.PlayerPeer(attacker);
                if (TrophyAssisterPlugin.LogKills.Value) credit.Who = attacker.m_name;
            }
            catch { /* never interfere with damage */ }
        }

        // The game session of the player who gets the kill (see GameUtil.PlayerPeer). 0 = no player.
        public static long Killer(Character victim)
        {
            if (victim != null && LastAttacker.TryGetValue(victim, out var credit)) return credit.Peer;
            return GameUtil.LastHitPeer(victim); // e.g. creature changed owner mid-fight
        }

        // For the kill log: who got the credit-deciding hit.
        public static string LastAttackerName(Character victim) =>
            victim != null && LastAttacker.TryGetValue(victim, out var credit) && credit.Who != null
                ? GameUtil.Localize(credit.Who)
                : "unknown (no tracked hit)";
    }

    // Kills of creatures another player's game owns. The owner's game rolls the drops,
    // so it sends the killer the creature and what the game dropped; the killer rolls
    // their own pity and spawns the trophy. Both need the mod: without it on the owner's
    // side the kill is vanilla, without it on the killer's side the message is ignored.
    [HarmonyPatch(typeof(Game), "Start")]
    internal static class TrophyNet
    {
        private const string RpcKill = "TrophyAssister_Kill";

        private static void Postfix()
        {
            try { ZRoutedRpc.instance?.Register<ZPackage>(RpcKill, OnKill); }
            catch (Exception e) { TrophyAssisterPlugin.Log.LogWarning($"Couldn't register kill message: {e.Message}"); }
        }

        // Same spot the game drops loot.
        public static Vector3 DropCenter(CharacterDrop drop)
        {
            var character = drop.GetComponent<Character>();
            Vector3 center = character != null ? character.GetCenterPoint() : drop.transform.position;
            return center + drop.transform.TransformVector(drop.m_spawnOffset);
        }

        public static void SendKill(long peer, string creatureKey, int level, Vector3 center, IEnumerable<string> dropped)
        {
            if (ZRoutedRpc.instance == null) return;
            var pkg = new ZPackage();
            pkg.Write(creatureKey);
            pkg.Write(level);
            pkg.Write(center);
            pkg.Write(string.Join(",", dropped));
            ZRoutedRpc.instance.InvokeRoutedRPC(peer, RpcKill, pkg);
        }

        private static void OnKill(long sender, ZPackage pkg)
        {
            try
            {
                if (!TrophyAssisterPlugin.Enabled.Value || ZNetScene.instance == null) return;
                string creatureKey = pkg.ReadString();
                int level = pkg.ReadInt();
                Vector3 center = pkg.ReadVector3();
                var dropped = new HashSet<string>(pkg.ReadString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

                var prefab = ZNetScene.instance.GetPrefab(creatureKey);
                var drops = prefab != null ? prefab.GetComponent<CharacterDrop>() : null;
                if (drops == null) return;
                var character = prefab.GetComponent<Character>();

                bool countPity = TrophyAssisterPlugin.PityEnabled.Value;
                if (TrophyAssisterPlugin.LogKills.Value)
                    TrophyAssisterPlugin.Log.LogInfo(
                        $"Kill: {creatureKey} ({level - 1} star), your kill on another player's game, counts for pity: {countPity}");

                string name = character != null ? GameUtil.Localize(character.m_name) : creatureKey;
                var state = GenerateDropListPatch.Build(drops.m_drops, creatureKey, name, level, countPity);
                if (state == null) return;

                var extra = GenerateDropListPatch.Resolve(state, dropped);
                if (extra.Key != null)
                    CharacterDrop.DropItems(new List<KeyValuePair<GameObject, int>> { extra }, center, 0.5f, false);
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogWarning($"Kill message error: {e.Message}");
            }
        }
    }

    public enum PityMode { Compound, Flat }

    // One formula used by both the drop roll and the trophy list display.
    internal static class Chance
    {
        public static float Next(float baseChance, int dry)
        {
            float chance = baseChance * TrophyAssisterPlugin.ChanceMultiplier.Value;
            if (TrophyAssisterPlugin.PityEnabled.Value && dry > 0)
            {
                if (TrophyAssisterPlugin.PityModeSetting.Value == PityMode.Compound)
                {
                    bool rare = baseChance <= TrophyAssisterPlugin.RareThreshold.Value + 0.0001f;
                    float growth = rare
                        ? TrophyAssisterPlugin.RareGrowthPerKill.Value
                        : TrophyAssisterPlugin.PityGrowthPerKill.Value;
                    chance *= Mathf.Pow(1f + growth, dry);
                }
                else
                    chance += TrophyAssisterPlugin.PityBonusPerKill.Value * dry;

                int hard = TrophyAssisterPlugin.GuaranteeAfterKills.Value;
                if (hard > 0 && dry >= hard) chance = 1f;
            }
            return Mathf.Clamp01(chance);
        }
    }

    // ---------------------------------------------------------------- helpers

    // Thin layer over game internals, done defensively so a renamed field in a
    // future patch disables one feature instead of crashing the mod.
    internal static class GameUtil
    {
        private static readonly FieldInfo LastHitField = AccessTools.Field(typeof(Character), "m_lastHit");

        public static bool IsTrophy(GameObject prefab, out ItemDrop item)
        {
            item = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            return item != null && item.m_itemData?.m_shared != null &&
                   item.m_itemData.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy;
        }

        public static long LastHitPeer(Character character)
        {
            long me = ZDOMan.GetSessionID();
            if (character == null || LastHitField == null) return me;
            try
            {
                if (LastHitField.GetValue(character) is HitData hit)
                    return PlayerPeer(hit.GetAttacker());
                return 0;
            }
            catch { return me; } // can't tell -> don't block pity
        }

        // The game session (peer ID) of the player an attacker belongs to: the player
        // themselves, or the owner of a summon - tamed creatures (Dead Raiser skeletons,
        // summoned trolls, tamed wolves) and player-spawned creatures that are friendly
        // without being tamed (Staff of the Wild roots). 0 = not a player's.
        public static long PlayerPeer(Character attacker)
        {
            if (attacker == null) return 0;
            if (attacker is Player) return Owner(attacker);
            if (!attacker.IsTamed() && attacker.GetFaction() != Character.Faction.PlayerSpawned) return 0;

            var ai = attacker.GetComponent<MonsterAI>();
            var target = ai != null ? ai.GetFollowTarget() : null;
            var followed = target != null ? target.GetComponent<Player>() : null;
            if (followed != null) return Owner(followed);

            var nview = attacker.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid()) return 0;
            string follow = nview.GetZDO().GetString(ZDOVars.s_follow);
            if (follow.Length > 0)
            {
                var player = Player.GetAllPlayers().FirstOrDefault(p => p != null && p.GetPlayerName() == follow);
                return player != null ? Owner(player) : 0;
            }

            // Not following anyone (roots, summons told to stay): it belongs to the game
            // that controls it - new objects belong to the game that spawned them.
            return nview.GetZDO().GetOwner();
        }

        // A player's own game always owns their character.
        private static long Owner(Character c)
        {
            var nview = c.GetComponent<ZNetView>();
            return nview != null && nview.IsValid() ? nview.GetZDO().GetOwner() : 0;
        }

        public static string PrefabName(GameObject go)
        {
            string n = go.name;
            int i = n.IndexOf("(Clone)", StringComparison.Ordinal);
            return (i >= 0 ? n.Substring(0, i) : n).Trim();
        }

        // Pity counters are keyed by the character's internal ID: it stays the same if
        // you rename, and differs between characters with the same name.
        // Null when no character is loaded.
        public static string CharKey(Player player)
        {
            long id = Game.instance != null ? Game.instance.GetPlayerProfile()?.GetPlayerID() ?? 0 : 0;
            if (id == 0 && player != null) id = player.GetPlayerID();
            return id != 0 ? id.ToString() : null;
        }

        public static string Localize(string token)
        {
            if (string.IsNullOrEmpty(token) || Localization.instance == null) return token;
            return Localization.instance.Localize(token);
        }
    }

    internal static class Hud
    {
        public static void Center(string text) => Show(MessageHud.MessageType.Center, text);
        public static void TopLeft(string text) => Show(MessageHud.MessageType.TopLeft, text);

        private static void Show(MessageHud.MessageType type, string text)
        {
            try { MessageHud.instance?.ShowMessage(type, text); }
            catch { /* HUD not ready */ }
        }
    }

    // Saves pity counters to BepInEx/config/TrophyAssister_pity.txt
    // Format per line: CharacterID|CreaturePrefab=DryKills
    // Kills only mark the counters as changed; they are written SaveDelay seconds later (so a fight is one write),
    // on logout and when the game closes.
    internal static class PityStore
    {
        private const float SaveDelay = 30f;

        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();
        private static string FilePath => Path.Combine(Paths.ConfigPath, "TrophyAssister_pity.txt");
        private static bool _dirty;
        private static float _saveAt;

        private static void MarkDirty()
        {
            if (_dirty) return;
            _dirty = true;
            _saveAt = Time.realtimeSinceStartup + SaveDelay;
        }

        // Called every frame from the plugin.
        public static void SaveIfDue()
        {
            if (_dirty && Time.realtimeSinceStartup >= _saveAt) Save();
        }

        public static void SaveIfDirty()
        {
            if (_dirty) Save();
        }

        private static string Key(string ch, string creature) => $"{ch}|{creature}";

        public static int Get(string ch, string creature) =>
            Counts.TryGetValue(Key(ch, creature), out var v) ? v : 0;

        public static int Increment(string ch, string creature, int amount = 1)
        {
            int v = Get(ch, creature) + Math.Max(1, amount);
            Counts[Key(ch, creature)] = v;
            MarkDirty();
            return v;
        }

        public static void Reset(string ch, string creature)
        {
            if (Counts.Remove(Key(ch, creature))) MarkDirty();
        }

        // creature prefab -> dry kills, for one character
        public static List<KeyValuePair<string, int>> ForCharacter(string ch)
        {
            string prefix = ch + "|";
            return Counts.Where(k => k.Key.StartsWith(prefix, StringComparison.Ordinal))
                .Select(k => new KeyValuePair<string, int>(k.Key.Substring(prefix.Length), k.Value))
                .OrderBy(k => k.Key)
                .ToList();
        }

        public static int ResetCharacter(string ch)
        {
            var keys = Counts.Keys.Where(k => k.StartsWith(ch + "|", StringComparison.Ordinal)).ToList();
            foreach (var k in keys) Counts.Remove(k);
            if (keys.Count > 0) Save();
            return keys.Count;
        }

        // Copy of the current file next to it, made before a bulk reset.
        public static string Backup()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string bak = Path.ChangeExtension(FilePath, ".bak");
                File.Copy(FilePath, bak, overwrite: true);
                return bak;
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log?.LogWarning($"Couldn't back up pity file: {e.Message}");
                return null;
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int eq = line.LastIndexOf('=');
                    if (eq <= 0) continue;
                    if (int.TryParse(line.Substring(eq + 1), out int n) && n > 0)
                        Counts[line.Substring(0, eq)] = n;
                }
            }
            catch (Exception e) { TrophyAssisterPlugin.Log?.LogWarning($"Couldn't load pity file: {e.Message}"); }
        }

        public static void Save()
        {
            _dirty = false;
            try
            {
                File.WriteAllLines(FilePath, Counts.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}"));
            }
            catch (Exception e) { TrophyAssisterPlugin.Log?.LogWarning($"Couldn't save pity file: {e.Message}"); }
        }
    }

    // Writes changed pity counters before the game leaves the world.
    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    internal static class SaveOnLogout
    {
        private static void Prefix() => PityStore.SaveIfDirty();
    }

    internal static class Patcher
    {
        public static void SafePatch(Harmony harmony, Type patchClass)
        {
            try
            {
                harmony.CreateClassProcessor(patchClass).Patch();
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogError(
                    $"Couldn't patch {patchClass.Name} - the game code probably changed in an update. " +
                    $"The mod is inactive but the game will run normally. Details: {e.Message}");
            }
        }
    }
}
