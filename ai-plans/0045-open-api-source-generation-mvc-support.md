# OpenAPI Source Generation MVC Support

## Rationale

The validation OpenAPI source generator already emits a framework-neutral static contract through `IPortableValidationOpenApiContract`. Minimal APIs can consume that contract via `ProducesPortableValidationProblemFor<TValidator>(...)`, but MVC actions still require manually duplicating the generated validation OpenAPI metadata on `[ProducesPortableValidationProblem]`.

This plan adds an MVC attribute-based adapter that applies the generated validator contract to the existing PortableResults OpenAPI metadata model. The source generator itself should remain unchanged and MVC-unaware.

## Acceptance Criteria

- [ ] MVC actions can document source-generated validation OpenAPI metadata with an attribute such as `[ProducesPortableValidationProblemFor<TValidator>]`.
- [ ] The new MVC attribute reuses `IPortableValidationOpenApiContract` and `PortableValidationProblemOpenApiBuilder` instead of introducing reflection, runtime validator instantiation, or MVC-specific generated source.
- [ ] The new MVC attribute ships from `Light.PortableResults.Validation.OpenApi` together with the existing source-generator APIs, without adding an OpenAPI dependency to `Light.PortableResults.AspNetCore.Mvc`.
- [ ] Existing Minimal API source-generation behavior remains unchanged.
- [ ] The attribute supports the same response-slot basics as `[ProducesPortableValidationProblem]`: status code, content type, validation problem format override, top-level metadata type, and `AllowUnknownErrorCodes`.
- [ ] Generated schema narrowing, inline metadata contracts, typed validation helper contracts, and response examples appear in MVC OpenAPI documents the same way they appear for Minimal APIs.
- [ ] Documentation is updated to describe both Minimal API and MVC source-generation usage, including the attribute-specific limitation around additive endpoint-local customization.
- [ ] Automated tests are written.

## Technical Details

Add a public MVC-facing attribute to `Light.PortableResults.Validation.OpenApi`, for example `ProducesPortableValidationProblemForAttribute<TValidator>`. It should inherit from `ProducesPortableValidationProblemAttribute` and constrain `TValidator` to `IPortableValidationOpenApiContract`.

The constructor should accept the same status-code and content-type values as `ProducesPortableValidationProblemAttribute`, call the base constructor, then apply the generated validator contract by wrapping the current attribute in a `PortableValidationProblemOpenApiBuilder`:

```csharp
TValidator.ConfigurePortableValidationOpenApi(
    new PortableValidationProblemOpenApiBuilder(this));
```

To make that possible without placing the attribute in `Light.PortableResults.AspNetCore.OpenApi`, change the existing `PortableValidationProblemOpenApiBuilder(ProducesPortableValidationProblemAttribute attribute)` constructor from `internal` to `public`. This is acceptable because the builder is already the public configuration API and the attribute is already public metadata.

Do not modify the source generator analysis or emitter for MVC. The generated partial validator should continue to implement only `IPortableValidationOpenApiContract`; framework-specific consumption stays in small adapter APIs.

Named attribute properties inherited from `ProducesPortableValidationProblemAttribute` and its base classes continue to work for overriding final metadata after the constructor runs, especially `Format`, `TopLevelMetadataType`, and `AllowUnknownErrorCodes`. Avoid promising additive named-argument customization for array properties such as `ErrorCodes`, `InlineErrorMetadataCodes`, or `ErrorExamples`, because setting those properties on an attribute replaces the data populated by the generated contract. Endpoint-local additions for MVC should usually be modeled with validator hints or, if necessary, a future explicitly additive attribute API.

Add tests in the validation OpenAPI and ASP.NET Core OpenAPI test areas. Cover direct attribute construction, generated MVC document output, response examples, format overrides, top-level metadata, and non-exhaustive unknown-code behavior. Existing Minimal API tests should continue to pass unchanged. Keep the code coverage over 90% - use coverlet.collector to validate your code changes.

Update the README source-generation section so it no longer describes the feature as Minimal API-only. Show the Minimal API helper and the MVC attribute side by side, and document the MVC customization caveat.
