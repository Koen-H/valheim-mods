// The biome stage of raw materials, by item name. A material is revealed once the boss before its biome is
// defeated (see Stage). Materials not listed here get their stage from the creatures that drop them (the biome
// they spawn in) or from what they are made of (the latest stage of the ingredients).

using System.Collections.Generic;
using Koendertjh.Shared;

namespace MaterialTracker
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

    internal static class StageTable
    {
        /// <summary>The boss whose defeat reveals the materials of a stage.</summary>
        internal static Boss RevealedBy(Stage stage) => (Boss)(int)stage;

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
                case Heightmap.Biome.DeepNorth: return Stage.DeepNorth;
                default: return Stage.DeepNorth;
            }
        }

        internal static readonly Dictionary<string, Stage> Raw = new Dictionary<string, Stage>
        {
            // Meadows
            { "$item_wood", Stage.Meadows }, { "$item_stone", Stage.Meadows }, { "$item_flint", Stage.Meadows },
            { "$item_resin", Stage.Meadows }, { "$item_feathers", Stage.Meadows }, { "$item_leatherscraps", Stage.Meadows },
            { "$item_deerhide", Stage.Meadows }, { "$item_dandelion", Stage.Meadows }, { "$item_raspberries", Stage.Meadows },
            { "$item_mushroomcommon", Stage.Meadows }, { "$item_honey", Stage.Meadows }, { "$item_queenbee", Stage.Meadows },
            { "$item_hardantler", Stage.Meadows }, { "$item_beechseeds", Stage.Meadows }, { "$item_coins", Stage.Meadows },
            { "$item_fishingbait", Stage.Meadows }, { "$item_fish_raw", Stage.Meadows }, { "$item_necktail", Stage.Meadows },

            // Black Forest
            { "$item_finewood", Stage.BlackForest }, { "$item_roundlog", Stage.BlackForest },
            { "$item_copperore", Stage.BlackForest }, { "$item_tinore", Stage.BlackForest },
            { "$item_surtlingcore", Stage.BlackForest }, { "$item_greydwarfeye", Stage.BlackForest },
            { "$item_ancientseed", Stage.BlackForest }, { "$item_trollhide", Stage.BlackForest },
            { "$item_bonefragments", Stage.BlackForest }, { "$item_blueberries", Stage.BlackForest },
            { "$item_carrotseeds", Stage.BlackForest }, { "$item_thistle", Stage.BlackForest },
            { "$item_fircone", Stage.BlackForest }, { "$item_pinecone", Stage.BlackForest },
            { "$item_birchseeds", Stage.BlackForest }, { "$item_coal", Stage.BlackForest },
            { "$item_cryptkey", Stage.BlackForest }, { "$item_mushroomyellow", Stage.BlackForest },
            { "$item_bjornhide", Stage.BlackForest }, { "$item_bjornpaw", Stage.BlackForest },
            { "$item_fishingbait_forest", Stage.BlackForest },

            // Swamp (and the ocean)
            { "$item_ironscrap", Stage.Swamp }, { "$item_guck", Stage.Swamp }, { "$item_entrails", Stage.Swamp },
            { "$item_witheredbone", Stage.Swamp }, { "$item_elderbark", Stage.Swamp }, { "$item_bloodbag", Stage.Swamp },
            { "$item_ooze", Stage.Swamp }, { "$item_turnipseeds", Stage.Swamp }, { "$item_root", Stage.Swamp },
            { "$item_oakseeds", Stage.Swamp }, { "$item_chain", Stage.Swamp }, { "$item_serpentscale", Stage.Swamp },
            { "$item_serpentmeat", Stage.Swamp }, { "$item_chitin", Stage.Swamp }, { "$item_ymirremains", Stage.Swamp },
            { "$item_fishingbait_swamp", Stage.Swamp }, { "$item_fishingbait_ocean", Stage.Swamp },
            { "$item_amber", Stage.Swamp }, { "$item_amberpearl", Stage.Swamp }, { "$item_ruby", Stage.Swamp },

            // Mountain
            { "$item_silverore", Stage.Mountain }, { "$item_obsidian", Stage.Mountain }, { "$item_wolfpelt", Stage.Mountain },
            { "$item_wolffang", Stage.Mountain }, { "$item_freezegland", Stage.Mountain }, { "$item_crystal", Stage.Mountain },
            { "$item_dragonegg", Stage.Mountain }, { "$item_dragontear", Stage.Mountain }, { "$item_onionseeds", Stage.Mountain },
            { "$item_wolfclaw", Stage.Mountain }, { "$item_wolfhairbundle", Stage.Mountain },
            { "$item_jutered", Stage.Mountain }, { "$item_fishingbait_cave", Stage.Mountain },

            // Plains
            { "$item_barley", Stage.Plains }, { "$item_flax", Stage.Plains }, { "$item_needle", Stage.Plains },
            { "$item_loxpelt", Stage.Plains }, { "$item_blackmetalscrap", Stage.Plains }, { "$item_tar", Stage.Plains },
            { "$item_cloudberries", Stage.Plains }, { "$item_goblintotem", Stage.Plains },
            { "$item_yagluththing", Stage.Plains }, { "$item_fishingbait_plains", Stage.Plains },

            // Mistlands
            { "$item_blackmarble", Stage.Mistlands }, { "$item_carapace", Stage.Mistlands }, { "$item_mandible", Stage.Mistlands },
            { "$item_sap", Stage.Mistlands }, { "$item_yggdrasilwood", Stage.Mistlands }, { "$item_magecap", Stage.Mistlands },
            { "$item_jotunpuffs", Stage.Mistlands }, { "$item_royaljelly", Stage.Mistlands }, { "$item_softtissue", Stage.Mistlands },
            { "$item_bilebag", Stage.Mistlands }, { "$item_scalehide", Stage.Mistlands },
            { "$item_dvergrkeyfragment", Stage.Mistlands }, { "$item_mechanicalspring", Stage.Mistlands },
            { "$item_blackcore", Stage.Mistlands }, { "$item_wisp", Stage.Mistlands }, { "$item_copperscrap", Stage.Mistlands },
            { "$item_bronzescrap", Stage.Mistlands }, { "$item_juteblue", Stage.Mistlands },
            { "$item_queen_drop", Stage.Mistlands }, { "$item_seekerqueen_drop", Stage.Mistlands },
            { "$item_fishingbait_mistlands", Stage.Mistlands },

            // Ashlands
            { "$item_flametalore", Stage.Ashlands }, { "$item_grausten", Stage.Ashlands }, { "$item_charredbone", Stage.Ashlands },
            { "$item_morgenheart", Stage.Ashlands }, { "$item_morgensinew", Stage.Ashlands },
            { "$item_celestialfeather", Stage.Ashlands }, { "$item_bonemawtooth", Stage.Ashlands },
            { "$item_askhide", Stage.Ashlands }, { "$item_askbladder", Stage.Ashlands }, { "$item_charcoalresin", Stage.Ashlands },
            { "$item_blackwood", Stage.Ashlands }, { "$item_sulfurstone", Stage.Ashlands }, { "$item_voltureegg", Stage.Ashlands },
            { "$item_vineberry", Stage.Ashlands }, { "$item_vineberryseeds", Stage.Ashlands },
            { "$item_proustitepowder", Stage.Ashlands }, { "$item_gemstone_red", Stage.Ashlands },
            { "$item_gemstone_green", Stage.Ashlands }, { "$item_gemstone_blue", Stage.Ashlands },
            { "$item_moltencore", Stage.Ashlands }, { "$item_bellfragment", Stage.Ashlands },
            { "$item_fiddleheadfern", Stage.Ashlands }, { "$item_smokepuff", Stage.Ashlands },
            { "$item_fader_drop", Stage.Ashlands }, { "$item_charredskull", Stage.Ashlands },
            { "$item_charredcogwheel", Stage.Ashlands }, { "$item_pot_shard_green", Stage.Ashlands },
            { "$item_fishingbait_ashlands", Stage.Ashlands },

            // Deep North
            { "$item_frostwood", Stage.DeepNorth }, { "$item_ice", Stage.DeepNorth }, { "$item_frozenfuel", Stage.DeepNorth },
            { "$item_goldore", Stage.DeepNorth }, { "$item_glowworm", Stage.DeepNorth }, { "$item_fryslingcore", Stage.DeepNorth },
            { "$item_blubber", Stage.DeepNorth }, { "$item_sealhide", Stage.DeepNorth },
            { "$item_elakinghairbundle", Stage.DeepNorth }, { "$item_frostcore", Stage.DeepNorth },
            { "$item_moleclaws", Stage.DeepNorth }, { "$item_moosehide", Stage.DeepNorth }, { "$item_moosesinew", Stage.DeepNorth },
            { "$item_lingonberries", Stage.DeepNorth }, { "$item_oatseeds", Stage.DeepNorth }, { "$item_kaleseeds", Stage.DeepNorth },
            { "$item_poteitrseeds", Stage.DeepNorth }, { "$item_ooze_mork", Stage.DeepNorth },
            { "$item_hatefulblood", Stage.DeepNorth }, { "$item_frozenking_drop", Stage.DeepNorth },
            { "$item_fircone_big", Stage.DeepNorth }, { "$item_barkabranch", Stage.DeepNorth },
            { "$item_writhanroots", Stage.DeepNorth }, { "$item_faderember", Stage.DeepNorth },
            { "$item_ancientgemstone_black", Stage.DeepNorth }, { "$item_ancientgemstone_green", Stage.DeepNorth },
            { "$item_ancientgemstone_orange", Stage.DeepNorth }, { "$item_ancientgemstone_purple", Stage.DeepNorth },
            { "$item_ancientcoin", Stage.DeepNorth }, { "$item_fishingbait_deepnorth", Stage.DeepNorth },
        };
    }
}
