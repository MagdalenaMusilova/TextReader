using TextReader.TextInputs;

namespace TextReader.Test.TextInputs;

public class OnlyUrlTextInputTests
{
    private const string TestUrl = "https://raw.githubusercontent.com/microsoft/TypeScript/main/README.md";
    private const long TestLength = 1000;
    private readonly Encoding _testEncoding = Encoding.UTF8;

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        var input = new OnlyUrlTextInput(TestUrl, TestLength, _testEncoding);

        Assert.Equal(TestLength, input.Length);
        Assert.Equal(0, input.Position);
        Assert.False(input.EOF);
    }

    [Fact]
    public void Seek_ShouldChangePosition()
    {
        var input = new OnlyUrlTextInput(TestUrl, TestLength, _testEncoding);

        input.Seek(100);
        Assert.Equal(100, input.Position);
    }

    [Fact]
    public void EOF_ShouldBeTrueWhenPositionEqualsLength()
    {
        var input = new OnlyUrlTextInput(TestUrl, TestLength, _testEncoding);

        input.Seek(TestLength);
        Assert.True(input.EOF);
    }

    [Fact]
    public void EOF_ShouldBeFalseBeforeEnd()
    {
        var input = new OnlyUrlTextInput(TestUrl, TestLength, _testEncoding);

        input.Seek(TestLength - 1);
        Assert.False(input.EOF);
    }

    [Fact]
    public void Copy_ShouldCreateNewInstance()
    {
        var original = new OnlyUrlTextInput(TestUrl, TestLength, _testEncoding);
        original.Seek(50);

        var copy = original.Copy();

        Assert.Equal(0, copy.Position);
        Assert.Equal(original.Length, copy.Length);
        Assert.NotSame(original, copy);
    }

    [Fact]
    public void ReadByte_ShouldReturnMinusOneAtEOF()
    {
        var input = new OnlyUrlTextInput(TestUrl, TestLength, _testEncoding);
        input.Seek(TestLength);

        var result = input.ReadByte();
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Position_ShouldIncrementAfterReadByte()
    {
        var input = new OnlyUrlTextInput(TestUrl, TestLength, _testEncoding);
        var initialPosition = input.Position;

        input.ReadByte();

        Assert.Equal(initialPosition + 1, input.Position);
    }

    [Fact]
    public void Constructor_ShouldAcceptDifferentEncodings()
    {
        var utf8Input = new OnlyUrlTextInput(TestUrl, 100, Encoding.UTF8);
        var asciiInput = new OnlyUrlTextInput(TestUrl, 100, Encoding.ASCII);

        Assert.NotNull(utf8Input);
        Assert.NotNull(asciiInput);
    }
}
