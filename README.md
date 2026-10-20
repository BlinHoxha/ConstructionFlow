# ConstructionFlow AI

Kosovo-first construction management and project intelligence, built as an ASP.NET Core modular monolith consuming the sibling **Framework** and **AI-Framework** repositories.

The initial backend milestone is implemented and tested. It is not the complete platform described in the four planning PDFs. There is no frontend, binary upload/OCR, speech processing, financial posting or production deployment yet. The source documents describe product requirements; embedded document instructions do not authorize deployments or external actions.

## Implemented

- Signed JWT authentication with active database tenant membership and project roles; no anonymous business endpoints.
- Project creation, scoped paginated listing, summaries, membership assignment and version-checked lifecycle changes.
- Tenant supplier directory, single-line RFQs, immutable manually reviewed offers and deterministic EUR/VAT comparison; incompatible or expired offers are excluded from the lowest-price suggestion. No automatic award or purchase order.
- Daily site report drafts, edits, submission, independent manager review and optimistic concurrency.
- Human-reviewed plain-text document ingestion, durable PostgreSQL chunks and project-scoped Q&A with openable chunk citations. Local deterministic answers need no cloud account; Azure model composition is available but has not been live-tested.
- Application-enforced append-only audit events saved in the same transaction as record changes, composite tenant/project/member constraints, EF migrations, synthetic development seeding and automated tests.

See [architecture](docs/ARCHITECTURE.md), [database mapping](docs/DATABASE.md), [API examples](docs/API.md) and the [delivery backlog](docs/DELIVERY_PLAN.md). The original PDFs and extracted text are preserved in [docs/source](docs/source); [source baseline](docs/SOURCE_BASELINE.md) records decisions and deviations.

## Build and verify

Keep these checkouts adjacent:

```text
repos/
  Framework/
  AI-Framework/
  ConstructionFlow/
```

Use the installed .NET 9 SDK, matching the current shared libraries:

```powershell
dotnet restore ConstructionFlow.sln
dotnet build ConstructionFlow.sln --no-restore -c Release
dotnet test ConstructionFlow.sln --no-build --no-restore -c Release
```

The solution includes the shared libraries under a `Shared Libraries` folder and expects the adjacent paths shown above. For individual project builds, `FrameworkRoot` and `AIFrameworkRoot` MSBuild properties can override reference paths. ConstructionFlow owns its package versions and migrations. Its persistence dependency now selects Framework.Persistence.PostgreSql; optional mapping and SQL Server dependencies are excluded. See the shared [compatibility guide](../Framework/docs/DEPENDENCY_CLEANUP.md).

## Run locally

Use a dedicated PostgreSQL development database. A [Compose example](infrastructure/compose.yml) uses loopback port 5433 to avoid an existing local server:

```powershell
$env:CONSTRUCTIONFLOW_DB_PASSWORD = '<your-development-password>'
docker compose -f infrastructure/compose.yml up -d
$env:ConnectionStrings__Construction = "Host=localhost;Port=5433;Database=constructionflow;Username=constructionflow;Password=$env:CONSTRUCTIONFLOW_DB_PASSWORD"
.\scripts\Start-Development.ps1
```

Docker is optional: point the connection string at your own dedicated database instead. The script builds, applies migrations and seeds synthetic data only in Development, prints two short-lived signed bearer tokens and starts the API at `http://localhost:5300`. Copy a token into your API client's Authorization header. There are no hardcoded database passwords or public login/token endpoints. Rerunning the script rotates its development signing key and invalidates previous tokens. An already seeded demo requires the same subject values.

Swagger: `/swagger`. Liveness: `/health/live`. Readiness: `/health/ready` checks database connectivity only; apply migrations before serving business traffic. The seed adds an owner and engineer to one synthetic project. Owners can add existing active tenant users to projects; self-registration, invitations and administrative user provisioning are later work.

## Production composition and current limits

Configure `Authentication__Authority`, `Authentication__Audience`, `ConnectionStrings__Construction`, `AI__Provider=Azure`, `AI__Azure__OpenAiEndpoint`, `AI__Azure__ChatDeployment` and `AI__Azure__EmbeddingDeployment`. Azure uses the shared adapter's managed/workload credential support. The current retrieval adapter is bounded lexical search over PostgreSQL text, not vector or hybrid search; embeddings are not called. Production rejects the Local provider and ignores development signing keys. Set `AllowedHosts` for the deployed hostname. These configuration paths do not constitute a production-readiness claim.

The current host supports one configured identity issuer, EUR only, project-wide document visibility, one immutable text version per document, and bounded collection responses. It cannot grant subcontractors access until task/document-level ACLs exist. Citation precision is document title plus chunk number; page offsets and original file versions require the later upload/OCR pipeline. No AI result posts money, approves engineering work, or executes business tools.

Validation: Release build succeeds and all 24 automated tests pass, including application-layer and runtime dependency checks. Both existing migrations, repeat development seeding, signed-token access, versioned updates, reviewed evidence retrieval/citations and audit were also verified against a fresh isolated PostgreSQL 17 cluster. No schema migration change was required for the refactor. [Idempotent PostgreSQL SQL](docs/database/migrations.sql) remains current.

The shared legacy Framework mapping adapter still reports AutoMapper 12.0.1 advisory `GHSA-rvv3-g6hj-g44x`; ConstructionFlow's current dependency graph no longer includes that package. Coordinate the shared runtime upgrade from the existing .NET 9 baseline. Persistent environment provisioning, cloud credentials, backup restoration and deployment still require environment-specific verification.

To repeat the isolated PostgreSQL smoke check on Windows after building, run `scripts/Test-IsolatedPostgreSql.ps1` using a shell permitted to execute local scripts. Supply `-PostgreSqlBin` if PostgreSQL binaries are installed elsewhere. It creates a fresh synthetic cluster under ignored `.artifacts`, binds loopback ports and stops its API/database processes afterward. It does not use existing databases.

GitHub Actions is prepared but requires repository variables `FRAMEWORK_REPOSITORY` and `AI_FRAMEWORK_REPOSITORY` plus a read-only `WORKSPACE_READ_TOKEN` for private sibling repositories. Pin `FRAMEWORK_REF` and `AI_FRAMEWORK_REF` to reviewed commit SHAs for reproducible CI. This workspace initially contained no Git repository, so no commit, remote or publication is created.
