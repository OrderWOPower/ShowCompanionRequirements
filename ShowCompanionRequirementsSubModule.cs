using System.Reflection;
using HarmonyLib;
using TaleWorlds.MountAndBlade;

namespace ShowCompanionRequirements
{
    // This mod displays the companion requirements in the tooltips of notables who have available issues.
    public class ShowCompanionRequirementsSubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            new Harmony("mod.bannerlord.showcompanionrequirements").PatchAll();
            ShowCompanionRequirementsVMExtensions.CampaignSystemAssembly = Assembly.LoadFrom("TaleWorlds.CampaignSystem.dll");
            ShowCompanionRequirementsVMExtensions.SandBoxAssembly = Assembly.LoadFrom("..\\..\\Modules\\SandBox\\bin\\Win64_Shipping_Client\\SandBox.dll");
        }
    }
}
