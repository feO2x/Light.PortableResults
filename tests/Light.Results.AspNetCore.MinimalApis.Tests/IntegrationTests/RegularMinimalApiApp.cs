using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Light.Results.AspNetCore.MinimalApis.Tests.IntegrationTests;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Xunit;

[assembly: AssemblyFixture(typeof(RegularMinimalApiApp))]

namespace Light.Results.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class RegularMinimalApiApp : IAsyncLifetime
{
    public RegularMinimalApiApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddLightResultsForMinimalApis();

        App = builder.Build();
        App.MapGet("/api/contacts", GetContacts);
        App.MapGet("/api/contacts/{id:guid}", GetContact);
        App.MapPut("/api/contacts", CreateContact);
    }

    public WebApplication App { get; }

    public async ValueTask InitializeAsync() => await App.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await App.StopAsync();
        await App.DisposeAsync();
    }

    public HttpClient CreateHttpClient() => App.GetTestClient();

    private static LightResult<List<ContactDto>> GetContacts()
    {
        var contact1 = new ContactDto { Id = new Guid("D8FC9BEC-0606-4E9B-8EB4-04558B2B9D40"), Name = "Foo" };
        var contact2 = new ContactDto { Id = new Guid("AAA41889-0BD8-4247-9C0F-049567FA63C1"), Name = "Bar" };
        var contact3 = new ContactDto { Id = new Guid("3D43850A-69D1-4230-8BAA-75AA6C693E9D"), Name = "Baz" };
        List<ContactDto> contacts = [contact1, contact2, contact3];

        var metadata = MetadataObject.Create(
            ("Count", MetadataValue.FromInt64(contacts.Count, MetadataValueAnnotation.SerializeInHttpHeader))
        );
        var result = Result<List<ContactDto>>.Ok([contact1, contact2, contact3], metadata);
        return result.ToMinimalApiResult();
    }

    private static LightResult<ContactDto> GetContact(Guid id)
    {
        var contactDto = new ContactDto { Id = id, Name = "Foo" };
        var result = Result<ContactDto>.Ok(contactDto);
        return result.ToMinimalApiResult();
    }

    private static LightResult<ContactDto> CreateContact(ContactDto contactDto)
    {
        var result = Result<ContactDto>.Ok(contactDto);
        return result.ToHttp201CreatedMinimalApiResult(location: $"/api/contacts/{contactDto.Id}");
    }
}
