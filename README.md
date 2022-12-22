[![GitHub Workflow Status (branch)](https://img.shields.io/github/actions/workflow/status/droyad/MemorableIdGenerator/CI/main)](https://github.com/droyad/MemorableIdGenerator/actions/workflows/main.yml?query=branch%3Amain)
[![NuGet](https://img.shields.io/nuget/dt/MemorableIdGenerator.svg)](https://www.nuget.org/packages/MemorableIdGenerator)
[![NuGet](https://img.shields.io/nuget/v/MemorableIdGenerator.svg)](https://www.nuget.org/packages/MemorableIdGenerator)
[![Prerelease](https://img.shields.io/nuget/vpre/MemorableIdGenerator?color=orange&label=prerelease)](https://www.nuget.org/packages/MemorableIdGenerator)

# MemorableIdGenerator

This library generates an identifier that is easy to recognise and remember. Often called a Friendly Name.

## Usage

Start with `MemorableIdGen` and either call `With` or one of the presets. A word is created for each
item in the WordList array, that is then joined into a single string (using `""` by default).

The static methods calls on `MemorableIdGen` create a new instance, but from then on each method call 
(other than `Generate`) modifies that instance. You can setup a `MemorableIdGen` and call `Generate` 
multiple times. Calling `Generate` and the static methods is thread-safe.

Examples:
```C#
// BlueDolphin 
MemorableIdGen.ColourfulAnimal().Generate();

// angry-orange-koala
MemorableIdGen.DescriptiveColourfulAnimal().JoiningWith("-").Generate().ToLower();

// Custom words lists and must contain an a
// fast-pumpkin 
MemorableIdGen.With(WordList.Adjectives, WordList.Foods).Generate(s => s.Contains("a"));

// Check it is unique async (e.g. in a database)
await MemorableIdGen.ColourfulAnimal().Generate(async s => await CheckForUniqueness(s))

// Limit the length
await MemorableIdGen.ColourfulAnimal().LimitLengthTo(20);

// Deterministic (within the one version of the library), e.g. for tests
MemorableIdGen.DescriptiveColourfulAnimal().WithSeed(33).Generate();
```

## Number of Values

- Adjectives: 4649
- Animals: 1058
- Colours: 58
- Foods: 98