using TextReader.TextInputs;

namespace TextReader.Test.TextInputs;

public class UrlTextInputTests
{
    private const string TestUrl = "https://raw.githubusercontent.com/microsoft/TypeScript/main/README.md";

    public class BasicTests
    {
        private const string TestUrl = "https://raw.githubusercontent.com/microsoft/TypeScript/main/README.md";

        [Fact]
        public void Constructor_ShouldInitializeWithValidUrl()
        {
            var input = new UrlTextInput(TestUrl);

            Assert.True(input.ByteLength > 0);
            Assert.Equal(0, input.Position);
            Assert.False(input.IsEndOfFile);
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

            var firstByte = input.Read();
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
            Assert.Equal(original.ByteLength, copy.ByteLength);
        }

        [Fact]
        public void EOF_ShouldReflectUnderlyingInput()
        {
            var input = new UrlTextInput(TestUrl);
            Thread.Sleep(100); // Allow initialization

            Assert.False(input.IsEndOfFile);

            input.Seek(input.ByteLength);
            Assert.True(input.IsEndOfFile);
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

            Assert.True(input.ByteLength > 0);
        }
    }

    public class SaveToFileTests
    {
        private const string TestUrl = "https://raw.githubusercontent.com/microsoft/TypeScript/main/README.md";

        [Fact]
        public async Task SaveToFileAsync_ShouldSaveDownloadedContent()
        {
            var input = new UrlTextInput(TestUrl);
            Thread.Sleep(500); // Allow download to complete
            var destPath = Path.GetTempFileName();

            try
            {
                await input.SaveToFileAsync(destPath);

                Assert.True(File.Exists(destPath));
                var fileInfo = new FileInfo(destPath);
                Assert.True(fileInfo.Length > 0);
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
            var input = new UrlTextInput(TestUrl);
            Thread.Sleep(500); // Allow download to complete
            input.Seek(10);
            var destPath = Path.GetTempFileName();

            try
            {
                await input.SaveToFileAsync(destPath);

                Assert.Equal(10, input.Position);
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
        public async Task SaveToFileAsync_ShouldWaitForDownloadToComplete()
        {
            var input = new UrlTextInput(TestUrl);
            // Don't wait - call SaveToFileAsync immediately
            var destPath = Path.GetTempFileName();

            try
            {
                // This should wait internally for the download to complete
                await input.SaveToFileAsync(destPath);

                Assert.True(File.Exists(destPath));
                var fileInfo = new FileInfo(destPath);
                Assert.True(fileInfo.Length > 0);
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
}
