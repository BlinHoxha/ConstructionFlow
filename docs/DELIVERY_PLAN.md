# Delivery plan

The current implementation is an initial backend milestone with selected vertical slices. It does not represent completion of all eight sprints in the supplied technical plan. Each later milestone requires its own acceptance evidence.

| Milestone | State and deliverables | Acceptance gate |
| --- | --- | --- |
| Foundation | Implemented: solution, shared project integration, auth/membership, projects, migrations, scoped API, audit, tests, CI template | Local checks pass; CI sibling access configured; live PostgreSQL migration smoke test |
| Procurement and budgeting | Partial: suppliers, single-line RFQs/quotes and comparison. Next: cost codes, BOQ/manual quantities, immutable budget revisions, multi-line offers, commercial terms | Deterministic VAT/rounding; mismatched units/specs flagged; independent baseline review |
| Workspace UI and onboarding | Planned: React/TypeScript mobile-first Albanian UI, sq/en/sr resources, OIDC sign-in/invitations, role-aware forms, pagination | Owner creates a project, assigns an engineer, views only authorized resources; translated forms and keyboard accessibility |
| Source document pipeline | Partial: reviewed plain-text evidence only. Next: private object storage, signed upload expiry, MIME/size/hash checks, malware quarantine, immutable versions and preview | Malicious/unscanned files never become evidence; exact source version/page opens under same ACL |
| Durable jobs and extraction | Planned: transactional outbox, worker leases, retries/backoff/dead-letter, OCR/classifier/schema validation, field corrections and usage budgets | Duplicate delivery is idempotent; poison jobs visible; accepted extraction never bypasses financial review |
| Financial workflows | Planned: multi-stage policy decision, PO approvals, receipts, invoice matching/duplicates and immutable cost/reversal postings, CSV export | One command cannot post twice; stale approvals fail; source and reviewer required; overreceipt exceptions reviewed |
| Site operations | Partial: manual report draft/edit/submit/review. Next: activities/issues/tasks/photos/zone/shift, evaluated Albanian speech and approved-only weekly digest | Audio ambiguity stays editable; no invented quantities; rejected drafts excluded from digest; independent approval |
| Intelligence and dashboards | Partial: scoped durable lexical Q&A with chunk citations. Next: hybrid retrieval, source/page/version grounding, conflict signals, correction feedback, approved financial/portfolio summaries | Synthetic/consented Albanian evaluation set; citation precision and abstention measured; adversarial and cross-tenant tests |
| Production pilot | Planned: observability, dependency remediation, runtime modernization, quotas, deployment IaC, retention/export/deletion, threat review, backup restoration and UAT | Zero critical isolation defects; tested restore; reviewed migration; signed pilot acceptance; monitored AI costs |

## Immediate next engineering steps

1. Provision a persistent development PostgreSQL database and configure sibling repository CI access with pinned reviewed shared commits. Fresh migrations, repeat seeding and authenticated API flows now pass in an isolated PostgreSQL 17 cluster; persistent staging/production environments remain separate provisioning work.
2. Agree OIDC onboarding and organization administrator provisioning. Current identities are synthetic or operator-provisioned; an arbitrary token cannot create membership.
3. Build the Albanian-first project/supplier/report UI against the tested API. Confirm whether subcontractors need logins before enabling assignment-level permissions.
4. Add cost codes, versioned budgets and multi-line procurement. Confirm customer VAT/invoice conventions and approval thresholds; do not encode illustrative document values as legal defaults.
5. Select storage and OCR/speech providers using consented or synthetic sample data. Add the outbox/worker and immutable binary versions before supporting unattended file ingestion.
6. Implement PO/receipt/invoice workflows and posting reversals with replay/concurrency tests. Only then label summaries as committed/actual costs.

## Product decisions still open

The source database plan calls out legal-entity structure, subcontractor identity, stock tracking, project/phase budgets and multi-stage signatures. These are recorded choices for later modeling, not blockers to the implemented tenant/project foundation. Commercial subscriptions, AI allowances, region, document retention, Kosovo-specific obligations and pilot acceptance need owner/customer decisions before launch.

CI currently compiles and tests the backend, including migration drift. Dependency/secret scanning, live PostgreSQL CI, UI E2E, cloud adapter evaluations, restore drills, OpenTelemetry and deployment approval/rollback pipelines are outstanding; the current workflow does not claim those checks.
