using System.Text.RegularExpressions;
using FluentAssertions;
using MemorableIdGenerator;

namespace Tests;

public class Tests
{
    
    public static IEnumerable<TestCaseData> AllLists()
        => Enum.GetValues<WordList>()
            .Select(l => new TestCaseData(l) { TestName = l.ToString() });

    
    [Test]
    public void ColourfulAnimal()
    {
        MemorableIdGen.ColourfulAnimal()
            .UsingSeed(33)
            .Generate()
            .Should()
            .Be("WhiteLangur");
    }
    
    [Test]
    public void JoiningWith()
    {
        MemorableIdGen.DescriptiveColourfulAnimal()
            .UsingSeed(33)
            .JoiningWith("-!")
            .Generate()
            .Should()
            .Be("Viscous-!Khaki-!SpadefootToad");
    }

    [Test]
    public void DuplicatesAreNotReturned()
    {
        var results = new List<string>();

        var generator = MemorableIdGen.With(WordList.Colours)
            .UsingSeed(33);

        var action = () =>
        {
            for (var x = 0; x < 60; x++)
                results.Add(generator.Generate());
        };

        action.Should()
            .Throw<Exception>()
            .WithMessage(
                "The maximum number of attempts has been exceeded, increase the MaxLength value, or reduce the number of lists used");

        results.Should().NotBeEmpty();
        
        results.GroupBy(r => r)
            .Where(r => r.Count() > 1)
            .Should()
            .BeEmpty();
    }
    
    [Test]
    public void DuplicatesAreReturnedIfAllowed()
    {
        var results = new List<string>();

        var generator = MemorableIdGen.With(WordList.Colours)
            .UsingSeed(33)
            .AllowDuplicates();

        for (var x = 0; x < 60; x++) // Word list has less than 60
            results.Add(generator.Generate());

        results.GroupBy(r => r)
            .Where(r => r.Count() > 1)
            .Should()
            .NotBeEmpty();
    }
    
    [Test]
    public void CanGenerateManyUnique()
    {
        var items = new HashSet<string>();

        var generator = MemorableIdGen.DescriptiveColourfulAnimal();

        for (int x = 0; x < 10000; x++)
            items.Add(generator.Generate(s => !items.Contains(s)));

        items.Should().HaveCount(10000);
    }
    
    [TestCaseSource(nameof(AllLists))]
    public void NoDuplicates(WordList list)
    {
        var duplicates = MemorableIdGen.LoadList(list)
            .GroupBy(w => w, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();
        
        //var distinct = MemorableIdGen.LoadList(list).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s);
        //TestContext.Write(string.Join(Environment.NewLine, distinct));
        
        duplicates.Should().BeEmpty();
    }

    [TestCaseSource(nameof(AllLists))]
    public void NoSpecialCharacters(WordList list)
    {
        var invalid = MemorableIdGen.LoadList(list)
            .Where(w => Regex.IsMatch(w, "[^A-Za-z]"))
            .ToArray();

        invalid.Should().BeEmpty();
    }
    
    [TestCaseSource(nameof(AllLists))]
    public void StartWithCapital(WordList list)
    {
        var notCapital = MemorableIdGen.LoadList(list)
            .Where(w => !char.IsUpper(w[0]))
            .ToArray();

        notCapital.Should().BeEmpty();
    }
    
    [TestCaseSource(nameof(AllLists))]
    public void NoEmpty(WordList list)
    {
        var empty = MemorableIdGen.LoadList(list)
            .Where(string.IsNullOrEmpty)
            .ToArray();

        empty.Should().BeEmpty();
    }
}