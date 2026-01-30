using System.Text.RegularExpressions;
using FluentAssertions;
using MemorableIdGenerator;

namespace Tests;

public class DocumentationTests
{
    public static IEnumerable<TestCaseData> AllLists()
        => Enum.GetValues<WordList>()
            .Select(l => new TestCaseData(l) { TestName = l.ToString() });

    private static int GetDocumentedCount(WordList list)
    {
        // Find WordList.cs by searching up from the assembly location
        var generatorAssembly = typeof(WordList).Assembly;
        var assemblyLocation = generatorAssembly.Location;
        var currentDir = Path.GetDirectoryName(assemblyLocation);
        var listName = list.ToString();
        
        // Search up the directory tree for WordList.cs in MemorableIdGenerator folder
        while (currentDir != null)
        {
            var sourceFile = Path.Combine(currentDir, "MemorableIdGenerator", "WordList.cs");
            if (File.Exists(sourceFile))
            {
                var sourceContent = File.ReadAllText(sourceFile);
                
                // Extract count for specific list from XML comments: /// <summary>... (N words) </summary> ... ListName
                var xmlCommentPattern = new Regex(
                    $@"///\s*<summary>\s*\r?\n\s*///\s*.*?\((\d+)\s+words?\)\s*\r?\n\s*///\s*</summary>\s*\r?\n\s*{Regex.Escape(listName)}(?:,|\s*\}})",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
                
                var match = xmlCommentPattern.Match(sourceContent);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            
            // Also check current directory for WordList.cs
            var directFile = Path.Combine(currentDir, "WordList.cs");
            if (File.Exists(directFile))
            {
                var sourceContent = File.ReadAllText(directFile);
                
                var xmlCommentPattern = new Regex(
                    $@"///\s*<summary>\s*\r?\n\s*///\s*.*?\((\d+)\s+words?\)\s*\r?\n\s*///\s*</summary>\s*\r?\n\s*{Regex.Escape(listName)}(?:,|\s*\}})",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
                
                var match = xmlCommentPattern.Match(sourceContent);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            
            currentDir = Path.GetDirectoryName(currentDir);
        }
        
        throw new FileNotFoundException($"Could not find XML documentation for {listName} in WordList.cs starting from {assemblyLocation}");
    }
    
    [TestCaseSource(nameof(AllLists))]
    public void XmlCommentCountsMatchActualCounts(WordList list)
    {
        var documentedCount = GetDocumentedCount(list);
        var actualCount = MemorableIdGen.LoadList(list).Length;
        
        actualCount.Should().Be(
            documentedCount,
            $"XML documentation for {list} says {documentedCount} words, but actual count is {actualCount}");
    }
    
    private static int GetReadmeCount(WordList list)
    {
        // Find README.md by searching up from the assembly location
        var generatorAssembly = typeof(WordList).Assembly;
        var assemblyLocation = generatorAssembly.Location;
        var currentDir = Path.GetDirectoryName(assemblyLocation);
        var listName = list.ToString();
        
        // Search up the directory tree for README.md
        while (currentDir != null)
        {
            var readmeFile = Path.Combine(currentDir, "README.md");
            if (File.Exists(readmeFile))
            {
                var readmeContent = File.ReadAllText(readmeFile);
                
                // Extract count for specific list from README: - ListName: Count
                var readmePattern = new Regex(
                    $@"-\s*{Regex.Escape(listName)}:\s*(\d+)",
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
                
                var match = readmePattern.Match(readmeContent);
                if (match.Success)
                {
                    return int.Parse(match.Groups[1].Value);
                }
            }
            
            currentDir = Path.GetDirectoryName(currentDir);
        }
        
        throw new FileNotFoundException($"Could not find count for {listName} in README.md starting from {assemblyLocation}");
    }
    
    [TestCaseSource(nameof(AllLists))]
    public void ReadmeCountsMatchActualCounts(WordList list)
    {
        var readmeCount = GetReadmeCount(list);
        var actualCount = MemorableIdGen.LoadList(list).Length;
        
        actualCount.Should().Be(
            readmeCount,
            $"README for {list} says {readmeCount} words, but actual count is {actualCount}");
    }
}
