# Peer Review: Scaffolded EF Core Contexts

> [!NOTE]
> All five checklist areas covered. Ratings: **🔴 Bug/Risk**, **🟡 Concern/Verify**, **🟢 Pass**.

---

## 1 — JB5LegacyContext.cs — Model Configuration

### 1.1 OnModelCreating Completeness — 🟢 Pass

All `DbSet<T>` properties (lines 15–191) have a corresponding `modelBuilder.Entity<T>` block. Every `vw*` entity maps to `.ToView(...)`, not `.ToTable(...)` — no view-backed set is inadvertently writable.

### 1.2 Column Type Fidelity — 🟢 Pass

| Entity | Key Checks | Result |
|---|---|---|
| [Customer](JB2026.EfCore/Models/Customer.cs#L6-L36) | `CustomerName` → `HasMaxLength(64)`, `CreatedOn/ModifiedOn` → `smalldatetime`, `MetadataXml` → `xml` | ✔ |
| [JobOrder](JB2026.EfCore/Models/JobOrder.cs#L6-L82) | [OrderNumber](JB2026.Api/Services/EfJobManagementRepository.cs#L239-L244) → `HasMaxLength(10).IsUnicode(false)`, `InvoiceAmount` → `decimal(10,4)`, `Qty` → `decimal(10,4)` | ✔ |
| `Product` | `COGS`/`SellingPrice` → `money`, `ProductName` → `HasMaxLength(64)` | ✔ |
| [UserInfo](JB2026.EfCore/Models/UserInfo.cs#L6-L36) | `UserName/UserAlias` → `HasMaxLength(64)`, `UserId` → `HasDefaultValueSql("(newid())")` | ✔ |
| `SystemInfo` | `SystemId` → `HasDefaultValueSql("(newid())")`, `MetadataXml` → `xml` | ✔ |

### 1.3 ValueGeneratedNever() on Natural PKs — 🟢 Pass

`JobOrder.OrderId` ✔ (L356), `InvoiceHeader.HeaderId` ✔ (L267), `Product.ProductId` ✔ (L528), `JobPackingOnAir.OnAirId` ✔ (L391), `Supplier.SupplierId` ✔ (L821), `User.UserId` ✔ (L846). All natural Guid PKs correctly marked.

### 1.4 HasDefaultValueSql("(newid())") on DB-Assigned Defaults — 🟢 Pass

`Customer.CustomerId/CreatedBy/ModifiedBy`, `JobWorkflow.JobWorkflowId`, `SystemInfo.SystemId`, `UserInfo.UserId/CreatedBy/ModifiedBy` — all present. EF Core will not issue a spurious UPDATE after INSERT on any of these.

### 1.5 FK Cascade Behaviour — 🟢 Pass (with one item to verify)

All `ClientSetNull` entries are intentional child-to-parent relationships. No accidental `Cascade` outside HangFire.

> [!NOTE]
> **Item to verify:** `StockInOut → Product` and `InvoiceItems → SmlRtfHeader` have **no explicit `OnDelete`** configured. EF Core infers `ClientSetNull` for optional navigations. Confirm these FK constraints in SQL Server use `NO ACTION` — if they use `CASCADE`, a constraint conflict could surface at runtime.

### 1.6 HangFire Schema Isolation — 🟢 Pass

All 11 HangFire tables are `.ToTable("...", "HangFire")`. None mixed with business tables.

### 1.7 Keyless Entities / Views — 🟢 Pass

`Log4Net` → `HasNoKey()` ✔. All 39 `vw*` entities use `HasNoKey().ToView(...)`. No view mapped to `.ToTable()`.

---

## 2 — JB5LegacyReadContext / JB5LegacyWriteContext

### Read context — 🟢 Pass

`ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking` set in constructor. No `SaveChanges` override exists. This configuration reduces accidental tracked updates from query results, but does **not** make writes impossible if `Add/Update/Remove` are explicitly called.

> [!NOTE]
> [Program.cs](JB2026.Api/Program.cs) also passes `.UseQueryTrackingBehavior(NoTracking)` in the options builder (L59). The constructor assignment is redundant but harmless — double `NoTracking` configuration.

### Write context — 🟢 Pass

No `NoTracking` set; change tracking is active. Correct for Update/Remove to work.

### DI Lifetimes — 🟢 Pass

Both contexts registered via `AddDbContext<T>` (scoped). Neither is `Singleton`. No connection leak risk.

---

## 3 — EfJobManagementRepository.cs

### 3.1 Compiled Query Completeness — 🟡 One gap

| Method | Compiled Query? |
|---|---|
| [GetRange](JB2026.Api/Services/EfJobManagementRepository.cs#L47-L56), [GetJobDetail](JB2026.Api/Services/EfJobManagementRepository.cs#L57-L62), [GetStyleTitles](JB2026.Api/Services/EfJobManagementRepository.cs#L63-L78), [GetJobOrders](JB2026.Api/Services/EfJobManagementRepository.cs#L79-L85), [GetJobOrder](JB2026.Api/Services/EfJobManagementRepository.cs#L86-L91) | ✔ |
| [UpdateJobOrder](JB2026.Api/Services/EfJobManagementRepository.cs#L126-L149) | `_writeContext.JobOrders.FirstOrDefault(...)` — **ad-hoc** (L128) |
| [DeleteJobOrder](JB2026.Api/Services/EfJobManagementRepository.cs#L150-L163) | `_writeContext.JobOrders.FirstOrDefault(...)` — **ad-hoc** (L152) |

> [!WARNING]
> [UpdateJobOrder](JB2026.Api/Services/EfJobManagementRepository.cs#L126-L149) and [DeleteJobOrder](JB2026.Api/Services/EfJobManagementRepository.cs#L150-L163) use ad-hoc queries on the write context. A compiled query for [JB5LegacyReadContext](JB2026.EfCore/Data/JB5LegacyReadContext.cs#L5-L13) cannot be reused here (different context type), so these are architecturally correct. Flag as a future improvement: add compiled queries targeting [JB5LegacyWriteContext](JB2026.EfCore/Data/JB5LegacyWriteContext.cs#L7-L11) if write-path performance matters.

### 3.2 Include Chains vs Navigation Access — 🟢 Pass

`CompiledGetJobOrderById` includes `JobSchedules`, `JobWorkflows`, `JobAttachments`.

- [MapDetail](JB2026.Api/Services/EfJobManagementRepository.cs#L181-L214) accesses `JobWorkflows` ✔ and `JobAttachments` ✔ — both included.
- [GetStyleTitles](JB2026.Api/Services/EfJobManagementRepository.cs#L63-L78) accesses `JobWorkflows` ✔ — included.
- `CompiledGetRange` has no includes; consumed only by `GetRange → MapListItem` which accesses no navigation collections. ✔

No silent empty-collection bugs from missing includes.

### 3.3 In-Memory LINQ Post-Include — 🟢 Pass

`MapDetail.StyleTitles` (L197–202) and [GetStyleTitles](JB2026.Api/Services/EfJobManagementRepository.cs#L63-L78) (L71–76) chain `.OrderBy.Select.Where.Select` on the already-loaded `ICollection<JobWorkflow>` — purely in-memory, no DB round-trip. Same for `MapDetail.Attachments` (L203–211). No unintentional `IQueryable` continuation after `.Include`.

### 3.4 Synchronous SaveChanges — 🟡 Suggestion

All write methods call `.SaveChanges()` (sync).

> [!NOTE]
> If the corresponding API controllers are `async` (typical for ASP.NET Core), migrate to `.SaveChangesAsync()` to avoid blocking a thread-pool thread during the DB flush. This is a suggestion, not a bug.

---

## 4 — Program.cs — DI Registration

### 4.1 Gateway Registration Count — 🟡 Verify

Lines 65–89 register **25** stored-procedure gateways inside the `if (!string.IsNullOrWhiteSpace(primaryConnectionString))` block.

> [!IMPORTANT]
> Cross-check against implementation files. A missing `AddScoped` line only fails at runtime when a controller first requests the interface — not at startup. Verify with:
> ```powershell
> (Select-String -Path "JB2026.Api/Services/**/*.cs" -Pattern "class\s+\w+StoredProcedureGateway" | Measure-Object).Count
> ```
> Expected: 25.

### 4.2 In-Memory Fallback Safety — 🟢 Pass

The `else` branch (L91–94) registers only `InMemoryJobManagementRepository`. No gateways are registered in the fallback path — correct, since all gateways require a live DB. ✔

> [!NOTE]
> `InMemoryQuotationRepository` (L49) is registered as `Singleton` **unconditionally** outside the connection-string guard. Intentional, but worth noting: if `IQuotationRepository` is ever switched to an EF-backed implementation, this registration must move inside the `if` block.

---

## 5 — Entity Model Classes — Spot Check

Four models reviewed: [JobOrder](JB2026.EfCore/Models/JobOrder.cs#L6-L82), [Customer](JB2026.EfCore/Models/Customer.cs#L6-L36), [JobWorkflow](JB2026.EfCore/Models/JobWorkflow.cs#L6-L34), [UserInfo](JB2026.EfCore/Models/UserInfo.cs#L6-L36).

| Check | Result |
|---|---|
| No data annotation attributes (`[Required]`, `[MaxLength]`, etc.) | ✔ — pure POCO; all config via fluent API |
| All properties present vs [OnModelCreating](JB2026.EfCore/Data/JB5LegacyContext.cs#L193-L1809) config | ✔ |
| Navigation properties consistent with FK config | ✔ — [JobOrder](JB2026.EfCore/Models/JobOrder.cs#L6-L82) has all 4 collection navs; `JobWorkflow.Order` is required (`null!`), [Workflow](JB2026.EfCore/Models/JobWorkflow.cs#L6-L34) is optional — matching `ClientSetNull` / nullable FK config |

> [!NOTE]
> `JobOrder.CreatedBy`/`ModifiedBy` are application-assigned [Guid](JB2026.Api/Services/EfJobManagementRepository.cs#L245-L249) columns with **no `HasDefaultValueSql`** in the [JobOrder](JB2026.EfCore/Models/JobOrder.cs#L6-L82) block. This is correct **only if** the live SQL Server columns have no `newid()` default. Cross-check against the legacy DAL `GetSqlParameter` array — if the DB has a default, a surprise `UPDATE` after `INSERT` or a constraint violation could occur.

---

## Summary of Action Items

| # | Severity | File | Action Required |
|---|---|---|---|
| 1 | 🟡 Verify | [JB5LegacyContext.cs](JB2026.EfCore/Data/JB5LegacyContext.cs) | Confirm `StockInOut→Product` and `InvoiceItems→SmlRtfHeader` FK delete rules in SQL Server are `NO ACTION` |
| 2 | 🟡 Verify | [JB5LegacyContext.cs](JB2026.EfCore/Data/JB5LegacyContext.cs) | Confirm `JobOrder.CreatedBy`/`ModifiedBy` have no `newid()` DB default |
| 3 | 🟡 Future | [EfJobManagementRepository.cs](JB2026.Api/Services/EfJobManagementRepository.cs) | Consider compiled queries for [UpdateJobOrder](JB2026.Api/Services/EfJobManagementRepository.cs#L126-L149)/[DeleteJobOrder](JB2026.Api/Services/EfJobManagementRepository.cs#L150-L163) write-path lookups |
| 4 | 🟡 Suggestion | [EfJobManagementRepository.cs](JB2026.Api/Services/EfJobManagementRepository.cs) | Migrate `SaveChanges()` → `SaveChangesAsync()` if controllers are async |
| 5 | 🟡 Verify | [Program.cs](JB2026.Api/Program.cs) | File-count gateway implementations vs 25 `AddScoped` registrations |
| 6 | 🟢 Note | [Program.cs](JB2026.Api/Program.cs) | Double `NoTracking` (options + read context constructor) is redundant but harmless |

