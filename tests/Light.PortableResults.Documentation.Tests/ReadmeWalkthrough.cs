using System;
using System.Collections.Generic;
using System.Linq;

namespace Light.PortableResults.Documentation.Tests;

/// <summary>
/// Provides access to the code blocks of the walkthrough section in the root README.
/// </summary>
public static class ReadmeWalkthrough
{
    public const string SectionHeading = "Walkthrough";

    private static readonly Lazy<IReadOnlyList<MarkdownCodeBlock>> LazyCodeBlocks = new (LoadCodeBlocks);

    public static IReadOnlyList<MarkdownCodeBlock> CodeBlocks => LazyCodeBlocks.Value;

    public static IEnumerable<MarkdownCodeBlock> CSharpBlocks =>
        CodeBlocks.Where(static block => block.Language == "csharp");

    /// <summary>
    /// Gets the single HTTP block of the walkthrough that shows the validation problem details response.
    /// </summary>
    public static ReadmeHttpResponse ProblemDetailsResponse
    {
        get
        {
            var httpBlocks = CodeBlocks.Where(static block => block.Language == "http").ToList();
            if (httpBlocks.Count != 1)
            {
                throw new InvalidOperationException(
                    $"The README walkthrough must contain exactly one http block, but it contains {httpBlocks.Count}."
                );
            }

            return ReadmeHttpResponse.Parse(httpBlocks[0].Content);
        }
    }

    private static IReadOnlyList<MarkdownCodeBlock> LoadCodeBlocks()
    {
        var readme = MarkdownDocument.Load(RepositoryFiles.GetFullPath("README.md"));
        var (start, end) = readme.GetSection(SectionHeading);
        return readme.GetCodeBlocks(start, end);
    }
}

public sealed record ReadmeHttpResponse(int StatusCode, IReadOnlyDictionary<string, string> Headers, string Body)
{
    public static ReadmeHttpResponse Parse(string content)
    {
        var lines = content.Split('\n');
        var statusLineParts = lines[0].Split(' ', 3);
        var statusCode = int.Parse(statusLineParts[1]);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var index = 1;
        for (; index < lines.Length && lines[index].Trim().Length > 0; index++)
        {
            var separatorIndex = lines[index].IndexOf(':');
            headers[lines[index][..separatorIndex].Trim()] = lines[index][(separatorIndex + 1)..].Trim();
        }

        var body = string.Join('\n', lines.Skip(index + 1));
        return new ReadmeHttpResponse(statusCode, headers, body);
    }
}
