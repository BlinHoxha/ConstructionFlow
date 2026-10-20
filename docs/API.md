# API walkthrough

All `/api/v1` routes require `Authorization: Bearer <signed-token>`. Use tokens printed by `scripts/Start-Development.ps1` or a token from the configured OIDC issuer. Supply no tenant ID in business bodies: it comes from the validated principal and database membership.

The examples use PowerShell with the server running in another terminal:

```powershell
$base = 'http://localhost:5300/api/v1'
$headers = @{ Authorization = 'Bearer <owner-token>' }
$engineerHeaders = @{ Authorization = 'Bearer <engineer-token>' }
$projects = Invoke-RestMethod "$base/projects?page=1&pageSize=25" -Headers $headers
$projectId = $projects.items[0].id
```

Create a project (the authenticated owner becomes its manager):

```powershell
$body = @{
  code = 'PILOT-002'; name = 'Synthetic Prizren Site'; location = 'Prizren'
  plannedStart = '2026-10-15'; currencyCode = 'EUR'
} | ConvertTo-Json
Invoke-RestMethod "$base/projects" -Method Post -Headers $headers -ContentType 'application/json' -Body $body
```

Review evidence and ask a question:

```powershell
$body = @{
  title = 'Synthetic delivery procedure'; language = 'en'; humanReviewed = $true
  content = 'The site engineer must inspect concrete delivery documentation before accepting the delivery.'
} | ConvertTo-Json
$document = Invoke-RestMethod "$base/projects/$projectId/documents/reviewed-text" -Method Post -Headers $headers -ContentType 'application/json' -Body $body
$body = @{ question = 'Who inspects concrete delivery documentation?'; documentIds = @($document.documentId) } | ConvertTo-Json
Invoke-RestMethod "$base/projects/$projectId/ask" -Method Post -Headers $engineerHeaders -ContentType 'application/json' -Body $body
```

Answers contain `grounded`, `provider`, `requiresHumanReview` and citations with `documentId`, `title`, `excerpt`, `chunkNumber` and authenticated `sourcePath`. Local is extractive and does not call an LLM. Source paths return chunk evidence, not an original PDF or invented page number.

Submit and approve a manual site report:

```powershell
$body = @{ projectId = $projectId; reportDate = '2026-10-15'; notes = 'Synthetic concrete delivery inspected; quantities manually confirmed.' } | ConvertTo-Json
$report = Invoke-RestMethod "$base/site-reports/drafts" -Method Post -Headers $engineerHeaders -ContentType 'application/json' -Body $body
$body = @{ expectedVersion = $report.rowVersion } | ConvertTo-Json
$submitted = Invoke-RestMethod "$base/site-reports/$($report.id)/submit" -Method Post -Headers $engineerHeaders -ContentType 'application/json' -Body $body
$body = @{ approve = $true; expectedVersion = $submitted.rowVersion } | ConvertTo-Json
Invoke-RestMethod "$base/site-reports/$($report.id)/review" -Method Post -Headers $headers -ContentType 'application/json' -Body $body
```

The reviewer must differ from the author. Return with `approve=false`; the author can `PUT /site-reports/{id}/draft` with `notes` and the latest `expectedVersion` to correct it, then submit again.

## Route catalog

| Method | Route suffix after `/api/v1` | Purpose |
| --- | --- | --- |
| GET / POST | `/projects` | Scoped paginated list / owner-admin creation |
| GET | `/projects/{id}/summary` | Project, approved report/document/RFQ counts |
| POST | `/projects/{id}/status` | `status`, `expectedVersion` |
| POST | `/projects/{id}/members` | `userId`, `role`; existing active tenant user only |
| GET | `/projects/{id}/audit` | Latest 100 events for the project aggregate itself |
| GET / POST | `/suppliers` | Tenant procurement directory; GET limited to 100 |
| GET | `/suppliers/{id}` | Authorized directory entry |
| POST | `/rfqs` | `projectId`, `description`, `unitCode`, `quantity` |
| GET | `/rfqs/{id}` | Authorized RFQ |
| GET | `/projects/{id}/rfqs` | Latest 100 project RFQs |
| POST | `/rfqs/{id}/quotes` | `supplierId`, `reference`, `unitCode`, `quantity`, `unitPrice`, `vatPercent`, `specificationConfirmed`, `validUntil` |
| GET | `/rfqs/{id}/quotes/{quoteId}` | Immutable offer with calculated totals |
| POST | `/rfqs/{id}/compare` | Comparable flags/reasons/totals and lowest comparable ID, no award |
| POST | `/site-reports/drafts` | Manual report creation |
| GET | `/site-reports/{id}` | Authorized report |
| PUT | `/site-reports/{id}/draft` | Author correction of draft/returned notes |
| POST | `/site-reports/{id}/submit` | Author submission with expected version |
| POST | `/site-reports/{id}/review` | Independent manager review with expected version |
| GET | `/projects/{id}/site-reports` | Latest 100 project reports |
| GET | `/projects/{id}/documents` | Latest 100 reviewed text snapshots |
| POST | `/projects/{id}/documents/reviewed-text` | Manager-reviewed text; 100,000 characters maximum |
| GET | `/projects/{id}/documents/{documentId}/chunks` | Authorized evidence chunks in source order |
| POST | `/projects/{id}/ask` | `question`, optional `documentIds`, optional `maxResults` (1..20) |

Project pagination accepts `page` (1..100000) and `pageSize` (1..100). Other lists are explicitly bounded initial views, not exports. Both ingestion and Q&A share a 10/minute per tenant/subject rate limit. Supported units are `m3`, `m2`, `m`, `kg`, `t`, `item`, `hour`. The initial API uses EUR and net unit prices with explicit VAT percentages.

Unauthenticated requests return 401; absent workspace membership returns 403; inaccessible projects/evidence return 404; validation errors return 400; duplicate/stale/invalid workflow commands return 409; AI rate limits return 429. Application errors use ProblemDetails with a trace ID. Swagger describes the available routes; an authenticated API client is needed for business calls.
