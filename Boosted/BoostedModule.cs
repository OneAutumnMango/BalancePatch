using HarmonyLib;
using MageQuitModFramework.Modding;
using MageQuitModFramework.Data;
using MageQuitModFramework.Spells;

namespace MageKit.Boosted
{
    public class BoostedModule : BaseModule
    {
        public override string ModuleName => "Boosted";

        protected override void OnLoad(Harmony harmony)
        {
            BoostedPatch.PopulateManualModifierRejections();

            if (GameDataInitializer.IsLoaded)
                ApplyBoostedPatches(harmony);
            else
                GameDataInitializer.OnGameDataLoaded += () => ApplyBoostedPatches(harmony);
        }

        private void ApplyBoostedPatches(Harmony harmony)
        {
            Plugin.Log.LogInfo("Applying Boosted patches");
            BoostedPatch.PopulateSpellModifierTable();
            BoostedPatch.PatchAll(harmony);
            PatchGroup(harmony, typeof(BoostedPatch));
        }

        protected override void OnUnload(Harmony harmony)
        {
            BoostedPatch.ResetSpellModifierTableMults();
            Plugin.CurrentUpgradeOptions.Clear();
            SpellModificationSystem.ClearTable("boosted");
            harmony.UnpatchSelf();
        }
    }
}
