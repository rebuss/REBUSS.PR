using REBUSS.Pure.Services.LocalReview;

namespace REBUSS.Pure.Tests.Services.LocalReview;

public class LocalReviewScopeTests
{
    [Theory]
    [InlineData(null, LocalReviewScopeKind.WorkingTree, null)]
    [InlineData("", LocalReviewScopeKind.WorkingTree, null)]
    [InlineData("working-tree", LocalReviewScopeKind.WorkingTree, null)]
    [InlineData("WORKING-TREE", LocalReviewScopeKind.WorkingTree, null)]
    [InlineData("staged", LocalReviewScopeKind.Staged, null)]
    [InlineData("STAGED", LocalReviewScopeKind.Staged, null)]
    [InlineData("main", LocalReviewScopeKind.BranchDiff, "main")]
    [InlineData("origin/main", LocalReviewScopeKind.BranchDiff, "origin/main")]
    [InlineData("refs/heads/develop", LocalReviewScopeKind.BranchDiff, "refs/heads/develop")]
    public void Parse_ProducesCorrectScope(string? input, LocalReviewScopeKind expectedKind, string? expectedBase)
    {
        var scope = LocalReviewScope.Parse(input);

        Assert.Equal(expectedKind, scope.Kind);
        Assert.Equal(expectedBase, scope.BaseBranch);
    }

    [Fact]
    public void Staged_HasCorrectKind()
    {
        var scope = LocalReviewScope.Staged();
        Assert.Equal(LocalReviewScopeKind.Staged, scope.Kind);
        Assert.Null(scope.BaseBranch);
    }

    [Fact]
    public void WorkingTree_HasCorrectKind()
    {
        var scope = LocalReviewScope.WorkingTree();
        Assert.Equal(LocalReviewScopeKind.WorkingTree, scope.Kind);
        Assert.Null(scope.BaseBranch);
    }

    [Fact]
    public void BranchDiff_StoresBaseBranch()
    {
        var scope = LocalReviewScope.BranchDiff("main");
        Assert.Equal(LocalReviewScopeKind.BranchDiff, scope.Kind);
        Assert.Equal("main", scope.BaseBranch);
    }

    [Fact]
    public void ToString_WorkingTree_ReturnsExpectedString()
    {
        Assert.Equal("working-tree", LocalReviewScope.WorkingTree().ToString());
    }

    [Fact]
    public void ToString_Staged_ReturnsExpectedString()
    {
        Assert.Equal("staged", LocalReviewScope.Staged().ToString());
    }

    [Fact]
    public void ToString_BranchDiff_IncludesBaseBranch()
    {
        Assert.Equal("branch-diff:main", LocalReviewScope.BranchDiff("main").ToString());
    }
}
