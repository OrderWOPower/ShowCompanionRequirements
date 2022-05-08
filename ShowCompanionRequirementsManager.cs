namespace ShowCompanionRequirements
{
    public class ShowCompanionRequirementsManager
    {
        private static readonly ShowCompanionRequirementsManager _showCompanionRequirementsManager = new ShowCompanionRequirementsManager();

        public static ShowCompanionRequirementsManager Current => _showCompanionRequirementsManager;

        public int RequiredTroopCount { get; set; }

        public int MinimumTier { get; set; }

        public bool MountedRequired { get; set; }

        public void SetRequiredTroops(int requiredTroopCount, int minimumTier, bool mountedRequired)
        {
            RequiredTroopCount = requiredTroopCount;
            MinimumTier = minimumTier;
            MountedRequired = mountedRequired;
        }
    }
}
