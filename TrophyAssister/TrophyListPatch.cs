// Trophies tab (inventory -> Trophies) additions:
//  - every trophy shows "Next drop: X%" plus your dry-kill count
//  - trophies you haven't collected yet are added in their own slot, greyed out
//  - SpoilerFree: trophies of creatures you've never killed show a '?' and '??????'
//  - a small wishbone button on each trophy to track it with the wishbone (see TrophyTracker)
//
// Vanilla builds the list in InventoryGui.UpdateTrophyList each time the tab opens.
// We run after it, reuse its own element prefab, and place missing trophies the
// way vanilla does: each trophy has a fixed grid slot (m_trophyPos).
// The private element list is looked up by name, so if a game update renames it
// this feature disables itself (logged) instead of breaking the inventory.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyAssister
{
    [HarmonyPatch]
    internal static class TrophyListPatch
    {
        private static readonly FieldInfo ListField = AccessTools.Field(typeof(InventoryGui), "m_trophyList");

        private static readonly Color GreyIcon = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        private static readonly Color GreyText = new Color(0.55f, 0.55f, 0.55f, 1f);

        private static bool Prepare()
        {
            bool ok = TargetMethod() != null && ListField != null;
            if (!ok)
                TrophyAssisterPlugin.Log.LogWarning(
                    "Trophy list layout changed in this game version - list additions disabled, drops still boosted.");
            return ok;
        }

        private static MethodBase TargetMethod() =>
            AccessTools.Method(typeof(InventoryGui), "UpdateTrophyList");

        private static void Postfix(InventoryGui __instance)
        {
            bool track = TrophyAssisterPlugin.TrackerEnabled.Value;
            if (!TrophyAssisterPlugin.ShowChancesInList.Value && !TrophyAssisterPlugin.ShowMissingTrophies.Value && !track) return;

            try
            {
                var player = Player.m_localPlayer;
                if (player == null || ObjectDB.instance == null) return;

                var elements = ListField.GetValue(__instance) as List<GameObject>;
                var prefab = __instance.m_trophieElementPrefab;
                var root = __instance.m_trophieListRoot;
                string charKey = GameUtil.CharKey(player);
                if (elements == null || prefab == null || root == null || charKey == null) return;

                TrophyIndex.Build();
                var collected = new HashSet<string>(player.GetTrophies());

                // 1. Annotate the vanilla (collected) entries.
                if (TrophyAssisterPlugin.ShowChancesInList.Value || track)
                {
                    foreach (var el in elements)
                    {
                        if (el == null) continue;
                        string trophy = TrophyIndex.TrophyForIcon(UiUtil.IconSprite(el.transform));
                        if (trophy == null) continue;
                        if (track) TrackButton.Add(el.transform, trophy, charKey);
                        if (!TrophyAssisterPlugin.ShowChancesInList.Value) continue;
                        string line = ChanceLine(trophy, charKey, collected: true);
                        if (line != null) UiUtil.PrependText(el.transform, "description", line);
                    }
                }

                // 2. Add missing trophies, greyed out (or hidden if never killed).
                if (!TrophyAssisterPlugin.ShowMissingTrophies.Value) return;

                var hidden = new HashSet<string>(
                    TrophyAssisterPlugin.HiddenTrophies.Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0),
                    StringComparer.OrdinalIgnoreCase);
                var missing = TrophyIndex.AllTrophies
                    .Where(t => !collected.Contains(t.PrefabName) && !hidden.Contains(t.PrefabName))
                    .OrderBy(t => t.SortKey)
                    .ToList();
                if (missing.Count == 0) return;

                var slots = new SlotGrid(elements, __instance.m_trophieListSpace);
                var killed = Discovery.KilledCreatures();
                float lowestY = slots.LowestY;

                foreach (var t in missing)
                {
                    var go = UnityEngine.Object.Instantiate(prefab, root);
                    go.SetActive(true);
                    var rt = (RectTransform)go.transform;
                    rt.anchoredPosition = slots.Take(t.Slot);
                    lowestY = Mathf.Min(lowestY, rt.anchoredPosition.y - slots.Space);

                    bool discovered = !TrophyAssisterPlugin.SpoilerFree.Value ||
                                      Discovery.HasKilled(t.PrefabName, charKey, killed);

                    var icon = UiUtil.IconImage(rt);
                    if (discovered)
                    {
                        if (icon != null)
                        {
                            icon.sprite = t.Icon;
                            icon.color = GreyIcon;
                        }
                        UiUtil.SetText(rt, "name", t.DisplayName, GreyText);

                        string desc = "<i>Not collected yet</i>";
                        if (TrophyAssisterPlugin.ShowChancesInList.Value)
                        {
                            string line = ChanceLine(t.PrefabName, charKey, collected: false);
                            if (line != null) desc = line + "\n" + desc;
                        }
                        UiUtil.SetText(rt, "description", desc, GreyText);
                        if (track) TrackButton.Add(rt, t.PrefabName, charKey);
                    }
                    else
                    {
                        UiUtil.ShowQuestionMark(rt, icon, GreyText);
                        UiUtil.SetText(rt, "name", "??????", GreyText);
                        UiUtil.SetText(rt, "description", "<i>Not discovered yet</i>", GreyText);
                    }

                    elements.Add(go); // vanilla destroys everything in this list on the next refresh
                }

                if (root.rect.height < -lowestY)
                    root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -lowestY);
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogWarning($"Trophy list addition failed: {e.Message}");
            }
        }

        // "Next drop: 15% - Greydwarf - 2 dry kills"  (best creature if several drop it)
        private static string ChanceLine(string trophy, string charKey, bool collected)
        {
            if (!TrophyIndex.Sources.TryGetValue(trophy, out var sources) || sources.Count == 0)
                return "<color=#9a9a9a>Drop chance: unknown</color>";

            bool boosted = TrophyAssisterPlugin.Enabled.Value &&
                           !(TrophyAssisterPlugin.OnlyUncollected.Value && collected);

            TrophySource best = null;
            float bestChance = -1f;
            int bestDry = 0;
            foreach (var s in sources)
            {
                int dry = boosted ? PityStore.Get(charKey, s.CreaturePrefab) : 0;
                float c = boosted ? Chance.Next(s.BaseChance, dry) : s.BaseChance;
                if (c > bestChance) { bestChance = c; best = s; bestDry = dry; }
            }

            string color = bestChance >= 0.5f ? "#7CFC7C" : bestChance >= 0.2f ? "#FFD24A" : "#FF9A5A";
            string line = $"<color={color}>Next drop: {bestChance:P0}</color>";
            if (sources.Count > 1) line += $" - {best.CreatureName}";
            if (bestDry > 0) line += $" - {bestDry} dry kill{(bestDry == 1 ? "" : "s")}";
            if (!boosted && TrophyAssisterPlugin.Enabled.Value) line += " - (no boost)";
            return line;
        }
    }

    // ---------------------------------------------------------------- data

    internal class TrophySource
    {
        public string CreaturePrefab;
        public string CreatureToken; // Character.m_name, e.g. "$enemy_greydwarf" (key of the game's kill stats)
        public string CreatureName;
        public float BaseChance;
    }

    internal class TrophyInfo
    {
        public string PrefabName;
        public string DisplayName;
        public Sprite Icon;
        public string SortKey;
        public Vector2Int Slot;
    }

    // Scans the game's creature prefabs once to learn which creature drops which
    // trophy and at what base chance.
    internal static class TrophyIndex
    {
        public static readonly Dictionary<string, List<TrophySource>> Sources = new Dictionary<string, List<TrophySource>>();
        public static readonly List<TrophyInfo> AllTrophies = new List<TrophyInfo>();
        private static readonly Dictionary<Sprite, string> ByIcon = new Dictionary<Sprite, string>();
        private static bool _built;

        public static string TrophyForIcon(Sprite s) =>
            s != null && ByIcon.TryGetValue(s, out var n) ? n : null;

        public static void Build()
        {
            if (_built || ObjectDB.instance == null || ZNetScene.instance == null) return;
            Sources.Clear();
            AllTrophies.Clear();
            ByIcon.Clear();

            // Who drops what
            foreach (var go in ZNetScene.instance.m_prefabs)
            {
                var cd = go != null ? go.GetComponent<CharacterDrop>() : null;
                if (cd?.m_drops == null) continue;
                var ch = go.GetComponent<Character>();
                foreach (var d in cd.m_drops)
                {
                    if (d == null || !GameUtil.IsTrophy(d.m_prefab, out _)) continue;
                    if (!Sources.TryGetValue(d.m_prefab.name, out var list))
                        Sources[d.m_prefab.name] = list = new List<TrophySource>();
                    list.Add(new TrophySource
                    {
                        CreaturePrefab = go.name,
                        CreatureToken = ch != null ? ch.m_name : null,
                        CreatureName = ch != null ? GameUtil.Localize(ch.m_name) : go.name,
                        BaseChance = d.m_chance
                    });
                }
            }

            // Every trophy item in the game
            foreach (var go in ObjectDB.instance.m_items)
            {
                if (!GameUtil.IsTrophy(go, out var item)) continue;
                var icon = item.m_itemData.GetIcon();
                string name = GameUtil.Localize(item.m_itemData.m_shared.m_name);
                if (name.EndsWith(" trophy", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 7);

                // Unobtainable/test trophies (no creature drops them) are skipped
                if (!Sources.ContainsKey(go.name)) continue;

                AllTrophies.Add(new TrophyInfo
                {
                    PrefabName = go.name,
                    DisplayName = name,
                    Icon = icon,
                    // Rarer trophies last; then by name (only matters for trophies without a free slot)
                    SortKey = $"{1f - Sources[go.name].Max(s => s.BaseChance):0.000}_{name}",
                    Slot = item.m_itemData.m_shared.m_trophyPos
                });
                if (icon != null && !ByIcon.ContainsKey(icon)) ByIcon[icon] = go.name;
            }

            _built = AllTrophies.Count > 0;
        }
    }

    // "Have you killed this at least once?" Uses the game's own per-character kill
    // stats (they include kills from before the mod was installed), plus our dry-kill
    // counters for kills the game doesn't register to you (e.g. a summon-only kill).
    internal static class Discovery
    {
        public static Dictionary<string, float> KilledCreatures()
        {
            try
            {
                return Game.instance?.GetPlayerProfile()?.m_playerStats[0].m_enemyStats[0];
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogWarning($"Couldn't read kill stats: {e.Message}");
                return null;
            }
        }

        public static bool HasKilled(string trophy, string charKey, Dictionary<string, float> killed)
        {
            if (!TrophyIndex.Sources.TryGetValue(trophy, out var sources)) return false;
            foreach (var s in sources)
            {
                if (killed != null && s.CreatureToken != null &&
                    killed.TryGetValue(s.CreatureToken, out var n) && n > 0f) return true;
                if (PityStore.Get(charKey, s.CreaturePrefab) > 0) return true;
            }
            return false;
        }
    }

    // Vanilla puts trophy (x, y) at (x * space, -y * space). We hand out those same
    // slots to missing trophies; a slot that's taken (shouldn't happen) falls back
    // to the next free spot below the grid.
    internal class SlotGrid
    {
        public readonly float Space;
        public float LowestY { get; private set; }
        private readonly HashSet<Vector2Int> _used = new HashSet<Vector2Int>();
        private int _columns = 1;
        private int _nextOverflow = -1;

        public SlotGrid(List<GameObject> elements, float space)
        {
            Space = space > 0 ? space : 160f;
            LowestY = 0f;
            foreach (var e in elements)
            {
                if (e == null) continue;
                var p = ((RectTransform)e.transform).anchoredPosition;
                MarkUsed(new Vector2Int(Mathf.RoundToInt(p.x / Space), Mathf.RoundToInt(-p.y / Space)));
            }
            foreach (var t in TrophyIndex.AllTrophies)
                _columns = Mathf.Max(_columns, t.Slot.x + 1);
        }

        public Vector2 Take(Vector2Int slot)
        {
            if (_used.Contains(slot))
            {
                int maxRow = TrophyIndex.AllTrophies.Count > 0 ? TrophyIndex.AllTrophies.Max(t => t.Slot.y) : 0;
                maxRow = Mathf.Max(maxRow, _used.Count > 0 ? _used.Max(u => u.y) : 0);
                if (_nextOverflow < 0) _nextOverflow = (maxRow + 1) * _columns;
                do
                {
                    slot = new Vector2Int(_nextOverflow % _columns, _nextOverflow / _columns);
                    _nextOverflow++;
                } while (_used.Contains(slot));
            }
            MarkUsed(slot);
            return new Vector2(slot.x * Space, -slot.y * Space);
        }

        private void MarkUsed(Vector2Int slot)
        {
            _used.Add(slot);
            LowestY = Mathf.Min(LowestY, -slot.y * Space - Space);
        }
    }

    internal static class UiUtil
    {
        public static Image IconImage(Transform el)
        {
            var t = el.Find("icon_bkg/icon");
            if (t != null && t.GetComponent<Image>() is Image img) return img;
            return el.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.name == "icon");
        }

        public static Sprite IconSprite(Transform el) => IconImage(el)?.sprite;

        // Hides the trophy icon and puts a big '?' in its place, made from a copy
        // of the element's own name text so it uses the game's font.
        public static void ShowQuestionMark(Transform el, Image icon, Color color)
        {
            if (icon == null) return;
            icon.enabled = false;

            var nameText = el.Find("name");
            if (nameText == null) return;
            var q = UnityEngine.Object.Instantiate(nameText.gameObject, icon.transform);
            q.name = "trophyassister_unknown";
            var rt = (RectTransform)q.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;

            var tmp = q.GetComponent<TMP_Text>();
            if (tmp == null) return;
            float h = ((RectTransform)icon.transform).rect.height;
            tmp.enableAutoSizing = false;
            tmp.fontSize = h > 0 ? h * 0.8f : 48f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.text = "?";
        }

        private static TMP_Text Text(Transform el, string child)
        {
            var t = el.Find(child);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }

        public static void SetText(Transform el, string child, string value, Color? color = null)
        {
            var text = Text(el, child);
            if (text == null) return;
            text.text = value;
            if (color.HasValue) text.color = color.Value;
        }

        public static void PrependText(Transform el, string child, string line)
        {
            var text = Text(el, child);
            if (text != null) text.text = line + "\n" + text.text;
        }
    }
}
