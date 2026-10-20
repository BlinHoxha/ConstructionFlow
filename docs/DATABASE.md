# Database implementation map

The application owns its PostgreSQL migrations in `src/ConstructionFlow.Infrastructure/Migrations`. Naming is snake_case; keys are UUIDs; timestamps are UTC; monetary calculations use decimal; quantities/prices map to numeric(18,4), VAT to numeric(5,2), and mutable tenant entities use an application-managed bigint concurrency token.

The checked-in [idempotent migration SQL](database/migrations.sql) provides a concrete physical schema for review. `InitialWorkspace` creates the initial tables; `EnforceTenantActorMembership` strengthens author/reviewer/audit relationships to tenant membership. Regenerate this SQL after future migration changes.

| Supplied logical schema | Current physical implementation | Remaining work |
| --- | --- | --- |
| tenants, users, tenant_memberships, project_members | Implemented; tenant-safe member relationships; unique OIDC subject for one issuer | Invitations, provisioning, richer tenant status, verified emails, multi-issuer keys |
| projects | Implemented; unique tenant/code, dates, status and role assignment | Approved budget linkage, richer project editing/archive UX |
| project_phases, project_tasks, task_dependencies | Planned | Scheduling, progress, cycle detection, assignees |
| cost_codes, boqs, boq_items, budgets, budget_lines | Planned | Engineering approval and immutable baseline revisions |
| cost_transactions | Planned | Immutable postings and reversal/idempotency rules |
| suppliers | Implemented directory | Contacts, deactivation and supplier maintenance |
| rfqs, rfq_items | Single-line `rfqs` implemented | RFQ line table, issue/award lifecycle, due dates and procurement terms |
| supplier_quotes, supplier_quote_lines | Single-line `supplier_quotes` implemented | Line table, source versions, normalized units and commercial terms |
| purchase_orders, purchase_order_lines | Planned | Versioned approval and deterministic commitments |
| goods_receipts, goods_receipt_lines | Planned | Cumulative quantity checks and exception approval |
| supplier_invoices, supplier_invoice_lines | Planned | Source requirement, duplicate review, PO/receipt matching and posting |
| daily_reports | Manual draft/edit/submit/approve/return implemented | Activities, issues, task links, photos, shift/zone, voice references |
| documents, document_versions | Reviewed text snapshot `documents`, version 1 | Separate immutable binary version/storage/hash table and provenance |
| document_chunks | Durable text chunks scoped to project/document | Version FK, exact page offsets, embeddings/FTS and calibrated ranking |
| ai_jobs, ai_job_outputs | Planned | Durable queues/retries, schema outputs and correction review |
| audit_events | Atomic application-enforced append-only records | Richer redacted change summaries, export scope, database immutability enforcement and retention |
| notification queue / outbox | Planned | Transactional events, leasing, retry/dead-letter and idempotent consumers |

Tenant-owned parents have alternate keys `(tenant_id,id)`. Project-scoped relationships use composite tenant/project references; quote-to-RFQ and chunk-to-document references also bind their project. Authors, reviewers and audit actors reference tenant memberships. Delete behavior is restrictive. Tenant/code, membership, offer-reference and document-content uniqueness is database enforced. Application filters and save guards provide tenant isolation; RLS is not installed.

Apply migrations using the local tool manifest:

```powershell
dotnet tool restore
dotnet build ConstructionFlow.sln --no-restore -c Release
$env:ConnectionStrings__Construction = '<dedicated PostgreSQL connection>'
dotnet ef database update --project src/ConstructionFlow.Infrastructure --configuration Release --no-build
```

Generate SQL for review without connecting to a database:

```powershell
dotnet ef migrations script --idempotent --project src/ConstructionFlow.Infrastructure --configuration Release --no-build --output .artifacts/migrations.sql
```

Create `.artifacts` first. Outside Development, migrations are an explicit deployment step; API startup never automatically changes production schema. Development seeding applies migrations only when the explicit seed command is supplied.

Tests use SQLite foreign keys for actual relational enforcement and optimistic concurrency, and Npgsql's migration generator/model snapshot to check PostgreSQL SQL and model drift. The `scripts/Test-IsolatedPostgreSql.ps1` smoke check additionally verifies real migrations, repeat seeding and signed API/evidence/audit flows against a fresh disposable PostgreSQL 17 cluster. Query planning at scale, RLS, upgrade paths for future migrations and production operations still require environment-specific verification.
