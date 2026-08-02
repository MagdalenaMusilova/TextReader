using TextReader.TextInputs;

namespace TextReader.Test.TextInputs;

public class UrlTextInputTests
{
    private const string TestUrl = "https://raw.githubusercontent.com/microsoft/TypeScript/main/README.md";

    [Fact]
    public void Constructor_ShouldInitializeWithValidUrl()
    {
        var input = new UrlTextInput(TestUrl);

        Assert.True(input.Length > 0);
        Assert.Equal(0, input.Position);
        Assert.False(input.EOF);
    }

    [Fact]
    public void Seek_ShouldChangePosition()
    {
        var input = new UrlTextInput(TestUrl);

        input.Seek(10);
        Assert.Equal(10, input.Position);
    }

    [Fact]
    public void ReadByte_ShouldReadData()
    {
        var input = new UrlTextInput(TestUrl);

        var firstByte = input.ReadByte();
        Assert.True(firstByte >= 0);
        Assert.Equal(1, input.Position);
    }

    [Fact]
    public void Read_ShouldReadSpecifiedAmount()
    {
        var input = new UrlTextInput(TestUrl);

        var result = input.Read(10);
        Assert.Equal(10, result.Length);
        Assert.Equal(10, input.Position);
    }

    [Fact]
    public void Copy_ShouldCreateIndependentCopy()
    {
        var original = new UrlTextInput(TestUrl);
        Thread.Sleep(100); // Allow initialization
        original.Seek(5);

        var copy = original.Copy();

        Assert.NotSame(original, copy);
        Assert.Equal(original.Length, copy.Length);
    }

    [Fact]
    public void EOF_ShouldReflectUnderlyingInput()
    {
        var input = new UrlTextInput(TestUrl);
        Thread.Sleep(100); // Allow initialization

        Assert.False(input.EOF);

        input.Seek(input.Length);
        Assert.True(input.EOF);
    }

    [Fact]
    public void Position_ShouldReflectUnderlyingInput()
    {
        var input = new UrlTextInput(TestUrl);
        Thread.Sleep(100); // Allow initialization

        input.Seek(50);
        Assert.Equal(50, input.Position);
    }

    [Fact]
    public void Length_ShouldReturnCorrectValue()
    {
        var input = new UrlTextInput(TestUrl);
        Thread.Sleep(100); // Allow initialization

        Assert.True(input.Length > 0);
    }
}
