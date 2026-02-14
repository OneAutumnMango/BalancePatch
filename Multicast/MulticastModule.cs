using HarmonyLib;
using MageQuitModFramework.Modding;

namespace MageKit.Multicast
{
    public class MulticastModule : BaseModule
    {
        public override string ModuleName => "Multicast";

        protected override void OnLoad(Harmony harmony)
        {
            PatchGroup(harmony, typeof(MulticastPatch));
        }

        protected override void OnUnload(Harmony harmony)
        {
            harmony.UnpatchSelf();
        }
    }
}