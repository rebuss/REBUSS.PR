using Microsoft.Extensions.Options;

namespace REBUSS.Pure.AzureDevOpsIntegration.Configuration
{
    public class AzureDevOpsOptionsValidator : IValidateOptions<AzureDevOpsOptions>
    {
        public ValidateOptionsResult Validate(string? name, AzureDevOpsOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.OrganizationName))
                failures.Add($"{nameof(AzureDevOpsOptions.OrganizationName)} is required in {AzureDevOpsOptions.SectionName} configuration");

            if (string.IsNullOrWhiteSpace(options.ProjectName))
                failures.Add($"{nameof(AzureDevOpsOptions.ProjectName)} is required in {AzureDevOpsOptions.SectionName} configuration");

            if (string.IsNullOrWhiteSpace(options.RepositoryName))
                failures.Add($"{nameof(AzureDevOpsOptions.RepositoryName)} is required in {AzureDevOpsOptions.SectionName} configuration");

            if (string.IsNullOrWhiteSpace(options.PersonalAccessToken))
                failures.Add($"{nameof(AzureDevOpsOptions.PersonalAccessToken)} is required in {AzureDevOpsOptions.SectionName} configuration");

            return failures.Count > 0
                ? ValidateOptionsResult.Fail(failures)
                : ValidateOptionsResult.Success;
        }
    }
}
