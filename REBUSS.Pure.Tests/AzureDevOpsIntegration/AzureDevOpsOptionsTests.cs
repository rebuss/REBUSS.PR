using REBUSS.Pure.AzureDevOpsIntegration.Configuration;
using Microsoft.Extensions.Options;

namespace REBUSS.Pure.Tests.AzureDevOpsIntegration;

public class AzureDevOpsOptionsValidatorTests
{
    private readonly AzureDevOpsOptionsValidator _validator = new();

    private static AzureDevOpsOptions CreateValid() => new()
    {
        OrganizationName = "Org",
        ProjectName = "Proj",
        RepositoryName = "Repo",
        PersonalAccessToken = "pat"
    };

    [Fact]
    public void Validate_Succeeds_WhenAllFieldsProvided()
    {
        var result = _validator.Validate(null, CreateValid());
        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(nameof(AzureDevOpsOptions.OrganizationName))]
    [InlineData(nameof(AzureDevOpsOptions.ProjectName))]
    [InlineData(nameof(AzureDevOpsOptions.RepositoryName))]
    [InlineData(nameof(AzureDevOpsOptions.PersonalAccessToken))]
    public void Validate_Fails_WhenRequiredFieldMissing(string fieldName)
    {
        var options = CreateValid();
        typeof(AzureDevOpsOptions)
            .GetProperty(fieldName)!
            .SetValue(options, string.Empty);

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(fieldName, result.FailureMessage);
    }

    [Fact]
    public void Validate_ReportsAllFailures_WhenMultipleFieldsMissing()
    {
        var options = new AzureDevOpsOptions();

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(nameof(AzureDevOpsOptions.OrganizationName), result.FailureMessage);
        Assert.Contains(nameof(AzureDevOpsOptions.PersonalAccessToken), result.FailureMessage);
    }
}
