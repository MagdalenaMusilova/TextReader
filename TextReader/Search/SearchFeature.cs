using System.IO;
using TextReader.TextInputs;

namespace TextReader;

/// <summary>
/// Provides text search functionality with caching optimization
/// </summary>
public class SearchFeature
{
    private readonly Dictionary<string, List<int>> _searchCache = new();
    private readonly ITextInput _input;

    public SearchFeature(ITextInput input)
    {
        _input = input;
    }

    /// <summary>
    /// Searches for all occurrences of a word in the text (case-insensitive)
    /// Uses cached results and substring optimization when possible
    /// </summary>
    /// <param name="searchWord">Word to search for</param>
    /// <returns>List of byte indices where the word was found</returns>
    public List<int> Search(string searchWord)
    {
        searchWord = searchWord.ToLower();

        // Return cached result if available
        if (_searchCache.ContainsKey(searchWord))
        {
            return _searchCache[searchWord];
        }

        // Optimization: Check if we have cached results for any prefix of this word
        // If we searched for "test" before, we can narrow down results when searching "testing"
        for (int i = searchWord.Length - 1; i >= 1; i--)
        {
            string prefixWord = searchWord.Substring(0, i);
            if (_searchCache.ContainsKey(prefixWord))
            {
                return FindAllOccurrencesFromCandidates(searchWord, _searchCache[prefixWord]);
            }
        }

        // No optimization possible, perform full text search
        return FindAllOccurrencesRaw(searchWord);
    }
    
    /// <summary>
    /// Searches for word occurrences by checking only candidate positions from a previous search
    /// </summary>
    /// <param name="searchWord">Word to search for</param>
    /// <param name="candidatePositions">Byte positions to check (from a prefix search)</param>
    /// <returns>List of byte indices where the word was found</returns>
    private List<int> FindAllOccurrencesFromCandidates(string searchWord, List<int> candidatePositions)
    {
        _searchCache.Add(searchWord, new List<int>());

        foreach (var byteIndex in candidatePositions)
        {
            _input.Seek(byteIndex);
            if (IsWordAtPosition(_input, searchWord))
            {
                _searchCache[searchWord].Add(byteIndex);
            }
        }

        return _searchCache[searchWord];
    }

    /// <summary>
    /// Performs a full text search by checking every position in the input
    /// </summary>
    /// <param name="searchWord">Word to search for</param>
    /// <returns>List of byte indices where the word was found</returns>
    private List<int> FindAllOccurrencesRaw(string searchWord)
    {
        _searchCache[searchWord] = new List<int>();
        int byteIndex = 0;

        while (!_input.IsEndOfFile)
        {
            _input.Seek(byteIndex);
            if (IsWordAtPosition(_input, searchWord))
            {
                _searchCache[searchWord].Add(byteIndex);
            }
            byteIndex++;
        }

        return _searchCache[searchWord];
    }

    /// <summary>
    /// Checks if the specified word exists at the current input position (case-insensitive)
    /// </summary>
    /// <param name="input">Text input to read from</param>
    /// <param name="searchWord">Word to match (should be lowercase)</param>
    /// <returns>True if the word matches at the current position</returns>
    private bool IsWordAtPosition(ITextInput input, string searchWord)
    {
        for (int i = 0; i < searchWord.Length; i++)
        {
            int readByte = input.Read();
            if (readByte != searchWord[i] && readByte != CharToUpperCase(searchWord[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Converts a lowercase character to uppercase (simple ASCII conversion)
    /// </summary>
    /// <param name="character">Character code to convert</param>
    /// <returns>Uppercase character code if lowercase letter, otherwise unchanged</returns>
    private int CharToUpperCase(int character)
    {
        if (character >= 'a' && character <= 'z')
        {
            return character - ('a' - 'A');
        }

        return character;
    }
}