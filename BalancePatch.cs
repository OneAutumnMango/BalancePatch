using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System;

using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;


[BepInPlugin("org.bepinex.plugins.balancepatch", "Balance Patch", "1.0.0")]
public class Plugin : BaseUnityPlugin
{
    public static ManualLogSource LoggerStatic;

    private void Awake()
    {
        LoggerStatic = Logger;
        LoggerStatic.LogInfo("Balance Plugin loaded!");

        var harmony = new Harmony("org.bepinex.plugins.balancepatch");
        harmony.PatchAll();
    }
}

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

// [HarmonyPatch(typeof(Chameleon), "Initialize")]
// public static class Patch_SpellInitialize
// {
//    static void Postfix(Spell __instance, Identity identity, Vector3 position, Quaternion rotation, float curve, int spellIndex = -1, bool selfCast = false, SpellName spellNameForCooldown = SpellName.Fireball)
//    {
//        try
//        {
//            if (__instance == null) return;

//            var sb = new StringBuilder();
//            sb.AppendLine($"[SpellLogger] Initialized Spell: {__instance.spellName}");
//            sb.AppendLine($"  Description: {__instance.description}");
//            sb.AppendLine($"  AnimationName: {__instance.animationName}");
//            sb.AppendLine($"  Cooldown: {__instance.cooldown}");
//            sb.AppendLine($"  WindUp: {__instance.windUp}");
//            sb.AppendLine($"  WindDown: {__instance.windDown}");
//            sb.AppendLine($"  MinRange: {__instance.minRange}");
//            sb.AppendLine($"  MaxRange: {__instance.maxRange}");
//            sb.AppendLine($"  CurveMultiplier: {__instance.curveMultiplier}");
//            sb.AppendLine($"  InitialVelocity: {__instance.initialVelocity}");
//            sb.AppendLine($"  IgnorePath: {__instance.ignorePath}");
//            sb.AppendLine($"  Uses: {__instance.uses}");
//            sb.AppendLine($"  AdditionalCasts Count: {__instance.additionalCasts?.Length ?? 0}");
//            sb.AppendLine("  StackTrace:");

//            sb.AppendLine("  StackTrace:");
//            sb.AppendLine(Environment.StackTrace); // append entire stack trace at once

//            Debug.Log(sb.ToString());
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"[SpellLogger] Exception logging spell: {ex}");
//        }
//    }
// }

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

// show hitboxes (dont use wormhole)
// [HarmonyPatch(typeof(GameUtility), "GetAllInSphere")]
// public static class Patch_GetAllInSphere_Debug
// {
//     static void Prefix(Vector3 center, float radius)
//     {
//         DrawDebugSphere(center, radius);
//     }

//     static void DrawDebugSphere(Vector3 pos, float radius)
//     {
//         var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
//         go.transform.position = pos;
//         go.transform.localScale = Vector3.one * radius * 2f;

//         var col = go.GetComponent<Collider>();
//         if (col) col.enabled = false;

//         var mr = go.GetComponent<MeshRenderer>();
//         mr.material = new Material(Shader.Find("Sprites/Default"));
//         mr.material.color = new Color(1f, 0f, 0f, 0.25f);

//         GameObject.Destroy(go, 0.1f);
//     }
// }

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

// shorter chameleon cooldown 13s -> 9s
[HarmonyPatch(typeof(Chameleon), "Initialize")]
public static class Patch_ChameleonInitialize
{
    static void Prefix(Spell __instance)
    {
        if (__instance == null) return;
        __instance.cooldown = 9f;
    }
}

// increased sustain damage from 3 to 5
[HarmonyPatch(typeof(SustainObjectObject), "rpcImpact")]
public static class Patch_SustainObject_rpcImpact_SetDamage
{
    static void Prefix(SpellObject __instance)
    {
        const float NewDamage = 5f;

        typeof(SpellObject)
            .GetField("DAMAGE", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(__instance, NewDamage);
    }
}

// reduce ignite dps from 1.8 to 1.5
[HarmonyPatch(typeof(IgniteObject), "FixedUpdate")]
public static class Patch_IgniteObject_FixedUpdate_SetDPS
{
    static readonly AccessTools.FieldRef<IgniteObject, List<UnitStatus>> TargetsField =
        AccessTools.FieldRefAccess<IgniteObject, List<UnitStatus>>("targets");

    static readonly AccessTools.FieldRef<IgniteObject, Identity> IdField =
        AccessTools.FieldRefAccess<IgniteObject, Identity>("id");

    static bool Prefix(IgniteObject __instance)
    {
        if (__instance == null) return true;

        var targets = TargetsField(__instance);
        if (targets == null || targets.Count == 0)
            return false;

        var id = IdField(__instance);
        if (id == null) return false;

        float newDPS = 1.5f;
        int owner = id.owner;

        foreach (var unit in targets)
        {
            if (unit != null)
                unit.ApplyDamage(newDPS * Time.fixedDeltaTime, owner, 13);
        }

        return false;  // skip original FixedUpdate
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

// [HarmonyPatch(typeof(WizardStatus), "rpcApplyDamage")]
// public static class Patch_WizardStatus_rpcApplyDamage
// {
//     static void Prefix(WizardStatus __instance, float damage, int owner, int source)
//     {
//         var idField = typeof(WizardStatus).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic);
//         var idValue = idField?.GetValue(__instance);

//         int wizardOwner = -1;
//         if (idValue != null)
//         {
//             var ownerField = idValue.GetType().GetField("owner", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
//             if (ownerField != null)
//                 wizardOwner = (int)ownerField.GetValue(idValue);
//         }

//         Debug.Log($"[Damage Log] Wizard {wizardOwner} is about to take {damage} damage from {owner}, source {source}");
//     }

//     static void Postfix(WizardStatus __instance, float damage, int owner, int source)
//     {
//         Debug.Log($"[Damage Log] Wizard's remaining health: {__instance.health}");
//     }
// }