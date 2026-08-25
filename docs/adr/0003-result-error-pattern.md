# ADR-0003: Result/Error pattern instead of exception-based control flow

**Status:** Accepted

## Context
Domain rules (out-of-stock, ongoing order exists, warranty expired, promo inactive…) are expected outcomes, not exceptional ones. Throwing across module boundaries made success paths untestable and error responses inconsistent.

## Decision
Services return `Result` / `Result<TValue>`; controllers translate with a ternary:

```csharp
return result.IsSucceed ? Ok(result.Value) : result.ToProblem();
```

- `Result` enforces its invariant in the constructor: success must carry no error, failure must carry one.
- Errors are machine-readable records `(Code, Description, StatusCode)`, grouped in static catalogs (`ProductErrors.NotFound`, …).
- `ToProblem()` renders RFC-7807 ProblemDetails with an `errors[]` extension so clients can branch on codes.
- Only *unexpected* failures (concurrency conflicts, infrastructure faults) throw; a global `IExceptionHandler` converts those to 409/500 respectively.

## Consequences
+ Callers cannot ignore failure; the happy path reads linearly.
+ Error contracts are stable and testable without HTTP.
− Slightly more ceremony per service method.
