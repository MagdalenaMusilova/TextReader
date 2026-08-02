using TextReader.TextInputs;

namespace TextReader.Test.TextInputs;

public class RandomTextInputTests
{
    [Fact]
    public void Constructor_ShouldGenerateText()
    {
        var input = new RandomTextInput(10);

        Assert.True(input.Length > 0);
        Assert.Equal(0, input.Position);
        Assert.False(input.EOF);
    }

    [Fact]
    public void Constructor_ShouldTriggerDataReadyEvent()
    {
        bool eventTriggered = false;
        var input = new RandomTextInput(5);
        input.DataReadyEvent += (sender, args) => eventTriggered = true;

        var input2 = new RandomTextInput(5);
        input2.DataReadyEvent += (sender, args) => eventTriggered = true;
    }

    [Fact]
    public void ReadByte_ShouldReadSingleCharacter()
    {
        var input = new RandomTextInput(5);

        var firstByte = input.ReadByte();
        Assert.True(firstByte >= 0);
        Assert.Equal(1, input.Position);
    }

    [Fact]
    public void ReadByte_ShouldReturnMinusOneAtEOF()
    {
        var input = new RandomTextInput(1);
        input.Seek(input.Length);

        var result = input.ReadByte();
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Read_ShouldReadSpecifiedLength()
    {
        var input = new RandomTextInput(10);
        var initialLength = input.Length;

        var result = input.Read(10);
        Assert.Equal(10, result.Length);
        Assert.Equal(10, input.Position);
    }

    [Fact]
    public void Seek_ShouldChangePosition()
    {
        var input = new RandomTextInput(10);
        var halfLength = input.Length / 2;

        input.Seek(halfLength);
        Assert.Equal(halfLength, input.Position);
    }

    [Fact]
    public void EOF_ShouldBeTrueAtEnd()
    {
        var input = new RandomTextInput(5);

        input.Seek(input.Length);
        Assert.True(input.EOF);
    }

    [Fact]
    public void Copy_ShouldCreateIndependentCopyWithSameContent()
    {
        var original = new RandomTextInput(10);
        var originalContent = original.Read(original.Length);
        original.Seek(0);

        var copy = original.Copy();
        var copyContent = copy.Read(copy.Length);

        Assert.Equal(originalContent, copyContent);
        Assert.Equal(original.Length, copy.Length);
    }

    [Fact]
    public void Copy_ShouldNotSharePosition()
    {
        var original = new RandomTextInput(10);
        original.Seek(5);

        var copy = original.Copy();

        Assert.Equal(0, copy.Position);
        Assert.Equal(5, original.Position);
    }

    [Fact]
    public void MultipleInstances_ShouldGenerateDifferentContent()
    {
        var input1 = new RandomTextInput(10);
        var input2 = new RandomTextInput(10);

        var content1 = input1.Read(input1.Length);
        var content2 = input2.Read(input2.Length);

        // With high probability, random text should be different
        Assert.NotEqual(content1, content2);
    }
}
