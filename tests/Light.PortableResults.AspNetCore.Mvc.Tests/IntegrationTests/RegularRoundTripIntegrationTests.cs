using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Metadata;
using Xunit;

namespace Light.PortableResults.AspNetCore.Mvc.Tests.IntegrationTests;

public sealed class RegularRoundTripIntegrationTests
{
    private readonly RegularMvcApp _fixture;

    public RegularRoundTripIntegrationTests(RegularMvcApp fixture) => _fixture = fixture;

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_GenericSuccess()
    {
        using var httpClient = _fixture.CreateHttpClient();
        var id = new Guid("D1A5D89D-A5ED-4990-8BFC-8DF56D8E0A96");

        using var response = await httpClient.GetAsync(
            $"/api/contacts/{id}",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();
        var result =
            await response.ReadResultAsync<ContactDto>(cancellationToken: TestContext.Current.CancellationToken);

        var expectedResult = Result<ContactDto>.Ok(
            new ContactDto
            {
                Id = id,
                Name = "Foo"
            }
        );
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldIgnoreHeaders_ByDefault()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/contacts",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();
        var result = await response.ReadResultAsync<List<ContactDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedResult = Result<List<ContactDto>>.Ok(CreateExpectedContacts());
        result
           .Equals(expectedResult, compareMetadata: true, valueComparer: ContactListComparer.Instance)
           .Should().BeTrue();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_HeaderMetadata_WhenConfigured()
    {
        var options = new PortableResultsHttpReadOptions
        {
            HeaderParsingService = new DefaultHttpHeaderParsingService(
                new AllowListHeaderSelectionStrategy(["Count"])
            )
        };
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/contacts",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();
        var result = await response.ReadResultAsync<List<ContactDto>>(
            options: options,
            cancellationToken: TestContext.Current.CancellationToken
        );

        var expectedMetadata = MetadataObject.Create(("Count", MetadataValue.FromInt64(3)));
        var expectedResult = Result<List<ContactDto>>.Ok(CreateExpectedContacts(), expectedMetadata);
        result
           .Equals(expectedResult, compareMetadata: true, valueComparer: ContactListComparer.Instance)
           .Should().BeTrue();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_GenericFailure()
    {
        using var httpClient = _fixture.CreateHttpClient();
        var id = new Guid("A1B2C3D4-E5F6-7890-ABCD-EF1234567890");

        using var response = await httpClient.GetAsync(
            $"/api/contacts/not-found/{id}",
            cancellationToken: TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var result =
            await response.ReadResultAsync<ContactDto>(cancellationToken: TestContext.Current.CancellationToken);

        var expectedResult = Result<ContactDto>.Fail(
            new Error
            {
                Message = $"Contact with id '{id}' was not found",
                Code = "ContactNotFound",
                Category = ErrorCategory.NotFound
            }
        );
        result.Should().Be(expectedResult);
    }

    private static List<ContactDto> CreateExpectedContacts() =>
    [
        new ()
        {
            Id = new Guid("D8FC9BEC-0606-4E9B-8EB4-04558B2B9D40"),
            Name = "Foo"
        },
        new ()
        {
            Id = new Guid("AAA41889-0BD8-4247-9C0F-049567FA63C1"),
            Name = "Bar"
        },
        new ()
        {
            Id = new Guid("3D43850A-69D1-4230-8BAA-75AA6C693E9D"),
            Name = "Baz"
        }
    ];

    private sealed class ContactListComparer : IEqualityComparer<List<ContactDto>?>
    {
        public static ContactListComparer Instance { get; } = new ();

        public bool Equals(List<ContactDto>? x, List<ContactDto>? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return x.SequenceEqual(y);
        }

        public int GetHashCode(List<ContactDto>? obj)
        {
            if (obj is null)
            {
                return 0;
            }

            var hashCode = new HashCode();
            foreach (var item in obj)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }
    }
}
