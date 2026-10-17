using System.Text.RegularExpressions;

namespace YMS.Architecture.Tests;

/// <summary>Rules about the source text itself, for what the compiled model can no longer tell apart.</summary>
public sealed partial class SourceTests
{
    // A column and its validator must share one constant, so their lengths cannot drift apart.
    [Fact]
    public void Max_lengths_are_named_constants()
    {
        var literals = SourceFiles()
            .SelectMany(file => File.ReadLines(file).Select((line, i) => (file, line, number: i + 1)))
            .Where(l => LiteralLength().IsMatch(l.line))
            .Select(l => $"{Path.GetRelativePath(RepositoryRoot, l.file)}:{l.number}: {l.line.Trim()}");

        Assert.Empty(literals);
    }

    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepositoryRoot, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Split(Path.DirectorySeparatorChar).Any(part => part is "Migrations" or "bin" or "obj"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "YMS.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("YMS.slnx not found above the test output.");
    }

    [GeneratedRegex(@"(HasMaxLength|MaximumLength|RequiredText|OptionalText)\((""[^""]*"",\s*)?\d")]
    private static partial Regex LiteralLength();
}
