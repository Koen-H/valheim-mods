// Shared by several mods (linked into each csproj): the biome stages of the game and the boss that reveals each.
// Defeating the boss of a biome reveals what belongs to the next one: beat Bonemass and the mountain shows.

using System;

namespace Koendertjh.Shared
{
    internal enum Stage
    {
        Meadows,
        BlackForest,
        Swamp,
        Mountain,
        Plains,
        Mistlands,
        Ashlands,
        DeepNorth
    }

    internal static class Stages
    {
        /// <summary>The boss whose defeat reveals a stage (None for the meadows).</summary>
        internal static Boss RevealedBy(Stage stage) => (Boss)(int)stage;

        internal static bool IsRevealed(Stage stage) => BossProgress.IsDefeated(RevealedBy(stage));

        internal static Stage FromBiome(Heightmap.Biome biome)
        {
            switch (biome)
            {
                case Heightmap.Biome.Meadows: return Stage.Meadows;
                case Heightmap.Biome.BlackForest: return Stage.BlackForest;
                case Heightmap.Biome.Swamp:
                case Heightmap.Biome.Ocean: return Stage.Swamp;
                case Heightmap.Biome.Mountain: return Stage.Mountain;
                case Heightmap.Biome.Plains: return Stage.Plains;
                case Heightmap.Biome.Mistlands: return Stage.Mistlands;
                case Heightmap.Biome.AshLands: return Stage.Ashlands;
                default: return Stage.DeepNorth;
            }
        }

        /// <summary>The earliest stage among a set of biome flags.</summary>
        internal static Stage Earliest(Heightmap.Biome biomes)
        {
            Stage best = Stage.DeepNorth;
            foreach (Heightmap.Biome biome in Enum.GetValues(typeof(Heightmap.Biome)))
            {
                if (biome == Heightmap.Biome.None || biome == Heightmap.Biome.All || (biomes & biome) != biome)
                    continue;
                Stage stage = FromBiome(biome);
                if (stage < best)
                    best = stage;
            }
            return best;
        }

        /// <summary>"the Black Forest", "the Mountains"...</summary>
        internal static string BiomeName(Stage stage)
        {
            switch (stage)
            {
                case Stage.Meadows: return "the Meadows";
                case Stage.BlackForest: return "the Black Forest";
                case Stage.Swamp: return "the Swamp";
                case Stage.Mountain: return "the Mountains";
                case Stage.Plains: return "the Plains";
                case Stage.Mistlands: return "the Mistlands";
                case Stage.Ashlands: return "the Ashlands";
                default: return "the Deep North";
            }
        }
    }
}
