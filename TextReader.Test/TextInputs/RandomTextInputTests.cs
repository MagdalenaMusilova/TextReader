using TextReader.TextInputs;

namespace TextReader.Test.TextInputs;

public class RandomTextInputTests
{
    [Fact]
    public void Constructor_ShouldGenerateText()
    {
        var input = new RandomTextInput(10);

        Assert.True(input.ByteLength > 0);
        Assert.Equal(0, input.Position);
        Assert.False(input.IsEndOfFile);
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

        var firstByte = input.Read();
        Assert.True(firstByte >= 0);
        Assert.Equal(1, input.Position);
    }

    [Fact]
    public void ReadByte_ShouldReturnMinusOneAtEOF()
    {
        var input = new RandomTextInput(1);
        input.Seek(input.ByteLength);

        var result = input.Read();
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Read_ShouldReadSpecifiedLength()
    {
        var input = new RandomTextInput(10);
        var initialLength = input.ByteLength;

        var result = input.Read(10);
        Assert.Equal(10, result.Length);
        Assert.Equal(10, input.Position);
    }

    [Fact]
    public void Seek_ShouldChangePosition()
    {
        var input = new RandomTextInput(10);
        var halfLength = input.ByteLength / 2;

        input.Seek(halfLength);
        Assert.Equal(halfLength, input.Position);
    }

    [Fact]
    public void EOF_ShouldBeTrueAtEnd()
    {
        var input = new RandomTextInput(5);

        input.Seek(input.ByteLength);
        Assert.True(input.IsEndOfFile);
    }

    [Fact]
    public void Copy_ShouldCreateIndependentCopyWithSameContent()
    {
        var original = new RandomTextInput(10);
        var originalContent = original.Read(original.ByteLength);
        original.Seek(0);

        var copy = original.Copy();
        var copyContent = copy.Read(copy.ByteLength);

        Assert.Equal(originalContent, copyContent);
        Assert.Equal(original.ByteLength, copy.ByteLength);
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

        var content1 = input1.Read(input1.ByteLength);
        var content2 = input2.Read(input2.ByteLength);

        // With high probability, random text should be different
        Assert.NotEqual(content1, content2);
    }

    [Fact]
    public async Task SaveToFileAsync_ShouldSaveGeneratedContent()
    {
        var input = new RandomTextInput(10);
        var originalContent = input.Read(input.ByteLength);
        input.Seek(0);
        var destPath = Path.GetTempFileName();

        try
        {
            await input.SaveToFileAsync(destPath);

            Assert.True(File.Exists(destPath));
            var savedContent = await File.ReadAllTextAsync(destPath);
            Assert.Equal(originalContent, savedContent);
        }
        finally
        {
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
        }
    }

    [Fact]
    public async Task SaveToFileAsync_ShouldNotAffectPosition()
    {
        var input = new RandomTextInput(10);
        input.Seek(5);
        var destPath = Path.GetTempFileName();

        try
        {
            await input.SaveToFileAsync(destPath);

            Assert.Equal(5, input.Position);
        }
        finally
        {
            if (File.Exists(destPath))
            {
                File.Delete(destPath);
            }
        }
    }
}
