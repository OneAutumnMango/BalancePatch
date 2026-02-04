using HarmonyLib;
using Patches.Randomiser;
using Patches.Boosted;
using Patches.Util;
using System;
using System.Reflection;
using UnityEngine;
using System.Threading;

namespace BalancePatch
{
    public static class Loader
    {
        private const string BalanceHarmonyId = "org.bepinex.plugins.balancepatch.balance";
        private const string DebugHarmonyId = "org.bepinex.plugins.balancepatch.debug";
        private const string RandomiserHarmonyId = "org.bepinex.plugins.balancepatch.randomiser";
        private const string BoostedHarmonyId = "org.bepinex.plugins.balancepatch.boosted";
        private const string UtilHarmonyId = "org.bepinex.plugins.balancepatch.util";

        private static Harmony _balanceHarmony;
        private static Harmony _debugHarmony;
        private static Harmony _randomiserHarmony;
        private static Harmony _boostedHarmony;
        private static Harmony _utilHarmony;

        public static bool BalanceLoaded { get; private set; }
        public static bool DebugLoaded { get; private set; }
        public static bool RandomiserLoaded { get; private set; }
        public static bool BoostedLoaded { get; private set; }
        // public static bool BoostedWaiting { get; private set; }
        public static bool UtilLoaded { get; private set; }
        public static bool SpellManagerLoaded()
        {
            return Util.spellManagerIsLoaded;
        }

        public static void LoadUtil()
        {
            if (UtilLoaded) return;

            _utilHarmony = new Harmony(UtilHarmonyId);
            PatchGroup(_utilHarmony, typeof(Patches.Util.Util));

            Util.PopulateDefaultClassAttributes();

            UtilLoaded = true;
            Plugin.Log.LogInfo("Util loaded");
        }

        // ---------------- Balance ----------------

        public static void LoadBalance()
        {
            if (BalanceLoaded) return;

            _balanceHarmony = new Harmony(BalanceHarmonyId);
            PatchGroup(_balanceHarmony, typeof(Patches.Balance.BalancePatches));

            BalanceLoaded = true;
            Plugin.Log.LogInfo("Balance patches loaded");
        }

        public static void UnloadBalance()
        {
            if (!BalanceLoaded) return;

            _balanceHarmony.UnpatchSelf();
            _balanceHarmony = null;

            BalanceLoaded = false;
            Plugin.Log.LogInfo("Balance patches unloaded");
        }

        // ---------------- Debug ----------------

        public static void LoadDebug()
        {
            if (DebugLoaded) return;

            _debugHarmony = new Harmony(DebugHarmonyId);
            PatchGroup(_debugHarmony, typeof(Patches.Debug.DebugPatches));

            DebugLoaded = true;
            Plugin.Log.LogInfo("Debug patches loaded");
        }

        public static void UnloadDebug()
        {
            if (!DebugLoaded) return;

            _debugHarmony.UnpatchSelf();
            _debugHarmony = null;

            DebugLoaded = false;
            Plugin.Log.LogInfo("Debug patches unloaded");
        }

        // ---------------- Randomiser ----------------

        public static void LoadRandomiser()
        {
            if (RandomiserLoaded) return;

            _randomiserHarmony = new Harmony(RandomiserHarmonyId);
            PatchGroup(_randomiserHarmony, typeof(Patches.Randomiser.RandomiserPatch));

            Patch_SpellManager_Randomiser.PrecomputeSpellAttributes();
            Patch_SpellManager_Randomiser.PatchAllSpellObjects(_randomiserHarmony);

            RandomiserLoaded = true;
            Plugin.Log.LogInfo("Randomiser patches loaded");
        }

        // public static void UnloadRandomiser()
        // {
        //     if (!RandomiserLoaded) return;

        //     _randomiserHarmony.UnpatchSelf();
        //     _randomiserHarmony = null;

        //     RandomiserLoaded = false;
        //     RandomiserUnloaded = true;
        //     Plugin.Log.LogInfo("Randomiser patches unloaded");
        // }

        // ---------------- Boosted ----------------

        public static void LoadBoosted()
        {
            if (BoostedLoaded) return;

            _boostedHarmony = new Harmony(BoostedHarmonyId);
            PatchGroup(_boostedHarmony, typeof(Patches.Boosted.BoostedPatch));

            if (!SpellManagerLoaded())
                Plugin.Log.LogError("SpellManager is not loaded. Boosted patches will break.");
            BoostedPatch.PopulateSpellModifierTable();
            BoostedPatch.PatchAllSpellObjects(_boostedHarmony);

            BoostedLoaded = true;
            Plugin.Log.LogInfo("Boosted patches loaded");
        }

        public static void UnloadBoosted()
        {
            if (!BoostedLoaded) return;

            _boostedHarmony.UnpatchSelf();
            _boostedHarmony = null;

            BoostedLoaded = false;
            Plugin.Log.LogInfo("Boosted patches unloaded");
        }

        // ---------------- Shared ----------------

        private static void PatchGroup(Harmony harmony, Type markerType)
        {
            var asm = Assembly.GetExecutingAssembly();
            var targetNamespace = markerType.Namespace;

            foreach (var type in asm.GetTypes())
            {
                if (type.Namespace == targetNamespace)
                {
                    harmony.CreateClassProcessor(type).Patch();
                }
            }
        }
    }
}