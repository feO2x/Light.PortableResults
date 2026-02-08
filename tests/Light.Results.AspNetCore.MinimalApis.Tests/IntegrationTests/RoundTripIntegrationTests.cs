using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Light.Results.Http.Reading;
using Light.Results.Metadata;
using Xunit;

namespace Light.Results.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class RegularRoundTripIntegrationTests
{
    private readonly RegularMinimalApiApp _fixture;

    public RegularRoundTripIntegrationTests(RegularMinimalApiApp fixture) => _fixture = fixture;

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
        result.Equals(expectedResult, compareMetadata: true, valueComparer: ContactListComparer.Instance).Should()
           .BeTrue();
    }

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_HeaderMetadata_WhenConfigured()
    {
        var options = new LightResultsHttpReadOptions
        {
            HeaderSelectionMode = HeaderSelectionMode.AllowList,
            HeaderAllowList = ["Count"]
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
        result.Equals(expectedResult, compareMetadata: true, valueComparer: ContactListComparer.Instance).Should()
           .BeTrue();
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
        new()
        {
            Id = new Guid("D8FC9BEC-0606-4E9B-8EB4-04558B2B9D40"),
            Name = "Foo"
        },
        new()
        {
            Id = new Guid("AAA41889-0BD8-4247-9C0F-049567FA63C1"),
            Name = "Bar"
        },
        new()
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

public sealed class ExtendedRoundTripIntegrationTests
{
    private readonly ExtendedMinimalApiApp _fixture;

    public ExtendedRoundTripIntegrationTests(ExtendedMinimalApiApp fixture) => _fixture = fixture;

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_GenericSuccessWithMetadata()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/value-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();
        var result = await response.ReadResultAsync<string>(cancellationToken: TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.Value.Should().Be("contact-42");
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("source", out var source).Should().BeTrue();
        source.Should().Be("value-metadata");
    }

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_NonGenericSuccessWithMetadata()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/non-generic-metadata",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.EnsureSuccessStatusCode();

        var result = await response.ReadResultAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("note", out var note).Should().BeTrue();
        result.Metadata.Value.TryGetInt64("count", out var count).Should().BeTrue();
        note.Should().Be("non-generic");
        count.Should().Be(3);
    }

    [Fact]
    public async Task ReadResultAsync_ShouldRoundTrip_RichValidationFailure()
    {
        using var httpClient = _fixture.CreateHttpClient();

        using var response = await httpClient.GetAsync(
            "/api/extended/validation-rich",
            cancellationToken: TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result =
            await response.ReadResultAsync<ContactDto>(cancellationToken: TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Count.Should().Be(2);
        result.Errors[0].Code.Should().Be("NameRequired");
        result.Errors[0].Category.Should().Be(ErrorCategory.Validation);
        result.Metadata.Should().NotBeNull();
        result.Metadata!.Value.TryGetString("source", out var source).Should().BeTrue();
        source.Should().Be("rich");
    }
}
