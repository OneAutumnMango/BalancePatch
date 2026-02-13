using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MageQuitModFramework.Spells;
using MageQuitModFramework.Utilities;
using MageQuitModFramework.Modding;

namespace MageKit.Balance
{
    public static class BalancePatches { }

    // stop flashflood refreshing primary
    [HarmonyPatch(typeof(SpellHandler), "RefreshPrimary")]
    public static class Patch_RefreshPrimary
    {
        static bool Prefix(SpellHandler __instance)
        {
            // Only skip if the caller is Flash Flood
            if (Environment.StackTrace.Contains("FlashFloodObject.localSpellObjectStart"))
            {
                return false;
            }
            return true;
        }
    }

    // act faster out of geyser (top of jump)
    [HarmonyPatch(typeof(Geyser), "Initialize")]
    public static class Patch_GeyserInitialize
    {
        static void Postfix(Spell __instance)
        {
            if (__instance == null) return;
            __instance.windDown = 0.5f;
        }
    }

    // reduce flameleap offset, make it slighly closer to landing site
    [HarmonyPatch(typeof(FlameLeapObject), "PrepareDestroy")]
    public static class Patch_FlameLeapPrepareDestroy
    {
        static void Prefix(FlameLeapObject __instance)
        {
            __instance.transform.position += __instance.transform.forward * -1f;
        }
    }

    // shorter chainmail duration 4.7s -> 3.5s
    [HarmonyPatch(typeof(ChainmailObject), "Update")]
    public static class Patch_ChainmailObject_Update
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return GameModificationHelpers.ReplaceFloatConstant(instructions, 4.7f, 3.5f);
        }
    }

    // source-based damage scaling
    [HarmonyPatch(typeof(WizardStatus), "rpcApplyDamage")]
    public static class Patch_WizardStatus_rpcApplyDamage_SourceScaling
    {
        static void Prefix(ref float damage, int owner, int source)
        {
            switch (source)
            {
                case 48:  // chainlightning
                    damage *= 0.75f;
                    break;

                case 37:  // sustain
                    damage *= 1.6f;
                    break;

                case 13:  // ignite
                    damage *= 0.833f;
                    break;

                case 66:  // brrage
                    damage *= 1.2f;
                    break;

                case 63:  // snowball 6+6t -> 7+7t
                    damage *= 1.166f;
                    break;
            }
        }
    }

    // increased steal trap distance by 50%
    [HarmonyPatch(typeof(StealTrapObject), "Init")]
    public static class Patch_StealTrapObject_Init_IncreaseVelocity
    {
        static void Prefix(ref float velocity)
        {
            velocity *= 1.5f;
        }
    }

    // increased duration of rocket by 20%
    [HarmonyPatch(typeof(RocketObject), "Awake")]
    public static class Patch_RocketObject_Awake_SetStartTime
    {
        static readonly AccessTools.FieldRef<SpellObject, float> StartTimeRef =
            AccessTools.FieldRefAccess<SpellObject, float>("START_TIME");

        static void Postfix(RocketObject __instance)
        {
            StartTimeRef(__instance) = 1.2f;
        }
    }

    // reduce tetherball duration 7s -> 5s
    [HarmonyPatch(typeof(TetherballObject), "Init")]
    public static class Patch_TetherballObject_Init_SetStartTime
    {
        static readonly AccessTools.FieldRef<SpellObject, float> StartTimeRef =
            AccessTools.FieldRefAccess<SpellObject, float>("START_TIME");

        static void Prefix(TetherballObject __instance)
        {
            StartTimeRef(__instance) = 5f;
        }
    }

    // somassault radius increase from 4 to 5
    [HarmonyPatch(typeof(SomAssaultObject), "Init")]
    public static class Patch_SomAssault_Init
    {
        static void Prefix(SomAssaultObject __instance)
        {
            if (ModManager.IsModuleLoaded("Boosted")) return;

            Traverse.Create(__instance)
                    .Field("RADIUS")
                    .SetValue(5f);
        }
    }

    // change brrage count from 5 to 3
    [HarmonyPatch(typeof(Brrage), "Initialize")]
    public static class Patch_Brrage_Initialize_Count
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return GameModificationHelpers.ReplaceIntConstant(instructions, 5, 3);
        }
    }

    // cooldown and description spell_table patches
    [HarmonyPatch(typeof(SpellManager), "Awake")]
    public static class Patch_SpellManager_Awake_Postfix_CooldownsAndDescriptions
    {
        static void Postfix(SpellManager __instance)
        {
            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.FlashFlood, spell =>
                spell.description = "Short range teleport that resets velocity. Can be reactivated to return to casting point.");

            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.Brrage, spell =>
                spell.description = "Fires a barrage of 3 icicles in the cast direction, dealing damage and creating new crystals after a delay. On cast, your Crystals become Inert, just sitting there.");

            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.Wormhole, spell =>
            {
                spell.description = "Gives you a temporary boost to your movement speed.";
                spell.cooldown = 9f;
            });

            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.Chameleon, spell => spell.cooldown = 9f);

            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.TowVine, spell =>
            {
                spell.cooldown = 9f;
                spell.additionalCasts[0].cooldown = 9f;
            });

            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.BullRush, spell => spell.cooldown = 13f);
            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.Echo, spell => spell.cooldown = 5f);
            SpellModificationSystem.ModifySpellTableEntry(__instance, SpellName.FlameLeap, spell => spell.cooldown = 12f);
        }
    }

    // dogs 20% slower
    [HarmonyPatch(typeof(CrystalObject), "Awake")]
    public static class Patch_CrystalObject_Awake_SetVelocity
    {
        static void Postfix(CrystalObject __instance)
        {
            if (__instance == null) return;
            float prev = __instance.velocity;
            __instance.velocity = prev * 0.8f;
        }
    }

    // turtle 25% faster
    [HarmonyPatch(typeof(BombshellObject), "FixedUpdate")]
    public static class Patch_BombshellObject_FixedUpdate_Multiplier
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return GameModificationHelpers.ReplaceFloatConstant(instructions, 0.275f, 0.275f * 1.25f);
        }
    }

    // tsunami 15% faster
    [HarmonyPatch(typeof(TsunamiObject), "FixedUpdate")]
    public static class Patch_TsunamiObject_FixedUpdate_Multiplier
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return GameModificationHelpers.ReplaceFloatConstant(instructions, 0.6f, 0.6f * 1.15f);
        }
    }

    // stop lava triggering bubblebreaker
    [HarmonyPatch(typeof(BubbleBreakerObject), "rpcRegisterDamage")]
    public static class Patch_BubbleBreakerObject_rpcRegisterDamage
    {
        static bool Prefix(float damage)
        {
            if (damage == 0.067f)  // lava damage, owner doesnt work
                return false;
            return true;
        }
    }

    // reduce hinder slow from 50% to 40%
    [HarmonyPatch(typeof(HinderObject), "localApplySlow")]
    public static class Patch_HinderObject_localApplySlow_Speed
    {
        public static readonly float oldSlowFactor = 0.5f;
        public static readonly float newSlowFactor = 0.65f;  // slows by (1 - newSlowFactor)

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return GameModificationHelpers.ReplaceFloatConstant(instructions, oldSlowFactor, newSlowFactor);
        }
    }

    // restore speed
    [HarmonyPatch(typeof(HinderObject), "OnDestroy")]
    static class Patch_HinderObject_OnDestroy
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return GameModificationHelpers.ReplaceFloatConstant(instructions,
                Patch_HinderObject_localApplySlow_Speed.oldSlowFactor,
                Patch_HinderObject_localApplySlow_Speed.newSlowFactor);
        }
    }

    // wormhole is a move speed buff now
    static class WormholePatchHelper
    {
        private static readonly float speedBuff = 1.8f;
        private static WizardController wc;

        public static void localSpellObjectStart(GameObject wizard, float duration = 5f)
        {
            wc = wizard.GetComponent<WizardController>();
            applySpeedBuff();

            wizard.GetComponent<MonoBehaviour>().StartCoroutine(RemoveBuffAfterDelay(duration));
        }

        private static IEnumerator RemoveBuffAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            removeSpeedBuff();
        }

        private static Action applySpeedBuff = () => wc.MOVEMENT_SPEED *= speedBuff;
        private static Action removeSpeedBuff = () => wc.MOVEMENT_SPEED /= speedBuff;
    }

    [HarmonyPatch(typeof(WormholeObject), "Init")]
    static class Patch_WormholeObject_Init
    {
        static bool Prefix(WormholeObject __instance, Identity identity)
        {
            WormholePatchHelper.localSpellObjectStart(identity.gameObject);
            global::UnityEngine.Object.Destroy(__instance.gameObject);

            return false;
        }
    }

    // 50% clone hp
    [HarmonyPatch(typeof(GameUtility), "CreateCloneOfWizard")]
    public static class Patch_GameUtility_CloneHP
    {
        private static readonly float hpMult = 0.5f;
        static void Postfix(GameObject __result)
        {
            if (__result == null) return;

            var status = __result.GetComponent<WizardStatus>();
            if (status == null) return;

            status.maxHealth *= hpMult;
            status.health *= hpMult;

            status.health = Math.Max(0.001f, status.health);
        }
    }

    // Change max rounds to 30
    [HarmonyPatch(typeof(SelectionMenu), "ChangeNumberOfRounds")]
    public static class Patch_SelectionMenu_ChangeNumberOfRounds_Max30
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return GameModificationHelpers.ReplaceIntConstant(instructions, 20, 35);
        }
    }

    // alter competitive mode preset
    [HarmonyPatch(typeof(SelectionMenu), "ShowPreset")]
    public static class Patch_SelectionMenu_ShowPreset_ReplaceCompetitive
    {
        static bool Prefix(SelectionMenu __instance)
        {
            if (PlayerManager.gameSettings.preset != GameModePresets.Competitive)
                return true;

            PlayerManager.gameSettings.healthMode = HealthMode.High;
            PlayerManager.gameSettings.mercyRuleMode = MercyRuleMode.Off;
			PlayerManager.gameSettings.spellSelectionMode = SpellSelectionMode.TwoRoundSnake;
            PlayerManager.gameSettings.stage = StageName.LessRandom;
            PlayerManager.gameSettings.elements = new ElementInclusionMode[10];
			PlayerManager.gameSettings.elements =
            [
                ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Possible,
				ElementInclusionMode.Banned
			];
            PlayerManager.gameSettings.numberOfRounds = 35;
            PlayerManager.finalRound = 35;

            AccessTools.Method(typeof(SelectionMenu), "ShowHealthMode")    ?.Invoke(__instance, null);
            AccessTools.Method(typeof(SelectionMenu), "ShowMercyRule")     ?.Invoke(__instance, null);
            AccessTools.Method(typeof(SelectionMenu), "ShowNumberOfRounds")?.Invoke(__instance, null);
            AccessTools.Method(typeof(SelectionMenu), "ShowStage")         ?.Invoke(__instance, null);
            AccessTools.Method(typeof(SelectionMenu), "ShowElements")      ?.Invoke(__instance, null);

            var presetsText = AccessTools.Field(typeof(SelectionMenu), "presetsText")?.GetValue(__instance);
            presetsText?.GetType()
                .GetProperty("text")
                ?.SetValue(presetsText, "Real MageQuit");

            var descriptionText = AccessTools.Field(typeof(SelectionMenu), "descriptionText")?.GetValue(__instance);
            descriptionText?.GetType()
                .GetProperty("text")
                ?.SetValue(descriptionText, "Extra long game recommended with Boosted.");

            return false;
        }
    }

}
