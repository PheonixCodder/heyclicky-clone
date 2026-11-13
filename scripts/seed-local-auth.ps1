param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Security

$RepoRoot = Split-Path -Parent $PSScriptRoot
$DistDir = Join-Path $RepoRoot "dist\kursor-desktop"
$ExePath = Join-Path $DistDir "HeyClicky.exe"
$Utf8NoBom = New-Object System.Text.UTF8Encoding($false)

# 1. Write camelCase (+ PascalCase) endpoints.json everywhere
$EndpointsJson = @"
{
  "workerBaseUrl": "http://127.0.0.1:8787",
  "supabaseUrl": "http://127.0.0.1:8787",
  "supabaseAnonKey": "kursor-local-anon-key",
  "WorkerBaseUrl": "http://127.0.0.1:8787",
  "SupabaseUrl": "http://127.0.0.1:8787",
  "SupabaseAnonKey": "kursor-local-anon-key"
}
"@

$TargetDirs = @(
    (Join-Path $env:LOCALAPPDATA "HeyClicky"),
    (Join-Path $env:LOCALAPPDATA "Kursor"),
    $DistDir,
    (Join-Path $env:LOCALAPPDATA "HeyClickyApp\current")
)

foreach ($dir in $TargetDirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    [System.IO.File]::WriteAllText((Join-Path $dir "endpoints.json"), $EndpointsJson, $Utf8NoBom)
    $prodFlag = Join-Path $dir "use-production-backend.flag"
    if (Test-Path $prodFlag) {
        Remove-Item $prodFlag -Force -ErrorAction SilentlyContinue
    }
}

# 2. Build a valid 10-year Local JWT matching src/backend/src/routes/auth.ts
function ConvertTo-Base64Url([string]$text) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($text)
    return [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$NowSec = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$ExpSec = $NowSec + 315360000 # 10 years
$UserId = "00000000-0000-0000-0000-000000000001"
$Email  = "local@kursor.dev"
$Name   = "Kursor User"
$Avatar = "https://api.dicebear.com/9.x/bottts/svg?seed=kursor"

$JwtHeader  = ConvertTo-Base64Url '{"alg":"HS256","typ":"JWT"}'
$JwtPayload = ConvertTo-Base64Url ("{`"sub`":`"$UserId`",`"email`":`"$Email`",`"role`":`"authenticated`",`"aud`":`"authenticated`",`"iat`":$NowSec,`"exp`":$ExpSec}")
$JwtSig     = ConvertTo-Base64Url "kursor-local-dev-signature"
$AccessToken = "$JwtHeader.$JwtPayload.$JwtSig"
$RefreshToken = "kursor_rt_" + (ConvertTo-Base64Url "${UserId}:${NowSec}")

# 3. Encrypt StoredAuthSession via Windows DPAPI ("HeyClicky.Auth.v1" entropy)
$StoredSessionObj = @{
    access_token = $AccessToken
    refresh_token = $RefreshToken
    user_id = $UserId
    email = $Email
    display_name = $Name
    avatar_url = $Avatar
    signup_date_epoch_seconds = [double]$NowSec
}
$SessionJsonText = $StoredSessionObj | ConvertTo-Json -Compress
$SessionBytes = [System.Text.Encoding]::UTF8.GetBytes($SessionJsonText)
$EntropyBytes = [System.Text.Encoding]::UTF8.GetBytes("HeyClicky.Auth.v1")
$ProtectedBytes = [System.Security.Cryptography.ProtectedData]::Protect(
    $SessionBytes,
    $EntropyBytes,
    [System.Security.Cryptography.DataProtectionScope]::CurrentUser
)

foreach ($appDirName in @("HeyClicky", "Kursor")) {
    $appDir = Join-Path $env:LOCALAPPDATA $appDirName
    foreach ($authFileName in @("auth.localdev.bin", "auth.bin")) {
        $authFilePath = Join-Path $appDir $authFileName
        if ($Force -or -not (Test-Path $authFilePath)) {
            [System.IO.File]::WriteAllBytes($authFilePath, $ProtectedBytes)
            Write-Host "[+] Seeded DPAPI session: $authFilePath" -ForegroundColor Green
        }
    }
}

# 4. Register heyclicky:// and clicky:// custom URL schemes in HKCU
if (Test-Path $ExePath) {
    foreach ($scheme in @("heyclicky", "clicky")) {
        $baseKey = "HKCU:\Software\Classes\$scheme"
        $cmdKey  = "$baseKey\shell\open\command"
        New-Item -Path $cmdKey -Force | Out-Null
        Set-ItemProperty -Path $baseKey -Name "(Default)" -Value "URL:HeyClicky ($scheme)"
        Set-ItemProperty -Path $baseKey -Name "URL Protocol" -Value ""
        Set-ItemProperty -Path $cmdKey  -Name "(Default)" -Value "`"$ExePath`" `"%1`""
    }
    Write-Host "[+] Registered heyclicky:// and clicky:// URI schemes -> $ExePath" -ForegroundColor Green
}

Write-Host "[+] Local Kursor endpoints & authentication session ready!" -ForegroundColor Cyan
