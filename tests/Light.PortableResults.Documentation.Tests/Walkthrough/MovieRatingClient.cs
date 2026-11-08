using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Light.PortableResults.Http.Reading;

namespace Light.PortableResults.Documentation.Tests.Walkthrough;

// The README walkthrough shows the body of this method verbatim (step 4). Keep both in sync -
// ReadmeSnippetTests fails when they drift apart.
public static class MovieRatingClient
{
    public static async Task<Result<MovieRating>> AddMovieRatingAsync(HttpClient httpClient, MovieRatingDto dto)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/movieRatings", dto);
        Result<MovieRating> result = await response.ReadResultAsync<MovieRating>();

        if (result.IsValid)
        {
            Console.WriteLine($"Stored rating {result.Value.Id}");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"{error.Target}: {error.Message} ({error.Code})");
            }
        }

        return result;
    }
}
