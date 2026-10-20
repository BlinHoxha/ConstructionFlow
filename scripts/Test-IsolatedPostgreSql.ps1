param([string]$PostgreSqlBin = 'C:\Program Files\PostgreSQL\17\bin')
$ErrorActionPreference = 'Stop'
$workspace = Split-Path -Parent $PSScriptRoot
$runDirectory = Join-Path $workspace ('.artifacts\postgres-smoke-' + [Guid]::NewGuid().ToString('N'))
$dataDirectory = Join-Path $runDirectory 'data'
$apiDll = Join-Path $workspace 'src\ConstructionFlow.Api\bin\Release\net9.0\ConstructionFlow.Api.dll'
if (!(Test-Path -LiteralPath $apiDll)) { throw 'Build the Release solution before running this verification.' }
foreach ($binary in @('initdb.exe','pg_ctl.exe','createdb.exe')) {
    if (!(Test-Path -LiteralPath (Join-Path $PostgreSqlBin $binary))) { throw "Missing PostgreSQL binary: $binary" }
}
function Get-FreeLoopbackPort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try { return $listener.LocalEndpoint.Port } finally { $listener.Stop() }
}
function Encode-Base64Url([byte[]]$Value) {
    return [Convert]::ToBase64String($Value).TrimEnd('=').Replace('+','-').Replace('/','_')
}
function New-SmokeToken([string]$Subject) {
    $header = Encode-Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
    $claims = @{ iss='constructionflow-development'; aud='constructionflow-api'; sub=$Subject;
        tenant_id='11111111-1111-1111-1111-111111111111'; exp=[DateTimeOffset]::UtcNow.AddMinutes(5).ToUnixTimeSeconds() } | ConvertTo-Json -Compress
    $payload = Encode-Base64Url ([Text.Encoding]::UTF8.GetBytes($claims))
    $unsigned = "$header.$payload"
    $hmac = New-Object Security.Cryptography.HMACSHA256
    $hmac.Key = [Text.Encoding]::UTF8.GetBytes($env:Authentication__DevelopmentSigningKey)
    try { return "$unsigned.$(Encode-Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned))))" }
    finally { $hmac.Dispose() }
}
$savedEnvironment = @{}
$settings = @{
    ASPNETCORE_ENVIRONMENT='Development'; AI__Provider='Local';
    Authentication__Authority=''; Authentication__Audience='';
    Authentication__DevelopmentSigningKey=([Guid]::NewGuid().ToString('N') + [Guid]::NewGuid().ToString('N'));
    DevelopmentSeed__OwnerSubject='smoke-owner'; DevelopmentSeed__EngineerSubject='smoke-engineer'
}
$databasePort = Get-FreeLoopbackPort
$apiPort = Get-FreeLoopbackPort
$settings['ConnectionStrings__Construction'] = "Host=127.0.0.1;Port=$databasePort;Database=constructionflow_smoke;Username=constructionflow_smoke;Pooling=false"
$databaseStarted = $false
$apiProcess = $null
try {
    New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
    foreach ($name in $settings.Keys) {
        $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $settings[$name], 'Process')
    }
    # Fresh synthetic cluster on loopback only; no existing PostgreSQL service/database is used.
    & (Join-Path $PostgreSqlBin 'initdb.exe') -D $dataDirectory -U constructionflow_smoke --auth=trust --locale=C --encoding=UTF8 *> (Join-Path $runDirectory 'initdb.log')
    if ($LASTEXITCODE -ne 0) { throw "Isolated initdb failed. See $runDirectory" }
    & (Join-Path $PostgreSqlBin 'pg_ctl.exe') -D $dataDirectory -l (Join-Path $runDirectory 'postgres.log') -o "-h 127.0.0.1 -p $databasePort" -w start
    if ($LASTEXITCODE -ne 0) { throw 'Isolated PostgreSQL startup failed.' }
    $databaseStarted = $true
    & (Join-Path $PostgreSqlBin 'createdb.exe') -h 127.0.0.1 -p $databasePort -U constructionflow_smoke constructionflow_smoke
    if ($LASTEXITCODE -ne 0) { throw 'Smoke database creation failed.' }
    & dotnet exec $apiDll --seed-development *> (Join-Path $runDirectory 'seed.log')
    if ($LASTEXITCODE -ne 0) { throw "Migration/seeding failed. See $runDirectory" }
    & dotnet exec $apiDll --seed-development *> (Join-Path $runDirectory 'seed-repeat.log')
    if ($LASTEXITCODE -ne 0) { throw 'Repeated development seeding failed.' }
    $base = "http://127.0.0.1:$apiPort"
    $apiProcess = Start-Process -FilePath (Get-Command dotnet).Source -ArgumentList @('exec', ('"' + $apiDll + '"'), '--urls', $base) -WorkingDirectory (Split-Path -Parent $apiDll) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $runDirectory 'api.log') -RedirectStandardError (Join-Path $runDirectory 'api-error.log')
    $ready = $false
    for ($attempt=0; $attempt -lt 30; $attempt++) {
        try { $null = Invoke-RestMethod "$base/health/ready"; $ready = $true; break }
        catch { if ($apiProcess.HasExited) { throw "API exited. See $runDirectory" }; Start-Sleep -Milliseconds 300 }
    }
    if (!$ready) { throw 'API readiness timed out.' }
    $headers = @{ Authorization = 'Bearer ' + (New-SmokeToken 'smoke-owner') }
    $projects = Invoke-RestMethod "$base/api/v1/projects" -Headers $headers
    if ($projects.total -ne 1) { throw 'Expected one seeded project.' }
    $projectId = $projects.items[0].id
    $statusBody = @{ status='Active'; expectedVersion=1 } | ConvertTo-Json
    $project = Invoke-RestMethod "$base/api/v1/projects/$projectId/status" -Headers $headers -Method Post -ContentType 'application/json' -Body $statusBody
    if ($project.rowVersion -ne 2) { throw 'PostgreSQL optimistic version update failed.' }
    $documentBody = @{ title='Synthetic inspection'; language='en'; humanReviewed=$true;
        content='The site engineer checks concrete delivery documentation before acceptance.' } | ConvertTo-Json
    $document = Invoke-RestMethod "$base/api/v1/projects/$projectId/documents/reviewed-text" -Headers $headers -Method Post -ContentType 'application/json' -Body $documentBody
    $askBody = @{ question='Who checks concrete delivery documentation?'; documentIds=@($document.documentId) } | ConvertTo-Json
    $answer = Invoke-RestMethod "$base/api/v1/projects/$projectId/ask" -Headers $headers -Method Post -ContentType 'application/json' -Body $askBody
    if (!$answer.grounded -or $answer.citations.Count -lt 1) { throw 'PostgreSQL retrieval/citations failed.' }
    $audit = Invoke-RestMethod "$base/api/v1/projects/$projectId/audit" -Headers $headers
    if ($audit.Count -lt 2) { throw 'Expected persisted project audit events.' }
    Write-Output 'PASS: real PostgreSQL migrations, repeat seeding, signed authentication, project versioning, reviewed evidence, retrieval/citations and audit.'
    Write-Output "Verification logs: $runDirectory"
}
finally {
    if ($null -ne $apiProcess -and !$apiProcess.HasExited) { Stop-Process -Id $apiProcess.Id -Force }
    if ($databaseStarted) { & (Join-Path $PostgreSqlBin 'pg_ctl.exe') -D $dataDirectory -m fast -w stop }
    foreach ($name in $savedEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process') }
}
