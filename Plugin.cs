using BepInEx;
using BepInEx.Logging;
using LowPolyWaterv2Demo;
using UnityEngine;

namespace BalancePatch
{
    [BepInPlugin("org.bepinex.plugins.balancepatch", "Balance Patch", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log;

        public static string seed = "";
        public static System.Random Randomiser;
        public static System.Random Random = new();
        public static System.Collections.Generic.List<Patches.Boosted.BoostedPatch.UpgradeOption> CurrentUpgradeOptions = [];
        public static System.Collections.Generic.HashSet<(SpellName, string)> BannedUpgrades = [];


        private static GUIStyle Green, Red;
        private static GUIStyle CommonStyle, RareStyle, LegendaryStyle;

        private int upgradesSelected = 0;
        private readonly int MaxUpgrades = 3;
        private int freeBans = 1;  // per round


        private void Awake()
        {
            Instance = this;
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

            // ---------------- Boosted ----------------
            if (Loader.BoostedLoaded || Loader.BoostedWaiting)
            {
                if (GUI.Button(new Rect(x2 + textW + spacing, y1, w, h), "Unload Boosted"))
                    Loader.UnloadBoosted();
            }
            else
            {
                if (GUI.Button(new Rect(x2 + textW + spacing, y1, w, h), "Load Boosted"))
                {
                    Loader.LoadBoosted();
                }
            }

            // ---------------- Upgrade Options ----------------
            if (CurrentUpgradeOptions.Count > 0)
            {
                int upgradeX = 20;
                int upgradeY = Screen.height / 2 - 100;
                int optionHeight = 35;
                int buttonWidth = 50;
                int labelWidth = 180;
                int banButtonWidth = 40;

                GUI.Box(new Rect(upgradeX - 5, upgradeY - 20, labelWidth + buttonWidth * 2 + banButtonWidth + 15, CurrentUpgradeOptions.Count * optionHeight + 20), "Upgrades");

                for (int i = 0; i < CurrentUpgradeOptions.Count; i++)
                {
                    var option = CurrentUpgradeOptions[i];
                    int yPos = upgradeY + i * optionHeight;

                    GUI.Label(new Rect(upgradeX, yPos, labelWidth, 30), option.GetDisplayText(), GetTierStyle(option.Tier));

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

                    if (GUI.Button(new Rect(upgradeX + labelWidth + buttonWidth * 2 + 5, yPos, banButtonWidth, 30), "Ban"))
                    {
                        BanUpgrade(option);
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

            CurrentUpgradeOptions.Remove(option);

            MaybeResetUpgrades();
        }

        void BanUpgrade(Patches.Boosted.BoostedPatch.UpgradeOption option)
        {
            Log.LogInfo($"Banned: {option.Spell} + {option.Attribute}");
            BannedUpgrades.Add((option.Spell, option.Attribute));

            CurrentUpgradeOptions.Remove(option);

            if (freeBans <= 0)
                upgradesSelected++;
            else
                freeBans--;

            MaybeResetUpgrades();
        }

        void MaybeResetUpgrades()
        {
            if (upgradesSelected < MaxUpgrades) return;
            CurrentUpgradeOptions.Clear();
            upgradesSelected = 0; // reset for next time
            freeBans = 1;
        }

        private static void InitColors()
        {
            Color upColor = new(0.3f, 0.85f, 0.3f);
            Color downColor = new(0.9f, 0.3f, 0.3f);

            Green = new GUIStyle(GUI.skin.button);
            Green.normal.textColor = upColor;
            Green.hover.textColor = upColor;
            Green.active.textColor = upColor;
            Green.focused.textColor = upColor;

            Red = new GUIStyle(GUI.skin.button);
            Red.normal.textColor = downColor;
            Red.hover.textColor = downColor;
            Red.active.textColor = downColor;
            Red.focused.textColor = downColor;

            CommonStyle = new GUIStyle(GUI.skin.label);

            RareStyle = new GUIStyle(GUI.skin.label);
            RareStyle.normal.textColor = new Color(0.75f, 0.4f, 0.9f); // purple

            LegendaryStyle = new GUIStyle(GUI.skin.label);
            LegendaryStyle.normal.textColor = new Color(1.0f, 0.82f, 0.2f); // gold
        }
        
        private static GUIStyle GetTierStyle(Patches.Boosted.Upgrades.Tier tier)
        {
            if (tier.Equals(Patches.Boosted.Upgrades.Legendary))
                return LegendaryStyle;

            if (tier.Equals(Patches.Boosted.Upgrades.Rare))
                return RareStyle;

            return CommonStyle;
        }

    }
}