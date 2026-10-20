param([string]$Url = 'http://localhost:5300')
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)
if ([string]::IsNullOrWhiteSpace($env:ConnectionStrings__Construction)) {
    throw 'Set ConnectionStrings__Construction to a dedicated development PostgreSQL database before running this script.'
}
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AI__Provider = 'Local'
$env:DevelopmentSeed__OwnerSubject = 'constructionflow-demo-owner'
$env:DevelopmentSeed__EngineerSubject = 'constructionflow-demo-engineer'
$bytes = New-Object byte[] 32
$random = [Security.Cryptography.RandomNumberGenerator]::Create()
$random.GetBytes($bytes)
$random.Dispose()
$env:Authentication__DevelopmentSigningKey = [Convert]::ToBase64String($bytes)
# Prevent an inherited cloud issuer from overriding the explicit development token setup.
$env:Authentication__Authority = ''
$env:Authentication__Audience = ''
function Base64Url([byte[]]$Value) {
    return [Convert]::ToBase64String($Value).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}
function New-DemoToken([string]$Subject) {
    $header = Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"HS256","typ":"JWT"}'))
    $claims = @{
        iss = 'constructionflow-development'; aud = 'constructionflow-api'; sub = $Subject
        tenant_id = '11111111-1111-1111-1111-111111111111'
        exp = [DateTimeOffset]::UtcNow.AddHours(2).ToUnixTimeSeconds()
    } | ConvertTo-Json -Compress
    $payload = Base64Url ([Text.Encoding]::UTF8.GetBytes($claims))
    $unsigned = "$header.$payload"
    $hmac = New-Object Security.Cryptography.HMACSHA256
    $hmac.Key = [Text.Encoding]::UTF8.GetBytes($env:Authentication__DevelopmentSigningKey)
    $signature = Base64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($unsigned)))
    $hmac.Dispose()
    return "$unsigned.$signature"
}
dotnet build ConstructionFlow.sln --no-restore -c Release
if ($LASTEXITCODE -ne 0) { throw 'Build failed. Run dotnet restore first.' }
dotnet run --project src/ConstructionFlow.Api --configuration Release --no-build --no-launch-profile -- --seed-development
if ($LASTEXITCODE -ne 0) { throw 'Development migration/seeding failed.' }
Write-Host "Owner bearer token (two hours, this local server only): $(New-DemoToken $env:DevelopmentSeed__OwnerSubject)"
Write-Host "Engineer bearer token (two hours, this local server only): $(New-DemoToken $env:DevelopmentSeed__EngineerSubject)"
dotnet run --project src/ConstructionFlow.Api --configuration Release --no-build --no-launch-profile -- --urls $Url
