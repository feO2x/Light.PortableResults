# Definition and Check Restructuring

## Rationale

The validation package currently has two different organizational problems in the same area. The `Checks` type is already split across several partial files, but the grouping is not consistent with how developers discover assertions. Some methods live in files whose names do not describe the assertion family clearly, such as `IsNotNull`, `IsNull`, and the various `IsEmpty` / `IsNotEmpty` overloads in `Checks.Equality.cs`. At the same time, `BuiltInValidationErrorDefinitions` has grown into a single large file that contains the public catalog API, all built-in concrete definition types, shared helper methods, and cache-key structs in one place.

This makes the project harder to navigate than necessary. A contributor who looks for all overloads of one assertion family should not have to guess whether they were grouped under strings, collections, equality, or some broader "common" bucket. The same applies to the built-in definition side: a contributor who finds a built-in check should be able to predict where the corresponding built-in definition lives, and vice versa.

The goal of this plan is therefore to establish one consistent assertion-centric file structure across both `Checks` and `BuiltInValidationErrorDefinitions`. Assertion families such as `Null`, `Empty`, `Equality`, `Comparable`, `Strings`, and `Count` should each have one obvious home, and the checks side should mirror the definitions side exactly. This improves locality, reduces ambiguity in future growth, and keeps the public API unchanged while making the implementation easier to understand and maintain.

## Acceptance Criteria

- [x] `Checks` is reorganized into assertion-centric partial files that use the same family boundaries as the built-in validation error definitions: `Null`, `Empty`, `Equality`, `Comparable`, `Strings`, `Count`, `Enums`, `Decimals`, and `Predicate`.
- [x] All overloads of `IsNotNull` and `IsNull` are colocated in `Checks.Null.cs`, and the corresponding built-in definitions are colocated in `BuiltInValidationErrorDefinitions.Null.cs`.
- [x] All overloads of `IsEmpty` and `IsNotEmpty` are colocated in `Checks.Empty.cs`, including the string, `Guid`, collection, and `ImmutableArray<T>` variants, and the corresponding built-in definitions are colocated in `BuiltInValidationErrorDefinitions.Empty.cs`.
- [x] `IsNotNullOrWhiteSpace` remains in the string-oriented group together with the other string assertions in `Checks.Strings.cs`, and its built-in definition remains with the other string-oriented definitions in `BuiltInValidationErrorDefinitions.Strings.cs`.
- [x] `Checks.Equality.cs` contains only equality assertions after the restructuring, and `Checks.Count.cs` contains only count-based assertions after the restructuring.
- [x] `BuiltInValidationErrorDefinitions` is converted to a public static partial type whose implementation is split across matching files: `BuiltInValidationErrorDefinitions.Null.cs`, `.Empty.cs`, `.Equality.cs`, `.Comparable.cs`, `.Strings.cs`, `.Count.cs`, `.Enums.cs`, `.Decimals.cs`, and `.Predicate.cs`.
- [x] `BuiltInValidationErrorDefinitions` remains the single public built-in definition catalog. This plan does not introduce additional public helper or catalog types for custom-definition authoring.
- [x] The built-in definition members and nested definition classes are colocated by assertion family so that each `Checks.*.cs` file has a directly corresponding `BuiltInValidationErrorDefinitions.*.cs` file wherever a built-in definition exists for that assertion family.
- [x] The old grouping files that are superseded by the new assertion-centric layout are removed or renamed as part of the restructuring, so the codebase does not retain obsolete parallel file structures such as `Checks.Collections.cs` beside `Checks.Count.cs`.
- [x] Private helper methods and private cache-key types from `BuiltInValidationErrorDefinitions` remain non-public. They are either kept in one shared implementation file or placed in the relevant family files when only used there.
- [x] The restructuring does not change the public names, including the existing nested built-in definition type names, validation behavior, error codes, metadata shape, caching semantics, or namespace placement of the existing built-in checks and definitions.
- [x] Automated tests are updated or extended as needed, and the validation test suite continues to verify that the restructuring caused no behavioral regressions.

## Technical Details

Keep `Checks` as a `public static partial class` in `Light.PortableResults.Validation`, but change the file boundaries from broad semantic groups to assertion families. The goal is that a developer can open one file and see all overloads that belong to one assertion family without having to decide first whether that family is primarily about strings, collections, equality, or presence.

Use the following file structure on the checks side:

| File | Members |
|---|---|
| `Checks.Null.cs` | `IsNotNull`, `IsNull` |
| `Checks.Empty.cs` | all `IsEmpty` / `IsNotEmpty` overloads |
| `Checks.Equality.cs` | `IsEqualTo`, `IsNotEqualTo` |
| `Checks.Comparable.cs` | `IsGreaterThan`, `IsGreaterThanOrEqualTo`, `IsLessThan`, `IsLessThanOrEqualTo`, `IsIn`, `IsNotIn`, `IsInExclusiveRange` |
| `Checks.Strings.cs` | `IsNotNullOrWhiteSpace`, `HasMinLength`, `HasMaxLength`, `HasLengthIn`, `Matches`, `IsEmail`, `ContainsOnlyDigits`, `ContainsOnlyLettersAndDigits` |
| `Checks.Count.cs` | `HasCount`, `HasMinCount`, `HasMaxCount` |
| `Checks.Enums.cs` | `IsInEnum`, `IsEnumName` |
| `Checks.Decimals.cs` | `HasPrecisionAndScale` |
| `Checks.Predicate.cs` | `Must`, `Custom` |

This layout intentionally keeps all overloads of `IsEmpty` / `IsNotEmpty` together, even though some operate on strings and some on collection-like types. The same reasoning keeps `IsNotNullOrWhiteSpace` in `Checks.Strings.cs`: callers looking for string assertions should find all string-specific assertion families in the same place. Count-based assertions should similarly move out of `Checks.Collections.cs` into `Checks.Count.cs` so the file name describes the assertion family rather than the runtime shapes that happen to be supported.

Keep `BuiltInValidationErrorDefinitions` as the single public catalog type in `Light.PortableResults.Validation.Definitions`, but declare it as `partial` and split the implementation into matching assertion-family files. The mapping should be:

| File | Members / nested definition types |
|---|---|
| `BuiltInValidationErrorDefinitions.Null.cs` | `NotNull`, `Null` |
| `BuiltInValidationErrorDefinitions.Empty.cs` | `Empty`, `NotEmpty` |
| `BuiltInValidationErrorDefinitions.Equality.cs` | `EqualTo`, `NotEqualTo` |
| `BuiltInValidationErrorDefinitions.Comparable.cs` | `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `IsIn`, `IsNotIn`, `IsInExclusiveRange` |
| `BuiltInValidationErrorDefinitions.Strings.cs` | `NotNullOrWhiteSpace`, `MinLength`, `MaxLength`, `LengthIn`, `Matches`, `Email`, `DigitsOnly`, `LettersAndDigitsOnly` |
| `BuiltInValidationErrorDefinitions.Count.cs` | `Count`, `MinCount`, `MaxCount` |
| `BuiltInValidationErrorDefinitions.Enums.cs` | `IsInEnum`, `EnumName` |
| `BuiltInValidationErrorDefinitions.Decimals.cs` | `PrecisionScale` |
| `BuiltInValidationErrorDefinitions.Predicate.cs` | `Predicate` |

Each `BuiltInValidationErrorDefinitions.*.cs` file should contain both the public catalog members and the nested concrete definition types that belong to that assertion family so that the structure mirrors the `Checks` side wherever a built-in definition exists. `Checks.Predicate.cs` is the intentional exception on the checks side because `Custom` is imperative validation logic and therefore does not require a matching built-in definition. For example, `BuiltInValidationErrorDefinitions.Empty.cs` should contain the shared `Empty` and `NotEmpty` definition instances together with the nested `EmptyValidationErrorDefinition` and `NotEmptyValidationErrorDefinition` types. `BuiltInValidationErrorDefinitions.Strings.cs` should contain both `NotNullOrWhiteSpace` and the rest of the string-oriented definitions because they belong to the same assertion-discovery path even though `NotNullOrWhiteSpace` also has guard semantics.

Do not turn the existing helper methods and cache-key structs into public API. Methods such as metadata creation and stable-provider resolution, as well as helper structs such as the various definition-cache keys, are implementation details of the built-in definition catalog. If a helper is used only by one family, place it in that family file as a private member. If a helper is genuinely shared across multiple families, keep it in one shared private implementation file such as `BuiltInValidationErrorDefinitions.Shared.cs`. The same applies to the private enum-definition cache.

This plan is intentionally structural, not behavioral. Avoid renaming public members, changing namespaces, changing XML-doc contracts, redesigning the cache model, renaming the existing nested built-in definition types, or reshaping the broader folder and namespace structure of the validation project. The expected code changes are file splits, movement of members between partial files, removal or renaming of superseded grouping files within the existing `Checks` and `Definitions` areas, and any small `using` or visibility adjustments required to keep the refactored layout compiling cleanly. Tests should primarily guard against accidental regressions in behavior while the code is being rearranged rather than introducing new validation semantics.
