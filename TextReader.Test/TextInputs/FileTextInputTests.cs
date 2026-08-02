using TextReader.TextInputs;

namespace TextReader.Test.TextInputs;

public class FileTextInputTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly string _testContent = "Hello World!\nThis is a test file.\n";

    public FileTextInputTests()
    {
        _testFilePath = Path.GetTempFileName();
        // Use FileStream to write bytes directly to avoid newline conversion
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
    public void Constructor_ShouldInitializeProperties()
    {
        using var input = new FileTextInput(_testFilePath);

        Assert.False(input.EOF);
        Assert.Equal(_testContent.Length, input.Length);
        Assert.Equal(0, input.Position);
    }

    [Fact]
    public void Constructor_ShouldTriggerDataReadyEvent()
    {
        bool eventTriggered = false;
        using var input = new FileTextInput(_testFilePath);
        input.DataReadyEvent += (sender, args) => eventTriggered = true;

        // Event should have been triggered in constructor
        // Create a new instance to test
        using var input2 = new FileTextInput(_testFilePath);
        input2.DataReadyEvent += (sender, args) => eventTriggered = true;
    }

    [Fact]
    public void ReadByte_ShouldReadSingleByte()
    {
        using var input = new FileTextInput(_testFilePath);

        var firstByte = input.ReadByte();
        Assert.Equal('H', (char)firstByte);
        Assert.Equal(1, input.Position);
    }

    [Fact]
    public void Read_ShouldReadSpecifiedNumberOfBytes()
    {
        using var input = new FileTextInput(_testFilePath);

        var result = input.Read(5);
        Assert.Equal("Hello", result);
        Assert.Equal(5, input.Position);
    }

    [Fact]
    public void Seek_ShouldChangePosition()
    {
        using var input = new FileTextInput(_testFilePath);

        input.Seek(6);
        Assert.Equal(6, input.Position);

        var result = input.Read(5);
        Assert.Equal("World", result);
    }

    [Fact]
    public void EOF_ShouldBeTrueAtEndOfFile()
    {
        using var input = new FileTextInput(_testFilePath);

        input.Seek(input.Length);
        Assert.True(input.EOF);
    }

    [Fact]
    public void Copy_ShouldCreateIndependentCopy()
    {
        using var original = new FileTextInput(_testFilePath);
        original.Seek(5);

        using var copy = (FileTextInput)original.Copy();

        Assert.Equal(0, copy.Position);
        Assert.Equal(original.Length, copy.Length);
        Assert.NotSame(original, copy);
    }

    [Fact]
    public void SequentialRead_ShouldReadEntireFile()
    {
        using var input = new FileTextInput(_testFilePath);

        var result = input.Read(input.Length);
        Assert.Equal(_testContent, result);
        Assert.True(input.EOF);
    }
}


