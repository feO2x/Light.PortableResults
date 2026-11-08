using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Light.PortableResults.Documentation.Tests;

/// <summary>
/// A deliberately small Markdown reader that understands the subset of GitHub Flavored Markdown used in this
/// repository: ATX headings, fenced code blocks, inline links, and explicit HTML anchors.
/// </summary>
public sealed partial class MarkdownDocument
{
    private MarkdownDocument(string filePath, string[] lines)
    {
        FilePath = filePath;
        Lines = lines;
        Blocks = ParseBlocks(lines);
    }

    public string FilePath { get; }

    public string[] Lines { get; }

    public IReadOnlyList<MarkdownLine> Blocks { get; }

    public static MarkdownDocument Load(string filePath) =>
        new (filePath, File.ReadAllText(filePath).ReplaceLineEndings("\n").Split('\n'));

    public IEnumerable<MarkdownHeading> GetHeadings() =>
        Blocks
           .Where(static line => !line.IsInsideCodeBlock)
           .Select(static line => (line, match: HeadingRegex().Match(line.Text)))
           .Where(static tuple => tuple.match.Success)
           .Select(
                static tuple => new MarkdownHeading(
                    tuple.match.Groups["hashes"].Value.Length,
                    tuple.match.Groups["text"].Value,
                    tuple.line.LineNumber
                )
            );

    public HashSet<string> GetAnchors()
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var heading in GetHeadings())
        {
            var slug = CreateGitHubSlug(heading.Text);
            if (occurrences.TryGetValue(slug, out var count))
            {
                occurrences[slug] = count + 1;
                anchors.Add($"{slug}-{count}");
            }
            else
            {
                occurrences[slug] = 1;
                anchors.Add(slug);
            }
        }

        foreach (var line in Blocks.Where(static line => !line.IsInsideCodeBlock))
        {
            foreach (Match match in ExplicitAnchorRegex().Matches(line.Text))
            {
                anchors.Add(match.Groups["id"].Value);
            }
        }

        return anchors;
    }

    public IEnumerable<MarkdownLink> GetLinks()
    {
        foreach (var line in Blocks.Where(static line => !line.IsInsideCodeBlock))
        {
            var text = InlineCodeRegex().Replace(line.Text, string.Empty);
            foreach (Match match in LinkRegex().Matches(text))
            {
                yield return new MarkdownLink(match.Groups["target"].Value, line.LineNumber);
            }
        }
    }

    public IReadOnlyList<MarkdownCodeBlock> GetCodeBlocks(int startLineNumber = 1, int endLineNumber = int.MaxValue)
    {
        var codeBlocks = new List<MarkdownCodeBlock>();
        string? language = null;
        var startLine = 0;
        StringBuilder? content = null;
        foreach (var line in Blocks)
        {
            if (line.LineNumber < startLineNumber || line.LineNumber > endLineNumber)
            {
                continue;
            }

            if (line.IsFence)
            {
                if (content is null)
                {
                    language = line.Text.Trim().TrimStart('`', '~').Trim();
                    startLine = line.LineNumber;
                    content = new StringBuilder();
                }
                else
                {
                    codeBlocks.Add(new MarkdownCodeBlock(language!, content.ToString(), startLine));
                    content = null;
                }

                continue;
            }

            if (content is not null)
            {
                content.Append(line.Text).Append('\n');
            }
        }

        return codeBlocks;
    }

    /// <summary>
    /// Gets the first and last line numbers of the level-two section whose heading contains the specified text.
    /// </summary>
    public (int StartLineNumber, int EndLineNumber) GetSection(string headingText)
    {
        var headings = GetHeadings().ToList();
        var index = headings.FindIndex(
            heading => heading.Level == 2 && heading.Text.Contains(headingText, StringComparison.Ordinal)
        );
        if (index < 0)
        {
            throw new InvalidOperationException($"'{FilePath}' has no level-two heading containing '{headingText}'.");
        }

        var next = headings.Skip(index + 1).FirstOrDefault(static heading => heading.Level <= 2);
        return (headings[index].LineNumber, next?.LineNumber - 1 ?? Lines.Length);
    }

    /// <summary>
    /// Creates the anchor that GitHub generates for a heading: inline Markdown is reduced to its text, the
    /// result is lowercased, every character except letters, marks, numbers, connector punctuation, spaces,
    /// and hyphens is removed, and spaces become hyphens.
    /// </summary>
    public static string CreateGitHubSlug(string headingText)
    {
        // Code spans keep their content verbatim (including angle brackets), while HTML tags outside of code
        // spans are not part of the rendered heading text.
        var segments = headingText.Split('`');
        for (var i = 0; i < segments.Length; i++)
        {
            if (i % 2 == 0)
            {
                segments[i] = HtmlTagRegex().Replace(InlineLinkRegex().Replace(segments[i], "${text}"), string.Empty);
            }
        }

        var text = string.Concat(segments).ToLowerInvariant();

        var builder = new StringBuilder(text.Length);
        foreach (var character in text)
        {
            if (character == ' ')
            {
                builder.Append('-');
                continue;
            }

            if (character == '-')
            {
                builder.Append(character);
                continue;
            }

            switch (char.GetUnicodeCategory(character))
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.NonSpacingMark:
                case UnicodeCategory.SpacingCombiningMark:
                case UnicodeCategory.EnclosingMark:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherNumber:
                case UnicodeCategory.ConnectorPunctuation:
                    builder.Append(character);
                    break;
            }
        }

        return builder.ToString();
    }

    private static List<MarkdownLine> ParseBlocks(string[] lines)
    {
        var result = new List<MarkdownLine>(lines.Length);
        string? openFence = null;
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            var fenceMatch = FenceRegex().Match(trimmed);
            if (openFence is null && fenceMatch.Success)
            {
                openFence = fenceMatch.Groups["fence"].Value;
                result.Add(new MarkdownLine(lines[i], i + 1, IsInsideCodeBlock: true, IsFence: true));
                continue;
            }

            if (openFence is not null &&
                fenceMatch.Success &&
                fenceMatch.Groups["fence"].Value[0] == openFence[0] &&
                fenceMatch.Groups["fence"].Value.Length >= openFence.Length &&
                fenceMatch.Groups["info"].Value.Trim().Length == 0)
            {
                openFence = null;
                result.Add(new MarkdownLine(lines[i], i + 1, IsInsideCodeBlock: true, IsFence: true));
                continue;
            }

            result.Add(new MarkdownLine(lines[i], i + 1, openFence is not null, IsFence: false));
        }

        return result;
    }

    [GeneratedRegex(@"^(?<fence>`{3,}|~{3,})(?<info>.*)$")]
    private static partial Regex FenceRegex();

    [GeneratedRegex(@"^(?<hashes>#{1,6})\s+(?<text>.*?)\s*#*\s*$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex("""<a\s+(?:id|name)="(?<id>[^"]+)"\s*>""")]
    private static partial Regex ExplicitAnchorRegex();

    [GeneratedRegex(@"\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"`[^`]*`")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"!?\[(?<text>[^\]]*)\]\([^)]*\)")]
    private static partial Regex InlineLinkRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}

public sealed record MarkdownLine(string Text, int LineNumber, bool IsInsideCodeBlock, bool IsFence);

public sealed record MarkdownHeading(int Level, string Text, int LineNumber);

public sealed record MarkdownLink(string Target, int LineNumber);

public sealed record MarkdownCodeBlock(string Language, string Content, int StartLineNumber);
