using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BalancePatch;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace Patches.Randomiser
{
    public static class RandomiserPatch { }

    // cooldown and description spell_table patches
    [HarmonyPatch(typeof(SpellManager), "Awake")]
    public static class Patch_SpellManager_Awake_Postfix_Randomiser
    {
        public static SpellManager mgr;
        static void Postfix(SpellManager __instance)
        {
            mgr = __instance ?? Globals.spell_manager;
            if (mgr == null || mgr.spell_table == null) return;

            System.Random rng = Plugin.Randomiser;
            float bound = 1.6f;

            foreach (SpellName name in SpellName.GetValues(typeof(SpellName)))
            {
                if (mgr.spell_table.TryGetValue(name, out Spell spell))
                {
                    Func<float, float> tweakFunc =
                        spell.spellButton == SpellButton.Primary
                            ? oldValue => NextGaussian(rng, oldValue, 0.1f * oldValue)
                            : oldValue => RandomTweak(rng, oldValue);

                    spell.cooldown        = tweakFunc(spell.cooldown);
                    spell.windUp          = Math.Min(tweakFunc(spell.windUp), spell.windUp * bound);
                    spell.windDown        = Math.Min(tweakFunc(spell.windDown), spell.windDown * bound);
                    spell.initialVelocity = Math.Max(tweakFunc(spell.initialVelocity), spell.initialVelocity / bound);
                    spell.spellRadius     = tweakFunc(spell.spellRadius);
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

        public static float RandomTweak(System.Random rng, float original, float stdDev = 0.45f, float rareMultiplier = 3f, float rareChance = 0.1f)
        {
            float value = NextGaussian(rng, original, stdDev * original); // small wiggle
            if (rng.NextDouble() < rareChance)
                value = original + (float)((rng.NextDouble() * 2 - 1) * rareMultiplier * original); // big deviation
            return value;
        }

        public static void PatchAllSpellObjects(Harmony harmony)
        {
            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                string fullTypeName = $"BalancePatch.{name}Object"; // include namespace
                Type spellType = AppDomain.CurrentDomain.GetAssemblies()
                                    .Select(a => a.GetType(fullTypeName))
                                    .FirstOrDefault(t => t != null);
                if (spellType == null) continue;

                MethodInfo initMethod = spellType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (initMethod == null) continue;

                MethodInfo postfixMethod = typeof(Patch_SpellManager_Awake_Postfix_Randomiser).GetMethod(
                    nameof(Postfix_SpellObjectInit),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                harmony.Patch(initMethod, postfix: new HarmonyMethod(postfixMethod));
            }
        }

        private static void Postfix_SpellObjectInit(object __instance)
        {
            var rng = Plugin.Randomiser;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            string[] tweakFields = { "DAMAGE", "RADIUS", "POWER", "Y_POWER" };

            foreach (var fieldName in tweakFields)
            {
                FieldInfo field = __instance.GetType().GetField(fieldName, flags);
                if (field != null && field.FieldType == typeof(float))
                {
                    float oldValue = (float)field.GetValue(__instance);
                    float newValue = RandomTweak(rng, oldValue);
                    field.SetValue(__instance, newValue);

                    Plugin.Log.LogInfo($"[{__instance.GetType().Name}] {field.Name}: {oldValue:F2} -> {newValue:F2}");
                }
            }
        }
    }
}
