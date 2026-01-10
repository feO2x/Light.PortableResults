# Metadata System Architecture

## Internal vs Public Types Decision

This document explains the architectural decision to keep certain backing types internal while exposing a clean public
API.

## Public API Surface

The following types constitute the public API:

- **`MetadataObject`** - Immutable dictionary-like structure with string keys and `MetadataValue` values
- **`MetadataArray`** - Immutable array of `MetadataValue` items
- **`MetadataValue`** - Discriminated union representing various value types

## Internal Implementation Types

The following types are intentionally kept **internal**:

- **`MetadataObjectData`** - Backing storage for `MetadataObject`
- **`MetadataArrayData`** - Backing storage for `MetadataArray`
- **`MetadataPayload`** - Union-style memory layout for `MetadataValue` storage

## Rationale for Keeping Types Internal

### 1. No Consumer Benefit

Users interact exclusively with `MetadataObject` and `MetadataArray`. The backing types provide no additional
functionality that isn't already exposed through the public API. Exposing them would only increase API surface area
without adding value.

### 2. Preserve Optimization Freedom

The internal types contain performance optimizations that may need to evolve:

- **Lazy dictionary creation**: `MetadataObjectData` uses linear search for ≤8 entries, then lazily builds a dictionary
  for larger collections
- **Memory layout**: `MetadataPayload` uses explicit struct layout with overlapping fields to minimize memory
  footprint (16 bytes total)

Making these public would lock us into these implementation details forever, preventing future optimizations.

### 3. Prevent Breaking Changes

Once public, any change to these types becomes a breaking change:

- Cannot modify the `MetadataPayload` explicit layout
- Cannot change the dictionary threshold constant
- Cannot refactor internal storage strategies
- Cannot add/remove/modify public members without breaking consumers

### 4. Encapsulation and Immutability

The internal types expose methods like `GetEntries()` and `GetValues()` that return the backing arrays. If made public,
this would:

- Break immutability guarantees (consumers could mutate the arrays)
- Expose implementation details that should remain hidden
- Create maintenance burden requiring defensive copies

### 5. Clear Separation of Concerns

The current design follows a clean facade pattern:

```
Public API (MetadataObject/MetadataArray)
    ↓ delegates to
Internal Storage (MetadataObjectData/MetadataArrayData)
    ↓ uses
Internal Primitives (MetadataPayload)
```

This separation allows the public API to remain stable while internal implementation evolves.
