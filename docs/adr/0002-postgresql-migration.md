# ADR-0002: PostgreSQL over SQL Server

**Status:** Accepted (supersedes initial SQL Server choice)

## Context
The original implementation used SQL Server 2022 (container). Two pain points surfaced:
1. The default container image lacks Full-Text Search components; enabling FTS required a different SKU/image.
2. Search quality for a marketplace needs accent-insensitivity (`café` ≈ `cafe`) and typo tolerance — SQL Server FTS covers neither without additional tooling.

## Decision
Move to **PostgreSQL 17** via `Npgsql.EntityFrameworkCore.PostgreSQL`.

Gained:
- Native full-text: `to_tsvector/to_tsquery` with `unaccent` dictionary and GIN expression indexes.
- Typo tolerance via `pg_trgm` similarity operators, also GIN-indexed.
- `xmin` system column as a free optimistic-concurrency token (see ADR-0004).
- OSS stack consistency for a portfolio project.

Conversions applied:
- `byte[] rowversion` tokens → `uint` mapped to `xmin`.
- All migrations regenerated under the Npgsql provider.

## Consequences
+ Zero-config FTS/trigram in the standard Docker image.
− Unit tests use the InMemory provider, which supports neither feature; those paths are covered by integration tests against real PostgreSQL instead.
