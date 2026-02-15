using System.Collections.Generic;
using HarmonyLib;
using MageQuitModFramework.Utilities;
using UnityEngine;

namespace MageKit.SpellRain
{
    [HarmonyPatch]
    public static class SpellRainPatches
    {
        [HarmonyPatch(typeof(SpellHandler), nameof(SpellHandler.StartSpell))]
        [HarmonyPostfix]
        static void MarkOneTimeSpellAsUsed(SpellHandler __instance, SpellButton button)
        {
            Identity id = __instance.GetComponent<Identity>();
            if (id == null) return;

            int owner = id.owner;

            if (!SpellRainSpawner.oneTimeSpells.TryGetValue(owner, out var playerSpells)) return;
            if (!playerSpells.TryGetValue(button, out var oneTime)) return;

            if (!oneTime.used)
            {
                oneTime.used = true;
                Plugin.Log.LogInfo($"Player {owner} used one-time spell: {oneTime.spellName}");
            }
        }

        [HarmonyPatch(typeof(SpellHandler), "Update")]
        [HarmonyPostfix]
        static void RemoveUsedOneTimeSpells(SpellHandler __instance)
        {
            Identity id = __instance.GetComponent<Identity>();
            if (id == null) return;

            int owner = id.owner;

            if (!SpellRainSpawner.oneTimeSpells.TryGetValue(owner, out var playerSpells)) return;

            var spellStateEnum = GameModificationHelpers.GetPrivateField<int>(__instance, "spellState");

            if (spellStateEnum == 2)  // complete
            {
                List<SpellButton> toRemove = [];

                foreach (var kvp in playerSpells)
                {
                    if (kvp.Value.used)
                    {
                        toRemove.Add(kvp.Key);
                    }
                }

                foreach (SpellButton spellButton in toRemove)
                {
                    if (!playerSpells.TryGetValue(spellButton, out var spell))
                        continue;

                    if (PlayerManager.players.TryGetValue(owner, out var player))
                    {
                        if (player.spell_library.ContainsKey(spellButton))
                        {
                            player.spell_library.Remove(spellButton);
                        }

                        if (player.cooldowns.ContainsKey(spell.spellName))
                        {
                            player.cooldowns.Remove(spell.spellName);
                        }
                    }

                    playerSpells.Remove(spellButton);

                    SpellRainHelper.HideHudButton(spellButton);

                    Plugin.Log.LogInfo($"Removed one-time spell {spell.spellName} from player {owner} slot {spellButton}");
                }
            }
        }
    }
}

