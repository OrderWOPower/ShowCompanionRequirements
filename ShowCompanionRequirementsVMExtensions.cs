using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;

namespace ShowCompanionRequirements
{
    [HarmonyPatch(typeof(PropertyBasedTooltipVMExtensions), "UpdateTooltip", new Type[] { typeof(PropertyBasedTooltipVM), typeof(Hero), typeof(bool) })]
    public static class ShowCompanionRequirementsVMExtensions
    {
        // Add the issue's duration, required troops, and required companion skills to the issue giver's tooltip.
        public static void Postfix(PropertyBasedTooltipVM propertyBasedTooltipVM, Hero hero)
        {
            IssueBase issue = hero.Issue;
            if (issue != null && issue.IsThereAlternativeSolution && !hero.IsLord)
            {
                List<Hero> bestCompanions = new List<Hero>();
                List<ValueTuple<int, int>> casualtyRates = new List<ValueTuple<int, int>>();
                List<int> successRates = new List<int>();
                IssueModel issueModel = Campaign.Current.Models.IssueModel;
                ShowCompanionRequirementsManager manager = ShowCompanionRequirementsManager.Current;
                ValueTuple<int, int> lowestCasualtyRate = new ValueTuple<int, int>();
                ValueTuple<SkillObject, int> skill = new ValueTuple<SkillObject, int>();
                int highestSuccessRate = 0;
                string bestCompanionNames = null;
                bool isSpecial = issue is HeadmanVillageNeedsDraughtAnimalsIssueBehavior.HeadmanVillageNeedsDraughtAnimalsIssue || issue is LandLordTheArtOfTheTradeIssueBehavior.LandLordTheArtOfTheTradeIssue;
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
                        else
                        {
                            highestSuccessRate = 100;
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
                    bestCompanionNames += i + 1 != bestCompanions.Count ? bestCompanions[i].Name.ToString() + ",\n" : bestCompanionNames += bestCompanions[i].Name.ToString();
                }
                if (bestCompanions.Any())
                {
                    skill = issue.GetAlternativeSolutionSkill(bestCompanions.First());
                }
                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements01}Companion Requirements").ToString(), new TextObject("{=ShowCompanionRequirements02}(Summary)").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements03}Days").ToString(), issue.GetTotalAlternativeSolutionDurationInDays().ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements04}Troops").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), issue.DoTroopsSatisfyAlternativeSolution(MobileParty.MainParty.MemberRoster, out _) ? UIColors.PositiveIndicator : UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                if (highestSuccessRate == 100)
                {
                    propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.PositiveIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                else if (highestSuccessRate < 100 && highestSuccessRate >= 50)
                {
                    propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.Gold, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                else
                {
                    propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements07}Troops Required").ToString(), " ", 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements08}Number").ToString(), isSpecial ? manager.RequiredTroopCount.ToString() : issue.GetTotalAlternativeSolutionNeededMenCount().ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements09}Minimum Tier").ToString(), isSpecial ? manager.MinimumTier.ToString() : "2", 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements10}Cavalry").ToString(), manager.MountedRequired ? new TextObject("{=ShowCompanionRequirements11}Yes").ToString() : new TextObject("{=ShowCompanionRequirements12}No").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements13}Skills Required").ToString(), new TextObject("{=ShowCompanionRequirements14}(One of these)").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                propertyBasedTooltipVM.AddProperty(skill.Item1?.ToString(), skill.Item2.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements15}Best Companion(s)").ToString(), bestCompanionNames, 0, TooltipProperty.TooltipPropertyFlags.None);
                if (lowestCasualtyRate.Item2 > 0)
                {
                    propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements16}Projected Casualties").ToString(), lowestCasualtyRate.Item1 == lowestCasualtyRate.Item2 ? lowestCasualtyRate.Item1.ToString() : lowestCasualtyRate.Item1.ToString() + "-" + lowestCasualtyRate.Item2.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements17}Chance of Success").ToString(), highestSuccessRate.ToString() + "%", 0, TooltipProperty.TooltipPropertyFlags.None);
            }
        }
    }
}
