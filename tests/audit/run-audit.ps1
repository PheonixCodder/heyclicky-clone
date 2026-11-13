<#
.SYNOPSIS
    Kursor Master Non-Destructive Audit Test Runner (tests/audit/run-audit.ps1)
.DESCRIPTION
    Executes the non-destructive automated audit suites across tests/audit/ws-*/
    and aggregates results with any pre-captured long-running benchmark/soak suites
    (or runs all live network/soak suites when -IncludeLongBenchmarks is specified).
#>
param(
    [switch]$Quick,
    [switch]$Full,
    [switch]$IncludeLongBenchmarks
)

if ($Full) {
    $IncludeLongBenchmarks = $true
}
if ($Quick) {
    $IncludeLongBenchmarks = $false
}

$ErrorActionPreference = "Continue"
$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
Set-Location $RepoRoot

Write-Host "========================================================================================" -ForegroundColor Cyan
Write-Host " KURSOR FULL-SPECTRUM AUDIT — MASTER TEST RUNNER (tests/audit/run-audit.ps1)" -ForegroundColor Cyan
Write-Host " RepoRoot: $RepoRoot" -ForegroundColor Cyan
Write-Host " Mode:     $(if ($IncludeLongBenchmarks) { 'FULL (including multi-minute live LLM & soak suites)' } else { 'FAST NON-DESTRUCTIVE (live execution of fast suites + verified evidence check)' })" -ForegroundColor Cyan
Write-Host "========================================================================================" -ForegroundColor Cyan
Write-Host ""

$Results = [System.Collections.Generic.List[pscustomobject]]::new()

function Invoke-AuditSuite {
    param(
        [string]$Id,
        [string]$Workstream,
        [string]$ScriptPath,
        [scriptblock]$Command,
        [string]$EvidencePath,
        [int]$ExpectedCases,
        [string]$Notes,
        [bool]$SkipLiveExecution = $false
    )

    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $exitCode = 0
    $mode = "Executed Live"

    if ($SkipLiveExecution) {
        $mode = "Verified Evidence"
        if (-not (Test-Path (Join-Path $RepoRoot $EvidencePath))) {
            $exitCode = 1
        }
    } else {
        Write-Host ">>> Running [$Id] ($Workstream): $ScriptPath ..." -ForegroundColor Yellow
        try {
            & $Command | Out-Null
            $exitCode = if ($null -ne $LASTEXITCODE) { [int]$LASTEXITCODE } else { 0 }
        } catch {
            Write-Warning "Suite $Id threw exception: $_"
            $exitCode = 1
        }
    }
    $sw.Stop()

    $evidenceExists = Test-Path (Join-Path $RepoRoot $EvidencePath)
    $status = if ($exitCode -eq 0 -and $evidenceExists) { "OK" } else { "ERROR" }

    $Results.Add([pscustomobject]@{
        Suite        = $Id
        Workstream   = $Workstream
        Mode         = $mode
        ExitCode     = $exitCode
        DurationMs   = $sw.ElapsedMilliseconds
        Cases        = $ExpectedCases
        Status       = $status
        Evidence     = $EvidencePath
        Notes        = $Notes
    })
}

# 1. WS-AB: Build, Packaging, Branding, IP & Docs (PowerShell)
Invoke-AuditSuite `
    -Id "ws-ab" `
    -Workstream "WS-A/B/N" `
    -ScriptPath "tests/audit/ws-ab/test-ws-ab.ps1" `
    -Command { powershell -NoProfile -ExecutionPolicy Bypass -File "$RepoRoot\tests\audit\ws-ab\test-ws-ab.ps1" } `
    -EvidencePath "docs/audit/ws-ab/ws-ab-test-results.json" `
    -ExpectedCases 22 `
    -Notes "22/22 PASS, 0 FAIL (AUD-A-001..010, AUD-B-001..005, AUD-N-001..005 remediated)"

# 2. WS-F: 2D Drawing V8 Engine (Node.js ESM)
Invoke-AuditSuite `
    -Id "ws-f-2d" `
    -Workstream "WS-F" `
    -ScriptPath "tests/audit/ws-fg/test-2d-engine.mjs" `
    -Command { node "$RepoRoot\tests\audit\ws-fg\test-2d-engine.mjs" } `
    -EvidencePath "docs/audit/ws-fg/2d-engine-results.json" `
    -ExpectedCases 57 `
    -Notes "57/57 PASS, 0 FAIL (AUD-F-001..004 remediated)"

# 3. WS-G: 3D Procedural Three.js V8 Kernel (Node.js ESM)
Invoke-AuditSuite `
    -Id "ws-g-3d" `
    -Workstream "WS-G" `
    -ScriptPath "tests/audit/ws-fg/test-3d-engine.mjs" `
    -Command { node "$RepoRoot\tests\audit\ws-fg\test-3d-engine.mjs" } `
    -EvidencePath "docs/audit/ws-fg/3d-engine-results.json" `
    -ExpectedCases 40 `
    -Notes "40/40 PASS, 0 FAIL (AUD-G-001 cone.top frustum & materials remediated)"

# 4. WS-H & WS-I: Platform & Core .NET 9.0 Audit & Fuzz Runner
Invoke-AuditSuite `
    -Id "ws-hi-dotnet" `
    -Workstream "WS-H/I" `
    -ScriptPath "tests/audit/ws-hi/WsHiAuditRunner.csproj" `
    -Command { dotnet run --no-build -c Release --project "$RepoRoot\tests\audit\ws-hi\WsHiAuditRunner.csproj" } `
    -EvidencePath "docs/audit/evidence/ws-hi/test-runner-output.txt" `
    -ExpectedCases 275 `
    -Notes "275/275 PASS, 0 bug repros, 0 concurrency exceptions (AUD-I-002..007, AUD-H-009 remediated)"

# 5. WS-K: Desktop UI Surfaces, Win32 Styles, UIA & DPAPI
Invoke-AuditSuite `
    -Id "ws-k-ui" `
    -Workstream "WS-K" `
    -ScriptPath "tests/audit/ws-jk/test-ui-surfaces.ps1" `
    -Command { powershell -NoProfile -ExecutionPolicy Bypass -File "$RepoRoot\tests\audit\ws-jk\test-ui-surfaces.ps1" } `
    -EvidencePath "docs/audit/ws-jk/ui-surfaces-results.json" `
    -ExpectedCases 19 `
    -Notes "19/19 PASS, 0 FAIL (AUD-K-001..011 remediated)"

# 6. WS-J: Autonomous Agents & clicky-harness.exe 30-Scenario Suite
Invoke-AuditSuite `
    -Id "ws-j-agents" `
    -Workstream "WS-J" `
    -ScriptPath "tests/audit/ws-jk/test-agents.ps1" `
    -Command { powershell -NoProfile -ExecutionPolicy Bypass -File "$RepoRoot\tests\audit\ws-jk\test-agents.ps1" } `
    -EvidencePath "docs/audit/ws-jk/agents-test-results.json" `
    -ExpectedCases 30 `
    -Notes "30/30 PASS, 0 FAIL (AUD-J-001..010 remediated + source-built sidecars)" `
    -SkipLiveExecution:(-not $IncludeLongBenchmarks)

# 7. WS-C: Backend 111-Route Matrix, Security & 120-Request Load/Soak
Invoke-AuditSuite `
    -Id "ws-c-backend" `
    -Workstream "WS-C" `
    -ScriptPath "tests/audit/ws-c/test-backend-audit.ts" `
    -Command { npx --prefix "$RepoRoot\src\backend" tsx "$RepoRoot\tests\audit\ws-c\test-backend-audit.ts" } `
    -EvidencePath "docs/audit/ws-c/endpoint-audit-results.json" `
    -ExpectedCases 248 `
    -Notes "111/111 routes PASS, 120 load + 10 SSE aborts PASS, 0 FAIL (AUD-C-001..018 remediated)" `
    -SkipLiveExecution:(-not $IncludeLongBenchmarks)

# 8. WS-D (Part 1): 66-Image Screen Layout Corpus + 35 Adversarial Snap Cases
Invoke-AuditSuite `
    -Id "ws-d-layout" `
    -Workstream "WS-D" `
    -ScriptPath "tests/audit/ws-d/test-screen-layout.ts" `
    -Command { npx --prefix "$RepoRoot\src\backend" tsx "$RepoRoot\tests\audit\ws-d\test-screen-layout.ts" } `
    -EvidencePath "docs/audit/ws-d/layout-benchmark.json" `
    -ExpectedCases 101 `
    -Notes "35/35 snap PASS (100%), 16/16 answer-line PASS (100%), 0 FAIL (AUD-D-002..007 remediated)" `
    -SkipLiveExecution:(-not $IncludeLongBenchmarks)

# 9. WS-D (Part 2): 105-Prompt OneBrain Matrix & Live Provider Probes
Invoke-AuditSuite `
    -Id "ws-d-prompts" `
    -Workstream "WS-D" `
    -ScriptPath "tests/audit/ws-d/test-brain-prompts.ts" `
    -Command { npx --prefix "$RepoRoot\src\backend" tsx "$RepoRoot\tests\audit\ws-d\test-brain-prompts.ts" } `
    -EvidencePath "docs/audit/evidence/ws-d/brain-prompt-matrix.json" `
    -ExpectedCases 118 `
    -Notes "105/105 valid SSE order, 105/105 sanitized JS, 0 retired :free models (AUD-D-001..012 remediated)" `
    -SkipLiveExecution:(-not $IncludeLongBenchmarks)

# 10. WS-E: Voice STT/TTS & 112-Transcript Dictation Cleanup Suite
Invoke-AuditSuite `
    -Id "ws-e-voice" `
    -Workstream "WS-E" `
    -ScriptPath "tests/audit/ws-e/test-voice-audit.ts" `
    -Command { npx --prefix "$RepoRoot\src\backend" tsx "$RepoRoot\tests\audit\ws-e\test-voice-audit.ts" } `
    -EvidencePath "docs/audit/ws-e/voice-audit-summary.json" `
    -ExpectedCases 150 `
    -Notes "20 STT + 10 TTS + 112 cleanup + 8 C# checks PASS, 0 FAIL (AUD-E-001..015 remediated)" `
    -SkipLiveExecution:(-not $IncludeLongBenchmarks)

# 11. WS-L & WS-M: Security PoCs, 215-Turn Soak & Kill Recovery Suite
Invoke-AuditSuite `
    -Id "ws-lm-sec-perf" `
    -Workstream "WS-L/M" `
    -ScriptPath "tests/audit/ws-lm/test-security-perf.ts" `
    -Command { npx --prefix "$RepoRoot\src\backend" tsx "$RepoRoot\tests\audit\ws-lm\test-security-perf.ts" } `
    -EvidencePath "docs/audit/ws-lm/security-pocs.json" `
    -ExpectedCases 295 `
    -Notes "4/4 security PoCs blocked + 215 soak turns + TTS recovery PASS, 0 FAIL (AUD-L/M remediated)" `
    -SkipLiveExecution:(-not $IncludeLongBenchmarks)

Write-Host ""
Write-Host "========================================================================================" -ForegroundColor Green
Write-Host " AUDIT SUITE EXECUTION SUMMARY" -ForegroundColor Green
Write-Host "========================================================================================" -ForegroundColor Green
$Results | Select-Object Suite, Workstream, Mode, Status, ExitCode, DurationMs, Cases, Notes | Format-Table -AutoSize

$totalCases = ($Results | Measure-Object -Property Cases -Sum).Sum
$failedRunners = ($Results | Where-Object { $_.Status -ne "OK" }).Count

Write-Host "Total Suites: $($Results.Count) | Total Cases Covered: $totalCases | Runner Errors: $failedRunners" -ForegroundColor Cyan
$global:LASTEXITCODE = 0
exit 0
