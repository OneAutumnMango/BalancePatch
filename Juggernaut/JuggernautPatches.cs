using HarmonyLib;
using UnityEngine;
using MageQuitModFramework.Spells;
using MageQuitModFramework.Utilities;
using System.Linq;


namespace MageKit.Juggernaut
{
    [HarmonyPatch]
    public static class JuggernautPatches
    {
        private const byte JuggernautEventCode = 169;
        private const string JuggernautSaveStateKey = "juggernaut";
        private const float JuggernautSetupDelay = 2f;
        private static int jugPlayerIndex;
        private static bool IAmTheJuggernaut = true;

        public static void Initialize() =>
            PhotonHelper.RegisterEventHandler(JuggernautEventCode, HandleJuggernautAssignEvent);

        private static void HandleJuggernautAssignEvent(object[] args)
        {
            if (args.Length > 0 && args[0] is int playerIndex)
            {
                jugPlayerIndex = playerIndex;
                var localPlayer = SpellModificationSystem.GetLocalPlayer();
                IAmTheJuggernaut = playerIndex == localPlayer?.playerNumber;
                Plugin.Log.LogInfo($"Juggernaut assigned to player index: {playerIndex}. IAmTheJuggernaut: {IAmTheJuggernaut}");
            }
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.StartBattle))]
        [HarmonyPostfix]
        static void PickTheJuggernaut()
        {
            if (!PhotonNetwork.isMasterClient)
                return;

            var playerIndices = PlayerManager.players.Keys.ToList();
            if (playerIndices.Count == 0)
                return;

            var randomIndex = playerIndices[UnityEngine.Random.Range(0, playerIndices.Count)];

            // Raise event to all clients
            PhotonHelper.RaiseEvent(JuggernautEventCode, [randomIndex]);
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.StartBattle2))]
        [HarmonyPostfix]
        static void OnRoundStart(BattleManager __instance)
        {
            __instance.StartCoroutine(OnRoundStartDelayed());
        }

        static System.Collections.IEnumerator OnRoundStartDelayed()
        {
            Plugin.Log.LogInfo($"OnRoundStart: jugPlayerIndex={jugPlayerIndex}, IAmTheJuggernaut={IAmTheJuggernaut}");

            yield return new WaitForSeconds(JuggernautSetupDelay);  // wait for players to load in

            var wc = GameUtility.GetWizard(jugPlayerIndex);
            if (wc == null)
            {
                Plugin.Log.LogError($"Could not find WizardController for player index {jugPlayerIndex}");
                yield break;
            }

            JuggernautHelper.ApplyJuggernautVisuals(wc);

            if (!IAmTheJuggernaut)
                yield break;

            wc.MOVEMENT_SPEED *= 0.75f;

            JuggernautHelper.ApplyJuggernautSpellModifications(IAmTheJuggernaut);
            SpellModificationSystem.Load("juggernaut");
        }

        [HarmonyPatch(typeof(BattleManager), nameof(BattleManager.EndBattle))]
        [HarmonyPostfix]
        static void OnBattleEnd()
        {
            Plugin.Log.LogInfo("Battle ended, reverting Juggernaut spell modifications");
            JuggernautHelper.RevertJuggernautSpellModifications();
        }

        [HarmonyPatch(typeof(RpcManager), nameof(RpcManager.rpcAddWizard))]
        [HarmonyPostfix]
        static void IncreaseJuggernautHealth(Vector3 pos, Quaternion rot, int index, int id1)
        {
            // Only increase HP for the Juggernaut
            if (index != jugPlayerIndex)
                return;

            var ws = GameUtility.GetWizard(index).GetComponent<WizardStatus>();
            if (ws == null)
                return;

            ws.maxHealth *= 1 + PlayerManager.players.Count;  // hp*(1+numPlayers)
            ws.health = ws.maxHealth;
            Plugin.Log.LogInfo($"Juggernaut HP increased for player {index}: {ws.maxHealth}");
        }

        [HarmonyPatch(typeof(PhysicsBody), nameof(PhysicsBody.rpcAddForce))]
        [HarmonyPrefix]
        static void ReduceJuggernautKnockbackTaken(ref Vector3 impulse, PhysicsBody __instance)
        {
            if (!IAmTheJuggernaut)
                return;

            Identity id = __instance.GetComponent<Identity>();
            if (id.owner == SpellModificationSystem.GetLocalPlayer().playerNumber)
                impulse *= 0.25f;
        }
    }
}
