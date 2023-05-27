using HarmonyLib;
using Helpers;

namespace ShowCompanionRequirements
{
    [HarmonyPatch(typeof(QuestHelper), "CheckRosterForAlternativeSolution")]
    public static class ShowCompanionRequirementsHelper
    {
        public static void Postfix(int minimumTier, bool mountedRequired) => ShowCompanionRequirementsManager.Current.SetTierAndMounted(minimumTier, mountedRequired);
    }
}
