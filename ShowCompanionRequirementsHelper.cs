using HarmonyLib;
using Helpers;

namespace ShowCompanionRequirements
{
    [HarmonyPatch(typeof(QuestHelper), "CheckRosterForAlternativeSolution")]
    public static class ShowCompanionRequirementsHelper
    {
        public static void Postfix(int requiredTroopCount, int minimumTier, bool mountedRequired) => ShowCompanionRequirementsVMExtensions.SetRequiredTroops(requiredTroopCount, minimumTier, mountedRequired);
    }
}
