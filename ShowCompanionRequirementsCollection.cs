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
    [HarmonyPatch(typeof(TooltipRefresherCollection), "RefreshHeroTooltip")]
    public static class ShowCompanionRequirementsCollection
    {
        public static void Postfix(PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
        {
            Hero hero = (Hero)args[0];
            IssueBase issue = hero.Issue;

            if (issue != null && issue.IsThereAlternativeSolution && !hero.IsLord)
            {
                IssueModel issueModel = Campaign.Current.Models.IssueModel;
                List<Hero> bestCompanions = new List<Hero>();
                List<ValueTuple<int, int>> casualtyRates = new List<ValueTuple<int, int>>();
                List<int> durations = new List<int>(), troopCounts = new List<int>(), successRates = new List<int>();
                ValueTuple<SkillObject, int> skill = new ValueTuple<SkillObject, int>();
                ValueTuple<int, int> lowestCasualtyRate = new ValueTuple<int, int>();
                int lowestDuration = issue.GetBaseAlternativeSolutionDurationInDays(), lowestTroopCount = issue.AlternativeSolutionBaseNeededMenCount, highestSuccessRate = 100;
                float skillFulfillment = 0f;
                string namesOfBestCompanions = null;
                bool isSpecial = issue is LandLordTheArtOfTheTradeIssueBehavior.LandLordTheArtOfTheTradeIssue;

                foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(e => e.Character.IsHero && !e.Character.IsPlayerCharacter && e.Character.HeroObject.CanHaveQuestsOrIssues()))
                {
                    Hero companion = troopRosterElement.Character.HeroObject;

                    bestCompanions.Add(companion);

                    if (issue.AlternativeSolutionHasScaledDuration)
                    {
                        durations.Add((int)issueModel.GetDurationOfResolutionForHero(companion, issue).ToDays);
                    }

                    if (issue.AlternativeSolutionHasScaledRequiredTroops)
                    {
                        troopCounts.Add(issueModel.GetTroopsRequiredForHero(companion, issue));
                    }

                    if (issue.AlternativeSolutionHasCasualties)
                    {
                        casualtyRates.Add(issueModel.GetCausalityForHero(companion, issue));
                    }

                    if (issue.AlternativeSolutionHasFailureRisk)
                    {
                        successRates.Add(100 - (int)(issueModel.GetFailureRiskForHero(companion, issue) * 100f));
                    }
                }

                if (durations.Any())
                {
                    lowestDuration = durations.Min();
                }

                if (troopCounts.Any())
                {
                    lowestTroopCount = troopCounts.Min();
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
                    bool hasLowestDuration = !issue.AlternativeSolutionHasScaledDuration || (int)issueModel.GetDurationOfResolutionForHero(companion, issue).ToDays == lowestDuration;
                    bool hasLowestTroopCount = !issue.AlternativeSolutionHasScaledRequiredTroops || issueModel.GetTroopsRequiredForHero(companion, issue) == lowestTroopCount;
                    bool hasLowestCasualtyRate = !issue.AlternativeSolutionHasCasualties || issueModel.GetCausalityForHero(companion, issue) == lowestCasualtyRate;
                    bool hasHighestSuccessRate = !issue.AlternativeSolutionHasFailureRisk || 100 - (int)(issueModel.GetFailureRiskForHero(companion, issue) * 100f) == highestSuccessRate;

                    if (!hasLowestDuration || !hasLowestTroopCount || !hasLowestCasualtyRate || !hasHighestSuccessRate)
                    {
                        bestCompanions.Remove(companion);
                    }
                }

                for (int i = 0; i < bestCompanions.Count; i++)
                {
                    namesOfBestCompanions += i + 1 != bestCompanions.Count ? bestCompanions[i].Name.ToString() + ",\n " : bestCompanions[i].Name.ToString();
                }

                if (bestCompanions.Any())
                {
                    skill = issueModel.GetIssueAlternativeSolutionSkill(bestCompanions.First(), issue);
                    skillFulfillment = bestCompanions.First().GetSkillValue(skill.Item1) / (float)skill.Item2;
                }

                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements01}Companion Requirements").ToString(), new TextObject("{=ShowCompanionRequirements02}(Summary)").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                // Add the issue's duration to the issue giver's tooltip.
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements03}Days").ToString(), lowestDuration.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements04}Troops").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), issue.DoTroopsSatisfyAlternativeSolution(MobileParty.MainParty.MemberRoster, out _) ? UIColors.PositiveIndicator : UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);

                if (skillFulfillment >= 1f)
                {
                    propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.PositiveIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                else if (skillFulfillment < 1f && skillFulfillment >= 0.5f)
                {
                    propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.Gold, 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                else
                {
                    propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                }

                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                // Add the issue's required troops to the issue giver's tooltip.
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements07}Troops Required").ToString(), " ", 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements08}Number").ToString(), lowestTroopCount.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements09}Minimum Tier").ToString(), isSpecial ? ShowCompanionRequirementsManager.Current.MinimumTier.ToString() : "2", 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements10}Cavalry").ToString(), !isSpecial && ShowCompanionRequirementsManager.Current.MountedRequired ? new TextObject("{=ShowCompanionRequirements11}Yes").ToString() : new TextObject("{=ShowCompanionRequirements12}No").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                // Add the issue's required skills to the issue giver's tooltip.
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements13}Skills Required").ToString(), new TextObject("{=ShowCompanionRequirements14}(One of these)").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);

                if (skill.Item1 != null)
                {
                    propertyBasedTooltipVM.AddProperty(skill.Item1.ToString(), skill.Item2.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }

                propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements15}Best Companion(s)").ToString(), namesOfBestCompanions, 0, TooltipProperty.TooltipPropertyFlags.None);

                if (lowestCasualtyRate.Item2 > 0)
                {
                    propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements16}Projected Casualties").ToString(), lowestCasualtyRate.Item1 == lowestCasualtyRate.Item2 ? lowestCasualtyRate.Item1.ToString() : lowestCasualtyRate.Item1.ToString() + " - " + lowestCasualtyRate.Item2.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }

                propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements17}Chance of Success").ToString(), highestSuccessRate.ToString() + "%", 0, TooltipProperty.TooltipPropertyFlags.None);
            }
        }
    }
}
