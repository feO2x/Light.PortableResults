# Validation Target Normalization Optimization

## Rationale

`DefaultValidationTargetNormalizer.NormalizeCore` currently performs several avoidable string allocations on cache misses, including whole-path trimming, per-segment substring creation, segment cleanup, and casing rewrites. This plan streamlines the miss path with a single-pass parser over the input characters so that validation target normalization remains semantically stable while producing less cold-path allocation churn for unique target expressions.

## Acceptance Criteria

- [x] `DefaultValidationTargetNormalizer.NormalizeCore` is redesigned to parse the raw path in a single pass without using `Substring` for member segments and without allocating intermediate strings for segment cleanup.
- [x] The optimized implementation preserves the current public normalization behavior for whitespace trimming, root-parameter removal, member separators, indexers, casing modes, empty inputs, and malformed indexers.
- [x] The implementation keeps the existing cache behavior and thread-safety characteristics of `DefaultValidationTargetNormalizer`.
- [x] The new normalization pipeline uses a stack-first output strategy with a safe fallback for longer targets so short and medium paths avoid unnecessary heap allocations beyond the final normalized string.
- [x] Any dead or redundant logic uncovered by the parser rewrite is removed or folded into the new flow without changing externally observable behavior.
- [x] Automated tests cover the optimized normalizer with representative simple, nested, indexed, whitespace-padded, verbatim-identifier, casing-preserving, empty, and malformed-path inputs.

## Technical Details

Keep the change localized to `Light.PortableResults.Validation.DefaultValidationTargetNormalizer`. The public API of `IValidationTargetNormalizer`, the constructor surface of `DefaultValidationTargetNormalizer`, and the surrounding cache ownership model should remain unchanged. The optimization target is the work done after a cache miss, not the dictionary lookup itself.

Replace the current normalization flow with a `ReadOnlySpan<char>`-based parser that walks the trimmed input once from left to right and writes to a `Span<char>`-backed output buffer. Instead of creating per-segment substrings and then post-processing them, compute the effective start and end indexes for each segment directly from the source characters. The parser should continue to strip the leading root segment before the first member separator when present, preserve bracketed indexers verbatim, and apply the configured casing convention only to actual member-name characters.

Handle trimming manually by computing the first and last non-whitespace positions in the original string before the main parse begins and then slicing that range as a `ReadOnlySpan<char>`. This removes the full-string `Trim` allocation. During segment handling, trim segment-local whitespace by advancing and retreating indexes within the same source span rather than materializing temporary strings.

The parser should write normalized output into a stack-first builder abstraction. When the trimmed input length is at most 512 characters, use stack-allocated storage for the output buffer so short and medium paths avoid renting or allocating an intermediate heap buffer. When the trimmed input length exceeds 512 characters, fall back to a pooled buffer rented from `ArrayPool<char>.Shared` and return that buffer reliably after the final string has been created. Avoid using a plain growable heap buffer such as `StringBuilder` or a repeatedly resized `char[]`, because the purpose of this fallback is to keep even unusually long targets on a low-allocation path. This threshold should be treated as a deliberate implementation constant rather than an incidental detail so that future benchmark work can tune it explicitly if needed. The builder should emit exactly one final normalized string once parsing is complete. Keep this helper private to the normalizer unless reuse becomes clearly justified inside the validation project.

Segment cleanup should be folded into the parse loop. In particular, remove ignored prefixes such as `@` directly from the segment window before writing, and only transform the first significant character when camel-case or pascal-case conversion is required. Characters after the first significant character should be copied through as-is. This avoids the current pattern of building a cleaned segment string and then building another derived string for casing.

Preserve current edge-case behavior deliberately. Empty or whitespace-only inputs must still normalize to `string.Empty`. A path that consists only of a root segment must still normalize to `string.Empty` after root removal. Malformed indexers without a closing `]` should continue to keep the unmatched tail rather than throwing. Before implementation, capture the current behavior with tests so the rewrite can be validated against the existing semantics rather than an inferred interpretation.

Review the current helper methods while implementing the parser. Helpers that exist only because of the substring-based implementation should be removed or collapsed into span-aware logic. If any currently unreachable cleanup logic is discovered, remove it only after tests demonstrate that no public scenario depends on it.
