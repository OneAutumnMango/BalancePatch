using HarmonyLib;
using UnityEngine;
using MageQuitModFramework.Spells;
using MageQuitModFramework.Utilities;
using MageQuitModFramework.Modding;
using DG.Tweening;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MageKit.Juggernaut
{
    [HarmonyPatch]
    public static class JuggernautPatches
    {
        private const byte JuggernautEventCode = 169;
        private const string JuggernautSaveStateKey = "juggernaut";
        private static int jugPlayerIndex;
        private static bool IAmTheJuggernaut = true;
        private static bool JuggernautModsApplied = false;
        private static SpellModifierTable _juggernautTable;
        private static string _previouslyLoadedTableKey;

        public static void Initialize()
        {
            Plugin.Log.LogInfo($"[DEBUG] JuggernautPatches.Initialize() called. Registering handler for event code {JuggernautEventCode}");
            // Register handler for when the Juggernaut event arrives
            PhotonHelper.RegisterEventHandler(JuggernautEventCode, HandleJuggernautAssignEvent);
            Plugin.Log.LogInfo($"Juggernaut event system initialized (event code {JuggernautEventCode})");
        }

        private static void HandleJuggernautAssignEvent(object[] args)
        {
            Plugin.Log.LogInfo($"[DEBUG] HandleJuggernautAssignEvent called with {args?.Length ?? 0} args");
            if (args.Length > 0 && args[0] is int playerIndex)
            {
                jugPlayerIndex = playerIndex;
                var localPlayer = SpellModificationSystem.GetLocalPlayer();
                IAmTheJuggernaut = playerIndex == localPlayer?.playerNumber;
                Plugin.Log.LogInfo($"Juggernaut assigned to player index: {playerIndex}. IAmTheJuggernaut: {IAmTheJuggernaut}");
            }
            else
            {
                Plugin.Log.LogError($"[DEBUG] HandleJuggernautAssignEvent received invalid args");
            }
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.StartBattle))]
        [HarmonyPostfix]
        static void PickTheJuggernaut()
        {
            Plugin.Log.LogInfo("Picking Juggernaut for this battle...");
            if (!PhotonNetwork.isMasterClient)
                return;
            Plugin.Log.LogInfo("I am master client, picking Juggernaut...");

            var playerIndices = PlayerManager.players.Keys.ToList();
            if (playerIndices.Count == 0)
                return;

            var randomIndex = playerIndices[UnityEngine.Random.Range(0, playerIndices.Count)];
            Plugin.Log.LogInfo($"Juggernaut assigned to player index: {randomIndex}");

            // Raise event to all clients
            Plugin.Log.LogInfo($"[DEBUG] About to raise event {JuggernautEventCode} with player index {randomIndex}");
            PhotonHelper.RaiseEvent(JuggernautEventCode, [randomIndex]);
            Plugin.Log.LogInfo($"[DEBUG] Finished raising event");
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.StartBattle2))]
        [HarmonyPostfix]
        static void OnRoundStart()
        {
            Plugin.Log.LogInfo($"OnRoundStart: jugPlayerIndex={jugPlayerIndex}, IAmTheJuggernaut={IAmTheJuggernaut}");
            Plugin.Log.LogInfo($"Available players: {string.Join(", ", PlayerManager.players.Keys)}");

            if (!PlayerManager.players.TryGetValue(jugPlayerIndex, out var player))
            {
                Plugin.Log.LogError($"Failed to find player {jugPlayerIndex} for Juggernaut patch");
                return;
            }

            if (player.wizard == null || !player.wizard)  // Unity's special null check
            {
                Plugin.Log.LogWarning($"Player {jugPlayerIndex} wizard not yet spawned, skipping Juggernaut visuals");
                return;
            }

            Plugin.Log.LogInfo($"Found player {jugPlayerIndex}, getting wizard...");
            var wc = player.wizard.GetComponent<WizardController>();
            if (wc == null)
            {
                Plugin.Log.LogError($"Failed to find player wizard {jugPlayerIndex} controller for Juggernaut patch. player.wizard={player.wizard}");
                return;
            }

            Plugin.Log.LogInfo($"Applying Juggernaut visuals to player {jugPlayerIndex}");
            ApplyJuggernautVisuals(wc);


            if (!IAmTheJuggernaut)
                return;
            ApplyJuggernautSpellModifications();
            SpellModificationSystem.Load("juggernaut");
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.EndBattle))]
        [HarmonyPostfix]
        static void OnBattleEnd()
        {
            Plugin.Log.LogInfo("Battle ended, reverting Juggernaut spell modifications");
            RevertJuggernautSpellModifications();
        }

        private static void ApplyJuggernautSpellModifications()
        {
            if (!IAmTheJuggernaut)
                return;

            if (JuggernautModsApplied)
            {
                Plugin.Log.LogWarning("Juggernaut mods already applied this round");
                return;
            }

            _previouslyLoadedTableKey = SpellModificationSystem.LoadedTableKey;
            _juggernautTable = SpellModificationSystem.GetTable(_previouslyLoadedTableKey).Copy();

            string[] buffAttributes     = ["DAMAGE", "RADIUS", "POWER", "Y_POWER", "HEAL", "initialVelocity"];
            string[] debuffAttributes   = ["cooldown", "windUp", "windDown"];

            foreach (SpellName spellName in Enum.GetValues(typeof(SpellName)))
            {
                // 2x multiplier (multiplicative)
                foreach (var attribute in buffAttributes)
                {
                    _juggernautTable.TryMultiplyModifier(spellName, attribute, 2.0f);
                }

                // 0.5x multiplier (multiplicative)
                foreach (var attribute in debuffAttributes)
                {
                    _juggernautTable.TryMultiplyModifier(spellName, attribute, 0.5f);
                }
            }

            SpellModificationSystem.RegisterTable("juggernaut", _juggernautTable);
            JuggernautModsApplied = true;
            Plugin.Log.LogInfo("Juggernaut spell modifications applied");
        }

        private static void RevertJuggernautSpellModifications()
        {
            if (!JuggernautModsApplied)
                return;

            Plugin.Log.LogInfo("Reverting Juggernaut spell modifications");

            SpellModificationSystem.ClearTable("juggernaut");
            _juggernautTable = null;

            SpellModificationSystem.Load(_previouslyLoadedTableKey);

            JuggernautModsApplied = false;
            Plugin.Log.LogInfo("Juggernaut spell modifications reverted");
        }

        private static void ApplyJuggernautVisuals(WizardController wc)
        {
            wc.transform.DOKill(); // prevent stacking if triggered again

            wc.transform  // make big
                .DOScale(1.3f, 0.7f)
                .SetDelay(3f)
                .SetEase(Ease.OutBack);

            // Make wizard bright/glowing
            Color[] customColors = [new Color(1f, 0.9f, 0.2f), Color.white];
            GameUtility.SetWizardColor(customColors, wc.gameObject, true); // true = extraBloom
            ApplyJuggernautGlow(wc.gameObject);
        }

        private static void ApplyJuggernautGlow(GameObject wizardObject)
        {
            foreach (var renderer in wizardObject.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.materials;
                for (var i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null)
                    {
                        continue;
                    }

                    if (material.HasProperty(MaterialHashes.EmissionColor))
                    {
                        material.EnableKeyword("_EMISSION");
                        var emission = material.GetColor(MaterialHashes.EmissionColor);
                        material.SetColor(MaterialHashes.EmissionColor, emission * 2.25f);
                    }
                }
            }

            foreach (var light in wizardObject.GetComponentsInChildren<Light>(true))
            {
                light.intensity = Mathf.Max(light.intensity, 2.5f);
                light.range = Mathf.Max(light.range, 8f);
            }
        }
    }
}
