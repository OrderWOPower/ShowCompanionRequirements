using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;

namespace ShowCompanionRequirements
{
    [HarmonyPatch(typeof(TooltipVMExtensions), "UpdateTooltip", new Type[] { typeof(TooltipVM), typeof(Hero) })]
    public static class ShowCompanionRequirementsVMExtensions
    {
        // Get the issue giver and find the position in their tooltip to add the companion requirements.
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            List<CodeInstruction> codesToInsert = new List<CodeInstruction>();
            int index = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].operand is "str_hero_has_issue")
                {
                    index = i + 7;
                }
            }
            codesToInsert.Add(new CodeInstruction(OpCodes.Ldarg_0));
            codesToInsert.Add(new CodeInstruction(OpCodes.Ldarg_1));
            codesToInsert.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ShowCompanionRequirementsVMExtensions), "AddCompanionRequirements", new Type[] { typeof(TooltipVM), typeof(Hero) })));
            codes.InsertRange(index, codesToInsert);
            return codes;
        }
        // Add the issue's duration, required troops, and required companion skills to the issue giver's tooltip.
        public static void AddCompanionRequirements(TooltipVM tooltipVM, Hero issueGiver)
        {
            _requiredTroopCount = 0;
            _minimumTier = 0;
            _mountedRequired = false;
            IssueBase issue = issueGiver.Issue;
            if (issue.IsThereAlternativeSolution)
            {
                List<Hero> bestCompanions = new List<Hero>();
                List<ValueTuple<int, int>> casualtyRates = new List<ValueTuple<int, int>>();
                List<int> successRates = new List<int>();
                IssueModel issueModel = Campaign.Current.Models.IssueModel;
                ValueTuple<int, int> lowestCasualtyRate = new ValueTuple<int, int>();
                int highestSuccessRate = 0;
                string bestCompanionNames = null;
                bool isSpecialType = issue.GetType().Name == "HeadmanVillageNeedsDraughtAnimalsIssue" || issue.GetType().Name == "LandLordTheArtOfTheTradeIssue";
                foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
                {
                    if (troopRosterElement.Character.IsHero && !troopRosterElement.Character.IsPlayerCharacter && troopRosterElement.Character.HeroObject.CanHaveQuestsOrIssues())
                    {
                        Hero companion = troopRosterElement.Character.HeroObject;
                        bestCompanions.Add(companion);
                        if (issue.AlternativeSolutionHasCasualties)
                        {
                            casualtyRates.Add(issueModel.GetCausalityForHero(companion, issue));
                        }
                        if (issue.AlternativeSolutionHasFailureRisk)
                        {
                            successRates.Add(100 - (int)(issueModel.GetFailureRiskForHero(companion, issue) * 100f));
                        }
                    }
                }
                if (casualtyRates.Any())
                {
                    lowestCasualtyRate = casualtyRates.Min();
                }
                if (successRates.Any())
                {
                    highestSuccessRate = successRates.Max();
                }
                foreach (Hero companion in bestCompanions.ToList())
                {
                    if (issue.AlternativeSolutionHasFailureRisk && 100 - (int)(issueModel.GetFailureRiskForHero(companion, issue) * 100f) < highestSuccessRate)
                    {
                        bestCompanions.Remove(companion);
                    }
                }
                for (int i = 0; i < bestCompanions.Count; i++)
                {
                    if (i + 1 != bestCompanions.Count)
                    {
                        bestCompanionNames += bestCompanions[i].Name.ToString() + ",\n";
                    }
                    else
                    {
                        bestCompanionNames += bestCompanions[i].Name.ToString();
                    }
                }
                if (isSpecialType)
                {
                    _requiredTroopCount = issue.GetTotalAlternativeSolutionNeededMenCount();
                    _minimumTier = 2;
                }
                tooltipVM.AddProperty("", "(" + issue.Title.ToString() + ")", 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddColoredProperty("Companion Requirements", "(Summary)", UIColors.Gold, 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                tooltipVM.AddProperty("Days", issue.GetTotalAlternativeSolutionDurationInDays().ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddColoredProperty("Troops", "See below", issue.DoTroopsSatisfyAlternativeSolution(MobileParty.MainParty.MemberRoster, out _) ? UIColors.PositiveIndicator : UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("Skills", "See below", 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("Troops Required", " ", 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                tooltipVM.AddProperty("Number", _requiredTroopCount.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("Minimum Tier", _minimumTier.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("Cavalry", _mountedRequired ? "Yes" : "No", 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("Skills Required", "(One of these)", 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                foreach (SkillObject skillObject in issue.GetAlternativeSolutionRequiredCompanionSkill(out int requiredSkillLevel))
                {
                    tooltipVM.AddProperty(skillObject.Name.ToString(), requiredSkillLevel.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
                tooltipVM.AddProperty("Best Companion(s)", bestCompanionNames, 0, TooltipProperty.TooltipPropertyFlags.None);
                if (lowestCasualtyRate.Item2 > 0)
                {
                    tooltipVM.AddProperty("Projected Casualties", lowestCasualtyRate.Item1 == lowestCasualtyRate.Item2 ? lowestCasualtyRate.Item1.ToString() : lowestCasualtyRate.Item1.ToString() + "-" + lowestCasualtyRate.Item2.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                if (highestSuccessRate > 0)
                {
                    tooltipVM.AddProperty("Chance of Success", highestSuccessRate.ToString() + "%", 0, TooltipProperty.TooltipPropertyFlags.None);
                }
            }
        }
        public static void SetRequiredTroops(int requiredTroopCount, int minimumTier, bool mountedRequired)
        {
            _requiredTroopCount = requiredTroopCount;
            _minimumTier = minimumTier;
            _mountedRequired = mountedRequired;
        }
        private static int _requiredTroopCount;
        private static int _minimumTier;
        private static bool _mountedRequired;
    }
}
