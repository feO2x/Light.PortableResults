#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
FEED_DIR="${TMPDIR:-/tmp}/light-portable-results-validation-openapi-feed"
CONSUMER_DIR="${TMPDIR:-/tmp}/light-portable-results-validation-openapi-consumer"
CONFIGURATION="${CONFIGURATION:-Release}"

rm -rf "${FEED_DIR}" "${CONSUMER_DIR}"
mkdir -p "${FEED_DIR}" "${CONSUMER_DIR}"

dotnet pack "${REPO_ROOT}/src/Light.PortableResults/Light.PortableResults.csproj" \
  --configuration "${CONFIGURATION}" \
  --output "${FEED_DIR}"
dotnet pack "${REPO_ROOT}/src/Light.PortableResults.AspNetCore.Shared/Light.PortableResults.AspNetCore.Shared.csproj" \
  --configuration "${CONFIGURATION}" \
  --output "${FEED_DIR}"
dotnet pack "${REPO_ROOT}/src/Light.PortableResults.AspNetCore.OpenApi/Light.PortableResults.AspNetCore.OpenApi.csproj" \
  --configuration "${CONFIGURATION}" \
  --output "${FEED_DIR}"
dotnet pack "${REPO_ROOT}/src/Light.PortableResults.Validation/Light.PortableResults.Validation.csproj" \
  --configuration "${CONFIGURATION}" \
  --output "${FEED_DIR}"
dotnet pack "${REPO_ROOT}/src/Light.PortableResults.Validation.OpenApi/Light.PortableResults.Validation.OpenApi.csproj" \
  --configuration "${CONFIGURATION}" \
  --output "${FEED_DIR}"

dotnet new web --framework net10.0 --output "${CONSUMER_DIR}"

cat > "${CONSUMER_DIR}/NuGet.config" <<NUGET
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local-light-portable-results" value="${FEED_DIR}" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
NUGET

cat > "${CONSUMER_DIR}/Directory.Build.props" <<'PROPS'
<Project>
  <PropertyGroup>
    <InterceptorsNamespaces>$(InterceptorsNamespaces);Microsoft.AspNetCore.OpenApi.Generated</InterceptorsNamespaces>
  </PropertyGroup>
</Project>
PROPS

dotnet add "${CONSUMER_DIR}/light-portable-results-validation-openapi-consumer.csproj" package \
  Light.PortableResults.Validation.OpenApi \
  --version 0.6.0 \
  --no-restore

cat > "${CONSUMER_DIR}/Program.cs" <<'CSHARP'
using Light.PortableResults.AspNetCore.OpenApi;
using Light.PortableResults.Validation;
using Light.PortableResults.Validation.OpenApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddPortableResultsOpenApi(contracts => contracts.RegisterBuiltInValidationErrors());

var app = builder.Build();
app.MapPost("/ratings", static () => Results.BadRequest())
   .ProducesPortableValidationProblemFor<RatingValidator>();

public sealed class RatingDto
{
    public int Rating { get; init; }
}

[GeneratePortableValidationOpenApi]
public sealed partial class RatingValidator : Validator<RatingDto>
{
    public RatingValidator(IValidationContextFactory validationContextFactory)
        : base(validationContextFactory) { }

    protected override ValidatedValue<RatingDto> PerformValidation(
        ValidationContext context,
        ValidationCheckpoint checkpoint,
        RatingDto dto
    )
    {
        context.Check(dto.Rating).IsInRange(1, 5);
        return checkpoint.ToValidatedValue(dto);
    }
}
CSHARP

dotnet restore "${CONSUMER_DIR}/light-portable-results-validation-openapi-consumer.csproj" \
  --configfile "${CONSUMER_DIR}/NuGet.config"

dotnet build "${CONSUMER_DIR}/light-portable-results-validation-openapi-consumer.csproj" \
  --configuration "${CONFIGURATION}" \
  --no-restore
