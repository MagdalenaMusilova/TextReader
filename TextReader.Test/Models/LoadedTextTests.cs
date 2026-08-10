using TextReader.Models;
using TextReader.TextInputs;

namespace TextReader.Test.Models;

public class LoadedTextTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly string _testFilePathLF;
    private readonly string _testFilePathCR;
    private readonly string _testFilePathCRLF;

    public LoadedTextTests()
    {
        _testFilePath = Path.GetTempFileName();
        _testFilePathLF = Path.GetTempFileName();
        _testFilePathCR = Path.GetTempFileName();
        _testFilePathCRLF = Path.GetTempFileName();

        // Regular test file
        var content = "Line 1\nLine 2\nLine 3\n";
        using (var fs = new FileStream(_testFilePath, FileMode.Create, FileAccess.Write))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            fs.Write(bytes);
        }

        // LF only
        using (var fs = new FileStream(_testFilePathLF, FileMode.Create, FileAccess.Write))
        {
            byte[] bytes = Encoding.UTF8.GetBytes("Line 1\nLine 2\nLine 3");
            fs.Write(bytes);
        }

        // CR only
        using (var fs = new FileStream(_testFilePathCR, FileMode.Create, FileAccess.Write))
        {
            byte[] bytes = Encoding.UTF8.GetBytes("Line 1\rLine 2\rLine 3");
            fs.Write(bytes);
        }

        // CRLF
        using (var fs = new FileStream(_testFilePathCRLF, FileMode.Create, FileAccess.Write))
        {
            byte[] bytes = Encoding.UTF8.GetBytes("Line 1\r\nLine 2\r\nLine 3");
            fs.Write(bytes);
        }
    }

    public void Dispose()
    {
        Thread.Sleep(500);
        File.Delete(_testFilePath);
        File.Delete(_testFilePathLF);
        File.Delete(_testFilePathCR);
        File.Delete(_testFilePathCRLF);
    }

    [Fact]
    public void Constructor_ShouldInitializeLoadedText()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        Assert.True(loadedText.LinesCount > 0);
    }

    [Fact]
    public void LinesCount_ShouldReturnCorrectCountWhenCalculated()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        Assert.Equal(3, loadedText.LinesCount);
    }

    [Fact]
    public void GetLines_ShouldReturnCorrectLines()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        var buffer = new string[3];
        var count = loadedText.GetLines(0, 3, buffer);

        Assert.Equal(3, count);
        Assert.Equal("Line 1\n", buffer[0]);
        Assert.Equal("Line 2\n", buffer[1]);
        Assert.Equal("Line 3\n", buffer[2]);
    }

    [Fact]
    public void GetLines_ShouldReturnPartialLinesWhenRequested()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        var buffer = new string[2];
        var count = loadedText.GetLines(1, 2, buffer);

        Assert.Equal(2, count);
        Assert.Equal("Line 2\n", buffer[0]);
        Assert.Equal("Line 3\n", buffer[1]);
    }

    [Fact]
    public void GetLines_ShouldReturnZeroWhenStartIndexOutOfBounds()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        var buffer = new string[1];
        var count = loadedText.GetLines(100, 1, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void GetLines_ShouldClampLineCountWhenExceedingFileEnd()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        var buffer = new string[10];
        var count = loadedText.GetLines(1, 10, buffer);

        Assert.Equal(2, count);
    }

    [Fact]
    public void IndexToWordPosition_ShouldReturnCorrectPosition()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        // Index 7 should be the start of "Line 2"
        var position = loadedText.IndexToWordPosition(7);

        Assert.Equal(1, position.lineIndex);
        Assert.Equal(0, position.lineOffset);
    }

    [Fact]
    public void IndexToWordPosition_ShouldHandleOffsetInLine()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        // Index 9 should be "n" in "Line 2"
        var position = loadedText.IndexToWordPosition(9);

        Assert.Equal(1, position.lineIndex);
        Assert.Equal(2, position.lineOffset);
    }

    [Fact]
    public void Search_ShouldReturnWordPositions()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        // Wait for line offsets to be calculated
        Thread.Sleep(200);

        var results = loadedText.Search("Line");

        Assert.Equal(3, results.Count);
        Assert.Equal(0, results[0].lineIndex);
        Assert.Equal(1, results[1].lineIndex);
        Assert.Equal(2, results[2].lineIndex);
    }

    [Fact]
    public void NewLineType_LF_ShouldBeHandled()
    {
        using var input = new FileTextInput(_testFilePathLF);
        var loadedText = new LoadedText(input);

        Thread.Sleep(200);

        Assert.Equal(3, loadedText.LinesCount);
    }

    [Fact]
    public void NewLineType_CR_ShouldBeHandled()
    {
        using var input = new FileTextInput(_testFilePathCR);
        var loadedText = new LoadedText(input);

        Thread.Sleep(200);

        Assert.Equal(3, loadedText.LinesCount);
    }

    [Fact]
    public void NewLineType_CRLF_ShouldBeHandled()
    {
        using var input = new FileTextInput(_testFilePathCRLF);
        var loadedText = new LoadedText(input);

        Thread.Sleep(200);

        Assert.Equal(3, loadedText.LinesCount);
    }

    [Fact]
    public void FinishedLoadingEvent_ShouldBeRaised()
    {
        using var input = new FileTextInput(_testFilePath);
        var loadedText = new LoadedText(input);

        bool eventRaised = false;
        loadedText.FinishedLoadingEvent += (sender, args) => eventRaised = true;

        // Wait for event
        Thread.Sleep(300);

        Assert.True(eventRaised);
    }

    [Fact]
    public void LineOffsets_LargeFile_ShouldCalculateCorrectly()
    {
        // Create a large file with known number of lines
        var largePath = Path.GetTempFileName();
        try
        {
            const int lineCount = 100000;
            using (var fs = new FileStream(largePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < lineCount; i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"Line {i} with some content\n");
                    fs.Write(bytes);
                }
            }

            using var input = new FileTextInput(largePath);
            var loadedText = new LoadedText(input);

            // Wait for line offsets to be calculated
            Thread.Sleep(2000);

            Assert.Equal(lineCount, loadedText.LinesCount);
        }
        finally
        {
            File.Delete(largePath);
        }
    }

    [Fact]
    public void LineOffsets_LargeFile_GetLinesAtAnyPosition()
    {
        // Create a large file and verify we can get lines from any position
        var largePath = Path.GetTempFileName();
        try
        {
            const int lineCount = 50000;
            using (var fs = new FileStream(largePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < lineCount; i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"Line {i}\n");
                    fs.Write(bytes);
                }
            }

            using var input = new FileTextInput(largePath);
            var loadedText = new LoadedText(input);

            // Wait for line offsets to be calculated
            Thread.Sleep(1500);

            // Test getting lines from beginning
            var buffer = new string[5];
            var count = loadedText.GetLines(0, 5, buffer);
            Assert.Equal(5, count);
            Assert.Equal("Line 0\n", buffer[0]);
            Assert.Equal("Line 4\n", buffer[4]);

            // Test getting lines from middle
            count = loadedText.GetLines(25000, 5, buffer);
            Assert.Equal(5, count);
            Assert.Equal("Line 25000\n", buffer[0]);
            Assert.Equal("Line 25004\n", buffer[4]);

            // Test getting lines near end
            count = loadedText.GetLines(lineCount - 5, 5, buffer);
            Assert.Equal(5, count);
            Assert.Equal("Line 49995\n", buffer[0]);
            Assert.Equal("Line 49999\n", buffer[4]);
        }
        finally
        {
            File.Delete(largePath);
        }
    }

    [Fact]
    public void LineOffsets_LargeFile_CRLF_ShouldCalculateCorrectly()
    {
        // Test large file with CRLF line endings
        var largePath = Path.GetTempFileName();
        try
        {
            const int lineCount = 30000;
            using (var fs = new FileStream(largePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < lineCount; i++)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes($"Line {i}\r\n");
                    fs.Write(bytes);
                }
            }

            using var input = new FileTextInput(largePath);
            var loadedText = new LoadedText(input);

            // Wait for line offsets to be calculated
            Thread.Sleep(1200);

            Assert.Equal(lineCount, loadedText.LinesCount);

            // Verify we can read lines correctly
            var buffer = new string[3];
            var count = loadedText.GetLines(10000, 3, buffer);
            Assert.Equal(3, count);
            Assert.Equal("Line 10000\r\n", buffer[0]);
        }
        finally
        {
            File.Delete(largePath);
        }
    }

    [Fact]
    public void LineOffsets_LargeFile_VaryingLineLengths()
    {
        // Test with varying line lengths to ensure offset calculation handles it
        var largePath = Path.GetTempFileName();
        try
        {
            const int lineCount = 20000;
            using (var fs = new FileStream(largePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < lineCount; i++)
                {
                    // Create lines with varying lengths
                    string padding = new string('x', i % 100);
                    byte[] bytes = Encoding.UTF8.GetBytes($"Line {i} {padding}\n");
                    fs.Write(bytes);
                }
            }

            using var input = new FileTextInput(largePath);
            var loadedText = new LoadedText(input);

            // Wait for line offsets to be calculated
            Thread.Sleep(1000);

            Assert.Equal(lineCount, loadedText.LinesCount);

            // Verify random access works correctly
            var buffer = new string[1];
            loadedText.GetLines(5000, 1, buffer);
            Assert.StartsWith("Line 5000 ", buffer[0]);

            loadedText.GetLines(15000, 1, buffer);
            Assert.StartsWith("Line 15000 ", buffer[0]);
        }
        finally
        {
            File.Delete(largePath);
        }
    }

    [Fact]
    public void LineOffsets_LargeFile_ExtremelyVaryingLineLengths()
    {
        // Test with extremely varying line lengths (short to very long)
        var largePath = Path.GetTempFileName();
        try
        {
            const int lineCount = 10000;
            using (var fs = new FileStream(largePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < lineCount; i++)
                {
                    // Create lines with wildly varying lengths
                    // Short lines (1-10 chars), medium (100 chars), long (1000 chars)
                    int length = (i % 3) switch
                    {
                        0 => i % 10 + 1,      // Very short: 1-10 chars
                        1 => 100,              // Medium: 100 chars
                        _ => 1000              // Long: 1000 chars
                    };
                    string padding = new string('x', length);
                    byte[] bytes = Encoding.UTF8.GetBytes($"{i}:{padding}\n");
                    fs.Write(bytes);
                }
            }

            using var input = new FileTextInput(largePath);
            var loadedText = new LoadedText(input);

            // Wait for line offsets to be calculated
            Thread.Sleep(1500);

            Assert.Equal(lineCount, loadedText.LinesCount);

            // Test accessing various positions with different line lengths
            var buffer = new string[1];

            // Test short line
            loadedText.GetLines(0, 1, buffer);
            Assert.StartsWith("0:", buffer[0]);

            // Test medium line
            loadedText.GetLines(1, 1, buffer);
            Assert.StartsWith("1:", buffer[0]);
            Assert.Equal(103, buffer[0].Length); // "1:" + 100 x's + "\n"

            // Test long line
            loadedText.GetLines(2, 1, buffer);
            Assert.StartsWith("2:", buffer[0]);
            Assert.Equal(1003, buffer[0].Length); // "2:" + 1000 x's + "\n"

            // Test in middle of file
            loadedText.GetLines(5000, 1, buffer);
            Assert.StartsWith("5000:", buffer[0]);

            // Test near end
            loadedText.GetLines(9999, 1, buffer);
            Assert.StartsWith("9999:", buffer[0]);
        }
        finally
        {
            File.Delete(largePath);
        }
    }

    [Fact]
    public void LineOffsets_LargeFile_MixedNewlinesAndVaryingLengths()
    {
        // Test with varying line lengths AND different newline types
        var largePath = Path.GetTempFileName();
        try
        {
            const int lineCount = 15000;
            using (var fs = new FileStream(largePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < lineCount; i++)
                {
                    string padding = new string('a', (i * 7) % 500); // Varying 0-499 chars
                    string line = $"Line{i}:{padding}";
                    byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
                    fs.Write(bytes);
                }
            }

            using var input = new FileTextInput(largePath);
            var loadedText = new LoadedText(input);

            // Wait for line offsets to be calculated
            Thread.Sleep(1200);

            Assert.Equal(lineCount, loadedText.LinesCount);

            // Verify sequential reads work correctly
            var buffer = new string[10];
            var count = loadedText.GetLines(1000, 10, buffer);
            Assert.Equal(10, count);

            for (int i = 0; i < 10; i++)
            {
                Assert.StartsWith($"Line{1000 + i}:", buffer[i]);
            }
        }
        finally
        {
            File.Delete(largePath);
        }
    }

    [Fact]
    public void LineOffsets_LargeFile_VeryLongLines()
    {
        // Test with very long lines (simulating files with minimal line breaks)
        var largePath = Path.GetTempFileName();
        try
        {
            const int lineCount = 1000;
            const int paddingLength = 10000; // 10KB of padding per line

            using (var fs = new FileStream(largePath, FileMode.Create, FileAccess.Write))
            {
                for (int i = 0; i < lineCount; i++)
                {
                    string content = new string('A', paddingLength);
                    byte[] bytes = Encoding.UTF8.GetBytes($"Line {i} start {content} end\n");
                    fs.Write(bytes);
                }
            }

            using var input = new FileTextInput(largePath);
            var loadedText = new LoadedText(input);

            // Wait for line offsets to be calculated (might take longer due to file size)
            Thread.Sleep(2000);

            Assert.Equal(lineCount, loadedText.LinesCount);

            // Test reading very long lines
            var buffer = new string[1];
            loadedText.GetLines(0, 1, buffer);
            Assert.StartsWith("Line 0 start", buffer[0]);
            Assert.EndsWith("end\n", buffer[0]);
            // "Line 0 start " (13) + 10000 A's + " end\n" (5) = 10018 total
            Assert.True(buffer[0].Length >= 10018);

            // Test middle - "Line 500 start " is 15 chars + 10000 A's + 5 = 10020
            loadedText.GetLines(500, 1, buffer);
            Assert.StartsWith("Line 500 start", buffer[0]);
            Assert.True(buffer[0].Length >= 10020);

            // Test end - "Line 999 start " is 15 chars + 10000 A's + 5 = 10020
            loadedText.GetLines(999, 1, buffer);
            Assert.StartsWith("Line 999 start", buffer[0]);
            Assert.True(buffer[0].Length >= 10020);
        }
        finally
        {
            File.Delete(largePath);
        }
    }
}
