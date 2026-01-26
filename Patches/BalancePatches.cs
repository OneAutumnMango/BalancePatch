using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Patches.Balance
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
            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldc_R4 && instr.operand is float f && f == 4.7f)
                {
                    instr.operand = 3.5f;
                }
                yield return instr;
            }
        }
    }

    [HarmonyPatch(typeof(SpellManager), "Awake")]
    public static class Patch_SpellManager_Awake_Postfix_Chameleon
    {
        static void Postfix(SpellManager __instance)
        {
            var mgr = __instance ?? Globals.spell_manager;
            if (mgr == null || mgr.spell_table == null) return;

            if (mgr.spell_table.TryGetValue(SpellName.Chameleon, out Spell chameleonSpell))
            {
                chameleonSpell.cooldown = 9f;
            }
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
                    damage = 5f;
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
    [HarmonyPatch(typeof(SomAssaultObject), "PrepareDestroy")]
    public static class Patch_SomAssault_PrepareDestroy
    {
        static void Prefix(SomAssaultObject __instance)
        {
            Traverse.Create(__instance)
                    .Field("RADIUS")
                    .SetValue(5f);
        }
    }
}
