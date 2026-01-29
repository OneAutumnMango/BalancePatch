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
    public static class Patch_SpellManager_Randomiser
    {
        public static SpellManager mgr;
        private static readonly float bound = 1.6f;

        static void Postfix(SpellManager __instance)
        {
            mgr = __instance ?? Globals.spell_manager;
            if (mgr == null || mgr.spell_table == null) return;

            System.Random rng = Plugin.Randomiser;

            foreach (SpellName name in SpellName.GetValues(typeof(SpellName)))
            {
                if (mgr.spell_table.TryGetValue(name, out Spell spell))
                {
                    Plugin.Log.LogInfo($"[Randomiser] Patching {name}");

                    Func<float, float> tweakFunc =
                        spell.spellButton == SpellButton.Primary
                            ? oldValue => NextGaussian(rng, oldValue, 0.1f * oldValue)
                            : oldValue => RandomTweak(rng, oldValue);

                    spell.cooldown = tweakFunc(spell.cooldown);
                    spell.windUp = Math.Min(tweakFunc(spell.windUp), spell.windUp * bound);
                    spell.windDown = Math.Min(tweakFunc(spell.windDown), spell.windDown * bound);
                    spell.initialVelocity = Math.Max(tweakFunc(spell.initialVelocity), spell.initialVelocity / bound);
                    spell.spellRadius = tweakFunc(spell.spellRadius);
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
            {
                if (original == 0f)
                    original += 0.1f;  // surely have some fun
                value = original + (float)((rng.NextDouble() * 2 - 1) * rareMultiplier * original); // big deviation
                Plugin.Log.LogInfo($"[RandomTweak] {original:F2} -> {Math.Max(0.1f * original, value):F2}");
            }
            return Math.Max(0.1f * original, value);
        }

        public static void PatchAllSpellObjects(Harmony harmony)
        {
            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                string fullTypeName = $"{name}Object";
                Type spellType = AppDomain.CurrentDomain.GetAssemblies()
                                    .Select(a => a.GetType(fullTypeName))
                                    .FirstOrDefault(t => t != null);
                if (spellType == null) continue;

                MethodInfo initMethod = spellType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (initMethod == null) continue;

                MethodInfo prefixMethod = typeof(Patch_SpellManager_Randomiser).GetMethod(
                    nameof(Prefix_SpellObjectInit),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                harmony.Patch(initMethod, prefix: new HarmonyMethod(prefixMethod));
            }
        }

        private static readonly Dictionary<Type, Dictionary<string, float>> PrecomputedSpellValues = [];

        public static void PrecomputeSpellAttributes()
        {
            var rng = Plugin.Randomiser;
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            string[] tweakFields = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"];

            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                string fullTypeName = $"{name}Object";
                Type spellType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(fullTypeName, false))
                    .FirstOrDefault(t => t != null);

                if (spellType == null)
                    continue;

                // Construct a dummy instance
                object instance = Activator.CreateInstance(spellType);

                var values = new Dictionary<string, float>();

                foreach (var fieldName in tweakFields)
                {
                    FieldInfo field = spellType.GetField(fieldName, flags);
                    if (field != null && field.FieldType == typeof(float))
                    {
                        Plugin.Log.LogInfo($"[AwkwardRandomiser] Patching {name} {fieldName}");

                        float original = (float)field.GetValue(instance);
                        float tweaked = RandomTweak(rng, original);

                        if (fieldName == "RADIUS")
                            tweaked = Mathf.Clamp(tweaked, original / bound, original * bound);

                        values[fieldName] = tweaked;
                    }
                }

                PrecomputedSpellValues[spellType] = values;
            }
        }

        private static void Prefix_SpellObjectInit(object __instance)
        {
            Type t = __instance.GetType();

            if (!PrecomputedSpellValues.TryGetValue(t, out var cached))
                return;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var kvp in cached)
            {
                FieldInfo field = t.GetField(kvp.Key, flags);
                field.SetValue(__instance, kvp.Value);
            }
        }


    }
}
