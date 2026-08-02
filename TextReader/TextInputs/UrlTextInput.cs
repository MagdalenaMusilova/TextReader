using System.IO;
using System.Net.Http;
using System.Text;

namespace TextReader.TextInputs;

public class UrlTextInput : ITextInput
{
    public event EventHandler? DataReadyEvent;

    private static readonly HttpClient HttpClient = new HttpClient();
    
    private string _url;
    private ITextInput _usedInput;
    
    private string? _tmpFilePath;
    private bool _tmpFileUsed = false;

    private long _length;
    private Encoding _encoding;
    private bool _supportsPartialRequests;

    public bool EOF => _usedInput.EOF;
    public long Length => _usedInput.Length;
    public long Position => _usedInput.Position;

    public UrlTextInput(string url)
    {
        _url = url;
        Initialize();

        if (_supportsPartialRequests)
        {
            // start downloading the whole file in background, use OnlyUrlTextInput for now
            _usedInput = new OnlyUrlTextInput(_url, _length, _encoding);
            _usedInput.DataReadyEvent += (sender, args) => DataReadyEvent?.Invoke(this, args);
            Task.Run(() =>
            {
                DownloadFullFile();
                var fileTextImput = new FileTextInput(_tmpFilePath);
                fileTextImput.DataReadyEvent += (sender, args) =>
                {
                    fileTextImput.Seek(Position);
                    _usedInput = fileTextImput;
                };
            });   
        }
        else
        {
            DownloadFullFile();
            _usedInput = new FileTextInput(_tmpFilePath);
            _usedInput.DataReadyEvent += (sender, args) => DataReadyEvent?.Invoke(this, args);
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
        var request = new HttpRequestMessage(HttpMethod.Head, _url);

        using var response = HttpClient
            .SendAsync(request)
            .GetAwaiter()
            .GetResult();

        response.EnsureSuccessStatusCode();

        // Get file length
        _length = response.Content.Headers.ContentLength ?? 0;

        // Get encoding from Content-Type header
        _encoding = response.Content.Headers.ContentType?.CharSet != null
            ? Encoding.GetEncoding(response.Content.Headers.ContentType.CharSet)
            : Encoding.UTF8;

        // Check if server supports partial requests
        _supportsPartialRequests =
            response.Headers.AcceptRanges != null && response.Headers.AcceptRanges.Contains("bytes");
    }

    public void Seek(long index) => _usedInput.Seek(index);

    public int ReadByte() => _usedInput.ReadByte();


    public string Read(long size) => _usedInput.Read(size);

    private void DownloadFullFile()
    {
        _tmpFilePath = Path.GetTempFileName();

        using var response = HttpClient
            .GetAsync(_url, HttpCompletionOption.ResponseHeadersRead)
            .GetAwaiter()
            .GetResult();

        response.EnsureSuccessStatusCode();

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
}