using System.IO;
using System.Net.Http;
using System.Text;

namespace TextReader.TextInputs;

public class UrlTextInput : ITextInput
{
    public event EventHandler? DataReadyEvent;

    private static readonly HttpClient HttpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    
    private string _url;
    private ITextInput _usedInput;
    
    private string? _tmpFilePath;
    private bool _tmpFileUsed = false;
    private bool _fileTextInputUsed = false;

    private long _length;
    private Encoding _encoding;
    private bool _supportsPartialRequests;
    private long _position; // needed to ensure that switching from OnlyUrlTextInput to FileTextInput won't change position. Race condition - position is updated only after reading is finished. So change source while reading -> wrong position

    public bool IsEndOfFile => _usedInput.IsEndOfFile;
    public long Position => _usedInput.Position;
    public long ByteLength => _usedInput.ByteLength;

    public UrlTextInput(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException($"Invalid URL format: '{url}'. URL must start with http:// or https://", nameof(url));

        _url = url;

        try
        {
            Initialize();

            if (_supportsPartialRequests)
            {
                // start downloading the whole file in background, use OnlyUrlTextInput for now
                _usedInput = new OnlyUrlTextInput(_url, _length, _encoding);
                _usedInput.DataReadyEvent += (sender, args) => DataReadyEvent?.Invoke(this, args);
                Task.Run(() =>
                {
                    try
                    {
                        DownloadFullFile();
                        var fileTextImput = new FileTextInput(_tmpFilePath, _position);
                        _usedInput = fileTextImput;
                        _fileTextInputUsed = true;
                    }
                    catch
                    {
                        // If background download fails, keep using OnlyUrlTextInput
                    }
                });
            }
            else
            {
                DownloadFullFile();
                _usedInput = new FileTextInput(_tmpFilePath);
                _fileTextInputUsed = true;
                _usedInput.DataReadyEvent += (sender, args) => DataReadyEvent?.Invoke(this, args);
            }
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to connect to '{url}'. Please check the URL and your internet connection.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException($"Request to '{url}' timed out. The server may be slow or unreachable.", ex);
        }
    }

    private UrlTextInput()
    {
    }
    
    public ITextInput Copy()
    {
        return _usedInput.Copy();   // todo kinda a bad copy :D 
    }

    ~UrlTextInput()
    {
        if (_tmpFileUsed)
        {
            (_usedInput as FileTextInput).Close();
            File.Delete(_tmpFilePath);
        }
    }
    
    private void Initialize()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, _url);

            using var response = HttpClient
                .SendAsync(request)
                .GetAwaiter()
                .GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Server returned error {(int)response.StatusCode} ({response.StatusCode}) for URL '{_url}'.");
            }

            // Get file length
            _length = response.Content.Headers.ContentLength ?? 0;

            if (_length == 0)
            {
                throw new InvalidDataException($"The resource at '{_url}' is empty or does not specify content length.");
            }

            // Get encoding from Content-Type header
            try
            {
                _encoding = response.Content.Headers.ContentType?.CharSet != null
                    ? Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet)
                    : Encoding.UTF8;
            }
            catch (ArgumentException)
            {
                // If encoding is not recognized, default to UTF8
                _encoding = Encoding.UTF8;
            }

            // Check if server supports partial requests
            _supportsPartialRequests =
                response.Headers.AcceptRanges != null && response.Headers.AcceptRanges.Contains("bytes");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (TaskCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize connection to '{_url}'.", ex);
        }
    }

    public void Seek(long byteIndex)
    {
        _position = byteIndex;
        _usedInput.Seek(byteIndex);
    }

    public int Read()
    {
        _position++;
        return _usedInput.Read();
    }

    public int Peek()
    {
        return _usedInput.Peek();
    }


    public string Read(long size)
    {
        _position += size;
        return _usedInput.Read(size);
    }

    public async Task SaveToFileAsync(string destFileName)
    {
        if (string.IsNullOrWhiteSpace(destFileName))
            throw new ArgumentException("Destination file path cannot be null or empty.", nameof(destFileName));

        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(destFileName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            while (!_fileTextInputUsed) //todo mby use event instead? But that could create race condition?
            {
                await Task.Delay(100);
            }

            await _usedInput.SaveToFileAsync(destFileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied when saving to '{destFileName}'. Check file permissions.", ex);
        }
        catch (IOException ex)
        {
            throw new IOException($"Unable to save file to '{destFileName}'. The path may be invalid or the disk may be full.", ex);
        }
    }

    private void DownloadFullFile()
    {
        try
        {
            _tmpFilePath = Path.GetTempFileName();

            using var response = HttpClient
                .GetAsync(_url, HttpCompletionOption.ResponseHeadersRead)
                .GetAwaiter()
                .GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to download file. Server returned error {(int)response.StatusCode} ({response.StatusCode}).");
            }

            using var input = response.Content
                .ReadAsStreamAsync()
                .GetAwaiter()
                .GetResult();

            using var output = new FileStream(
                _tmpFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920);
            _tmpFileUsed = true;

            input.CopyTo(output);
        }
        catch (HttpRequestException ex)
        {
            CleanupTempFile();
            throw new HttpRequestException($"Failed to download file from '{_url}'. {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            CleanupTempFile();
            throw new TimeoutException($"Download from '{_url}' timed out.", ex);
        }
        catch (IOException ex)
        {
            CleanupTempFile();
            throw new IOException($"Failed to save downloaded content to temporary file. {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            CleanupTempFile();
            throw new InvalidOperationException($"Unexpected error while downloading file from '{_url}'.", ex);
        }
    }

    private void CleanupTempFile()
    {
        if (_tmpFileUsed && !string.IsNullOrEmpty(_tmpFilePath) && File.Exists(_tmpFilePath))
        {
            try
            {
                File.Delete(_tmpFilePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
    
    public void Dispose()
    {

    }
}