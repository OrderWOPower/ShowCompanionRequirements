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
        public static void Postfix(string ___ExtendKeyId, PropertyBasedTooltipVM propertyBasedTooltipVM, object[] args)
        {
            IssueBase issue = ((Hero)args[0]).Issue;

            if (issue != null && issue.IsThereAlternativeSolution)
            {
                if (!propertyBasedTooltipVM.IsExtended)
                {
                    propertyBasedTooltipVM.AddProperty("", "", -1, TooltipProperty.TooltipPropertyFlags.None);
                    GameTexts.SetVariable("EXTEND_KEY", propertyBasedTooltipVM.GetKeyText(___ExtendKeyId));
                    propertyBasedTooltipVM.AddProperty("", GameTexts.FindText("str_map_tooltip_info", null).ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                }
                else
                {
                    IssueModel issueModel = Campaign.Current.Models.IssueModel;
                    List<Hero> bestCompanions = new List<Hero>();
                    List<ValueTuple<SkillObject, int>> skills = new List<ValueTuple<SkillObject, int>>();
                    List<float> skillFulfillments = new List<float>();
                    ValueTuple<int, int> lowestCasualtyRate = new ValueTuple<int, int>();
                    int lowestDuration = issue.GetBaseAlternativeSolutionDurationInDays(), lowestTroopCount = issue.AlternativeSolutionBaseNeededMenCount, highestSuccessRate = 100;
                    float highestSkillFulfillment = 0f;
                    string namesOfBestCompanions = null;
                    bool isSpecial = issue is LandLordTheArtOfTheTradeIssueBehavior.LandLordTheArtOfTheTradeIssue;

                    foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster().Where(e => e.Character.IsHero && !e.Character.IsPlayerCharacter && e.Character.HeroObject.CanHaveQuestsOrIssues()))
                    {
                        Hero companion = troopRosterElement.Character.HeroObject;
                        ValueTuple<SkillObject, int> skill = issueModel.GetIssueAlternativeSolutionSkill(companion, issue);

                        bestCompanions.Add(companion);
                        skillFulfillments.Add(companion.GetSkillValue(skill.Item1) / (float)skill.Item2);

                        if (!skills.Contains(skill))
                        {
                            skills.Add(skill);
                        }
                    }

                    if (skillFulfillments.Any())
                    {
                        highestSkillFulfillment = skillFulfillments.Max();
                    }

                    foreach (Hero companion in bestCompanions.ToList())
                    {
                        ValueTuple<SkillObject, int> skill = issueModel.GetIssueAlternativeSolutionSkill(companion, issue);

                        if (companion.GetSkillValue(skill.Item1) / (float)skill.Item2 < highestSkillFulfillment)
                        {
                            bestCompanions.Remove(companion);
                        }
                        else
                        {
                            if (issue.AlternativeSolutionHasScaledDuration)
                            {
                                lowestDuration = (int)issueModel.GetDurationOfResolutionForHero(companion, issue).ToDays;
                            }

                            if (issue.AlternativeSolutionHasScaledRequiredTroops)
                            {
                                lowestTroopCount = issueModel.GetTroopsRequiredForHero(companion, issue);
                            }

                            if (issue.AlternativeSolutionHasCasualties)
                            {
                                lowestCasualtyRate = issueModel.GetCausalityForHero(companion, issue);
                            }

                            if (issue.AlternativeSolutionHasFailureRisk)
                            {
                                highestSuccessRate = 100 - (int)(issueModel.GetFailureRiskForHero(companion, issue) * 100f);
                            }
                        }
                    }

                    for (int i = 0; i < bestCompanions.Count; i++)
                    {
                        namesOfBestCompanions += i + 1 != bestCompanions.Count ? bestCompanions[i].Name.ToString() + ",\n " : bestCompanions[i].Name.ToString();
                    }

                    propertyBasedTooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                    propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements01}Companion Requirements").ToString(), new TextObject("{=ShowCompanionRequirements02}(Summary)").ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                    propertyBasedTooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                    // Add the issue's duration to the issue giver's tooltip.
                    propertyBasedTooltipVM.AddProperty(new TextObject("{=ShowCompanionRequirements03}Days").ToString(), lowestDuration.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                    propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements04}Troops").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), issue.DoTroopsSatisfyAlternativeSolution(MobileParty.MainParty.MemberRoster, out _) ? UIColors.PositiveIndicator : UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);

                    if (highestSkillFulfillment >= 1f)
                    {
                        propertyBasedTooltipVM.AddColoredProperty(new TextObject("{=ShowCompanionRequirements06}Skills").ToString(), new TextObject("{=ShowCompanionRequirements05}See below").ToString(), UIColors.PositiveIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                    }
                    else if (highestSkillFulfillment < 1f && highestSkillFulfillment >= 0.5f)
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

                    foreach (ValueTuple<SkillObject, int> skill in skills)
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
}
