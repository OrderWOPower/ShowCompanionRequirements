using System;
using System.Collections.Generic;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace ShowCompanionRequirements
{
    [HarmonyPatch(typeof(QuestHelper))]
    public class ShowCompanionRequirementsHelper
    {
        [HarmonyPostfix]
        [HarmonyPatch("CheckRosterForAlternativeSolution")]
        public static void Postfix1(int requiredTroopCount, int minimumTier, bool mountedRequired)
        {
            ShowCompanionRequirementsVMExtensions.RequiredTroopCount = requiredTroopCount;
            ShowCompanionRequirementsVMExtensions.MinimumTier = minimumTier;
            ShowCompanionRequirementsVMExtensions.MountedRequired = mountedRequired;
        }
        [HarmonyPostfix]
        [HarmonyPatch("CheckCompanionForAlternativeSolution", new Type[] { typeof(CharacterObject), typeof(TextObject), typeof(Dictionary<SkillObject, int>), typeof(Dictionary<SkillObject, int>) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
        public static void Postfix2(Dictionary<SkillObject, int> shouldHaveAll, Dictionary<SkillObject, int> shouldHaveOneOfThem)
        {
            ShowCompanionRequirementsVMExtensions.AllSkillsRequired = shouldHaveAll;
            ShowCompanionRequirementsVMExtensions.OneSkillRequired = shouldHaveOneOfThem;
        }
    }
}
