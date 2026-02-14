using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MageQuitModFramework.Spells;

namespace MageKit.Multicast
{
    [HarmonyPatch]
    public static class MulticastPatch
    {
        private static readonly Dictionary<int, float> MulticastChances = new()
        {
            [4] = .15f,
            [3] = .30f,
            [2] = .75f
        };

        private static bool _isMulticasting = false;

        private static int RollMulticastCount()
        {
            float roll = Random.Range(0f, 1f);

            if      (roll < MulticastChances[4]) return 4;
            else if (roll < MulticastChances[3]) return 3;
            else if (roll < MulticastChances[2]) return 2;

            return 1;
        }

        [HarmonyPatch(typeof(SpellManager), nameof(SpellManager.CastSpell))]
        [HarmonyPostfix]
        static void MulticastSpell(
            SpellName spellName,
            Identity identity,
            Vector3 position,
            Quaternion rotation,
            float curve,
            int spellIndex,
            bool selfCast,
            SpellName spellNameForCooldown,
            SpellManager __instance)
        {
            // Prevent recursive multicast triggering
            if (_isMulticasting)
                return;

            int multicastCount = RollMulticastCount();
            if (multicastCount <= 1)
                return;

            __instance.StartCoroutine(MulticastCoroutine(
                __instance,
                spellName,
                identity,
                position,
                rotation,
                curve,
                spellIndex,
                selfCast,
                spellNameForCooldown,
                multicastCount));
        }

        private static IEnumerator MulticastCoroutine(
            SpellManager spellManager,
            SpellName spellName,
            Identity identity,
            Vector3 position,
            Quaternion rotation,
            float curve,
            int spellIndex,
            bool selfCast,
            SpellName spellNameForCooldown,
            int multicastCount)
        {
            _isMulticasting = true;
            var player = SpellModificationSystem.GetLocalPlayer();
            try
            {
                for (int i = 1; i < multicastCount; i++)
                {
                    yield return new WaitForSeconds(0.5f);
                    if (player?.wizard?.transform?.position is Vector3 pos)
                        position = pos;
                    if (player?.wizard?.GetComponent<WizardController>()?.aimer.rotation is Quaternion rot)
                        rotation = rot;

                    spellManager.CastSpell(spellName, identity, position, rotation, curve, spellIndex, selfCast, spellNameForCooldown);
                }
            }
            finally
            {
                _isMulticasting = false;
            }

            Plugin.Log.LogInfo($"Multicast! {multicastCount}x {spellName}");
        }
    }
}