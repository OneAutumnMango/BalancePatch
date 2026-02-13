using HarmonyLib;
using MageQuitModFramework.Utilities;
using MageQuitModFramework.Data;
using MageQuitModFramework.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MageKit.Boosted
{
    public static class BoostedPatch
    {
        private static readonly string[] ClassAttributeKeys = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"];
        private static readonly string[] SpellTableKeys = ["cooldown", "windUp", "windDown", "initialVelocity"];
        private static readonly string[] CustomKeys = ["HEAL"];

        private static Dictionary<SpellName, string[]> ManualModifierRejections = [];
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
                    "HEAL"            => "Healing",
                    _ => Attribute
                };
                return $"{Spell}: {attrDisplay}";
            }
        }

        public static void PatchAll(Harmony harmony)
        {
            // harmony.PatchAll(typeof(BoostedPatch));
            MethodInfo prefixMethod = typeof(BoostedPatch).GetMethod(
                nameof(Prefix_SpellObjectInit),
                BindingFlags.Static | BindingFlags.NonPublic
            );
            SpellModificationSystem.PatchAllSpellObjectInit(harmony, prefixMethod);
        }

        public static void PopulateManualModifierRejections()
        {
            // cooldown, windup, winddown, initialVelocity
            ManualModifierRejections = new Dictionary<SpellName, string[]>
            {
                [SpellName.FrogOfLife] = ["DAMAGE", "POWER", "Y_POWER"],
                [SpellName.Suspend] = ["DAMAGE", "RADIUS", "POWER", "Y_POWER"],
                [SpellName.Sapshot] = ["DAMAGE", "RADIUS", "POWER"],
                [SpellName.Vacuum] = ["DAMAGE"],
                [SpellName.FlashFlood] = ["RADIUS", "POWER", "Y_POWER", "windUp", "windDown"],
                [SpellName.Preserve] = ["RADIUS", "POWER", "windUp", "windDown"],
                [SpellName.BubbleBreaker] = ["RADIUS", "POWER", "windUp", "windDown"],
                [SpellName.Urchain] = ["RADIUS", "POWER"]
            };
        }

        public static void PopulateSpellModifierTable()
        {
            SpellModificationSystem.Initialize(
                GameDataInitializer.DefaultSpellTable,
                GameDataInitializer.DefaultClassAttributes
            );
        }

        public static void ResetSpellModifierTableMults()
        {
            SpellModificationSystem.ResetAllMultipliers();
        }

        public static bool TryGetUpDownMultFromOption(UpgradeOption option, out float upMult, out float downMult)
        {
            upMult = 0;
            downMult = 0;

            if (!SpellModificationSystem.TryGetMultiplier(option.Spell, option.Attribute, out float mult))
                return false;

            upMult = mult + option.Tier.Up;
            downMult = mult + option.Tier.Down;
            return true;
        }

        public static void ApplyUpgrade(UpgradeOption option, bool isPositive)
        {
            float change = isPositive ? option.Tier.Up : option.Tier.Down;
            SpellModificationSystem.TryUpdateModifier(option.Spell, option.Attribute, change);

            var player = PlayerManager.players.Values.FirstOrDefault(p => p.localPlayerNumber >= 0);
            SpellModificationSystem.ApplyModifiersToGame(Globals.spell_manager, player);

            Plugin.Log.LogInfo($"[Boosted] Applied {(isPositive ? "+" : "")}{change * 100:F0}% to {option.GetDisplayText()}");
        }

        public static bool IsUpgradeAllowed(SpellName spellName, string attribute)
        {
            if (spellName != SpellName.FrogOfLife && attribute == "HEAL")
                return false;

            if (ManualModifierRejections.ContainsKey(spellName) && ManualModifierRejections[spellName].Contains(attribute))
                return false;

            if (Plugin.BannedUpgrades.Contains((spellName, attribute)))
                return false;

            if (SpellModificationSystem.TryGetDefaultValue(spellName, attribute, out float defaultValue) && defaultValue == 0)
                return false;

            SpellModificationSystem.TryGetMultiplier(spellName, attribute, out float mult);

            if (mult <= 0)
                return false;

            switch (attribute)
            {
                case "cooldown" when mult <= 0.6f:
                    return false;
            }

            if (!Globals.spell_manager.spell_table.TryGetValue(spellName, out Spell spell))
                return true;

            if (spell.spellButton == SpellButton.Movement)
            {
                switch (attribute)
                {
                    case "RADIUS" when mult >= 2f:
                        return false;
                }
            }

            if (spell.spellButton == SpellButton.Melee)
            {
                switch (attribute)
                {
                    case "cooldown":
                    case "RADIUS" when mult >= 2.5f:
                    case "windUp" when mult <= 0.5f:
                        return false;
                }
            }

            if (spell.spellButton == SpellButton.Primary)
            {
                switch (attribute)
                {
                    case "cooldown"when mult <= 0.7f:
                    case "DAMAGE" when mult >= 2f:
                    case "POWER" when mult >= 2f:
                        return false;
                }
            }
            return true;
        }

        public static List<UpgradeOption> GenerateUpgradeOptions(Player player, int count = 3)
        {
            var options = new List<UpgradeOption>();
            var spells = player?.cooldowns?.Keys.ToList() ?? [];

            if (spells.Count == 0) return options;

            var allAttributes = ClassAttributeKeys.Concat(SpellTableKeys).Concat(CustomKeys).ToArray();
            var possibleUpgrades = new List<(SpellName spell, string attr)>();

            foreach (var spell in spells)
            {
                foreach (var attr in allAttributes)
                {
                    if (!IsUpgradeAllowed(spell, attr))
                        continue;

                    possibleUpgrades.Add((spell, attr));
                }
            }

            var rng = Plugin.Random;
            for (int i = 0; i < count && possibleUpgrades.Count > 0; i++)
            {
                int index = rng.Next(possibleUpgrades.Count);
                var (spell, attr) = possibleUpgrades[index];
                possibleUpgrades.RemoveAt(index);

                options.Add(new UpgradeOption
                {
                    Spell = spell,
                    Attribute = attr,
                    Tier = Upgrades.GetRandom()
                });
            }

            return options;
        }

        private static void Prefix_SpellObjectInit(object __instance)
        {
            Type t = __instance.GetType();
            var matchedSpell = SpellModificationSystem.GetSpellNameFromTypeName(t.Name);

            if (!matchedSpell.HasValue) return;

            var values = new Dictionary<string, float>();

            if (SpellModificationSystem.TryGetModifier(matchedSpell.Value, "DAMAGE", out var damage))
                values["DAMAGE"] = damage;
            if (SpellModificationSystem.TryGetModifier(matchedSpell.Value, "RADIUS", out var radius))
                values["RADIUS"] = radius;
            if (SpellModificationSystem.TryGetModifier(matchedSpell.Value, "POWER", out var power))
                values["POWER"] = power;
            if (SpellModificationSystem.TryGetModifier(matchedSpell.Value, "Y_POWER", out var yPower))
                values["Y_POWER"] = yPower;

            GameModificationHelpers.ApplyFieldValuesToInstance(__instance, values);
        }

        [HarmonyPatch(typeof(FrogOfLifeObject), "Heal")]
        public static class Patch_FrogOfLifeObject_Heal
        {
            public static void Prefix(ref float amount)
            {
                if (SpellModificationSystem.TryGetModifier(SpellName.FrogOfLife, "HEAL", out var healMod))
                    amount *= healMod.Mult;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);

                for (int i = 0; i < codes.Count; i++)
                {
                    if (i + 2 < codes.Count &&
                        codes[i].opcode == OpCodes.Ldarg_1 &&
                        codes[i + 1].opcode == OpCodes.Ldc_R4 &&
                        Math.Abs((float)codes[i + 1].operand - 15f) < 1e-6f &&
                        codes[i + 2].opcode == OpCodes.Ceq)
                    {
                        var branchTarget = codes[i + 2].operand;
                        yield return new CodeInstruction(OpCodes.Ldarg_1);
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 15f);
                        yield return new CodeInstruction(OpCodes.Bge_S, branchTarget);
                        i += 2;
                        continue;
                    }

                    yield return codes[i];
                }
            }
        }

        [HarmonyPatch(typeof(NetworkManager), "CombineRoundScores")]
        public static class Patch_NetworkManager_CombineRoundScores
        {
            private static void Prefix()
            {
                Plugin.Log.LogInfo($"[Boosted] Round {PlayerManager.round}");
            }

            private static void Postfix()
            {
                if (PlayerManager.round > 0)
                {
                    Player player = PlayerManager.players.Values.FirstOrDefault(p => p.localPlayerNumber >= 0);
                    if (player == null)
                    {
                        Plugin.Log.LogError("[Boosted] No local player found");
                        return;
                    }

                    var options = GenerateUpgradeOptions(player, numUpgradesPerRound);
                    Plugin.CurrentUpgradeOptions.Clear();
                    Plugin.CurrentUpgradeOptions.AddRange(options);

                    Plugin.Log.LogInfo($"[Boosted] Generated {options.Count} upgrade options");
                }
            }
        }
    }
}
