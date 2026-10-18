// Trophy tracker: click the little wishbone on a trophy in the Trophies tab to track it. While the wishbone is
// equipped, it then also finds living creatures that drop a tracked trophy, the same way it finds buried
// treasure: the closest one in range is picked once a second, and it pings more often the closer you get.
// These pings are red and show up on the side of you the creature is on, and the wishbone's status icon
// turns red while a tracked creature is in range.
//
// The wishbone's own search (SE_Finder) is left alone; we only borrow its ping effects and timings, so
// treasure and silver keep pinging as usual next to ours.
// Tracked trophies are saved per character in BepInEx/config/TrophyAssister_tracked.txt.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Koendertjh.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace TrophyAssister
{
    // Format per line: CharacterID|TrophyPrefab
    internal static class TrackedStore
    {
        private static readonly HashSet<string> Tracked = new HashSet<string>();
        private static string FilePath => Path.Combine(Paths.ConfigPath, "TrophyAssister_tracked.txt");

        // Bumped on every change so the finder knows to rebuild its creature list.
        public static int Version { get; private set; }

        private static string Key(string ch, string trophy) => $"{ch}|{trophy}";

        public static bool IsTracked(string ch, string trophy) => Tracked.Contains(Key(ch, trophy));

        public static bool Toggle(string ch, string trophy)
        {
            string key = Key(ch, trophy);
            bool now = !Tracked.Remove(key);
            if (now) Tracked.Add(key);
            Version++;
            Save();
            return now;
        }

        public static List<string> ForCharacter(string ch)
        {
            string prefix = ch + "|";
            return Tracked.Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .Select(k => k.Substring(prefix.Length))
                .ToList();
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var line in File.ReadAllLines(FilePath))
                    if (line.IndexOf('|') > 0) Tracked.Add(line.Trim());
                Version++;
            }
            catch (Exception e) { TrophyAssisterPlugin.Log?.LogWarning($"Couldn't load tracked trophies: {e.Message}"); }
        }

        private static void Save()
        {
            try { File.WriteAllLines(FilePath, Tracked.OrderBy(k => k)); }
            catch (Exception e) { TrophyAssisterPlugin.Log?.LogWarning($"Couldn't save tracked trophies: {e.Message}"); }
        }
    }

    // The wishbone search for tracked creatures, run every frame from the plugin. Mirrors SE_Finder.
    internal static class TrophyFinder
    {
        private const float SearchInterval = 1f;
        private const float PingOffset = 3f;

        private static readonly HashSet<int> Creatures = new HashSet<int>(); // prefab hashes of tracked creatures
        private static string _creaturesFor;
        private static int _creaturesVersion = -1;

        private static Character _target;
        private static float _searchTimer;
        private static float _pingTimer;

        // A tracked creature is in range of the equipped wishbone.
        public static bool HasTarget => _target != null;

        public static void Tick()
        {
            var player = Player.m_localPlayer;
            SE_Finder finder = TrophyAssisterPlugin.Enabled.Value && TrophyAssisterPlugin.TrackerEnabled.Value
                ? Wishbone(player)
                : null;
            if (finder == null)
            {
                _target = null;
                _searchTimer = 0f;
                return;
            }

            float dt = Time.deltaTime;
            _searchTimer += dt;
            if (_searchTimer > SearchInterval)
            {
                _searchTimer = 0f;
                var closest = FindClosest(player);
                if (closest != _target)
                {
                    _target = closest;
                    _pingTimer = 0f;
                }
            }

            if (_target == null) return;
            if (_target.IsDead())
            {
                _target = null;
                return;
            }

            Transform me = player.transform;
            float distance = Utils.DistanceXZ(me.position, _target.transform.position);
            float t = Mathf.Clamp01(distance / PingRange());
            _pingTimer += dt;
            if (_pingTimer <= Mathf.Lerp(finder.m_closeFrequency, finder.m_distantFrequency, t)) return;

            _pingTimer = 0f;
            EffectList ping = t < 0.2f ? finder.m_pingEffectNear : t < 0.6f ? finder.m_pingEffectMed : finder.m_pingEffectFar;
            RedTint.Apply(ping.Create(PingPosition(me, _target), PingRotation(me, _target), null, 1f, -1, player.GetZDOID()));
        }

        // The ping shows (and sounds) a few meters from you towards the creature, at chest height, so you can tell
        // which way to go. Not parented to you: turning around doesn't swing it with you.
        private static Vector3 PingPosition(Transform me, Character target)
        {
            Vector3 dir = Direction(me, target);
            float offset = Mathf.Min(PingOffset, Utils.DistanceXZ(me.position, target.transform.position));
            return me.position + dir * offset + Vector3.up;
        }

        private static Quaternion PingRotation(Transform me, Character target) =>
            Quaternion.LookRotation(Direction(me, target), Vector3.up);

        private static Vector3 Direction(Transform me, Character target)
        {
            Vector3 dir = target.transform.position - me.position;
            dir.y = 0f;
            return dir.sqrMagnitude > 0.01f ? dir.normalized : me.forward;
        }

        // The distance the ping speed scales over (slowest at this distance). With Range 0 that's roughly how far
        // creatures are loaded: the game simulates the zones around yours, NearSimulationDistance zones out.
        private static float PingRange()
        {
            float range = TrophyAssisterPlugin.TrackerRange.Value;
            if (range > 0f) return range;
            int zones = ZNet.instance != null ? ZNet.instance.GetSyncedSimulationDistance().NearSimulationDistance : 1;
            return (Mathf.Max(1, zones) + 0.5f) * ZoneSystem.c_ZoneSize;
        }

        // The wishbone's status effect, present only while it's equipped.
        private static SE_Finder Wishbone(Player player)
        {
            if (player == null) return null;
            foreach (var se in player.GetSEMan().GetStatusEffects())
                if (se is SE_Finder finder) return finder;
            return null;
        }

        private static Character FindClosest(Player player)
        {
            if (!RefreshCreatures()) return null;

            Vector3 me = player.transform.position;
            float range = TrophyAssisterPlugin.TrackerRange.Value;
            Character best = null;
            float bestDistance = range > 0f ? range : float.MaxValue; // 0: GetAllCharacters only holds loaded ones
            foreach (var c in Character.GetAllCharacters())
            {
                if (c == null || c is Player || c.IsDead() || c.IsTamed()) continue;
                float d = Vector3.Distance(me, c.transform.position);
                if (d >= bestDistance) continue;
                var nview = c.GetComponent<ZNetView>();
                var zdo = nview != null && nview.IsValid() ? nview.GetZDO() : null;
                if (zdo == null || !Creatures.Contains(zdo.GetPrefab())) continue;
                best = c;
                bestDistance = d;
            }
            return best;
        }

        // Which creatures drop a tracked trophy, rebuilt when tracking or the character changes.
        // False when nothing is tracked.
        private static bool RefreshCreatures()
        {
            string charKey = GameUtil.CharKey(Player.m_localPlayer);
            if (charKey != _creaturesFor || TrackedStore.Version != _creaturesVersion)
            {
                TrophyIndex.Build();
                Creatures.Clear();
                if (charKey != null)
                    foreach (var trophy in TrackedStore.ForCharacter(charKey))
                        if (TrophyIndex.Sources.TryGetValue(trophy, out var sources))
                            foreach (var s in sources)
                                Creatures.Add(s.CreaturePrefab.GetStableHashCode());
                _creaturesFor = charKey;
                _creaturesVersion = TrackedStore.Version;
            }
            return Creatures.Count > 0;
        }
    }

    // Recolours a freshly spawned ping effect red, keeping each colour's brightness and alpha.
    // Particle colours are set before the first particles are emitted (the next frame).
    internal static class RedTint
    {
        private static readonly int[] ColorProps =
            { Shader.PropertyToID("_Color"), Shader.PropertyToID("_TintColor"), Shader.PropertyToID("_EmissionColor") };
        private static MaterialPropertyBlock _block;

        public static void Apply(GameObject[] effects)
        {
            if (effects == null) return;
            try
            {
                foreach (var go in effects)
                {
                    if (go == null) continue;
                    foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var main = ps.main;
                        main.startColor = Red(main.startColor);
                        var overLife = ps.colorOverLifetime;
                        if (overLife.enabled) overLife.color = Red(overLife.color);
                    }
                    foreach (var light in go.GetComponentsInChildren<Light>(true))
                        light.color = Red(light.color);
                    foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                        TintRenderer(r);
                }
            }
            catch (Exception e)
            {
                TrophyAssisterPlugin.Log.LogWarning($"Couldn't colour the wishbone ping: {e.Message}");
            }
        }

        // A property block instead of r.materials: no material copies left behind.
        private static void TintRenderer(Renderer r)
        {
            var mat = r.sharedMaterial;
            if (mat == null) return;
            _block = _block ?? new MaterialPropertyBlock();
            r.GetPropertyBlock(_block);
            bool any = false;
            foreach (int prop in ColorProps)
            {
                if (!mat.HasProperty(prop)) continue;
                _block.SetColor(prop, Red(mat.GetColor(prop)));
                any = true;
            }
            if (any) r.SetPropertyBlock(_block);
        }

        public static Color Red(Color c)
        {
            float v = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
            return new Color(v, v * 0.12f, v * 0.08f, c.a);
        }

        private static Gradient Red(Gradient g)
        {
            if (g == null) return null;
            var keys = g.colorKeys;
            for (int i = 0; i < keys.Length; i++) keys[i].color = Red(keys[i].color);
            var result = new Gradient { mode = g.mode };
            result.SetKeys(keys, g.alphaKeys);
            return result;
        }

        private static ParticleSystem.MinMaxGradient Red(ParticleSystem.MinMaxGradient g)
        {
            switch (g.mode)
            {
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Red(g.colorMin), Red(g.colorMax));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(Red(g.gradient));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Red(g.gradientMin), Red(g.gradientMax));
                case ParticleSystemGradientMode.RandomColor:
                    return new ParticleSystem.MinMaxGradient(Red(g.gradient)) { mode = ParticleSystemGradientMode.RandomColor };
                default:
                    return new ParticleSystem.MinMaxGradient(Red(g.color));
            }
        }
    }

    // The wishbone's status icon (top right) turns red while a tracked creature is in range.
    // The game resets the icon colour to white every frame, so this runs right after it.
    [HarmonyPatch]
    internal static class WishboneIconPatch
    {
        private static readonly FieldInfo IconsField = AccessTools.Field(typeof(global::Hud), "m_statusEffects");
        private static readonly Color IconRed = new Color(1f, 0.3f, 0.25f, 1f);

        private static bool Prepare() => TargetMethod() != null && IconsField != null;

        private static MethodBase TargetMethod() => AccessTools.Method(typeof(global::Hud), "UpdateStatusEffects");

        private static void Postfix(global::Hud __instance, List<StatusEffect> statusEffects)
        {
            if (!TrophyFinder.HasTarget || statusEffects == null) return;
            try
            {
                if (!(IconsField.GetValue(__instance) is List<RectTransform> icons)) return;
                // Same pairing as the game: icon i shows status effect i.
                for (int i = 0; i < statusEffects.Count && i < icons.Count; i++)
                {
                    if (!(statusEffects[i] is SE_Finder) || icons[i] == null) continue;
                    var image = icons[i].Find("Icon")?.GetComponent<Image>();
                    if (image != null) image.color = IconRed;
                }
            }
            catch { /* only a colour */ }
        }
    }

    // The track button on each trophy in the Trophies tab: a small wishbone, grey or red when tracked.
    internal static class TrackButton
    {
        private const float Size = 30f;
        private const string TrackTip = "Track with the wishbone";
        private const string UntrackTip = "Stop tracking";

        private static readonly Color Tracked = new Color(1f, 0.25f, 0.2f, 1f);
        private static readonly Color Untracked = new Color(0.6f, 0.6f, 0.6f, 0.6f);

        private static Sprite _sprite;

        // Puts the button on the top-right corner of the trophy's icon.
        public static void Add(Transform el, string trophy, string charKey)
        {
            var icon = UiUtil.IconImage(el);
            var frame = icon != null && icon.transform.parent != el ? icon.transform.parent : icon?.transform;
            if (frame == null) return;

            var go = new GameObject("TrophyAssister_track", typeof(RectTransform), typeof(Image), typeof(Button));
            var rt = (RectTransform)go.transform;
            // Child of the element itself and drawn last, so the name and description texts can't cover it.
            rt.SetParent(el, false);
            rt.SetAsLastSibling();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(Size, Size);
            var corners = new Vector3[4];
            ((RectTransform)frame).GetWorldCorners(corners);
            rt.localPosition = el.InverseTransformPoint(corners[2]) - new Vector3(Size * 0.3f, Size * 0.3f, 0f);

            var image = go.GetComponent<Image>();
            image.sprite = Icon();
            image.preserveAspect = true;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            UITooltip tooltip = go.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = InventoryGui.instance?.m_repairButton?.GetComponent<UITooltip>()?.m_tooltipPrefab;
            tooltip.m_topic = "";

            Show(image, tooltip, TrackedStore.IsTracked(charKey, trophy));
            button.onClick.AddListener(() =>
            {
                bool now = TrackedStore.Toggle(charKey, trophy);
                Show(image, tooltip, now);
                string name = TrophyIndex.AllTrophies.FirstOrDefault(t => t.PrefabName == trophy)?.DisplayName ?? trophy;
                Hud.TopLeft(now ? $"Tracking {name} with the wishbone" : $"Stopped tracking {name}");
            });
        }

        private static void Show(Image image, UITooltip tooltip, bool tracked)
        {
            image.color = tracked ? Tracked : Untracked;
            tooltip.m_text = tracked ? UntrackTip : TrackTip;
        }

        // A wishbone traced from the game's item icon (y = 0 is the bottom): the stem with its knob points to the
        // top right; from the joint below it, one arm runs long to the left and the other curves down.
        private static Sprite Icon()
        {
            if (_sprite != null) return _sprite;
            var joint = new Vector2(100f, 86f);
            Vector2[] left = Curve(joint, new Vector2(54f, 77f), new Vector2(14f, 63f));
            Vector2[] down = Curve(joint, new Vector2(99f, 30f), new Vector2(58f, 15f));
            Vector2[] stem = Curve(joint, new Vector2(108f, 101f), new Vector2(115f, 115f));
            return _sprite = ShapeIcon.Create("TrophyAssister_Wishbone", (x, y) =>
                OnBone(left, 7f, 5.5f, x, y) || OnBone(down, 7f, 5.5f, x, y) || OnBone(stem, 8f, 8f, x, y) ||
                ShapeIcon.Ellipse(x, y, joint.x, joint.y, 9f, 9f) ||
                ShapeIcon.Ellipse(x, y, 13f, 63f, 7f, 7f) ||
                ShapeIcon.Ellipse(x, y, 57f, 19f, 7f, 7f) ||
                ShapeIcon.Ellipse(x, y, 115f, 116f, 8f, 9f));
        }

        // Points along a quadratic curve from a to c, bent towards b.
        private static Vector2[] Curve(Vector2 a, Vector2 b, Vector2 c)
        {
            var points = new Vector2[17];
            for (int i = 0; i < points.Length; i++)
            {
                float t = i / (points.Length - 1f);
                points[i] = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;
            }
            return points;
        }

        // Within the bone's thickness: `start` px from its centre line at the joint, tapering to `end` px.
        private static bool OnBone(Vector2[] bone, float start, float end, float x, float y)
        {
            var p = new Vector2(x, y);
            for (int i = 0; i < bone.Length - 1; i++)
            {
                Vector2 a = bone[i], ab = bone[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                float width = Mathf.Lerp(start, end, (i + t) / (bone.Length - 1));
                if ((p - (a + ab * t)).sqrMagnitude <= width * width) return true;
            }
            return false;
        }
    }
}
