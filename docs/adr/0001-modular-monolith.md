# ADR-0001: Modular Monolith over Microservices

**Status:** Accepted

## Context
The marketplace spans several bounded audiences (customer, seller, driver, admin) and domains (catalog, ordering, notifications). The team is small (1–2 devs) and the product is pre-product/market-fit; deployment and operational simplicity matter more than independent scaling of modules.

## Decision
Build a **modular monolith**: one deployable host (`Store.API`) composed of independently developed feature modules wired through `Add<X>Module()` + `AddApplicationPart(...)`, with a shared `ECommerce.Infrastructure` kernel.

Rules that keep the seams real:
- Modules may only reference `Infrastructure`, `Authentication`, and each other *explicitly* (e.g. `Seller.Management` → `Admin.Management` for the store-approval service).
- No module may reach into another module's internals — only its public services/contracts.
- Extraction to a separate service later means promoting the project, not rewriting it.

## Consequences
+ Single deployable, single transaction boundary per request.
+ Module boundaries are enforced by project references.
− Requires discipline: the compiler won't stop a module from referencing another's DbContext internals.
