using HarmonyLib;
using UnityEngine;
using MageQuitModFramework.Utilities;
using MageQuitModFramework.Spells;
using System.Collections.Generic;
using System;

namespace BalancePatch.Randomiser
{
    public static class RandomiserPatch
    {
        private static readonly float bound = 1.6f;
        private static Dictionary<Type, Dictionary<string, float>> PrecomputedSpellValues;

        public static void PatchAll(Harmony harmony)
        {
            harmony.PatchAll(typeof(RandomiserPatch));
            GameModificationHelpers.PatchAllSpellObjectInit(harmony, 
                prefixMethod: typeof(RandomiserPatch).GetMethod(nameof(Prefix_SpellObjectInit), 
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
        }

        [HarmonyPatch(typeof(SpellManager), "Awake")]
        public static class Patch_SpellManager_Awake
        {
            static void Postfix(SpellManager __instance)
            {
                System.Random rng = Plugin.RandomiserRng;

                GameModificationHelpers.ModifyAllSpells(__instance, spell =>
                {
                    Func<float, float> tweakFunc = spell.spellButton == SpellButton.Primary
                        ? oldValue => NextGaussian(rng, oldValue, 0.1f * oldValue)
                        : oldValue => RandomTweak(rng, oldValue);

                    spell.cooldown = tweakFunc(spell.cooldown);
                    spell.windUp = Math.Min(tweakFunc(spell.windUp), spell.windUp * bound);
                    spell.windDown = Math.Min(tweakFunc(spell.windDown), spell.windDown * bound);
                    spell.initialVelocity = Math.Max(tweakFunc(spell.initialVelocity), spell.initialVelocity / bound);
                });
            }
        }

        private static float NextGaussian(System.Random rng, float mean, float stdDev)
        {
            double u1 = 1.0 - rng.NextDouble();
            double u2 = 1.0 - rng.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return (float)(mean + stdDev * randStdNormal);
        }

        private static float RandomTweak(System.Random rng, float original, float stdDev = 0.45f, float rareMultiplier = 3f, float rareChance = 0.1f)
        {
            float value = NextGaussian(rng, original, stdDev * original);
            if (rng.NextDouble() < rareChance)
            {
                if (original == 0f)
                    original += 0.1f;
                value = original + (float)((rng.NextDouble() * 2 - 1) * rareMultiplier * original);
                Plugin.Log.LogInfo($"[Randomiser] Big tweak: {original:F2} -> {Math.Max(0.1f * original, value):F2}");
            }
            return Math.Max(0.1f * original, value);
        }

        public static void PrecomputeSpellAttributes()
        {
            var rng = Plugin.RandomiserRng;
            string[] tweakFields = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"];

            PrecomputedSpellValues = RandomiserHelpers.PrecomputeSpellAttributes(tweakFields, (fieldName, original) =>
            {
                float tweaked = RandomTweak(rng, original);
                
                if (fieldName == "RADIUS")
                    tweaked = Mathf.Clamp(tweaked, original / bound, original * bound);
                
                Plugin.Log.LogInfo($"[Randomiser] {fieldName}: {original} -> {tweaked}");
                return tweaked;
            });
        }

        private static void Prefix_SpellObjectInit(object __instance)
        {
            if (PrecomputedSpellValues != null && PrecomputedSpellValues.TryGetValue(__instance.GetType(), out var values))
            {
                GameModificationHelpers.ApplyFieldValuesToInstance(__instance, values);
            }
        }
    }
}
