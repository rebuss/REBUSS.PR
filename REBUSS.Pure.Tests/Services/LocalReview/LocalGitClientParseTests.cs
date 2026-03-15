using Microsoft.Extensions.Logging.Abstractions;
using REBUSS.Pure.Services.LocalReview;

namespace REBUSS.Pure.Tests.Services.LocalReview;

/// <summary>
/// Tests for the parsing logic in <see cref="LocalGitClient"/> that do not require
/// a real git executable. The parsing methods are internal and exercised indirectly
/// via reflection or through the public API with fake process output where feasible.
/// Since git invocation cannot be easily mocked without a seam, these tests focus on
/// the pure parsing of git output strings using the testable static helpers.
/// </summary>
public class LocalGitClientParseTests
{
    // The static parsing helpers are internal — accessible via InternalsVisibleTo.
    // We exercise them by invoking GetChangedFilesAsync against a fake git result
    // through a subclass that overrides process execution.

    [Theory]
    [InlineData("M  src/File.cs", 'M', "src/File.cs")]
    [InlineData("A  src/New.cs", 'A', "src/New.cs")]
    [InlineData("D  src/Old.cs", 'D', "src/Old.cs")]
    [InlineData(" M src/Modified.cs", 'M', "src/Modified.cs")]
    [InlineData("?? src/Untracked.cs", '?', "src/Untracked.cs")]
    public void ParsePorcelainStatus_ParsesStatusAndPath(
        string line, char expectedStatus, string expectedPath)
    {
        var parsed = InvokeParsePorcelain(line);
        Assert.Single(parsed);
        Assert.Equal(expectedStatus, parsed[0].Status);
        Assert.Equal(expectedPath, parsed[0].Path);
    }

    [Fact]
    public void ParsePorcelainStatus_HandlesRename()
    {
        var line = "R  old/Path.cs -> new/Path.cs";
        var parsed = InvokeParsePorcelain(line);
        Assert.Single(parsed);
        Assert.Equal('R', parsed[0].Status);
        Assert.Equal("new/Path.cs", parsed[0].Path);
        Assert.Equal("old/Path.cs", parsed[0].OriginalPath);
    }

    [Fact]
    public void ParsePorcelainStatus_IgnoresEmptyLines()
    {
        var parsed = InvokeParsePorcelain("   \n\n");
        Assert.Empty(parsed);
    }

    [Fact]
    public void ParseNameStatus_ParsesModified()
    {
        var line = "M\tsrc/File.cs";
        var parsed = InvokeParseNameStatus(line);
        Assert.Single(parsed);
        Assert.Equal('M', parsed[0].Status);
        Assert.Equal("src/File.cs", parsed[0].Path);
        Assert.Null(parsed[0].OriginalPath);
    }

    [Fact]
    public void ParseNameStatus_ParsesRenamed()
    {
        var line = "R95\told/Path.cs\tnew/Path.cs";
        var parsed = InvokeParseNameStatus(line);
        Assert.Single(parsed);
        Assert.Equal('R', parsed[0].Status);
        Assert.Equal("new/Path.cs", parsed[0].Path);
        Assert.Equal("old/Path.cs", parsed[0].OriginalPath);
    }

    [Fact]
    public void ParseNameStatus_ParsesAdded()
    {
        var line = "A\tsrc/NewFile.cs";
        var parsed = InvokeParseNameStatus(line);
        Assert.Single(parsed);
        Assert.Equal('A', parsed[0].Status);
    }

    [Fact]
    public void ParseNameStatus_ParsesDeleted()
    {
        var line = "D\tsrc/Removed.cs";
        var parsed = InvokeParseNameStatus(line);
        Assert.Single(parsed);
        Assert.Equal('D', parsed[0].Status);
    }

    [Fact]
    public void ParseNameStatus_IgnoresEmptyLines()
    {
        var parsed = InvokeParseNameStatus("\n\n");
        Assert.Empty(parsed);
    }

    // --- Helpers that invoke internal parsing via reflection ---

    private static IReadOnlyList<LocalFileStatus> InvokeParsePorcelain(string output)
    {
        var method = typeof(LocalGitClient).GetMethod(
            "ParsePorcelainStatus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (IReadOnlyList<LocalFileStatus>)method.Invoke(null, new object[] { output })!;
    }

    private static IReadOnlyList<LocalFileStatus> InvokeParseNameStatus(string output)
    {
        var method = typeof(LocalGitClient).GetMethod(
            "ParseNameStatusOutput",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        return (IReadOnlyList<LocalFileStatus>)method.Invoke(null, new object[] { output })!;
    }
}
