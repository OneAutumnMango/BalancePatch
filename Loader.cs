using HarmonyLib;
using System;
using System.Reflection;

namespace BalancePatch
{
    public static class Loader
    {
        private const string BalanceHarmonyId = "org.bepinex.plugins.balancepatch.balance";
        private const string DebugHarmonyId = "org.bepinex.plugins.balancepatch.debug";
        private const string RandomiserHarmonyId = "org.bepinex.plugins.balancepatch.randomiser";

        private static Harmony _balanceHarmony;
        private static Harmony _debugHarmony;
        private static Harmony _randomiserHarmony;

        public static bool BalanceLoaded { get; private set; }
        public static bool DebugLoaded { get; private set; }
        public static bool RandomiserLoaded { get; private set; }

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

            RandomiserLoaded = true;
            Plugin.Log.LogInfo("Randomiser patches loaded");
        }

        public static void UnloadRandomiser()
        {
            if (!RandomiserLoaded) return;

            _randomiserHarmony.UnpatchSelf();
            _randomiserHarmony = null;

            RandomiserLoaded = false;
            Plugin.Log.LogInfo("Randomiser patches unloaded");
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