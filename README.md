# E-Commerce Store API

A marketplace backend built with **.NET 10**, replicating a proven **modular-monolith** layout: one deployable host composed of audience-paired feature modules (`X.Management` for staff back-office, `X.Profile` for self-service) over a shared infrastructure kernel — with multi-tenant stores (Amazon-style sellers), dynamic permission authorization, optimistic locking, and automated unit tests.

## Architecture

```
Store.API .................... composition root (host, CORS, OpenAPI/Scalar, health checks)
ECommerce.Infrastructure ..... shared kernel: entities, AppDbContext + migrations,
                               Result/Error abstractions, caching, seeding, permissions catalog
ECommerce.Authentication ..... JWT auth, register/login/profile/password,
                               role + dynamic-permission authorization (Redis-cached)

Audience modules (cliniq-style pairing)
Admin.Management ............. store approvals, admin accounts, platform dashboard
Admin.Profile ................ staff self-service (job title / department)
Customer.Management .......... customer directory, suspend/reactivate profiles
Customer.Profile ............. customer profile + address book
Seller.Management ............ seller/store directory (admin view)
Seller.Profile ............... vendor portal: my store, my products (scoped CRUD), sold items
Driver.Management ............ driver directory, approve/suspend/reject applications
Driver.Profile ............... driver application/resubmission, deliveries & return pickups

Domain modules
Catalog.Public ............... storefront browsing: home, search, product details, reviews feed
Catalog.Management ........... first-party catalog: products (+media), categories, promo codes,
                               pickup addresses
Shopping.Customer ............ cart, wishlist, reviews
Ordering.Customer ............ checkout (COD / Paymob card & wallet), orders, timeline,
                               cancellations, return requests
Ordering.Management .......... order & return administration, transporter assignment
Roles.Management ............. role list, permission replacement, user role assignment
```

Dependency flow mirrors the template: `Infrastructure ← Authentication ← modules ← API`; each module exposes `Add<X>Module()` and controllers are pulled into the host via `AddApplicationPart(...)` under `MapGroup("api")`.

## Multi-tenant stores

- Every product belongs to a **Store**; the public catalog only exposes products of `Active` stores.
- Lifecycle: seller opens a store (`PendingVerification`) → admin approves → seller lists products; admins can suspend/reject with reason.
- Registering users start as `Customer`; opening their first store promotes them to `Seller` automatically.
- Seller product/order endpoints are ownership-scoped in the service layer.

## Roles & profiles (identical system to the clinic template)

Roles: **SuperAdmin** (all permissions), **Admin** (staff ops), **Customer**, **Seller**, **Driver**.
Permissions are `Permissions.<Resource>.<Action>` claims on roles, resolved dynamically by `[HasPermission]` through a custom `IAuthorizationPolicyProvider`, cached 30 min in Redis.

Profile tables use the shared-primary-key pattern (`profile.Id == user.Id`, cascade):

| Profile | Fields |
|---|---|
| `CustomerProfile` | Status (Active/Suspended), loyalty points |
| `AdminProfile` | Job title, department |
| `SellerProfile` | Link to owned Store |
| `DriverProfile` | Vehicle type/plate/license, status workflow (apply → verify → active/suspended/rejected→resubmit) |

Adding a future audience is copy-paste: new profile entity + config + module pair.

## Key patterns

- **Result/Error pipeline** — services return `Result<T>`; controllers map to `Ok/NoContent/CreatedAtAction` or RFC-7807 ProblemDetails with `errors[]` codes via `result.ToProblem()`.
- **Optimistic concurrency** — `rowversion` tokens on race-prone aggregates (Product, Cart, CartProduct, Order, ReturnRequest, PromoCode); global handler converts `DbUpdateConcurrencyException` into HTTP 409. Users/roles use Identity's `ConcurrencyStamp`.
- **Audit trail** — automatic field-level `EditHistory` for `IHasEditHistory` entities; soft-deletes logged in `DeleteHistory`.
- **Validation** — FluentValidation validators auto-enforced (SharpGrip).
- **Payments** — typed HttpClient Paymob integration + HMAC-SHA512 verified webhook that flips `Paying → Processing` and clears the cart.
- **Data retention** — `DataRetentionBackgroundService` purges consumed OTPs, expired/revoked refresh tokens, stale device tokens and old search telemetry on a 24h timer (config under `DataRetention`).
- **Uploads** — product media ≤10 MB/file, driver documents ≤8 MB/file, extension whitelisted, stored via a central `IFileStorage` (local disk now; blob storage is a drop-in swap).
- **Live tracking** — SignalR hub `/hubs/tracking` (JWT via `access_token`; anonymous connections aborted on connect). Customers `JoinOrder(orderId)` after an ownership check and receive `orderStatusChanged` + `driverLocationChanged` events pushed by every status transition. Drivers stream positions with `POST /api/driver/orders/{id}/location` (assigned-driver only); last ping lives in Redis (15-min TTL) and is also available via REST at `GET /api/orders/{id}/driver-location`, which includes a haversine-based ETA to the delivery address (`Address.Latitude/Longitude`).

Money is stored as integer cents (`bigint`). **Database: PostgreSQL 17** (Npgsql EF Core provider) with native `xmin` optimistic-concurrency tokens and GIN-backed full-text search over products. Order status changes append `OrderStatusEvent` timeline rows.

## Getting started

```bash
docker compose up -d          # PostgreSQL 17 + Redis
dotnet run --project Store.API   # migrates + seeds in Development
# Scalar UI: https://localhost:<port>/scalar   |   Health: /health
```

Seeded accounts:

| Role | Email | Password |
|---|---|---|
| SuperAdmin | superadmin@store.com | SuperAdmin@123! |
| Admin | admin@store.com | Admin@123! |
| Seller (TechNova) | seller@store.com | Seller@123! |
| Driver | driver@store.com | Driver@123! |
| Customer | customer@store.com | Customer@123! |

Two demo stores are seeded: *StoreFront Official* (first-party) and *TechNova* (independent electronics seller).

### Configuration

```bash
cd Store.API
dotnet user-secrets set "Jwt:Key" "<64+ char random key>"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=ecommerce;Username=...;Password=..."
dotnet user-secrets set "Paymob:ApiKey" "..."        # optional: online payments
dotnet user-secrets set "Paymob:HmacSecret" "..."    # callback signature verification
```

## API surface (all under `/api`)

| Area | Routes |
|---|---|
| Auth | `auth/register` (emails OTP), `auth/verify-email`, `auth/resend-verification`, `auth/login`, `auth/refresh` (rotation + 5s grace + reuse→family revoke), `auth/logout`, `auth/forgot-password`, `auth/reset-password`, `auth/send-phone-otp`, `auth/verify-phone`, `auth/sessions` (list / revoke one / all), `auth/me`, `auth/profile` (GET/PUT), `auth/change-password`, `auth/permissions` |
| Rate limiting | `auth` policy 10/min per IP on register/login/refresh/logout; `otp` policy 5/5min on OTP & reset endpoints |
| Storefront | `store/home`, `store/categories`, `store/products/{id}`, `store/search/{category}/{keyword}` (PostgreSQL FTS with `unaccent` + `pg_trgm` typo tolerance, GIN-indexed) |
| Cart | `cart`, `cart/items`, `cart/promo-code` |
| Addresses | `addresses` (GET/POST/DELETE) |
| Reviews | `products/{id}/reviews` (GET/POST), `PUT/DELETE reviews/{id}` |
| Wishlist | `wishlist/{productId}` (GET/POST/DELETE) |
| Orders | `orders`, `orders/{id}`, `orders/{id}/timeline`, `orders/{id}/driver-location`, `orders/checkout`, `DELETE orders/{id}` |
| Tracking | SignalR hub `/hubs/tracking` — `JoinOrder(orderId)` → `orderStatusChanged`, `driverLocationChanged` |
| Payments | `payments/paymob/callback` |
| Returns | `returns`, `returns/order-products/{id}` |
| Push tokens | `notifications/device-tokens` (POST register/upsert, GET my devices, DELETE unregister) |
| Seller | `seller/store` (GET/POST/PUT), `seller/products` (CRUD+stock), `seller/order-items` |
| Admin catalog | `admin/products`, `admin/categories`, `admin/promo-codes`, `admin/store-addresses` |
| Admin ordering | `admin/orders`, `admin/returns` (+ `/status`, `/transporter`) |
| Admin stores/sellers | `admin/stores/{id}/status`, `admin/sellers` (+ status) |
| Admin customers | `admin/customers` (+ `/status`) |
| Admin drivers | `admin/drivers` (+ `/status`) |
| Driver | `driver/requests/apply` (multipart: license + registration + ID docs), `driver/requests/resubmit`, `driver/profile` (GET/PUT), `driver/deliveries`, `driver/pickups`, `driver/orders/{id}/location` |
| Admin drivers | `admin/driver-requests` (pending join queue), `admin/driver-requests/{id}/status`, `admin/drivers` (+ `/status`) |
| Staff | `admin/admins` (+ `/status`), `admin/profile` (GET/PUT), `admin/dashboard` |
| Roles | `admin/roles`, `admin/roles/{id}/permissions`, `admin/roles/users/{id}/roles` |

## Testing

Two layers, **97 xUnit tests** total (`ECommerce.UnitTests`):

**Unit tests (82)** — services against EF InMemory with real ASP.NET Identity: auth flows, refresh-token rotation/grace/reuse-detection, OTP challenge rules, profile & password rules, cart/promo math, checkout fees + stock + cart clearing, review purchase-guards, Paymob HMAC verification, dashboard aggregation, seller store lifecycle & cross-store isolation, driver application workflow, customer suspension, device-token registry upsert/prune, permissions catalog integrity, Result invariants, audit tracking.

**Integration tests (15)** — `ApiFactory` boots the real host (every module wired) against throwaway PostgreSQL databases:
- AuthzMatrixTests — anonymous/5-role coverage over every protected route family (401 vs 200 vs 403)
- MoneyPathSmokeTests — register → confirm → login → cart → COD checkout end-to-end over real HTTP

```bash
dotnet test   # integration tier requires the docker compose PostgreSQL + Redis running
```

## Architecture Decision Records

See [docs/adr](docs/adr) — modular monolith, PostgreSQL migration, Result pattern, xmin concurrency.
