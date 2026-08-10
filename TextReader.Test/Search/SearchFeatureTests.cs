using TextReader.TextInputs;

namespace TextReader.Test.Search;

public class SearchFeatureTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly string _testContent = "Hello World!\nHello there.\nWorld is great.";

    public SearchFeatureTests()
    {
        _testFilePath = Path.GetTempFileName();
        using var fs = new FileStream(_testFilePath, FileMode.Create, FileAccess.Write);
        byte[] bytes = Encoding.UTF8.GetBytes(_testContent);
        fs.Write(bytes);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public void Search_ShouldFindSingleOccurrence()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        var results = searchFeature.Search("there");

        Assert.Single(results);
        Assert.Equal(19, results[0]);
    }

    [Fact]
    public void Search_ShouldFindMultipleOccurrences()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        var results = searchFeature.Search("Hello");

        Assert.Equal(2, results.Count);
        Assert.Equal(0, results[0]);
        Assert.Equal(13, results[1]);
    }

    [Fact]
    public void Search_ShouldReturnEmptyListWhenNoMatch()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        var results = searchFeature.Search("NotFound");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_ShouldUseCacheOnRepeatedSearches()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        var results1 = searchFeature.Search("World");
        var results2 = searchFeature.Search("World");

        Assert.Same(results1, results2);
        Assert.Equal(2, results1.Count);
    }

    [Fact]
    public void Search_ShouldOptimizeWithCachedSubstring()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        // First search for "Hel"
        searchFeature.Search("Hel");

        // Search for "Hello" should use cached "Hel" results
        var results = searchFeature.Search("Hello");

        Assert.Equal(2, results.Count);
        Assert.Equal(0, results[0]);
        Assert.Equal(13, results[1]);
    }

    [Fact]
    public void Search_ShouldFindWordAtEndOfFile()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        var results = searchFeature.Search("great.");

        Assert.Single(results);
        Assert.Equal(_testContent.Length - 6, results[0]);
    }

    [Fact]
    public void Search_ShouldHandleEmptyString()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        var results = searchFeature.Search("");

        Assert.NotNull(results);
    }

    [Fact]
    public void Search_ShouldBeCaseSensitive()
    {
        using var input = new FileTextInput(_testFilePath);
        var searchFeature = new SearchFeature(input);

        var results = searchFeature.Search("hello");

        Assert.Empty(results);
    }
}
