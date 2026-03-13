namespace REBUSS.Pure.AzureDevOpsIntegration.Configuration
{
    public class AzureDevOpsOptions
    {
        public const string SectionName = "AzureDevOps";

        public string OrganizationName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string RepositoryName { get; set; } = string.Empty;
        public string PersonalAccessToken { get; set; } = string.Empty;
    }
}
