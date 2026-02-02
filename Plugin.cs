using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace BalancePatch
{
    [BepInPlugin("org.bepinex.plugins.balancepatch", "Balance Patch", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource Log;

        public static string seed = "";
        public static System.Random Randomiser;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Balance Patch loaded");

            Loader.LoadBalance();
            Loader.LoadUtil();
        }

        private void OnGUI()
        {
            const int w = 130;
            const int h = 25;
            const int spacing = 10;

            int x = 20;
            int y1 = 20;
            int y2 = 55;

            // ---------------- Balance ----------------
            if (!Loader.BalanceLoaded)
            {
                if (UnityEngine.GUI.Button(new Rect(x, y1, w, h), "Load Balance"))
                    Loader.LoadBalance();
            }
            else
            {
                if (GUI.Button(new Rect(x, y1, w, h), "Unload Balance"))
                    Loader.UnloadBalance();
            }

            // ---------------- Debug ----------------
            if (!Loader.DebugLoaded)
            {
                if (GUI.Button(new Rect(x, y2, w, h), "Load Debug"))
                    Loader.LoadDebug();
            }
            else
            {
                if (GUI.Button(new Rect(x, y2, w, h), "Unload Debug"))
                    Loader.UnloadDebug();
            }

            int x2 = x + w + spacing;
            int textW = 80;

            GUI.enabled = !Loader.RandomiserLoaded;

            seed = GUI.TextField(new Rect(x2, y1, textW, h), seed);
            if (GUI.Button(new Rect(x2, y1 + h + spacing, textW, h), "Randomise"))
            {
                int seedInt = hash(seed);
                Randomiser = new System.Random(seedInt);
                Log.LogInfo($"BalancePatch input: '{seed}' -> seedInt={seedInt}");
                Loader.LoadRandomiser();
            }

            // Restore GUI
            GUI.enabled = true;

            // if (Loader.SpellManagerLoaded())
            // {
                if (Loader.BoostedLoaded)
                {
                    if (GUI.Button(new Rect(x2 + textW + spacing, y1, w, h), "Unload Boosted"))
                        Loader.UnloadBoosted();
                }
                else
                {
                    if (GUI.Button(new Rect(x2 + textW + spacing, y1, w, h), "Load Boosted"))
                        Loader.LoadBoosted();
            // }
            }
        }

        private static int hash(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;

            unchecked
            {
                int hash = 0;
                foreach (char c in s)
                {
                    hash ^= c;
                    hash *= 0x5bd1e995;
                    hash ^= hash >> 15;
                }
                return hash;
            }
        }
    }
}