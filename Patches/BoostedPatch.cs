using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using BalancePatch;
using System;
using System.Linq;
using System.IO;
using Patches.Util;


namespace Patches.Boosted
{
    internal static class Upgrades
    {
        public readonly struct Tier
        {
            public float Rate { get; }
            public float Up { get; }
            public float Down { get; }

            public Tier(float rate, float up, float down)
            {
                Rate = rate;
                Up = up;
                Down = down;
            }
        }

        public static readonly Tier Common = new Tier(1.00, .25, -.1);
        public static readonly Tier Rare = new Tier(0.25, .50, -.2);
        public static readonly Tier Legendary = new Tier(0.05, .75, -.3);

        // Optional: array for iteration
        public static readonly Tier[] AllTiers = { Common, Rare, Legendary };
    }

    public static class BoostedPatch
    {
        private static string[] ClassAttributeKeys = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"];
        private static string[] SpellTableKeys = ["cooldown", "windUp", "windDown", "initialVelocity", "spellRadius"];
        private static Dictionary<SpellName, Dictionary<String, Dictionary<String, float>>> SpellModifierTable = [];

        public static void PrintConfig()
        {
            void PrintTier(string name, Upgrades.Tier tier)
            {
                Plugin.Log.LogInfo($"Category: {name}");
                Plugin.Log.LogInfo($"  Rate: {tier.Rate}");
                Plugin.Log.LogInfo($"  Up:   {tier.Up}");
                Plugin.Log.LogInfo($"  Down: {tier.Down}");
            }

            PrintTier("Common", Upgrades.Common);
            PrintTier("Rare", Upgrades.Rare);
            PrintTier("Legendary", Upgrades.Legendary);
        }

        private static void TryUpdateModifier(SpellName name, string attribute, float mod)
        {
            if (SpellModifierTable.TryGetValue(name, out var attributeModifiers))
            {
                if (attributeModifiers.TryGetValue(attribute, out var modifiers))
                {
                    modifiers["mult"] += mod;
                }
            }
        }

        private static Upgrades.Tier GetRandomTier()
        {
            double roll = Plugin.Randomiser.NextDouble();
            if (roll < Upgrades.Legendary.Rate)
                return Upgrades.Legendary;
            else if (roll < Upgrades.Rare.Rate)
                return Upgrades.Rare;
            else
                return Upgrades.Common;
        }

        private static void ApplyTier(SpellName name, string attribute, Upgrades.Tier tier, bool up)
        {
            TryUpdateModifier(name, attribute, up ? tier.Up : -tier.Down);
        }

        public static void PopulateSpellModifierTable()
        {
            foreach (SpellName name in Util.Util.DefaultSpellTable.Keys)
            {
                Dictionary<string, Dictionary<String, float>> AttributeModifiers = [];

                foreach (String classAttributeKey in ClassAttributeKeys)
                {
                    AttributeModifiers[classAttributeKey] = new Dictionary<String, float>
                    {
                        ["base"] = Util.Util.DefaultClassAttributes[name][classAttributeKey],
                        ["mult"] = 1f
                    };
                }

                foreach (String spelltablekey in SpellTableKeys)
                {
                    FieldInfo field = typeof(Spell).GetField(spelltablekey, BindingFlags.Public | BindingFlags.Instance);
                    Spell spell = Util.Util.DefaultSpellTable[name];

                    AttributeModifiers[spelltablekey] = new Dictionary<String, float>
                    {
                        ["base"] = (float)field.GetValue(spell),
                        ["mult"] = 1f
                    };

                }
                SpellModifierTable[name] = AttributeModifiers;
            }
            // string inline = "{" + string.Join(", ",
            // SpellModifierTable.Select(spellKvp =>
            //     $"\"{spellKvp.Key}\": {{" +
            //     string.Join(", ", spellKvp.Value.Select(classKvp =>
            //         $"\"{classKvp.Key}\": {{" +
            //         string.Join(", ", classKvp.Value.Select(statKvp => $"\"{statKvp.Key}\": {statKvp.Value}")) +
            //         "}"
            //     )) +
            //     "}"
            // )) + "}";

            // Plugin.Log.LogInfo(inline);
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
