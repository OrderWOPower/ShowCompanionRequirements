using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.Issues;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Localization;

namespace ShowCompanionRequirements
{
    [HarmonyPatch(typeof(TooltipVMExtensions), "UpdateTooltip", new Type[] { typeof(TooltipVM), typeof(Hero) })]
    public static class ShowCompanionRequirementsVMExtensions
    {
        // Add the issue's duration, required troops, and required companion skills to the issue giver's tooltip.
        public static void Postfix(TooltipVM tooltipVM, Hero hero)
        {
            _requiredTroopCount = 0;
            _minimumTier = 0;
            _mountedRequired = false;
            IssueBase issue = hero.Issue;
            if (issue != null && issue.IsThereAlternativeSolution)
            {
                List<Hero> bestCompanions = new List<Hero>();
                List<ValueTuple<int, int>> casualtyRates = new List<ValueTuple<int, int>>();
                List<int> successRates = new List<int>();
                IssueModel issueModel = Campaign.Current.Models.IssueModel;
                ValueTuple<int, int> lowestCasualtyRate = new ValueTuple<int, int>();
                int highestSuccessRate = 0;
                string bestCompanionNames = null;
                bool isSpecialType = issue.GetType() == typeof(HeadmanVillageNeedsDraughtAnimalsIssueBehavior.HeadmanVillageNeedsDraughtAnimalsIssue) || issue.GetType() == typeof(LandLordTheArtOfTheTradeIssueBehavior.LandLordTheArtOfTheTradeIssue);
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
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements01}Companion Requirements").ToString(), new TextObject("{=ShowCompanionRequirements02}(Summary)").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements03}Days").ToString(), issue.GetTotalAlternativeSolutionDurationInDays().ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements04}Troops").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), issue.DoTroopsSatisfyAlternativeSolution(MobileParty.MainParty.MemberRoster, out _) ? UIColors.PositiveIndicator : UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                if (highestSuccessRate < 50)
                {
                    tooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                else if (highestSuccessRate >= 50 && highestSuccessRate < 100)
                {
                    tooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.Gold, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                else
                {
                    tooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.PositiveIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements07}Troops Required").ToString(), " ", 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements08}Number").ToString(), _requiredTroopCount.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements09}Minimum Tier").ToString(), _minimumTier.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements10}Cavalry").ToString(), _mountedRequired ? new TextObject("{=ShowCompanionRequirements11}Yes").ToString() : new TextObject("{=ShowCompanionRequirements12}No").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements13}Skills Required").ToString(), new TextObject("{=ShowCompanionRequirements14}(One of these)").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                foreach (SkillObject skillObject in issue.GetAlternativeSolutionRequiredCompanionSkill(out int requiredSkillLevel))
                {
                    tooltipVM.AddProperty(skillObject.Name.ToString(), requiredSkillLevel.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements15}Best Companion(s)").ToString(), bestCompanionNames, 0, TooltipProperty.TooltipPropertyFlags.None);
                if (lowestCasualtyRate.Item2 > 0)
                {
                    tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements16}Projected Casualties").ToString(), lowestCasualtyRate.Item1 == lowestCasualtyRate.Item2 ? lowestCasualtyRate.Item1.ToString() : lowestCasualtyRate.Item1.ToString() + "-" + lowestCasualtyRate.Item2.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                tooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements17}Chance of Success").ToString(), highestSuccessRate.ToString() + "%", 0, TooltipProperty.TooltipPropertyFlags.None);
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
