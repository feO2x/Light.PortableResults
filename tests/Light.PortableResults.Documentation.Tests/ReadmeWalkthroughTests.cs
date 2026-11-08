using System;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Documentation.Tests.Walkthrough;
using Light.PortableResults.Validation;
using Microsoft.AspNetCore.TestHost;
using Xunit;

namespace Light.PortableResults.Documentation.Tests;

public sealed class ReadmeWalkthroughTests
{
    [Fact]
    public async Task InvalidMovieRating_ShouldProduceTheProblemDetailsShownInTheReadme()
    {
        await using var app = MovieRatingApi.CreateApp([]);
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var httpClient = app.GetTestClient();
        var expectedResponse = ReadmeWalkthrough.ProblemDetailsResponse;

        using var response = await httpClient.PostAsJsonAsync(
            "/api/movieRatings",
            CreateInvalidDto(),
            TestContext.Current.CancellationToken
        );

        ((int) response.StatusCode).Should().Be(expectedResponse.StatusCode);
        response.Content.Headers.ContentType!.MediaType.Should().Be(expectedResponse.Headers["Content-Type"]);
        var actualBody = JsonNode.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)
        );
        var expectedBody = JsonNode.Parse(expectedResponse.Body);
        JsonNode.DeepEquals(actualBody, expectedBody).Should().BeTrue(
            "the README must show the actual response body, but the app responded with{0}{1}",
            Environment.NewLine,
            actualBody?.ToJsonString()
        );
    }

    [Fact]
    public async Task InvalidMovieRating_ShouldBeReadBackWithTheSameErrorsThatTheValidatorProduced()
    {
        await using var app = MovieRatingApi.CreateApp([]);
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var httpClient = app.GetTestClient();

        var result = await MovieRatingClient.AddMovieRatingAsync(httpClient, CreateInvalidDto());

        var expectedErrors = new MovieRatingValidator(DefaultValidationContextFactory.Create())
           .Validate(CreateInvalidDto())
           .Errors;
        expectedErrors.Should().HaveCount(3);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Equal(expectedErrors);
    }

    [Fact]
    public async Task ValidMovieRating_ShouldBeReadBackAsSuccessfulResult()
    {
        await using var app = MovieRatingApi.CreateApp([]);
        await app.StartAsync(TestContext.Current.CancellationToken);
        using var httpClient = app.GetTestClient();
        var dto = CreateInvalidDto() with { Rating = 5 };
        dto.Comment = "  The Answer Is Out There, Neo. It's Looking for You.  ";
        dto.UserName = "Trinity";

        var result = await MovieRatingClient.AddMovieRatingAsync(httpClient, dto);

        result.Should().Be(
            Result<MovieRating>.Ok(
                new MovieRating(
                    dto.Id,
                    dto.MovieId,
                    "Trinity",
                    "The Answer Is Out There, Neo. It's Looking for You.",
                    5
                )
            )
        );
    }

    [Fact]
    public void WalkthroughCodeBlocks_ShouldAppearVerbatimInTheTestedSources()
    {
        var walkthroughDirectory = RepositoryFiles.GetFullPath(
            "tests/Light.PortableResults.Documentation.Tests/Walkthrough"
        );
        var normalizedSources = Directory
           .GetFiles(walkthroughDirectory, "*.cs")
           .Select(static path => NormalizeLines(File.ReadAllText(path)))
           .ToList();
        var codeBlocks = ReadmeWalkthrough.CSharpBlocks.ToList();

        codeBlocks.Should().HaveCountGreaterThanOrEqualTo(4, "the walkthrough consists of four steps");
        foreach (var codeBlock in codeBlocks)
        {
            var normalizedBlock = NormalizeLines(codeBlock.Content);
            normalizedSources.Should().Contain(
                source => source.Contains(normalizedBlock, StringComparison.Ordinal),
                "the csharp block in README.md at line {0} must appear verbatim in {1}",
                codeBlock.StartLineNumber,
                walkthroughDirectory
            );
        }
    }

    private static string NormalizeLines(string text)
    {
        var lines = text.ReplaceLineEndings("\n").Split('\n').Select(static line => line.Trim());
        return "\n" + string.Join('\n', lines).Trim('\n') + "\n";
    }

    private static MovieRatingDto CreateInvalidDto() =>
        new ()
        {
            Id = new Guid("b507182e-f9ff-48d7-8a78-bcdc15cb4d0a"),
            MovieId = new Guid("5c200e1d-4a16-4572-b884-e3a3957771fc"),
            UserName = "   ",
            Comment = "Too short",
            Rating = 7
        };
}
