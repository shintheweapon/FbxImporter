using FbxImporter.Util;

namespace FbxImporter.Tests;

public class ErrorMessagesTests
{
    [Fact]
    public void AMissingFileNamesItsPath() =>
        Assert.Equal("File not found: x.fbx", ErrorMessages.Describe(new FileNotFoundException("anything", "x.fbx")));

    [Fact]
    public void AMissingFileWithoutAPathKeepsItsMessage() =>
        Assert.Equal("gone", ErrorMessages.Describe(new FileNotFoundException("gone")));

    [Fact]
    public void AMissingFolderKeepsItsMessage() =>
        Assert.Equal("no folder", ErrorMessages.Describe(new DirectoryNotFoundException("no folder")));

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(InvalidDataException))]
    public void AnExpectedFailureIsOneLine(Type type)
    {
        var e = (Exception)Activator.CreateInstance(type, "the reason")!;
        Assert.Equal("the reason", ErrorMessages.Describe(e));
    }

    [Fact]
    public void AnUnexpectedFailureKeepsItsStackTrace()
    {
        Exception e;
        try { throw new NullReferenceException("a bug"); }
        catch (Exception caught) { e = caught; }

        string text = ErrorMessages.Describe(e);

        Assert.StartsWith("System.NullReferenceException: a bug", text);
        Assert.Contains("   at ", text);
    }
}
