using Microsoft.Extensions.Logging.Abstractions;
using REBUSS.Pure.Services;

namespace REBUSS.Pure.Tests.Services;

public class UnifiedDiffBuilderTests
{
    private readonly IUnifiedDiffBuilder _builder =
        new UnifiedDiffBuilder(new LcsDiffAlgorithm(), NullLogger<UnifiedDiffBuilder>.Instance);

    [Fact]
    public void Build_ReturnsEmpty_WhenBothContentIdentical()
    {
        var result = _builder.Build("/src/File.cs", "hello", "hello");
        Assert.Empty(result);
    }

    [Fact]
    public void Build_ReturnsEmpty_WhenBothNull()
    {
        var result = _builder.Build("/src/File.cs", null, null);
        Assert.Empty(result);
    }

    [Fact]
    public void Build_NewFile_ContainsNewFileMarker()
    {
        var result = _builder.Build("/src/New.cs", null, "line1\nline2");

        Assert.Contains("new file mode 100644", result);
        Assert.Contains("--- /dev/null", result);
        Assert.Contains("+++ b/src/New.cs", result);
        Assert.Contains("+line1", result);
        Assert.Contains("+line2", result);
    }

    [Fact]
    public void Build_DeletedFile_ContainsDeletedFileMarker()
    {
        var result = _builder.Build("/src/Old.cs", "line1\nline2", null);

        Assert.Contains("deleted file mode 100644", result);
        Assert.Contains("--- a/src/Old.cs", result);
        Assert.Contains("+++ /dev/null", result);
        Assert.Contains("-line1", result);
        Assert.Contains("-line2", result);
    }

    [Fact]
    public void Build_ModifiedFile_ContainsMinusAndPlusLines()
    {
        var result = _builder.Build("src/File.cs", "aaa\nbbb\nccc", "aaa\nBBB\nccc");

        Assert.Contains("--- a/src/File.cs", result);
        Assert.Contains("+++ b/src/File.cs", result);
        Assert.Contains("-bbb", result);
        Assert.Contains("+BBB", result);
    }

    [Fact]
    public void Build_StripsLeadingSlashFromPath()
    {
        var result = _builder.Build("/src/File.cs", null, "x");

        Assert.Contains("diff --git a/src/File.cs b/src/File.cs", result);
        Assert.DoesNotContain("//", result);
    }

    [Fact]
    public void Build_ContainsHunkHeader()
    {
        var result = _builder.Build("a.txt", "old", "new");

        Assert.Contains("@@", result);
    }

    [Fact]
    public void Build_HandlesCrlf()
    {
        var result = _builder.Build("a.txt", "aaa\r\nbbb", "aaa\r\nccc");

        Assert.Contains("-bbb", result);
        Assert.Contains("+ccc", result);
    }
}
