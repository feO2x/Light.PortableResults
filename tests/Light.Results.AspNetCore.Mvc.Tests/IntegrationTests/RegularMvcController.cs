using System;
using System.Collections.Generic;
using Light.Results.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Light.Results.AspNetCore.Mvc.Tests.IntegrationTests;

[ApiController]
[Route("api/contacts")]
public sealed class RegularMvcController : ControllerBase
{
    [HttpGet]
    [ProducesLightResult<List<ContactDto>, Dictionary<string, long>>]
    public LightActionResult<List<ContactDto>> GetContacts()
    {
        var contact1 = new ContactDto { Id = new Guid("D8FC9BEC-0606-4E9B-8EB4-04558B2B9D40"), Name = "Foo" };
        var contact2 = new ContactDto { Id = new Guid("AAA41889-0BD8-4247-9C0F-049567FA63C1"), Name = "Bar" };
        var contact3 = new ContactDto { Id = new Guid("3D43850A-69D1-4230-8BAA-75AA6C693E9D"), Name = "Baz" };
        List<ContactDto> contacts = [contact1, contact2, contact3];

        var metadata = MetadataObject.Create(
            ("Count", MetadataValue.FromInt64(contacts.Count, MetadataValueAnnotation.SerializeInHttpHeader))
        );
        var result = Result<List<ContactDto>>.Ok([contact1, contact2, contact3], metadata);
        return result.ToMvcActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesLightResult<ContactDto>]
    public LightActionResult<ContactDto> GetContact(Guid id)
    {
        var contactDto = new ContactDto { Id = id, Name = "Foo" };
        var result = Result<ContactDto>.Ok(contactDto);
        return result.ToMvcActionResult();
    }

    [HttpPut]
    [ProducesLightResult<ContactDto>(statusCode: 201)]
    public LightActionResult<ContactDto> CreateContact([FromBody] ContactDto contactDto)
    {
        var result = Result<ContactDto>.Ok(contactDto);
        return result.ToHttp201CreatedMvcActionResult(location: $"/api/contacts/{contactDto.Id}");
    }

    [HttpGet("not-found/{id:guid}")]
    public LightActionResult<ContactDto> GetContactNotFound(Guid id)
    {
        var error = new Error
        {
            Message = $"Contact with id '{id}' was not found",
            Code = "ContactNotFound",
            Category = ErrorCategory.NotFound
        };
        var result = Result<ContactDto>.Fail(error);
        return result.ToMvcActionResult();
    }

    [HttpGet("validation-error")]
    public LightActionResult<ContactDto> GetContactValidationError()
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
        return result.ToMvcActionResult();
    }

    [HttpDelete("{id:guid}")]
    public LightActionResult DeleteContact(Guid id)
    {
        var result = Result.Ok();
        return result.ToMvcActionResult();
    }

    [HttpPost("action")]
    public LightActionResult PerformAction()
    {
        var result = Result.Ok();
        return result.ToHttp201CreatedMvcActionResult(location: "/api/contacts/action/completed");
    }

    [HttpPost("validate")]
    public LightActionResult ValidateContacts()
    {
        var error = new Error
        {
            Message = "Batch validation failed",
            Code = "BatchValidationError",
            Target = "",
            Category = ErrorCategory.Validation
        };
        var result = Result.Fail(error);
        return result.ToMvcActionResult();
    }
}
