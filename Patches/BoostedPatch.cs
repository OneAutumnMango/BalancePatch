using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using BalancePatch;
using System;
using System.Linq;
using Patches.Util;
using System.Drawing.Printing;


namespace Patches.Boosted
{
    public class AttributeModifier
    {
        public float Base { get; set; }
        public float Mult { get; set; }
        public float Value => Base * Mult;
        public AttributeModifier(float baseValue, float mult = 1f)
        {
            Base = baseValue;
            Mult = mult;
        }
        public void ResetMultiplier()
        {
            Mult = 1f;
        }
        public static implicit operator float(AttributeModifier mod) => mod.Value;
    }
    public class SpellModifiers
    {
        public AttributeModifier DAMAGE { get; set; }
        public AttributeModifier RADIUS { get; set; }
        public AttributeModifier POWER { get; set; }
        public AttributeModifier Y_POWER { get; set; }
        public AttributeModifier cooldown { get; set; }
        public AttributeModifier windUp { get; set; }
        public AttributeModifier windDown { get; set; }
        public AttributeModifier initialVelocity { get; set; }
        public void ResetMultipliers()
        {
            DAMAGE.ResetMultiplier();
            RADIUS.ResetMultiplier();
            POWER.ResetMultiplier();
            Y_POWER.ResetMultiplier();
            cooldown.ResetMultiplier();
            windUp.ResetMultiplier();
            windDown.ResetMultiplier();
            initialVelocity.ResetMultiplier();
        }
    }

    public static class Upgrades
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

        public static readonly Tier Common =    new(1.00f, 0.25f, -0.10f);
        public static readonly Tier Rare =      new(0.25f, 0.50f, -0.20f);
        public static readonly Tier Legendary = new(0.05f, 0.75f, -0.30f);

        // Optional: array for iteration
        public static readonly Tier[] AllTiers = { Common, Rare, Legendary };

        public static Tier GetRandom()
        {
            double roll = Plugin.Random.NextDouble();
            if (roll < Legendary.Rate)
                return Legendary;
            else if (roll < Rare.Rate)
                return Rare;
            else
                return Common;
        }
    }

    public static class BoostedPatch
    {
        private static readonly string[] ClassAttributeKeys = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"];
        private static readonly string[] SpellTableKeys = ["cooldown", "windUp", "windDown", "initialVelocity"];
        // private static Dictionary<SpellName, Dictionary<String, Dictionary<String, float>>> SpellModifierTable = [];
        public static Dictionary<SpellName, SpellModifiers> SpellModifierTable = [];
        public static int numUpgradesPerRound = 10;

        public class UpgradeOption
        {
            public SpellName Spell { get; set; }
            public string Attribute { get; set; }
            public Upgrades.Tier Tier { get; set; }

            public string GetDisplayText()
            {
                string attrDisplay = Attribute switch
                {
                    "DAMAGE"          => "Damage",
                    "RADIUS"          => "Impact Radius",
                    "POWER"           => "Knockback",
                    "Y_POWER"         => "Knockup",
                    "cooldown"        => "Cooldown",
                    "windUp"          => "Wind Up",
                    "windDown"        => "Wind Down",
                    "initialVelocity" => "Initial Velocity",
                    _ => Attribute
                };
                return $"{Spell}: {attrDisplay}";
            }
        }

        private static void TryUpdateModifier(SpellName name, string attribute, float mod)
        {
            if (!SpellModifierTable.TryGetValue(name, out var mods)) return;

            var prop = typeof(SpellModifiers).GetProperty(attribute);

            if (prop?.GetValue(mods) is AttributeModifier attrMod)
                attrMod.Mult += mod;
        }

        // for display
        public static bool TryGetUpDownMultFromOption(UpgradeOption option, out float upMult, out float downMult)
        {
            upMult = 0;
            downMult = 0;

            if (!TryGetMultFromOption(option, out float mult))
                return false;

            upMult = mult + option.Tier.Up;
            downMult = mult + option.Tier.Down;
            return true;
        }

        private static bool TryGetMultFromOption(UpgradeOption option, out float mult)
        {
            mult = 0;

            if (!SpellModifierTable.TryGetValue(option.Spell, out var mods))
                return false;

            var prop = typeof(SpellModifiers).GetProperty(option.Attribute);
            if (prop?.GetValue(mods) is not AttributeModifier attrMod)
                return false;

            mult = attrMod.Mult;
            return true;
        }

        public static List<SpellName> GetPlayerSpells(Player player) =>
            player?.cooldowns?.Keys.ToList() ?? [];

        private static void ApplyTier(SpellName name, string attribute, Upgrades.Tier tier, bool up)
        {
            TryUpdateModifier(name, attribute, up ? tier.Up : tier.Down);
        }

        public static void ApplyUpgrade(UpgradeOption option, bool isPositive)
        {
            ApplyTier(option.Spell, option.Attribute, option.Tier, isPositive);

            ApplyModifiers(Globals.spell_manager, PlayerManager.players.Values.FirstOrDefault(p => p.localPlayerNumber >= 0));

            float change = isPositive ? option.Tier.Up : option.Tier.Down;
            Plugin.Log.LogInfo($"[BoostedPatch.ApplyUpgrade] Applied {(isPositive ? "+" : "")}{change * 100:F0}% to {option.GetDisplayText()}");
        }

        private static bool TryGetDefaultValueFromSpellTable(SpellName name, string attribute, out float value)
        {
            value = 0;
            if (!SpellModifierTable.TryGetValue(name, out var spellModifier)) return false;

            var prop = typeof(SpellModifiers).GetProperty(attribute);
            if (prop?.GetValue(spellModifier) is AttributeModifier attrMod)
            {
                value = attrMod.Base;
                return true;
            }
            return false;
        }

        public static List<UpgradeOption> GenerateUpgradeOptions(Player player, int count = 3)
        {
            var options = new List<UpgradeOption>();
            var spells = GetPlayerSpells(player);
            if (spells.Count == 0) return options;
            var allAttributes = ClassAttributeKeys.Concat(SpellTableKeys).ToArray();
            var possibleUpgrades = new List<(SpellName spell, string attr)>();
            foreach (var spell in spells)
            {
                foreach (var attr in allAttributes)
                {
                    possibleUpgrades.Add((spell, attr));
                }
            }
            var rng = Plugin.Random;
            for (int i = 0; i < count && possibleUpgrades.Count > 0; i++)
            {
                int index = rng.Next(possibleUpgrades.Count);
                var (spell, attr) = possibleUpgrades[index];
                possibleUpgrades.RemoveAt(index);

                if (TryGetDefaultValueFromSpellTable(spell, attr, out float defaultValue) && defaultValue == 0)  // if attribute is 0 ignore it
                {
                    Plugin.Log.LogInfo($"[BoostedPatch.GenerateUpgradeOptions] Ignoring {spell} {attr} because it is 0");
                    i--;
                    continue;
                }
                else
                {
                    bool o = TryGetDefaultValueFromSpellTable(spell, attr, out float val);
                    Plugin.Log.LogInfo($"[BoostedPatch.GenerateUpgradeOptions] TryGetDefaultValueFromSpellTable: ret: {o}, val: {val}");
                }

                options.Add(new UpgradeOption
                {
                    Spell = spell,
                    Attribute = attr,
                    Tier = Upgrades.GetRandom()
                });
            }
            return options;
        }

        public static void PopulateSpellModifierTable()
        {
            foreach (SpellName name in Util.Util.DefaultSpellTable.Keys)
            {
                var spell = Util.Util.DefaultSpellTable[name];
                var classAttrs = Util.Util.DefaultClassAttributes[name];

                SpellModifierTable[name] = new SpellModifiers
                {
                    DAMAGE          = new AttributeModifier(classAttrs["DAMAGE"]),
                    RADIUS          = new AttributeModifier(classAttrs["RADIUS"]),
                    POWER           = new AttributeModifier(classAttrs["POWER"]),
                    Y_POWER         = new AttributeModifier(classAttrs["Y_POWER"]),
                    cooldown        = new AttributeModifier(spell.cooldown),
                    windUp          = new AttributeModifier(spell.windUp),
                    windDown        = new AttributeModifier(spell.windDown),
                    initialVelocity = new AttributeModifier(spell.initialVelocity),
                };
            }
        }

        private static void ApplyModifiersToSpellTable(SpellManager spellManager)
        {
            foreach (var kvp in SpellModifierTable)
            {
                SpellName name = kvp.Key;
                SpellModifiers mods = kvp.Value;

                if (spellManager.spell_table.TryGetValue(name, out Spell spell))
                {
                    spell.cooldown        = mods.cooldown;
                    spell.windUp          = mods.windUp;
                    spell.windDown        = mods.windDown;
                    spell.initialVelocity = mods.initialVelocity;
                }
            }
        }

        private static void ApplyModifiersToPlayer(Player player)
        {
            foreach (var kvp in player.cooldowns)
            {
                SpellName name = kvp.Key;
                Cooldown playerCooldown = kvp.Value;

                if (SpellModifierTable.TryGetValue(name, out SpellModifiers spellMods))
                {
                    playerCooldown.cooldown = spellMods.cooldown;
                }
            }
        }

        public static void ApplyModifiers(SpellManager spellManager, Player player)
        {
            ApplyModifiersToSpellTable(spellManager);
            ApplyModifiersToPlayer(player);
        }

        public static void ResetSpellModifierTableMults()
        {
            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                if (SpellModifierTable.TryGetValue(name, out SpellModifiers mods))
                {
                    mods.ResetMultipliers();
                }
            }
        }

        public static void PatchAllSpellObjects(Harmony harmony)
        {
            foreach (SpellName name in Enum.GetValues(typeof(SpellName)))
            {
                string fullTypeName = Util.Util.GetSpellObjectTypeName(name);
                Type spellType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(fullTypeName))
                    .FirstOrDefault(t => t != null);

                if (spellType == null)
                {
                    Plugin.Log.LogWarning("[BoostedPatch.PatchAllSpellObjects] No spell object found for spell: " + name);
                    continue;
                }

                MethodInfo initMethod = spellType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (initMethod == null)
                {
                    Plugin.Log.LogWarning("[BoostedPatch.PatchAllSpellObjects] No Init method found for spell: " + name);
                    continue;
                }

                MethodInfo prefixMethod = typeof(BoostedPatch).GetMethod(
                    nameof(Prefix_SpellObjectInit),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

                harmony.Patch(initMethod, prefix: new HarmonyMethod(prefixMethod));
            }
        }

        private static void Prefix_SpellObjectInit(object __instance)
        {
            Type t = __instance.GetType();
            var matchedSpell = Util.Util.GetSpellNameFromTypeName(t.Name);

            if (!SpellModifierTable.TryGetValue(matchedSpell.Value, out var mods))
            {
                Plugin.Log.LogWarning("[BoostedPatch.Prefix_SpellObjectInit] No spell modifiers found for spell: " + matchedSpell);
                return;
            }

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            t.GetField("DAMAGE" , flags)?.SetValue(__instance, (float)mods.DAMAGE);
            t.GetField("RADIUS" , flags)?.SetValue(__instance, (float)mods.RADIUS);
            t.GetField("POWER"  , flags)?.SetValue(__instance, (float)mods.POWER);
            t.GetField("Y_POWER", flags)?.SetValue(__instance, (float)mods.Y_POWER);
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

    // ROUND WATCHER
    [HarmonyPatch(typeof(NetworkManager), "CombineRoundScores")]
    public static class NetworkManager_CombineRoundScores_RoundLogger
    {
        private static void Prefix()
        {
            Plugin.Log.LogInfo(
                $"[NetworkManager.CombineRoundScores] round {PlayerManager.round}"
            );
        }

        private static void Postfix()
        {
            // if (Util.Util.mgr != null)
            // {
            //     BoostedPatch.ApplyModifiersToSpellTable(Util.Util.mgr);
            //     Plugin.Log.LogInfo("[NetworkManager.CombineRoundScores] Applied spell modifiers to spell table");
            // }

            if (PlayerManager.round > 0)
            {
                Player player = PlayerManager.players.Values.FirstOrDefault(p => p.localPlayerNumber >= 0);  // hoping >= 0 is always the local player
                if (player == null)
                {
                    Plugin.Log.LogError("[NetworkManager.CombineRoundScores] No local player found");
                    return;
                }

                // BoostedPatch.ApplyModifiersToPlayer(player);  // update player cooldowns

                var options = BoostedPatch.GenerateUpgradeOptions(player, BoostedPatch.numUpgradesPerRound);
                Plugin.CurrentUpgradeOptions.Clear();
                Plugin.CurrentUpgradeOptions.AddRange(options);  // thread safe

                Plugin.Log.LogInfo($"[NetworkManager.CombineRoundScores] Generated {options.Count} upgrade options:");
                foreach (var opt in options)
                {
                    Plugin.Log.LogInfo($"  {opt.GetDisplayText()}: +{opt.Tier.Up * 100:F0}% / {opt.Tier.Down * 100:F0}%");
                }
            }
        }
    }


    [HarmonyPatch(typeof(PlayerManager), "AddPlayer")]
    public static class Patch_PlayerManager_AddPlayer
    {
        static void Prefix(int number, InputType inputType)
        {
            Plugin.Log.LogInfo($"[PlayerManager.AddPlayer] Adding Player: {number}, InputType: {inputType}");
            Plugin.Log.LogInfo($"[PlayerManager.AddPlayer] Current Players: {string.Join(", ", PlayerManager.players.Keys)}");
        }
    }
}
