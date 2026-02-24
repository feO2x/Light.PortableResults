# Light.Results

*A lightweight .NET library implementing the Result Pattern where each result is serializable and deserializable. Comes
with integrations for ASP.NET Core Minimal APIs and MVC, `HttpResponseMessage`, and CloudEvents JSON format.*

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](https://github.com/feO2x/Light.Results/blob/main/LICENSE)
[![NuGet](https://img.shields.io/badge/NuGet-1.0.0-blue.svg?style=for-the-badge)](https://www.nuget.org/packages/Light.Results/0.1.0/)
[![Documentation](https://img.shields.io/badge/Docs-Changelog-yellowgreen.svg?style=for-the-badge)](https://github.com/feO2x/Light.Results/releases)

## ✨ Key Features

- 🧱 **Simple result model** — a `Result` / `Result<T>` is either a success value or one or more errors.
- 📝 **Structured errors** — errors can include message, code, target, category, and metadata.
- 🗂️ **Serializable metadata system** — metadata uses dedicated JSON-like types (instead of `Dictionary<string, object>`) so results stay reliably serializable.
- 🔁 **Functional helpers included** — common operations like `Map`, `Bind`, `Match`, and `Tap` are built in.
- 🌐 **HTTP support** — results can be serialized/deserialized for HTTP, including RFC-9457 / RFC-7807 Problem Details style payloads.
- ☁️ **CloudEvents JSON support** — results can be read/written for asynchronous messaging scenarios with CloudEvents Spec 1.0.
- 🧩 **ASP.NET Core integration** — dedicated packages for Minimal APIs and MVC allow you to easily transform `Result` / `Result<T>` to HTTP responses, supporting RFC-9457 / RFC-7807 Problem Details.
- ⚡ **Performance-oriented** — designed for minimal overhead using fast conversions and minimal allocations to reduce GC pressure.

## 📦 Installation

Install only the packages you need for your scenario.

- Core Result Pattern, Metadata, Functional Operators, and serialization support for HTTP and CloudEvents:

```bash
dotnet add package Light.Results
```

- ASP.NET Core Minimal APIs integration with support for Dependency Injection and `IResult`:

```bash
dotnet add package Light.Results.AspNetCore.MinimalApis
```

- ASP.NET Core MVC integration with support for Dependency Injection and `IActionResult`:

```bash
dotnet add package Light.Results.AspNetCore.Mvc
```

If you only need the Result Pattern itself, install `Light.Results` only.

## 🚀 Quick Start

```csharp
using System.Collections.Generic;
using Light.Results;
using Light.Results.AspNetCore.MinimalApis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLightResultsForMinimalApis();

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

public sealed class UpdateUserDto
{
	public string? Email { get; set; }
}

public sealed class UserDto
{
	public Guid Id { get; set; }
	public string Email { get; set; } = string.Empty;
}
```



