using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;

namespace ShowCompanionRequirements
{
    [HarmonyPatch(typeof(TooltipVMExtensions), "UpdateTooltip", new Type[] { typeof(TooltipVM), typeof(Hero) })]
    public class ShowCompanionRequirementsVMExtensions
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
        public static void AddCompanionRequirements(TooltipVM tooltipVM, Hero hero)
        {
            RequiredTroopCount = 0;
            MinimumTier = 0;
            MountedRequired = false;
            AllSkillsRequired = null;
            OneSkillRequired = null;
            List<Type> types = new List<Type>();
            types.AddRange(AccessTools.GetTypesFromAssembly(CampaignSystemAssembly));
            types.AddRange(AccessTools.GetTypesFromAssembly(SandBoxAssembly));
            foreach (Type type in types)
            {
                if (type == hero.Issue.GetType())
                {
                    object instance = AccessTools.CreateInstance(type);
                    bool hasAlternativeSolution = (bool)AccessTools.Property(type, "IsThereAlternativeSolution").GetValue(instance);
                    if (hasAlternativeSolution)
                    {
                        string suitableCompanions = null;
                        string skillName = null;
                        string skillValue = null;
                        bool isSpecialType = type.Name == "HeadmanVillageNeedsDraughtAnimalsIssue" || type.Name == "LandLordTheArtOfTheTradeIssue";
                        bool troopsSatisfy = (bool)AccessTools.Method(type, "DoTroopsSatisfyAlternativeSolution").Invoke(instance, new object[] { MobileParty.MainParty.MemberRoster, null });
                        bool skillsSatisfy = false;
                        List<TroopRosterElement> list = (from x in MobileParty.MainParty.MemberRoster.GetTroopRoster()
                                                         where x.Character.IsHero && !x.Character.IsPlayerCharacter && x.Character.HeroObject.CanHaveQuestsOrIssues()
                                                         select x).ToList<TroopRosterElement>();
                        for (int i = 0; i < list.Count; i++)
                        {
                            if ((bool)AccessTools.Method(type, "CompanionOrFamilyMemberClickableCondition").Invoke(instance, new object[] { list[i].Character.HeroObject, null }))
                            {
                                if (i + 1 != list.Count)
                                {
                                    suitableCompanions += list[i].Character.Name.ToString() + ",\n";
                                }
                                else
                                {
                                    suitableCompanions += list[i].Character.Name.ToString();
                                }
                                skillsSatisfy = true;
                            }
                        }
                        int duration = (int)AccessTools.Property(type, "AlternativeSolutionDurationInDays").GetValue(instance);
                        int countOfAllSkillsRequired = (AllSkillsRequired != null) ? AllSkillsRequired.Count : 0;
                        int countOfOneSkillRequired = (OneSkillRequired != null) ? OneSkillRequired.Count : 0;
                        if (isSpecialType)
                        {
                            RequiredTroopCount = (int)AccessTools.Property(type, "AlternativeSolutionNeededMenCount").GetValue(instance);
                            MinimumTier = 2;
                            skillName = DefaultSkills.Trade.Name.ToString();
                            skillValue = ((int)AccessTools.Property(type, "CompanionRequiredSkillLevel").GetValue(instance)).ToString();
                        }
                        tooltipVM.AddProperty("", "(" + hero.Issue.Title.ToString() + ")", 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddColoredProperty("Companion Requirements", "(Summary)", UIColors.Gold, 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                        tooltipVM.AddProperty("Days", duration.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddColoredProperty("Troops", "See below", troopsSatisfy ? UIColors.PositiveIndicator : UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddColoredProperty("Skills", "See below", skillsSatisfy ? UIColors.PositiveIndicator : UIColors.NegativeIndicator, 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Troops Required", " ", 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                        tooltipVM.AddProperty("Number", RequiredTroopCount.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Minimum Tier", MinimumTier.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Cavalry", MountedRequired ? "Yes" : "No", 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Skills Required", "(All of these)", 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                        if (countOfAllSkillsRequired > 0)
                        {
                            foreach (KeyValuePair<SkillObject, int> keyValuePair in AllSkillsRequired)
                            {
                                skillName = keyValuePair.Key.Name.ToString();
                                skillValue = keyValuePair.Value.ToString();
                                tooltipVM.AddProperty(skillName, skillValue, 0, TooltipProperty.TooltipPropertyFlags.None);
                            }
                        }
                        if (isSpecialType && Hero.MainHero.CompanionsInParty.Count() > 0)
                        {
                            tooltipVM.AddProperty(skillName, skillValue, 0, TooltipProperty.TooltipPropertyFlags.None);
                        }
                        tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Skills Required", "(One of these)", 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                        if (countOfOneSkillRequired > 0)
                        {
                            foreach (KeyValuePair<SkillObject, int> keyValuePair in OneSkillRequired)
                            {
                                skillName = keyValuePair.Key.Name.ToString();
                                skillValue = keyValuePair.Value.ToString();
                                tooltipVM.AddProperty(skillName, skillValue, 0, TooltipProperty.TooltipPropertyFlags.None);
                            }
                        }
                        tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.DefaultSeperator);
                        tooltipVM.AddProperty("Suitable Companions", suitableCompanions, 0, TooltipProperty.TooltipPropertyFlags.None);
                    }
                }
            }
        }
        public static int RequiredTroopCount { get; set; }
        public static int MinimumTier { get; set; }
        public static bool MountedRequired { get; set; }
        public static Dictionary<SkillObject, int> AllSkillsRequired { get; set; }
        public static Dictionary<SkillObject, int> OneSkillRequired { get; set; }
        public static Assembly CampaignSystemAssembly { get; set; }
        public static Assembly SandBoxAssembly { get; set; }
    }
}
