using System.IO;
using TextReader.TextInputs;

namespace TextReader;

public class SearchFeature
{
    private Dictionary<string, List<int>> cache = new Dictionary<string, List<int>>();
    private ITextInput _input;
    
    public SearchFeature(ITextInput input)
    {
        _input = input;
    }
    
    public List<int> Search(string word)
    {
        if (cache.ContainsKey(word))
        {
            return cache[word];
        }
        
        // try to help with the search from previous searches
        for (int i = word.Length - 1; i >= 1; i--)
        {
            string subWord = word.Substring(0, i);
            if (cache.ContainsKey(subWord))
            {
                return FindAllOccurencesFromPossibilities(word, cache[subWord]);
            }
        }
        
        return FindAllOccurencesRaw(word);
    }
    
    private List<int> FindAllOccurencesFromPossibilities(string word, List<int> possibilities)
    {
        cache.Add(word, new List<int>());
        foreach (var index in possibilities)
        {
            _input.Seek(index);
            if (IsWordAtPosition(_input, word))
            {
                cache[word].Add(index);
            }
        }
        return cache[word];
    }

    private List<int> FindAllOccurencesRaw(string word)
    {
        cache[word] = new List<int>();
        int index = 0;
        while (!_input.EOF)
        {
            _input.Seek(index);
            if (IsWordAtPosition(_input, word))
            {
                cache[word].Add(index);
            }
            index++;
        }
        return cache[word];
    }

    private bool IsWordAtPosition(ITextInput input, string word)
    {
        int readByte;
        for (int i = 0; i < word.Length; i++)
        {
            readByte = _input.Read();
            if (readByte != word[i])
            {
                return false;
            }
        }
        return true;
    }
}