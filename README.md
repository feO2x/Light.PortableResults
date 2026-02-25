# Light.PortableResults

*A lightweight .NET library implementing the Result Pattern where each result is serializable and deserializable. Comes
with integrations for ASP.NET Core Minimal APIs and MVC, `HttpResponseMessage`, and CloudEvents JSON format.*

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-0.1.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Light.PortableResults/0.1.0/)
[![Documentation](https://img.shields.io/badge/Docs-Changelog-yellowgreen.svg?style=for-the-badge)](https://github.com/feO2x/Light.PortableResults/releases)

## ✨ Key Features

- 🧱 **Zero-boilerplate result model** — `Result` / `Result<T>` is either a success value or one or more structured errors. No exceptions for expected failures.
- 📝 **Rich, machine-readable errors** — every `Error` carries a human-readable `Message`, stable `Code`, input `Target`, and `Category` — ready for API contracts and frontend mapping.
- 🗂️ **Serialization-safe metadata** — metadata uses a dedicated JSON-like type system instead of
  `Dictionary<string, object>`, so results serialize reliably across any protocol.
- 🔁 **Full functional operator suite** — `Map`, `Bind`, `Match`, `Ensure`, `Tap`, `Switch`, and their `Async` variants let you build clean, chainable pipelines.
- 🌐 **HTTP-native** — serialize results as HTTP response, including automatic support for RFC-9457 Problem Details, and
  deserialize `HttpResponseMessage` back into typed `Result` / `Result<T>`. Full round-trip support included.
- 🧩 **ASP.NET Core ready** — Minimal APIs and MVC packages translate `Result` and `Result<T>` directly to `IResult` /
  `IActionResult` with automatic HTTP status mapping and RFC-9457 Problem Details support.
- ☁️ **CloudEvents JSON support** — publish and consume results as CloudEvents Spec 1.0 payloads for reliable async
  messaging. Full round-trip support included.
- ⚡ **Allocation-minimal by design** — pooled buffers, struct-friendly internals, and fast paths keep GC pressure near zero even at high throughput.

## 📦 Installation

Install only the packages you need for your scenario.

- Core Result Pattern, Metadata, Functional Operators, and serialization support for HTTP and CloudEvents:

```bash
dotnet add package Light.PortableResults
```

- ASP.NET Core Minimal APIs integration with support for Dependency Injection and `IResult`:

```bash
dotnet add package Light.PortableResults.AspNetCore.MinimalApis
```

- ASP.NET Core MVC integration with support for Dependency Injection and `IActionResult`:

```bash
dotnet add package Light.PortableResults.AspNetCore.Mvc
```

If you only need the Result Pattern itself, install `Light.PortableResults` only.

## 🚀 HTTP Quick Start

### Minimal APIs

```csharp
using System;
using System.Collections.Generic;
using Light.PortableResults;
using Light.PortableResults.AspNetCore.MinimalApis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddPortableResultsForMinimalApis();

var app = builder.Build();

app.MapPut("/users/{id:guid}", (Guid id, UpdateUserDto dto) =>
{
	var result = UpdateUser(id, dto);
	return result.ToMinimalApiResult(); // LightResult<T> implements IResult
});

app.Run();

static Result<UserDto> UpdateUser(Guid id, UpdateUserDto dto)
{
	List<Error> errors = [];

	if (id == Guid.Empty)
	{
		errors.Add(new Error
		{
			Message = "User id must not be empty",
			Code = "user.invalid_id",
			Target = "id",
			Category = ErrorCategory.Validation
		});
	}

	if (string.IsNullOrWhiteSpace(dto.Email))
	{
		errors.Add(new Error
		{
			Message = "Email is required",
			Code = "user.email_required",
			Target = "email",
			Category = ErrorCategory.Validation
		});
	}

	if (errors.Count > 0)
	{
		return Result<UserDto>.Fail(errors.ToArray());
	}

	var response = new UserDto
	{
		Id = id,
		Email = dto.Email
	};

	return Result<UserDto>.Ok(response);
}

public sealed record UpdateUserDto
{
	public string? Email { get; init; }
}

public sealed record UserDto
{
	public Guid Id { get; set; }
	public string Email { get; init; } = string.Empty;
}
```

### MVC

```csharp
using System;
using System.Collections.Generic;
using Light.PortableResults;
using Light.PortableResults.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("users")]
public sealed class UsersController : ControllerBase
{
	[HttpPut("{id:guid}")]
	public LightActionResult<UserDto> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
	{
		var result = ValidateAndUpdate(id, dto);
		return result.ToMvcActionResult();
	}

	private static Result<UserDto> ValidateAndUpdate(Guid id, UpdateUserDto dto)
	{
		List<Error> errors = [];

		if (id == Guid.Empty)
		{
			errors.Add(new Error
			{
				Message = "User id must not be empty",
				Code = "user.invalid_id",
				Target = "id",
				Category = ErrorCategory.Validation
			});
		}

		if (string.IsNullOrWhiteSpace(dto.Email))
		{
			errors.Add(new Error
			{
				Message = "Email is required",
				Code = "user.email_required",
				Target = "email",
				Category = ErrorCategory.Validation
			});
		}

		if (errors.Count > 0)
		{
			return Result<UserDto>.Fail(errors.ToArray());
		}

		return Result<UserDto>.Ok(new UserDto
		{
			Id = id,
			Email = dto.Email!
		});
	}
}

public sealed record UpdateUserDto
{
	public string? Email { get; set; }
}

public sealed record UserDto
{
	public Guid Id { get; init; }
	public string Email { get; init; } = string.Empty;
}
```

MVC setup in `Program.cs`:

```csharp
builder.Services.AddControllers();
builder.Services.AddPortableResultsForMvc();

var app = builder.Build();
app.MapControllers();
```

### HTTP Response On the Wire

For both examples above (Minimal APIs and MVC), the HTTP response shape is the same.

Successful update (`200 OK`):

```http
PUT /users/6b8a4dca-779d-4f36-8274-487fe3e86b5a
Content-Type: application/json

{
	"email": "ada@example.com"
}
```

```http
HTTP/1.1 200 OK
Content-Type: application/json

{
	"id": "6b8a4dca-779d-4f36-8274-487fe3e86b5a",
	"email": "ada@example.com"
}
```

Validation failure (`400 Bad Request`):

```http
PUT /users/00000000-0000-0000-0000-000000000000
Content-Type: application/json

{}
```

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json

{
	"type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
	"title": "Bad Request",
	"status": 400,
	"detail": "One or more validation errors occurred.",
    // By default, we use ASP.NET Core compatible errors format here for backwards-compatibility.
    // We encourage you to use ValidationProblemSerializationFormat.Rich instead!
    "errors": {
		"id": ["User id must not be empty"],
		"email": ["Email is required"]
	},
	"errorDetails": [
		{
			"target": "id",
			"index": 0,
			"code": "user.invalid_id",
			"category": "Validation"
		},
		{
			"target": "email",
			"index": 0,
			"code": "user.email_required",
			"category": "Validation"
		}
	]
}
```

### Deserializing Result<T> back from HttpResponseMessage

```csharp
using System;
using System.Net.Http;
using System.Net.Http.Json;
using Light.PortableResults;
using Light.PortableResults.Http.Reading;

using var httpClient = new HttpClient
{
	BaseAddress = new Uri("https://localhost:5001")
};

var request = new UpdateUserDto { Email = "ada@example.com" };

using var response = await httpClient.PutAsJsonAsync(
	"/users/6b8a4dca-779d-4f36-8274-487fe3e86b5a",
	request
);

Result<UserDto> result = await response.ReadResultAsync<UserDto>();

if (result.IsValid)
{
	Console.WriteLine($"Updated user: {result.Value.Email}");
}
else
{
	foreach (var error in result.Errors)
	{
		Console.WriteLine($"{error.Target}: {error.Message}");
	}
}
```

## ☁️ CloudEvents Quick Start

The following example uses `RabbitMQ.Client` to publish and consume a CloudEvents JSON message carrying `Result<UserDto>`.

### Publish to RabbitMQ

```csharp
using System;
using Light.PortableResults;
using Light.PortableResults.CloudEvents;
using Light.PortableResults.CloudEvents.Writing;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "users.updated", durable: true, exclusive: false, autoDelete: false);

var result = Result<UserDto>.Ok(new UserDto
{
	Id = Guid.Parse("6b8a4dca-779d-4f36-8274-487fe3e86b5a"),
	Email = "ada@example.com"
});

byte[] cloudEvent = result.ToCloudEvent(
	successType: "users.updated",
	failureType: "users.update.failed",
	source: "urn:light-portable-results:sample:user-service",
	subject: "users/6b8a4dca-779d-4f36-8274-487fe3e86b5a"
);

var properties = new BasicProperties();
properties.ContentType = CloudEventsConstants.CloudEventsJsonContentType;

await channel.BasicPublishAsync(
	exchange: "",
	routingKey: "users.updated",
	mandatory: false,
	basicProperties: properties,
	body: cloudEvent
);
```

### Consume from RabbitMQ

```csharp
using Light.PortableResults;
using Light.PortableResults.CloudEvents.Reading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };
await using var connection = await factory.CreateConnectionAsync();
await using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "users.updated", durable: true, exclusive: false, autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (_, eventArgs) =>
{
	Result<UserDto> result = eventArgs.Body.ReadResult<UserDto>();

	if (result.IsValid)
	{
		Console.WriteLine($"Updated user: {result.Value.Email}");
	}
	else
	{
		foreach (var error in result.Errors)
		{
			Console.WriteLine($"{error.Target}: {error.Message}");
		}
	}

	await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
};

await channel.BasicConsumeAsync(queue: "users.updated", autoAck: false, consumer: consumer);
```

## 🧠 Basic Usage

If you are new to the Result Pattern, think of it like this:

- A method can either succeed or fail.
- Instead of throwing exceptions for expected failures (validation, not found, conflicts), the method returns a value that explicitly describes the outcome.
- Callers must handle both paths on purpose, which makes control flow easier to read and test.

With Light.PortableResults:

- `Result<T>` means: either a success value of type `T`, or one or more errors.
- `Result` (non-generic) means: success/failure without a return value (corresponds to `void`).
- Each `Error` can carry machine-readable details such as `Code`, `Target`, `Category`, and `Metadata`.

### When to use Result vs exceptions

Use `Result` / `Result<T>` for expected business outcomes:

- validation failed
- resource not found
- user is not authorized
- domain rule was violated

Use exceptions for truly unexpected failures:

- database/network outage
- misconfiguration
- programming bugs and invariant violations (detected via Guard Clauses)

This keeps exceptions exceptional, and keeps business outcomes explicit.

### Create success and failure results

```csharp
using Light.PortableResults;

Result<int> success = Result<int>.Ok(42);

var error = new Error
{
	Message = "The provided id is invalid",
	Code = "user.invalid_id",
	Target = "id",
	Category = ErrorCategory.Validation
};

Result<int> failure = Result<int>.Fail(error);
```

### Use non-generic `Result` for command-style operations

```csharp
using Light.PortableResults;

static Result DeleteUser(Guid id)
{
	if (id == Guid.Empty)
	{
		return Result.Fail(new Error
		{
			Message = "User id must not be empty",
			Code = "user.invalid_id",
			Target = "id",
			Category = ErrorCategory.Validation
		});
	}

	return Result.Ok();
}
```

### Return multiple validation errors

```csharp
using System.Collections.Generic;
using Light.PortableResults;

static Result<string> ValidateUser(string? name, string? email)
{
	List<Error> errors = [];

	if (string.IsNullOrWhiteSpace(name))
	{
		errors.Add(new Error
		{
			Message = "Name is required",
			Code = "user.name_required",
			Target = "name",
			Category = ErrorCategory.Validation
		});
	}

	if (string.IsNullOrWhiteSpace(email))
	{
		errors.Add(new Error
		{
			Message = "Email is required",
			Code = "user.email_required",
			Target = "email",
			Category = ErrorCategory.Validation
		});
	}

	if (errors.Count > 0)
	{
		return Result<string>.Fail(errors.ToArray());
	}

	return Result<string>.Ok("ok");
}
```

### Consume a result safely

`Result<T>.Value` is only valid when `IsValid` is `true`, otherwise an exception is thrown. Light.PortableResults
supports both imperative and functional styles.

Imperative / structured programming (`if/else`):

```csharp
using System;
using Light.PortableResults;

Result<int> result = GetCount();

if (result.IsValid)
{
	Console.WriteLine($"Count: {result.Value}");
}
else
{
	foreach (var error in result.Errors)
	{
		Console.WriteLine($"{error.Target}: {error.Message}");
	}
}
```

Functional style (`Match`):

```csharp
using Light.PortableResults;
using Light.PortableResults.FunctionalExtensions;

Result<int> result = GetCount();

string text = result.Match(
	onSuccess: count => $"Count: {count}",
	onError: errors => $"Request failed: {errors.First.Message}"
);
```

Supported functional operators:

| Category | Operators | What they are used for |
| --- | --- | --- |
| Transform success value | `Map`, `Bind` | Convert successful values or chain operations that already return `Result<T>`. |
| Transform errors | `MapError` | Normalize or translate errors (for example domain -> transport layer). |
| Add validation rules | `Ensure`, `FailIf` | Keep fluent pipelines while adding business or guard conditions. |
| Handle outcomes | `Match`, `MatchFirst`, `Else` | Turn a result into a value/fallback without manually branching every time. |
| Side effects | `Tap`, `TapError`, `Switch`, `SwitchFirst` | Perform logging/metrics/notifications on success or failure paths. |

All operators also provide async variants with the `Async` suffix (for example `BindAsync`, `MatchAsync`, `TapErrorAsync`).

Example pipeline:

```csharp
using Light.PortableResults;
using Light.PortableResults.FunctionalExtensions;

Result<string> message = GetUser(userId)
	.Ensure(user => user.IsActive, new Error
	{
		Message = "User is not active",
		Code = "user.inactive",
		Category = ErrorCategory.Forbidden
	})
	.Map(user => user.Email)
	.Match(
		onSuccess: email => $"User email: {email}",
		onError: errors => $"Failed: {errors.First.Message}"
	);
```

### Keep error payloads useful for clients

As a rule of thumb:

- `Message`: human-readable explanation
- `Code`: stable machine-readable identifier (great for frontend/API contracts)
- `Target`: which input field/header/value failed
- `Category`: determines transport mapping (for example HTTP status)

Using a consistent error shape early will make your APIs and message consumers easier to evolve.

## ⚙️ Configuration

### HTTP write options (`PortableResultsHttpWriteOptions`)

| Option                                 | Default                | Description                                                                                                                                                                                               |
|----------------------------------------|------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ValidationProblemSerializationFormat` | `AspNetCoreCompatible` | Controls how validation errors are serialized for HTTP 400/422 responses. Defaults to `AspNetCoreCompatible` for backwards-compatibility, we encourage you to use `Rich`.                                 |
| `MetadataSerializationMode`            | `ErrorsOnly`           | Controls whether metadata is serialized in response bodies (`ErrorsOnly` or `Always`).                                                                                                                    |
| `CreateProblemDetailsInfo`             | `null`                 | Optional custom factory for generating Problem Details fields (`type`, `title`, `detail`, etc.).                                                                                                          |
| `FirstErrorCategoryIsLeadingCategory`  | `true`                 | If `true`, the first error category decides the HTTP status code for failures. If `false`, Light.PortableResults checks if all errors have the same category and chooses `Unclassified` when they differ. |

### HTTP read options (`PortableResultsHttpReadOptions`)

| Option | Default | Description |
| --- | --- | --- |
| `HeaderParsingService` | `ParseNoHttpHeadersService.Instance` | Controls how HTTP headers are converted into metadata (default: skip all headers). |
| `MergeStrategy` | `AddOrReplace` | Strategy used when merging metadata with the same key from headers and body. |
| `PreferSuccessPayload` | `Auto` | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`). |
| `TreatProblemDetailsAsFailure` | `true` | If `true`, `application/problem+json` is treated as failure even for 2xx status codes. |
| `SerializerOptions` | `Module.DefaultSerializerOptions` | System.Text.JSON serializer options used for deserialization. |

### CloudEvents write options (`PortableResultsCloudEventsWriteOptions`)

| Option | Default | Description |
| --- | --- | --- |
| `Source` | `null` | Default CloudEvents `source` URI reference if not set per call. |
| `MetadataSerializationMode` | `Always` | Controls whether metadata is serialized into CloudEvents `data`. |
| `SerializerOptions` | `Module.DefaultSerializerOptions` | System.Text.JSON serializer options used for deserialization. |
| `ConversionService` | `DefaultCloudEventsAttributeConversionService.Instance` | Converts metadata entries into CloudEvents extension attributes. |
| `SuccessType` | `null` | Default CloudEvents `type` for successful results. |
| `FailureType` | `null` | Default CloudEvents `type` for failed results. |
| `Subject` | `null` | Default CloudEvents `subject`. |
| `DataSchema` | `null` | Default CloudEvents `dataschema` URI. |
| `Time` | `null` | Default CloudEvents `time` value (`UTC now` is used when omitted). |
| `IdResolver` | `null` | Optional function used to generate CloudEvents `id` values. |
| `ArrayPool` | `ArrayPool<byte>.Shared` | Buffer pool used for CloudEvents serialization. |
| `PooledArrayInitialCapacity` | `RentedArrayBufferWriter.DefaultInitialCapacity` | Initial buffer size used for pooled serialization, which is 2048 bytes. |

### CloudEvents read options (`PortableResultsCloudEventsReadOptions`)

| Option | Default | Description |
| --- | --- | --- |
| `SerializerOptions` | `Module.DefaultSerializerOptions` | System.Text.JSON serializer options used for deserialization. |
| `PreferSuccessPayload` | `Auto` | How to interpret successful payloads (`Auto`, `BareValue`, `WrappedValue`). |
| `IsFailureType` | `null` | Optional fallback classifier to decide failure based on CloudEvents `type`. |
| `ParsingService` | `null` | Optional parser for mapping extension attributes to metadata. |
| `MergeStrategy` | `AddOrReplace` | Strategy used when merging envelope extension attributes and payload metadata. |

### Configure HTTP behavior

```csharp
using Light.PortableResults.Http.Writing;
using Light.PortableResults.SharedJsonSerialization;

builder.Services.Configure<PortableResultsHttpWriteOptions>(options =>
{
	options.ValidationProblemSerializationFormat = ValidationProblemSerializationFormat.Rich;
	options.MetadataSerializationMode = MetadataSerializationMode.Always;
	options.FirstErrorCategoryIsLeadingCategory = false;
});
```

```csharp
using Light.PortableResults.Http.Reading;
using Light.PortableResults.Http.Reading.Headers;
using Light.PortableResults.Http.Reading.Json;

var readOptions = new PortableResultsHttpReadOptions
{
	HeaderParsingService = new DefaultHttpHeaderParsingService(new AllHeadersSelectionStrategy()),
	PreferSuccessPayload = PreferSuccessPayload.Auto,
	TreatProblemDetailsAsFailure = true
};

Result<UserDto> result = await response.ReadResultAsync<UserDto>(readOptions);
```

### Configure CloudEvents behavior

```csharp
using Light.PortableResults.CloudEvents.Writing;
using Light.PortableResults.SharedJsonSerialization;

builder.Services.Configure<PortableResultsCloudEventsWriteOptions>(options =>
{
	options.Source = "urn:light-portable-results:sample:user-service";
	options.SuccessType = "users.updated";
	options.FailureType = "users.update.failed";
	options.MetadataSerializationMode = MetadataSerializationMode.Always;
});
```

```csharp
using System;
using Light.PortableResults.CloudEvents.Reading;
using Light.PortableResults.Http.Reading.Json;

var cloudReadOptions = new PortableResultsCloudEventsReadOptions
{
	IsFailureType = eventType => eventType.EndsWith(".failed", StringComparison.Ordinal),
	PreferSuccessPayload = PreferSuccessPayload.Auto
};

Result<UserDto> result = messageBody.ReadResult<UserDto>(cloudReadOptions);
```

### Supported Error Categories

| `ErrorCategory` | HTTP Status Code |
| --- | --- |
| `Unclassified` | `500` |
| `Validation` | `400` |
| `Unauthorized` | `401` |
| `PaymentRequired` | `402` |
| `Forbidden` | `403` |
| `NotFound` | `404` |
| `MethodNotAllowed` | `405` |
| `NotAcceptable` | `406` |
| `Timeout` | `408` |
| `Conflict` | `409` |
| `Gone` | `410` |
| `LengthRequired` | `411` |
| `PreconditionFailed` | `412` |
| `ContentTooLarge` | `413` |
| `UriTooLong` | `414` |
| `UnsupportedMediaType` | `415` |
| `RequestedRangeNotSatisfiable` | `416` |
| `ExpectationFailed` | `417` |
| `MisdirectedRequest` | `421` |
| `UnprocessableContent` | `422` |
| `Locked` | `423` |
| `FailedDependency` | `424` |
| `UpgradeRequired` | `426` |
| `PreconditionRequired` | `428` |
| `TooManyRequests` | `429` |
| `RequestHeaderFieldsTooLarge` | `431` |
| `UnavailableForLegalReasons` | `451` |
| `InternalError` | `500` |
| `NotImplemented` | `501` |
| `BadGateway` | `502` |
| `ServiceUnavailable` | `503` |
| `GatewayTimeout` | `504` |
| `InsufficientStorage` | `507` |
