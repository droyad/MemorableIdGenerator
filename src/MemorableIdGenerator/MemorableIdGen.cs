using System.Collections.Concurrent;

namespace MemorableIdGenerator;

/// <summary>
/// A memory ID generator. Note that this class is stateful. Generate is threadsafe.
/// </summary>
public class MemorableIdGen
{
    internal static string[] LoadList(WordList list)
    {
        var resourceName = $"MemorableIdGenerator.Lists.{list}.txt";

        using var stream = typeof(MemorableIdGen).Assembly!.GetManifestResourceStream(resourceName)
                           ?? throw new Exception($"Resource {resourceName} not found");

        using var streamReader = new StreamReader(stream);
        return streamReader.ReadToEnd()
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();
    }

    private static readonly IReadOnlyDictionary<WordList, string[]> Lists = Enum.GetValues<WordList>()
        .ToDictionary(l => l, LoadList);

    private Random _rnd = new Random();
    private readonly IReadOnlyList<WordList> _lists;
    private string _joiner = "";
    private int _maxLength = int.MaxValue;
    private int _maxAttempts = 100;
    private bool _allowDuplicates;
    private readonly ConcurrentBag<string> _previouslyGenerated = new();

    public MemorableIdGen(params WordList[] lists)
    {
        if (lists.Length == 0)
            throw new ArgumentException("At least one word list must be specified");

        _lists = lists;
    }

    /// <summary>
    /// Generates 
    /// </summary>
    /// <param name="list"></param>
    /// <returns></returns>
    public static MemorableIdGen With(params WordList[] list)
        => new MemorableIdGen(list);

    /// <summary>
    /// Generates a 2 word identifier in the format [Adjective][Animal]
    /// Example: "HappyPenguin"
    /// </summary>
    /// <returns></returns>
    public static MemorableIdGen DescriptiveAnimal()
        => new MemorableIdGen(WordList.Adjectives, WordList.Animals);

    /// <summary>
    /// Generates a 2 word identifier in the format [Colour][Animal]
    /// Example: "BlueDuck"
    /// </summary>
    /// <returns></returns>
    public static MemorableIdGen ColourfulAnimal()
        => new MemorableIdGen(WordList.Colours, WordList.Animals);

    /// <summary>
    /// Generates a 3 word identifier in the format [Adjective][Colour][Animal]
    /// Example: ExpressiveGreenEmu
    /// </summary>
    /// <returns></returns>
    public static MemorableIdGen DescriptiveColourfulAnimal()
        => new MemorableIdGen(WordList.Adjectives, WordList.Colours, WordList.Animals);

    /// <summary>
    /// The string to join the values with. Example if "-" is specified, the result
    /// looks like: Hurried-Antelope
    ///
    /// The default is no joiner
    /// </summary>
    /// <param name="joiner">The string to use to join</param>
    /// <returns></returns>
    public MemorableIdGen JoiningWith(string joiner)
    {
        _joiner = joiner;
        return this;
    }

    /// <summary>
    /// Maximum length of the string. This must be larger than
    /// (8 + joiner.Length) * lists.length.
    ///
    /// The default is unlimited length 
    /// </summary>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public MemorableIdGen LimitLengthTo(int maxLength)
    {
        _maxLength = maxLength;
        return this;
    }

    /// <summary>
    /// The number of times to try to generate an identifier. This is used when
    /// the maxLength is set, or a validator is passed to the generate function
    /// </summary>
    /// <param name="maxAttempts">The maximum number of times to try</param>
    /// <returns></returns>
    public MemorableIdGen AttemptUpTo(int maxAttempts)
    {
        _maxAttempts = maxAttempts;
        return this;
    }

    /// <summary>
    /// Uses the specified seed for System.Random. This makes the generation deterministic.
    /// </summary>
    /// <param name="seed"></param>
    /// <returns></returns>
    public MemorableIdGen UsingSeed(int seed)
    {
        lock(_rnd)
            _rnd = new Random(seed);
        return this;
    }
    
    /// <summary>
    /// Allows duplicate ids to be generated
    /// </summary>
    /// <returns></returns>
    public MemorableIdGen AllowDuplicates()
    {
        _allowDuplicates = true;
        return this;
    }

    /// <summary>
    /// Generates an identifier using the settings specified previously
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string Generate()
    {
        ValidateArguments();

        for (var x = 0; x < _maxAttempts; x++)
        {
            var result = string.Join(_joiner, _lists.Select(GetWord));
            if (result.Length < _maxLength && !IsDuplicate(result))
            {
                return result;
            }
        }

        throw new Exception(
            "The maximum number of attempts has been exceeded, increase the MaxLength value, or reduce the number of lists used");
    }

    /// <summary>
    /// Generates an identifier using the settings specified previously
    /// </summary>
    /// <param name="validate">A function which allows the caller to veto the value, for example checking the values doesn't already exist in the database</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public string Generate(Func<string, bool> validate)
    {
        ValidateArguments();

        for (var x = 0; x < _maxAttempts; x++)
        {
            var result = string.Join(_joiner, _lists.Select(GetWord));
            if (result.Length < _maxLength && !IsDuplicate(result) && validate(result))
            {
                return result;
            }
        }

        throw new Exception(
            "The maximum number of attempts has been exceeded, increase the MaxLength value, reduce the number of lists used or change it so that the validation function passes more values.");
    }

    /// <summary>
    /// Generates an identifier using the settings specified previously
    /// </summary>
    /// <param name="validate">A function which allows the caller to veto the value, for example checking the values doesn't already exist in the database</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<string> Generate(Func<string, Task<bool>> validate)
    {
        ValidateArguments();

        for (var x = 0; x < _maxAttempts; x++)
        {
            var result = string.Join(_joiner, _lists.Select(GetWord));
            if (result.Length < _maxLength && !IsDuplicate(result) && await validate(result))
            {
                return result;
            }
        }

        throw new Exception(
            "The maximum number of attempts has been exceeded, increase the MaxLength value, reduce the number of lists used or change it so that the validation function passes more values.");
    }

    bool IsDuplicate(string value)
    {
        if (_allowDuplicates)
            return false;
            
        lock (_previouslyGenerated)
        {
            if (_previouslyGenerated.Contains(value))
                return true;
            
            _previouslyGenerated.Add(value);
            return false;
        }
    }

    string GetWord(WordList list)
    {
        var candidates = Lists[list];
        lock (_rnd)
            return candidates[_rnd.Next(0, candidates.Length)];
    }

    void ValidateArguments()
    {
        if (_lists.Count * (8 + _joiner.Length) > _maxLength)
        {
            throw new InvalidOperationException(
                "The max length must be greater or equal to lists.Count * (8 + join.length).");
        }
    }
}