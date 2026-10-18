// Finds the highest peaks of the world from the world generator, the same heights the terrain is built from.
// A coarse 64 m grid over the whole world (spread over many frames), local tops of that grid, then a fine
// search around the best ones. The result is kept for the world (seed) it was made for.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PeakFinder
{
    internal struct Peak
    {
        public Vector3 Position;
        public float Height => Position.y;
    }

    internal static class Peaks
    {
        private const float WorldRadius = 10000f;
        private const float Step = 64f;
        private const int SamplesPerFrame = 4000;
        // More candidates than ever shown, so OnlyExplored still has enough to pick from.
        private const int Candidates = 200;

        private static List<Peak> s_peaks;
        private static int s_seed;
        private static bool s_scanning;

        internal static bool Ready => s_peaks != null && WorldGenerator.instance != null && s_seed == Seed;

        /// <summary>All found peaks, highest first, at least MinDistance apart; empty until the scan is done.</summary>
        internal static List<Peak> All => Ready ? s_peaks : new List<Peak>();

        private static int Seed => WorldGenerator.instance?.m_world?.m_seed ?? 0;

        internal static void EnsureScan(MonoBehaviour host)
        {
            if (Ready || s_scanning || WorldGenerator.instance == null)
                return;
            s_scanning = true;
            host.StartCoroutine(Scan());
        }

        private static IEnumerator Scan()
        {
            WorldGenerator generator = WorldGenerator.instance;
            int seed = Seed;
            int size = Mathf.CeilToInt(WorldRadius * 2f / Step) + 1;
            var heights = new float[size, size];
            int done = 0;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    float x = -WorldRadius + i * Step;
                    float z = -WorldRadius + j * Step;
                    heights[i, j] = x * x + z * z > WorldRadius * WorldRadius ? float.MinValue : generator.GetHeight(x, z);
                    if (++done % SamplesPerFrame == 0)
                    {
                        yield return null;
                        if (WorldGenerator.instance != generator)
                        {
                            s_scanning = false;
                            yield break;
                        }
                    }
                }
            }

            // Grid points higher than their 8 neighbours.
            var tops = new List<Peak>();
            for (int i = 1; i < size - 1; i++)
            {
                for (int j = 1; j < size - 1; j++)
                {
                    float h = heights[i, j];
                    if (h <= 100f)
                        continue;
                    bool top = true;
                    for (int di = -1; di <= 1 && top; di++)
                    {
                        for (int dj = -1; dj <= 1; dj++)
                        {
                            if ((di != 0 || dj != 0) && heights[i + di, j + dj] > h)
                            {
                                top = false;
                                break;
                            }
                        }
                    }
                    if (top)
                        tops.Add(new Peak { Position = new Vector3(-WorldRadius + i * Step, h, -WorldRadius + j * Step) });
                }
            }
            tops.Sort((a, b) => b.Height.CompareTo(a.Height));

            var peaks = new List<Peak>();
            foreach (Peak top in tops)
            {
                if (peaks.Count >= Candidates)
                    break;
                Peak refined = Refine(generator, top.Position);
                if (!peaks.Exists(p => Flat(p.Position - refined.Position) < PeakFinderPlugin.MinDistance.Value))
                    peaks.Add(refined);
                if (peaks.Count % 20 == 0)
                    yield return null;
            }
            peaks.Sort((a, b) => b.Height.CompareTo(a.Height));

            s_peaks = peaks;
            s_seed = seed;
            s_scanning = false;
            PeakFinderPlugin.Log.LogInfo(peaks.Count > 0
                ? $"Found {peaks.Count} peaks; the highest is {peaks[0].Height:0} m."
                : "Found no peaks.");
        }

        // The highest point within one grid step: 8 m steps, then 1 m steps.
        private static Peak Refine(WorldGenerator generator, Vector3 around)
        {
            Vector3 best = around;
            foreach ((float range, float step) in new[] { (Step, 8f), (8f, 1f) })
            {
                Vector3 center = best;
                for (float dx = -range; dx <= range; dx += step)
                {
                    for (float dz = -range; dz <= range; dz += step)
                    {
                        float h = generator.GetHeight(center.x + dx, center.z + dz);
                        if (h > best.y)
                            best = new Vector3(center.x + dx, h, center.z + dz);
                    }
                }
            }
            return new Peak { Position = best };
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
