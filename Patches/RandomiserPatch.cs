using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BalancePatch;
using System;

namespace Patches.Randomiser
{
    public static class RandomiserPatch { }

    // cooldown and description spell_table patches
    [HarmonyPatch(typeof(SpellManager), "Awake")]
    public static class Patch_SpellManager_Awake_Postfix_CooldownsAndDescriptions
    {
        static void Postfix(SpellManager __instance)
        {
            var mgr = __instance ?? Globals.spell_manager;
            if (mgr == null || mgr.spell_table == null) return;

            System.Random rng = Plugin.Randomiser;

            foreach (SpellName name in SpellName.GetValues(typeof(SpellName)))
            {
                if (mgr.spell_table.TryGetValue(name, out Spell spell))
                {
                    // spell.cooldown        = RandomTweak(rng, spell.cooldown);
                    // spell.windUp          = RandomTweak(rng, spell.windUp);
                    // spell.windDown        = RandomTweak(rng, spell.windDown);
                    // spell.initialVelocity = RandomTweak(rng, spell.initialVelocity);
                    // spell.spellRadius     = RandomTweak(rng, spell.spellRadius);

                                // Cooldown
                    float oldValue = spell.cooldown;
                    spell.cooldown = RandomTweak(rng, oldValue);
                    Plugin.Log.LogInfo($"[{name}] cooldown: {oldValue} -> {spell.cooldown}");

                    // WindUp
                    oldValue = spell.windUp;
                    spell.windUp = RandomTweak(rng, oldValue);
                    Plugin.Log.LogInfo($"[{name}] windUp: {oldValue} -> {spell.windUp}");

                    // WindDown
                    oldValue = spell.windDown;
                    spell.windDown = RandomTweak(rng, oldValue);
                    Plugin.Log.LogInfo($"[{name}] windDown: {oldValue} -> {spell.windDown}");

                    // InitialVelocity
                    oldValue = spell.initialVelocity;
                    spell.initialVelocity = RandomTweak(rng, oldValue);
                    Plugin.Log.LogInfo($"[{name}] initialVelocity: {oldValue} -> {spell.initialVelocity}");

                    // SpellRadius
                    oldValue = spell.spellRadius;
                    spell.spellRadius = RandomTweak(rng, oldValue);
                    Plugin.Log.LogInfo($"[{name}] spellRadius: {oldValue} -> {spell.spellRadius}");
                }
            }
        }

        private static float NextGaussian(System.Random rng, float mean, float stdDev)
        {
            double u1 = 1.0 - rng.NextDouble(); // uniform(0,1]
            double u2 = 1.0 - rng.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return (float)(mean + stdDev * randStdNormal);
        }

        private static float RandomTweak(System.Random rng, float original, float stdDev = 0.3f, float rareMultiplier = 3f, float rareChance = 0.05f)
        {
            float value = NextGaussian(rng, original, stdDev * original); // small wiggle
            if (rng.NextDouble() < rareChance)
                value = original + (float)((rng.NextDouble() * 2 - 1) * rareMultiplier * original); // big deviation
            return value;
        }

    }
}
