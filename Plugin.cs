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
        public static System.Random Random = new();
        public static System.Collections.Generic.List<Patches.Boosted.BoostedPatch.UpgradeOption> CurrentUpgradeOptions = [];

        private static GUIStyle Green, Red;

        private int upgradesSelected = 0;
        private int MaxUpgrades = 3;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("Balance Patch loaded");

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

            InitColors();

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
                // if (Loader.BoostedWaiting)
                // {
                //     GUI.Label(new Rect(x2 + textW + spacing, y1, w, h), "Waiting for SpellManager");
                // }
                // else
                if (GUI.Button(new Rect(x2 + textW + spacing, y1, w, h), "Load Boosted"))
                    Loader.LoadBoosted();
            }
            // }

            // ---------------- Upgrade Options ----------------
            if (CurrentUpgradeOptions.Count > 0)
            {
                int upgradeX = 20;
                int upgradeY = Screen.height / 2 - 100;
                int optionHeight = 35;
                int buttonWidth = 60;
                int labelWidth = 180;

                GUI.Box(new Rect(upgradeX - 5, upgradeY - 5, labelWidth + buttonWidth * 2 + 20, CurrentUpgradeOptions.Count * optionHeight + 10), "Upgrades");

                for (int i = 0; i < CurrentUpgradeOptions.Count; i++)
                {
                    var option = CurrentUpgradeOptions[i];
                    int yPos = upgradeY + i * optionHeight;

                    GUI.Label(new Rect(upgradeX, yPos, labelWidth, 30), option.GetDisplayText());

                    if (!Patches.Boosted.BoostedPatch.TryGetUpDownMultFromOption(option, out float upMult, out float downMult))
                        continue;

                    if (GUI.Button(new Rect(upgradeX + labelWidth, yPos, buttonWidth, 30), $"{upMult * 100:F0}%", Green))
                    {
                        SelectUpgrade(option, true);
                    }

                    if (GUI.Button(new Rect(upgradeX + labelWidth + buttonWidth, yPos, buttonWidth, 30), $"{downMult * 100:F0}%", Red))
                    {
                        SelectUpgrade(option, false);
                    }
                }
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

        void SelectUpgrade(Patches.Boosted.BoostedPatch.UpgradeOption option, bool isUp)
        {
            Patches.Boosted.BoostedPatch.ApplyUpgrade(option, isUp);
            upgradesSelected++;

            // Optional: prevent selecting the same upgrade again
            CurrentUpgradeOptions.Remove(option);

            if (upgradesSelected >= MaxUpgrades)
            {
                CurrentUpgradeOptions.Clear();
                upgradesSelected = 0; // reset for next time
            }
        }


        private static void InitColors()
        {
            Color upColor = new(0.3f, 0.85f, 0.3f);
            Color downColor = new(0.9f, 0.3f, 0.3f);

            Green = new GUIStyle(GUI.skin.button);
            Green.normal.textColor  = upColor;
            Green.hover.textColor   = upColor;
            Green.active.textColor  = upColor;
            Green.focused.textColor = upColor;

            Red = new GUIStyle(GUI.skin.button);
            Red.normal.textColor  = downColor;
            Red.hover.textColor   = downColor;
            Red.active.textColor  = downColor;
            Red.focused.textColor = downColor;
        }
    }
}