using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Light.PortableResults.Documentation.Tests;

public sealed class MarkdownLinkTests
{
    private const string AbsoluteRepositoryPrefix = "https://github.com/feO2x/Light.PortableResults/blob/main/";

    private readonly Dictionary<string, HashSet<string>> _anchorsByFile = new (StringComparer.Ordinal);

    public static TheoryData<string> DocumentationFiles
    {
        get
        {
            var data = new TheoryData<string> { "README.md" };
            foreach (var file in Directory
                        .GetFiles(RepositoryFiles.GetFullPath("docs"), "*.md", SearchOption.AllDirectories)
                        .Select(RepositoryFiles.ToRepositoryRelativePath)
                        .Order(StringComparer.Ordinal))
            {
                data.Add(file);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(DocumentationFiles))]
    public void AllRepositoryLinks_ShouldResolve(string relativeFilePath)
    {
        var document = MarkdownDocument.Load(RepositoryFiles.GetFullPath(relativeFilePath));
        var problems = new List<string>();

        foreach (var link in document.GetLinks())
        {
            var problem = CheckLink(document, link);
            if (problem is not null)
            {
                problems.Add($"{relativeFilePath}:{link.LineNumber} '{link.Target}': {problem}");
            }
        }

        problems.Should().BeEmpty();
    }

    [Fact]
    public void Readme_ShouldOnlyUseAbsoluteLinksForRepositoryFiles()
    {
        var readme = MarkdownDocument.Load(RepositoryFiles.GetFullPath("README.md"));

        readme
           .GetLinks()
           .Where(static link => !IsExternal(link.Target) && !link.Target.StartsWith('#'))
           .Should()
           .BeEmpty("README.md is packed into the NuGet packages, where relative links do not resolve");
    }

    [Fact]
    public void DocsPages_ShouldOnlyUseRelativeLinksForRepositoryFiles()
    {
        var links =
            from file in Directory.GetFiles(RepositoryFiles.GetFullPath("docs"), "*.md", SearchOption.AllDirectories)
            from link in MarkdownDocument.Load(file).GetLinks()
            where link.Target.StartsWith(AbsoluteRepositoryPrefix, StringComparison.Ordinal)
            select $"{RepositoryFiles.ToRepositoryRelativePath(file)}:{link.LineNumber} {link.Target}";

        links.Should().BeEmpty("pages in docs/ link to repository files with relative links");
    }

    [Theory]
    [InlineData("Key Features", "key-features")]
    [InlineData("When to Use Result vs. Exceptions", "when-to-use-result-vs-exceptions")]
    [InlineData("What is `ValidatedValue<T>`?", "what-is-validatedvaluet")]
    [InlineData("HTTP write options (`PortableResultsHttpWriteOptions`)", "http-write-options-portableresultshttpwriteoptions")]
    [InlineData("2–6x faster [than](https://example.org) FluentValidation", "26x-faster-than-fluentvalidation")]
    [InlineData("snake_case and kebab-case", "snake_case-and-kebab-case")]
    public void CreateGitHubSlug_ShouldFollowGitHubRules(string headingText, string expectedSlug) =>
        MarkdownDocument.CreateGitHubSlug(headingText).Should().Be(expectedSlug);

    private string? CheckLink(MarkdownDocument document, MarkdownLink link)
    {
        var target = link.Target;
        if (target.StartsWith(AbsoluteRepositoryPrefix, StringComparison.Ordinal))
        {
            return CheckRepositoryTarget(
                RepositoryFiles.RootDirectory,
                Uri.UnescapeDataString(target[AbsoluteRepositoryPrefix.Length..])
            );
        }

        if (IsExternal(target))
        {
            return null;
        }

        if (target.StartsWith('#'))
        {
            return CheckAnchor(document.FilePath, target[1..]);
        }

        return CheckRepositoryTarget(Path.GetDirectoryName(document.FilePath)!, Uri.UnescapeDataString(target));
    }

    private string? CheckRepositoryTarget(string baseDirectory, string target)
    {
        var hashIndex = target.IndexOf('#');
        var path = hashIndex < 0 ? target : target[..hashIndex];
        var anchor = hashIndex < 0 ? null : target[(hashIndex + 1)..];
        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, path));

        if (!fullPath.StartsWith(RepositoryFiles.RootDirectory, StringComparison.Ordinal))
        {
            return "the link points outside of the repository";
        }

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            return $"'{RepositoryFiles.ToRepositoryRelativePath(fullPath)}' does not exist";
        }

        if (anchor is null)
        {
            return null;
        }

        if (!fullPath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return "anchors are only checked for Markdown files";
        }

        return CheckAnchor(fullPath, anchor);
    }

    private string? CheckAnchor(string markdownFilePath, string anchor)
    {
        if (!_anchorsByFile.TryGetValue(markdownFilePath, out var anchors))
        {
            anchors = MarkdownDocument.Load(markdownFilePath).GetAnchors();
            _anchorsByFile[markdownFilePath] = anchors;
        }

        return anchors.Contains(anchor) ?
            null :
            $"'{RepositoryFiles.ToRepositoryRelativePath(markdownFilePath)}' has no anchor '{anchor}'";
    }

    private static bool IsExternal(string target) =>
        target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
        target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
        target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
}
