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
        public static void AddCompanionRequirements(TooltipVM tooltipVM, Hero hero)
        {
            _requiredTroopCount = 0;
            _minimumTier = 0;
            _mountedRequired = false;
            _allSkillsRequired = null;
            _oneSkillRequired = null;
            List<Type> types = new List<Type>();
            types.AddRange(AccessTools.GetTypesFromAssembly(ShowCompanionRequirementsHelper.CampaignSystemAssembly));
            types.AddRange(AccessTools.GetTypesFromAssembly(ShowCompanionRequirementsHelper.SandBoxAssembly));
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
                        int countOfAllSkillsRequired = (_allSkillsRequired != null) ? _allSkillsRequired.Count : 0;
                        int countOfOneSkillRequired = (_oneSkillRequired != null) ? _oneSkillRequired.Count : 0;
                        if (isSpecialType)
                        {
                            _requiredTroopCount = (int)AccessTools.Property(type, "AlternativeSolutionNeededMenCount").GetValue(instance);
                            _minimumTier = 2;
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
                        tooltipVM.AddProperty("Number", _requiredTroopCount.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Minimum Tier", _minimumTier.ToString(), 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Cavalry", _mountedRequired ? "Yes" : "No", 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty(string.Empty, string.Empty, -1, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("Skills Required", "(All of these)", 0, TooltipProperty.TooltipPropertyFlags.None);
                        tooltipVM.AddProperty("", "", 0, TooltipProperty.TooltipPropertyFlags.RundownSeperator);
                        if (countOfAllSkillsRequired > 0)
                        {
                            foreach (KeyValuePair<SkillObject, int> keyValuePair in _allSkillsRequired)
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
                            foreach (KeyValuePair<SkillObject, int> keyValuePair in _oneSkillRequired)
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
        public static void SetTroopRequirements(int requiredTroopCount, int minimumTier, bool mountedRequired)
        {
            _requiredTroopCount = requiredTroopCount;
            _minimumTier = minimumTier;
            _mountedRequired = mountedRequired;
        }
        public static void SetSkillRequirements(Dictionary<SkillObject, int> shouldHaveAll, Dictionary<SkillObject, int> shouldHaveOneOfThem)
        {
            _allSkillsRequired = shouldHaveAll;
            _oneSkillRequired = shouldHaveOneOfThem;
        }
        private static int _requiredTroopCount;
        private static int _minimumTier;
        private static bool _mountedRequired;
        private static Dictionary<SkillObject, int> _allSkillsRequired;
        private static Dictionary<SkillObject, int> _oneSkillRequired;
    }
}
