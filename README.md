# HazaradDemo
# DatwiseSafetyDemo – Safety Hazards Module (WebForms + SQL Server)

A compact **ASP.NET Web Forms** demo module for managing **Safety Hazards**.
It demonstrates **DB-based authentication**, **role-based access control (RBAC)**, a **KPI dashboard with drill-down**, **CSV/PDF exports**, and an **internal JSON API**.

This implementation covers the chosen advanced options per the assignment:

- **Option 2 – Information security:** Authentication + RBAC + sensitive-data protection patterns
- **Option 3 – Dashboard / visualization / reports:** KPIs, breakdowns, drill-down, CSV/PDF export
- **Option 10 – Internal API:** Session-authenticated JSON endpoints (foundation for Web API / future microservices)

---

## Tech stack

- **ASP.NET Web Forms** (.NET Framework)
- **SQL Server** (SQL Express / LocalDB / full SQL Server)
- Bootstrap (UI)
- MSTest (unit tests)

---

## Demo credentials (seeded)

After running `seed.sql`:

| Username | Password        | Role          |
|---------:|-----------------|---------------|
| safety   | Password123!    | SafetyOfficer |
| manager  | Password123!    | SiteManager   |
| worker   | Password123!    | FieldWorker   |

---

## RBAC rules (summary)

- **SafetyOfficer**: Full access (view/create/edit/assign/change status)
- **SiteManager**: Can manage hazards in their operational scope (assigned-to / unassigned open items + their own)
- **FieldWorker**: Can see and act on hazards they reported or that are assigned to them

The same rules are enforced **server-side** (repositories + authorization helpers + stored procedures).

---

## Database setup

SQL scripts are consolidated to **exactly two files**:

- `DatwiseSafetyDemo/Sql/schema.sql` – schema + functions + stored procedures (final)
- `DatwiseSafetyDemo/Sql/seed.sql` – demo users + demo hazards/logs (idempotent for demo data)

### 1) Create schema
Run `DatwiseSafetyDemo/Sql/schema.sql` in SSMS against your SQL instance (it will create the `DatwiseSafetyDemo` database and required tables/procs if missing).

### 2) Insert seed data
Run `DatwiseSafetyDemo/Sql/seed.sql`.

---

## Configure connection string

Edit `DatwiseSafetyDemo/Web.config` → `HazardsConnection`.

(Optional) You may also add a second connection string name `DatwiseDb` pointing to the same value, but the code uses `HazardsConnection` by default.

Common examples:

- **SQL Express (local VM)**  
  `Server=localhost\SQLEXPRESS;Database=DatwiseSafetyDemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;`

- **LocalDB**  
  `Server=(localdb)\MSSQLLocalDB;Database=DatwiseSafetyDemo;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True;`

> Tip: If you connect to a container / remote SQL Server and see an SSL/certificate error in SSMS, enable **Trust server certificate**.

---

## Run the app (Visual Studio)

1. Open the **root** solution: `DatwiseSafetyDemo.sln`
2. Restore NuGet packages
3. Set `DatwiseSafetyDemo` as startup project
4. Run (IIS Express)

Login via `/Account/Login.aspx` and navigate to:
- **Dashboard**: `/Dashboard.aspx`
- **Hazards list**: `/Hazards/HazardList.aspx`

---

## Dashboard / drill-down (Option 3)

The dashboard displays:
- KPI tiles (total/open/resolved/overdue-open)
- Breakdown tables (by **Severity** and by **Type**)

Each breakdown row supports **drill-down** into the hazard list with the relevant filters applied.

---

## Reports / Export (Option 3)

Endpoint:
- `/Reports/HazardsReport.ashx?format=csv`
- `/Reports/HazardsReport.ashx?format=pdf`

Exports are scoped to your RBAC permissions.

---

## Internal API (Option 10)

Endpoint:
- `/Api/HazardsApi.ashx`

Supported actions (JSON):
- `?action=list` – hazards list (with filter querystring parameters)
- `?action=dashboard` – dashboard metrics + breakdowns

The API is **session-authenticated** (same login as the UI) and returns **401** when unauthenticated.

---

## Security hardening included (Option 2)

- Password hashing: **PBKDF2** with per-user salt + iterations
- Centralized RBAC rules (unit-testable)
- Server-side input validation + output encoding where relevant
- SQL access via **parameterized** commands + stored procedures
- Security headers (CSP-lite, X-Content-Type-Options, etc.)
- Basic login rate limiting to reduce brute-force attempts

---

## Tests

Open `DatwiseSafetyDemo.Tests` and run tests from Visual Studio Test Explorer.

---

## Folder map (quick)

- `DatwiseSafetyDemo/` – WebForms app
- `DatwiseSafetyDemo/Api/` – internal JSON handlers
- `DatwiseSafetyDemo/Reports/` – CSV/PDF exports
- `DatwiseSafetyDemo/Data/` – repositories + DB calls
- `DatwiseSafetyDemo/Infrastructure/` – auth, RBAC, security helpers
- `DatwiseSafetyDemo/Sql/` – `schema.sql`, `seed.sql`
- `DatwiseSafetyDemo.Tests/` – unit tests
