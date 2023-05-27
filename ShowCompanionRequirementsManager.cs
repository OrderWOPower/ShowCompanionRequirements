namespace ShowCompanionRequirements
{
    public class ShowCompanionRequirementsManager
    {
        private static readonly ShowCompanionRequirementsManager _showCompanionRequirementsManager = new ShowCompanionRequirementsManager();

        public static ShowCompanionRequirementsManager Current => _showCompanionRequirementsManager;

        public int MinimumTier { get; set; }

        public bool MountedRequired { get; set; }

        public void SetTierAndMounted(int minimumTier, bool mountedRequired)
        {
            MinimumTier = minimumTier;
            MountedRequired = mountedRequired;
        }
    }
}
