# OpenAPI Source Generation MVC Customization

## Rationale

MVC can now consume generated validation OpenAPI contracts through `[ProducesPortableValidationProblemFor<TValidator>]`, but it does not yet have feature parity with Minimal APIs for endpoint-local customization. Minimal APIs can apply the generated validator contract and then append additional builder calls through the `configure` callback. MVC attributes currently rely on named properties after construction, and array properties such as `ErrorCodes`, `InlineErrorMetadataCodes`, and `ErrorExamples` replace generated metadata instead of appending to it.

This plan adds an attribute-friendly additive customization path for MVC while keeping the source generator framework-neutral.

## Acceptance Criteria

- [ ] MVC actions can append endpoint-local validation OpenAPI metadata after the generated validator contract is applied.
- [ ] MVC supports the same `PortableValidationProblemOpenApiBuilder` customization surface that Minimal APIs support in `ProducesPortableValidationProblemFor<TValidator>(configure: ...)`.
- [ ] Generated validator metadata remains preserved when MVC endpoint-local metadata adds error codes, inline metadata contracts, typed helper contracts, response examples, format overrides, top-level metadata, or unknown-code behavior.
- [ ] Existing `[ProducesPortableValidationProblemFor<TValidator>]` behavior remains unchanged.
- [ ] Existing Minimal API source-generation behavior remains unchanged.
- [ ] The source generator remains MVC-unaware and continues to emit only `IPortableValidationOpenApiContract`.
- [ ] No OpenAPI dependency is added to `Light.PortableResults.AspNetCore.Mvc`.
- [ ] Documentation is updated to describe the MVC customization pattern and remove the limitation around missing additive endpoint-local customization.
- [ ] Automated tests are written.

## Technical Details

Add a second MVC attribute type in `Light.PortableResults.Validation.OpenApi` which is called `ProducesPortableValidationProblemForAttribute<TValidator, TEndpointContract>`. It should inherit from `ProducesPortableValidationProblemAttribute` and constrain both generic parameters to `IPortableValidationOpenApiContract`.

The constructor should accept the same status code and content type values as the existing MVC source-generation attribute. It should create one `PortableValidationProblemOpenApiBuilder` for `this`, call `TValidator.ConfigurePortableValidationOpenApi(builder)` first, and then call `TEndpointContract.ConfigurePortableValidationOpenApi(builder)` second. This mirrors Minimal API ordering where generated metadata is applied before the endpoint-local `configure` callback.

Endpoint-local MVC customization should be expressed as a small hand-written contract type implementing `IPortableValidationOpenApiContract`. That type provides a static `ConfigurePortableValidationOpenApi` method and can call the same builder APIs as Minimal APIs: `UseFormat`, `WithMetadata`, `WithErrorCodes`, `WithErrorMetadata`, typed validation helper extensions, `WithErrorExample`, and `AllowUnknownErrorCodes`.

Do not change `ValidatorOpenApiAnalyzer`, `ValidatorOpenApiEmitter`, or generated validator source. The existing one-generic-parameter MVC attribute remains the simple path for endpoints that only need generated validator metadata plus scalar named-property overrides.

Add unit tests for the new attribute proving that endpoint-local metadata appends to generated arrays instead of replacing them. Add MVC OpenAPI document integration tests comparing the new two-contract MVC attribute with an equivalent Minimal API endpoint using the `configure` callback. Update README source-generation examples to show Minimal API callback customization and MVC contract-based customization side by side.

Keep the test code coverage at least above 90%.
