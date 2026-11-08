# Functional operators

The functional operators turn `Result` and `Result<T>` into composable pipelines: you transform values, add rules, and handle both outcomes without writing `if (!result.IsValid)` after every call. Every operator has an `Async` variant that returns a `ValueTask`.

**Package:** `Light.PortableResults` (namespace `Light.PortableResults.FunctionalExtensions`)

## Overview

| Category | Operators | What they are used for |
| --- | --- | --- |
| Transform success value | `Map`, `Bind` | Convert successful values or chain operations that already return `Result<T>`. |
| Transform errors | `MapError` | Normalize or translate errors (for example domain → transport layer). |
| Add validation rules | `Ensure`, `FailIf` | Keep fluent pipelines while adding business or guard conditions. |
| Handle outcomes | `Match`, `MatchFirst`, `Else` | Turn a result into a value or fallback without manually branching every time. |
| Side effects | `Tap`, `TapError`, `Switch`, `SwitchFirst` | Perform logging, metrics, or notifications on success or failure paths. |

All operators provide async variants with the `Async` suffix (for example `BindAsync`, `MatchAsync`, `TapErrorAsync`).

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

## Operator reference

| Operator | Applies to | Behavior |
| --- | --- | --- |
| `Map(value => newValue)` | `Result<T>` | Wraps the mapped value in a new successful result. Failures pass through. Metadata is kept. |
| `Bind(value => otherResult)` | `Result<T>` | Continues with an operation that returns a `Result<TOut>`. Failures pass through. The metadata of both results is merged, by default with `MetadataMergeStrategy.AddOrReplace`; pass another strategy as the second argument. |
| `MapError(error => newError)` | `Result`, `Result<T>` | Transforms every error of a failed result. Successes pass through. |
| `Ensure(predicate, error)` | `Result`, `Result<T>` | Turns a success into a failure when the predicate returns `false`. Accepts an `Error` or an error factory. Metadata is kept. |
| `FailIf(predicate, error)` | `Result`, `Result<T>` | The inverse of `Ensure`: fails when the predicate returns `true`. |
| `Match(onSuccess, onError)` | `Result`, `Result<T>` | Returns the value of the handler that matches the outcome. `onError` receives all `Errors`. |
| `MatchFirst(onSuccess, onError)` | `Result`, `Result<T>` | Like `Match`, but `onError` receives only the first `Error`. |
| `Else(fallback)` | `Result<T>` | Returns the value, or the fallback value (or the result of a fallback factory that receives the errors). |
| `Tap(action)` | `Result<T>` | Runs a side effect with the value of a successful result and returns the result unchanged. |
| `TapError(action)` | `Result`, `Result<T>` | Runs a side effect with the errors of a failed result and returns the result unchanged. |
| `Switch(onSuccess, onError)` | `Result`, `Result<T>` | Runs one of two actions; `onError` receives all `Errors`. |
| `SwitchFirst(onSuccess, onError)` | `Result`, `Result<T>` | Like `Switch`, but `onError` receives only the first `Error`. |

The async variants accept delegates that return `ValueTask` or `ValueTask<T>`. They extend the result itself, not `ValueTask<Result<T>>`, so await each async step before you continue the chain:

```csharp
Result<Cart> cart = LoadCart(cartId).Ensure(cart => cart.Items.Count > 0, EmptyCartError);

// PlaceOrderAsync returns ValueTask<Result<OrderConfirmation>>
Result<OrderConfirmation> confirmation =
    await cart.BindAsync(validCart => _orders.PlaceOrderAsync(validCart, cancellationToken));

await confirmation.TapErrorAsync(errors => _auditLog.WriteAsync(errors, cancellationToken));
```

Use the [validation framework](validation/getting-started.md) instead of long `Ensure` chains when you validate input DTOs: it collects all errors in one pass, derives targets and codes, and is optimized for low allocations.
