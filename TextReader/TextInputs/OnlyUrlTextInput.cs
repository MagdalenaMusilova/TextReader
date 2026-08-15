using System.IO;
using System.Net.Http;
using System.Text;

namespace TextReader.TextInputs;

public class OnlyUrlTextInput : ITextInput
{   
    public event EventHandler? DataReadyEvent;

    private const int CHUNK_SIZE = 1024 * 10; // 10KB 
    
    private static readonly HttpClient HttpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
    
    private string _url;
    private Encoding _encoding;
    private long _byteLength;
    
    private long _currentPosition = 0;
    private Dictionary<long, string> _chunkCache = new Dictionary<long, string>();

    public bool EOF => _currentPosition >= _byteLength;
    public long Position => _currentPosition;
    public long ByteLength => _byteLength;

    public OnlyUrlTextInput(string url, long byteLength, Encoding encoding)
    {
        _url = url;
        _byteLength = byteLength;
        _encoding = encoding;
        DataReadyEvent?.Invoke(this, EventArgs.Empty);
    }
    
    public ITextInput Copy()
    {
        return new OnlyUrlTextInput(_url, _byteLength, _encoding);
    }
    
    private bool IsIndexCached(long index)
    {
        int pos = _chunkCache.Keys.ToList().BinarySearch(index); 

        if (pos < 0)
            pos = ~pos - 1;

        return pos >= 0 && index < _chunkCache.Keys.ElementAt(pos) + CHUNK_SIZE;
    }

    private long GetChunkIndex(long index)
    {
        int chunkIndex = _chunkCache.Keys.ToList().BinarySearch(index);

        if (chunkIndex < 0)
            chunkIndex = ~chunkIndex - 1;
        return chunkIndex;
    }

    public void Seek(long byteIndex)
    {
        _currentPosition = byteIndex;
    }

    public int Read()
    {
        if (EOF)
        {
            return -1;
        }
        
        if(!IsIndexCached(_currentPosition))
        {
            FetchChunkFromUrl(_currentPosition);
        }

        return GetByteFromCache();
    }

    public int Peak()
    {
        var res = GetByteFromCache();
        _currentPosition--;
        return res;
    }

    public string Read(long size)
    {
        List<string> results = new List<string>();

        long firstChunk = (_currentPosition / CHUNK_SIZE) * CHUNK_SIZE;
        long lastChunk = ((_currentPosition + size) / CHUNK_SIZE) * CHUNK_SIZE;
        for (long index = firstChunk;
             index <= lastChunk;
             index+= CHUNK_SIZE)
        {
            if (!_chunkCache.ContainsKey(index))
            {
                FetchChunkFromUrl(index);
            }

            long startOffset = index != firstChunk ? 0 : _currentPosition % CHUNK_SIZE;
            long chunkSize = index != lastChunk ? CHUNK_SIZE : (_currentPosition + size) % CHUNK_SIZE;
            var res = ReadChunk(index, (int)startOffset, (int)chunkSize);
            results.Add(res);
        }

        _currentPosition += size;
        return string.Concat(results);
    }

    public Task SaveToFileAsync(string destFileName)
    {
        throw new NotImplementedException("This text input only reading in memory, not saving to a file.");
    }
    
    private string ReadChunk(long index, int startOffset, int size)
    {
        var chunkIndex = GetChunkIndex(index);
        return _chunkCache[chunkIndex].Substring(startOffset, size);
    }
    
    private int GetByteFromCache()
    {
        if (!IsIndexCached(_currentPosition))
        {
            FetchChunkFromUrl(_currentPosition);
        }
        long chunkIndex = GetChunkIndex(_currentPosition);
        long chunkOffset = _currentPosition - _chunkCache.Keys.ElementAt((int)chunkIndex);
        ++_currentPosition;
        return _chunkCache[chunkIndex][(int)chunkOffset];
    }
    
    private void FetchChunkFromUrl(long index)
    {
        try
        {
            long chunkStartIndex = (index / CHUNK_SIZE) * CHUNK_SIZE;

            var request = new HttpRequestMessage(HttpMethod.Get, _url);
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(chunkStartIndex, chunkStartIndex + CHUNK_SIZE);

            using var response = HttpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead)
                .GetAwaiter()
                .GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to fetch chunk. Server returned error {(int)response.StatusCode} ({response.StatusCode}).");
            }

            var chunk = response.Content
                .ReadAsByteArrayAsync()
                .GetAwaiter()
                .GetResult();

            _chunkCache[chunkStartIndex] = _encoding.GetString(chunk);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpRequestException($"Failed to fetch data chunk from '{_url}'. {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new TimeoutException($"Request to '{_url}' timed out while fetching data chunk.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unexpected error while fetching data from '{_url}'.", ex);
        }
    }

    public void Dispose()
    {
        
    }
}