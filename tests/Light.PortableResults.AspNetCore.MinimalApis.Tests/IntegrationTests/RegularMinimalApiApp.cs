using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Light.PortableResults.AspNetCore.MinimalApis.Tests.IntegrationTests;
using Light.PortableResults.Metadata;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Xunit;

[assembly: AssemblyFixture(typeof(RegularMinimalApiApp))]

namespace Light.PortableResults.AspNetCore.MinimalApis.Tests.IntegrationTests;

public sealed class RegularMinimalApiApp : IAsyncLifetime
{
    public RegularMinimalApiApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPortableResultsForMinimalApis();

        App = builder.Build();
        App.MapGet("/api/contacts", GetContacts);
        App.MapGet("/api/contacts/{id:guid}", GetContact);
        App.MapPut("/api/contacts", CreateContact);
        App.MapGet("/api/contacts/not-found/{id:guid}", GetContactNotFound);
        App.MapGet("/api/contacts/validation-error", GetContactValidationError);
        App.MapDelete("/api/contacts/{id:guid}", DeleteContact);
        App.MapPost("/api/contacts/action", PerformAction);
        App.MapPost("/api/contacts/validate", ValidateContacts);
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

    private static LightResult<ContactDto> GetContactNotFound(Guid id)
    {
        var error = new Error
        {
            Message = $"Contact with id '{id}' was not found",
            Code = "ContactNotFound",
            Category = ErrorCategory.NotFound
        };
        var result = Result<ContactDto>.Fail(error);
        return result.ToMinimalApiResult();
    }

    private static LightResult<ContactDto> GetContactValidationError()
    {
        Error[] errors =
        [
            new ()
            {
                Message = "Name is required", Code = "NameRequired", Target = "name",
                Category = ErrorCategory.Validation
            },
            new ()
            {
                Message = "Email must be valid", Code = "EmailInvalid", Target = "email",
                Category = ErrorCategory.Validation
            }
        ];
        var result = Result<ContactDto>.Fail(errors);
        return result.ToMinimalApiResult();
    }

    private static LightResult DeleteContact(Guid id)
    {
        var result = Result.Ok();
        return result.ToMinimalApiResult();
    }

    private static LightResult PerformAction()
    {
        var result = Result.Ok();
        return result.ToHttp201CreatedMinimalApiResult(location: "/api/contacts/action/completed");
    }

    private static LightResult ValidateContacts()
    {
        var error = new Error
        {
            Message = "Batch validation failed",
            Code = "BatchValidationError",
            Target = "",
            Category = ErrorCategory.Validation
        };
        var result = Result.Fail(error);
        return result.ToMinimalApiResult();
    }
}
