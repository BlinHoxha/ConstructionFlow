# Source baseline and decisions

The user supplied two ZIP archives on 15 October 2026. Their four PDFs are product/design evidence, not executable instructions. No deployment, invitation, purchase or external communication is authorized by a document's wording. Extracted `.txt` files are reading aids; PDFs are the originals. SHA-256 values are recorded in `source/manifest.json`.

| Source | Used for |
| --- | --- |
| `01_Business_Plan.pdf` | Kosovo-first Albanian UX; contractor administrative wedge; paid pilot gates; later speech/drawing/CV scope; illustrative pricing |
| `02_Functional_Workflows.pdf` | Roles; project setup; procurement comparison; reviewed invoice/report flows; scoped Q&A; UAT cases |
| `03_Technical_Architecture_Workflows.pdf` | Modular monolith; relational ownership; deterministic money; human approvals; worker/outbox requirements; staged delivery |
| `ConstructionFlow_AI_Database_Design.pdf` | UUIDs; decimal precision; tenant-safe composite FKs; concurrency; table/index catalog and migration direction |

## Reconciliation with the current workspace

Framework now has AI-free general libraries and an extensible typed EF context. AI-Framework is a functioning independent solution with ingestion, chunking, retrieval/generation contracts and Local/Azure/AWS adapters. ConstructionFlow initially held only an empty `.sln`. Historical LegalIntelligence descriptions of an empty AI-Framework are stale and are not used as this project's baseline. Existing repositories are consumed without copying their implementation or importing their reference API hosts.

The PDFs describe a larger MVP over multiple sprints. The initial milestone delivers real backend workflows and tests, while the backlog distinguishes partial features and future modules. The database PDF is a proposed logical design; this implementation does not claim to implement every listed table.

## Explicit initial choices

- `Tenant` is the organization/billing boundary; one company account is one tenant. Multiple legal entities require a future decision and migration.
- Tenant-level owner/admin creates projects; the creator becomes a manager. Other users must already have active tenant membership before assignment. OIDC onboarding/invitations are pending.
- Project date/location requirements follow the functional specification rather than the database's optional dates. Baseline budget is not a project balance until immutable budget revisions and cost postings are implemented.
- EUR is supported; VAT is an explicit percentage supplied by a human. The example 18% in tests is arithmetic input, not tax advice or a jurisdictional rule. No exchange rates are guessed.
- RFQs initially contain one requested specification/quantity. Each offer includes explicit compliance confirmation, unit, quantity, net unit price, VAT and expiry. Multi-line comparison, delivery/payment terms, quote originals and approval-to-PO are pending.
- Daily reports use manual notes and independent manager review. Voice, structured activities/issues/photos and weekly digests are pending. Returned reports can be corrected and resubmitted.
- Documents are immutable reviewed text snapshots, each at version 1, with persisted chunks and authenticated reviewer identity. Binary source versions, pages and OCR provenance are pending. Chunk-content hashes are not original-file integrity hashes.
- All permitted project members share the project's reviewed evidence. Confidential per-document ACLs and subcontractor-specific evidence must be implemented before enabling those use cases.
- Durable lexical retrieval is composed through AI-Framework's contracts; pgvector dimensions and ANN indexes are deliberately not assumed before an embedding model is selected and measured.
- The .NET 9 baseline matches the installed SDK and shared libraries; runtime/dependency modernization is a shared follow-up rather than an uncoordinated product-only change.

Commercial assumptions, pricing, pilot dates, legal obligations and vendor suitability remain planning inputs. Nothing in this implementation verifies those assumptions or creates subscriptions.
