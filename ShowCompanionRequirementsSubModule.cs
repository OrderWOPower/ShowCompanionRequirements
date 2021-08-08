using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace ShowCompanionRequirements
{
    // This mod displays the companion requirements in the tooltips of notables who have available issues.
    public class ShowCompanionRequirementsSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad() => new Harmony("mod.bannerlord.showcompanionrequirements").PatchAll();
    }
}
