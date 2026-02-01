using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using BalancePatch;
using System;
using System.Linq;
using System.IO;


namespace Patches.Boosted
{
    internal static class BoostedConfig
    {
        public readonly struct Tier
        {
            public double Rate { get; }
            public double Up { get; }
            public double Down { get; }

            public Tier(double rate, double up, double down)
            {
                Rate = rate;
                Up = up;
                Down = down;
            }
        }

        public static readonly Tier Common = new Tier(1.00, 1.25, 0.90);
        public static readonly Tier Rare = new Tier(0.25, 1.50, 0.80);
        public static readonly Tier Ultra = new Tier(0.05, 1.75, 0.70);

        // Optional: array for iteration
        public static readonly Tier[] AllTiers = { Common, Rare, Ultra };
    }

    public static class BoostedPatch
    {
        public static void PrintConfig()
        {
            void PrintTier(string name, BoostedConfig.Tier tier)
            {
                Plugin.Log.LogInfo($"Category: {name}");
                Plugin.Log.LogInfo($"  Rate: {tier.Rate}");
                Plugin.Log.LogInfo($"  Up:   {tier.Up}");
                Plugin.Log.LogInfo($"  Down: {tier.Down}");
            }

            PrintTier("Common", BoostedConfig.Common);
            PrintTier("Rare", BoostedConfig.Rare);
            PrintTier("Ultra", BoostedConfig.Ultra);
        }
    }

    [HarmonyPatch(typeof(GustObject), "Init")]
    public static class Patch_GustObject_Init_SetDamage
    {
        static void Prefix(GustObject __instance)
        {
            const float NewDamage = 50f;

            typeof(GustObject)
                .GetField("DAMAGE", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.SetValue(__instance, NewDamage);
        }
    }

    // [HarmonyPatch(typeof(Player), "RegisterCooldown")]
    // public static class Patch_Player_RegisterCooldown_SetDamage
    // {
    //     static void Prefix(ref float cooldown)
    //     {
    //         cooldown = 100f;
    //     }
    // }
    [HarmonyPatch(typeof(SpellManager), "Awake")]
    public static class Patch_SpellManager_cheats
    {
        public static SpellManager mgr;

        static void Postfix(SpellManager __instance)
        {
            mgr = __instance ?? Globals.spell_manager;
            if (mgr == null || mgr.spell_table == null) return;


            if (mgr.spell_table.TryGetValue(SpellName.Gust, out Spell spell))
            {

                spell.cooldown = 100;
            }
        }
    }



    // // ROUND WATCHER
    // [HarmonyPatch(typeof(NetworkManager), "CombineRoundScores")]
    // public static class NetworkManager_CombineRoundScores_RoundLogger
    // {
    //     private static void Prefix()
    //     {
    //         // wait for 5 seconds and log each second
    //         for (int i = 5; i > 0; i--)
    //         {
    //             Plugin.Log.LogInfo($"[RoundWatcher] CombineRoundScores → waiting {i} seconds...");
    //             System.Threading.Thread.Sleep(1000);
    //         }

    //         Plugin.Log.LogInfo(
    //             $"[RoundWatcher] CombineRoundScores → round {PlayerManager.round}"
    //         );
    //     }
    // }
}
