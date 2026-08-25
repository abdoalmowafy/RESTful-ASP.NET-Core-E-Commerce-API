# ADR-0004: Optimistic concurrency via provider-native tokens

**Status:** Accepted

## Context
Marketplace writes race constantly: two buyers taking the last unit, an admin changing order status while a driver delivers, cart edits from two tabs. Pessimistic locking serializes everything and kills throughput; last-write-wins silently corrupts stock and state machines.

## Decision
Use **database-maintained version tokens** on every race-prone aggregate (Product, Cart, CartProduct, Order, ReturnRequest, PromoCode):

| Provider | Token | Mechanism |
|---|---|---|
| SQL Server (original) | `byte[] rowversion` | engine-incremented binary column |
| PostgreSQL (current) | `uint` mapped to **`xmin`** system column | transaction ID advances automatically |

EF Core appends the token to every UPDATE's WHERE clause. A lost race affects 0 rows → `DbUpdateConcurrencyException` → global handler returns RFC-7807 **409** with code `Common.ConcurrencyConflict`. Clients retry by reloading.

Identity users/roles keep ASP.NET's built-in `ConcurrencyStamp` (same idea).

## Consequences
+ No locks held across requests; no deadlocks.
+ Works for raw SQL too — any UPDATE bumps `xmin`.
− Clients must handle 409 by refetching (documented in README).
− Requires integration tests against the real engine (InMemory doesn't model it); verified with a two-context stale-writer harness.
