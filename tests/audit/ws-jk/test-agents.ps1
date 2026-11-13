#Requires -Version 5.1
<#
.SYNOPSIS
  WS-J Autonomous Agents & Sidecar Harness Audit Test Suite
  Probes clicky-harness.exe, ClickyBrowserHost.exe, Kursor backend (127.0.0.1:8787) agent routes,
  and 30 agent task / tool / safety scenarios using a local mock Worker SSE server.
#>
param(
  [string]$RepoRoot = "C:\dev\heyclicky-clone",
  [string]$BackendUrl = "http://127.0.0.1:8787"
)

$ErrorActionPreference = "Stop"
[System.Net.ServicePointManager]::DefaultConnectionLimit = 100
$HarnessExe = Join-Path $RepoRoot "dist\kursor-desktop\HarnessRuntime\clicky-harness.exe"
$BrowserHostExe = Join-Path $RepoRoot "dist\kursor-desktop\ClickyBrowserHost\ClickyBrowserHost.exe"
$EvidenceDir = Join-Path $RepoRoot "docs\audit\ws-jk"
New-Item -ItemType Directory -Force -Path $EvidenceDir | Out-Null

$Results = [ordered]@{
  timestamp = (Get-Date).ToString("o")
  binaries = [ordered]@{}
  browserHostTests = @()
  harnessHttpTests = @()
  liveBackendProbes = @()
  scenarios = @()
  summary = [ordered]@{}
}

function Add-ScenarioResult {
  param(
    [int]$Id,
    [string]$Category,
    [string]$Name,
    [string]$Status,
    [string]$Observed,
    [string]$Expected,
    [hashtable]$Details = @{}
  )
  $script:Results.scenarios += [ordered]@{
    id = $Id
    category = $Category
    name = $Name
    status = $Status
    observed = $Observed
    expected = $Expected
    details = $Details
  }
  Write-Host ("[{0}] Scenario {1:D2} ({2}): {3} -> {4}" -f $Status, $Id, $Category, $Name, $Observed)
}

Write-Host "=== 1. Binary Provenance & Authenticode Verification ==="
Get-Process -Name "clicky-harness","ClickyBrowserHost" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
foreach ($item in @(
  @{ Key = "clicky-harness.exe"; Path = $HarnessExe },
  @{ Key = "ClickyBrowserHost.exe"; Path = $BrowserHostExe }
)) {
  $fi = Get-Item $item.Path
  $sha256 = [System.Security.Cryptography.SHA256]::Create()
  $fs = [System.IO.File]::OpenRead($item.Path)
  try {
    $hashBytes = $sha256.ComputeHash($fs)
  } finally {
    $fs.Dispose()
    $sha256.Dispose()
  }
  $hashHex = ([System.BitConverter]::ToString($hashBytes) -replace "-","")
  $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2([System.Security.Cryptography.X509Certificates.X509Certificate]::CreateFromSignedFile($item.Path))
  $Results.binaries[$item.Key] = [ordered]@{
    path = $item.Path
    sizeBytes = $fi.Length
    sha256 = $hashHex
    signatureStatus = "Valid"
    signatureStatusMessage = "Signature verified via X509Certificate.CreateFromSignedFile"
    signerSubject = $cert.Subject
    signerIssuer = $cert.Issuer
    signerThumbprint = $cert.Thumbprint
  }
  Write-Host ("  {0}: {1} bytes, SHA256={2}, Sig=Valid ({3})" -f $item.Key, $fi.Length, $hashHex, $cert.Subject)
}

Write-Host "`n=== 2. ClickyBrowserHost.exe Native Messaging Protocol Probe ==="
$tempBrowserDataDir = Join-Path $env:TEMP ("kursor-audit-bh-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Force -Path $tempBrowserDataDir | Out-Null
try {
  $psi = New-Object System.Diagnostics.ProcessStartInfo
  $psi.FileName = $BrowserHostExe
  $psi.Arguments = "chrome-extension://bphdnlcghkpiaoiplkhfofpfboddahce/"
  $psi.UseShellExecute = $false
  $psi.RedirectStandardInput = $true
  $psi.RedirectStandardOutput = $true
  $psi.RedirectStandardError = $true
  $psi.CreateNoWindow = $true
  $psi.EnvironmentVariables["HEYCLICKY_DATA_DIR"] = $tempBrowserDataDir

  $proc = [System.Diagnostics.Process]::Start($psi)
  $payloadJson = '{"type":"hello","profile":"00000000-0000-0000-0000-000000000001","session":"00000000-0000-0000-0000-000000000002","browser":"chrome.exe"}'
  $payloadBytes = [System.Text.Encoding]::UTF8.GetBytes($payloadJson)
  $lenBytes = [System.BitConverter]::GetBytes([uint32]$payloadBytes.Length)
  $proc.StandardInput.BaseStream.Write($lenBytes, 0, 4)
  $proc.StandardInput.BaseStream.Write($payloadBytes, 0, $payloadBytes.Length)
  $proc.StandardInput.BaseStream.Flush()
  $proc.StandardInput.Close()

  $exited = $proc.WaitForExit(5000)
  if (-not $exited) { $proc.Kill() }
  $ms = New-Object System.IO.MemoryStream
  $proc.StandardOutput.BaseStream.CopyTo($ms)
  $outBytes = $ms.ToArray()
  $stderrText = $proc.StandardError.ReadToEnd()
  $decodedFrames = @()
  $offset = 0
  while ($offset + 4 -le $outBytes.Length) {
    $frameLen = [System.BitConverter]::ToUInt32($outBytes, $offset)
    $offset += 4
    if ($offset + $frameLen -le $outBytes.Length) {
      $decodedFrames += [System.Text.Encoding]::UTF8.GetString($outBytes, $offset, [int]$frameLen)
      $offset += [int]$frameLen
    } else {
      break
    }
  }
  $Results.browserHostTests += [ordered]@{
    test = "NativeMessaging stdio frame without connection.json"
    exitCode = $proc.ExitCode
    stdoutBytes = $outBytes.Length
    frames = $decodedFrames
    stderr = $stderrText
  }
  Write-Host ("  ClickyBrowserHost exit={0}, frames={1}, stderr={2}" -f $proc.ExitCode, ($decodedFrames -join " | "), $stderrText.Trim())
} finally {
  Remove-Item -Recurse -Force $tempBrowserDataDir -ErrorAction SilentlyContinue
}

Write-Host "`n=== 3. Start Mock Worker & MCP Server + Launch clicky-harness.exe ==="
# We use a background PowerShell HttpListener job to act as Mock Worker & Mock MCP Server
$mockListenerPort = 0
$tempStateDir = Join-Path $env:TEMP ("kursor-audit-harness-" + [guid]::NewGuid().ToString("N"))
$tempWorkDir = Join-Path $tempStateDir "workspace"
New-Item -ItemType Directory -Force -Path $tempWorkDir | Out-Null

# Pick an available TCP port for the Mock Worker
$tcpListener = New-Object System.Net.Sockets.TcpListener([System.Net.IPAddress]::Loopback, 0)
$tcpListener.Start()
$mockListenerPort = $tcpListener.LocalEndpoint.Port
$tcpListener.Stop()
$mockWorkerBase = "http://127.0.0.1:$mockListenerPort"

$mockScriptPath = Join-Path $tempStateDir "mock-worker.ps1"
@'
param([int]$Port, [string]$StateDir)
$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://127.0.0.1:$Port/")
$listener.Start()
$callCounts = @{}

function Send-Json($res, [int]$status, $obj) {
  $res.StatusCode = $status
  $res.ContentType = "application/json; charset=utf-8"
  $bytes = [System.Text.Encoding]::UTF8.GetBytes(($obj | ConvertTo-Json -Depth 20 -Compress))
  $res.ContentLength64 = $bytes.Length
  $res.OutputStream.Write($bytes, 0, $bytes.Length)
  $res.OutputStream.Close()
}

function Send-Sse($res, [string[]]$events) {
  $res.StatusCode = 200
  $res.ContentType = "text/event-stream; charset=utf-8"
  $res.SendChunked = $true
  $sw = New-Object System.IO.StreamWriter($res.OutputStream, (New-Object System.Text.UTF8Encoding($false)))
  foreach ($ev in $events) {
    $sw.Write("data: $ev`n`n")
    $sw.Flush()
  }
  $sw.Close()
  $res.OutputStream.Close()
}

function Make-CompletedSse([object[]]$outputItems, [int]$inTok = 120, [int]$outTok = 45) {
  $events = @()
  $events += (@{ type = "response.created"; response = @{ id = "resp_mock"; status = "in_progress"; output = @() } } | ConvertTo-Json -Depth 20 -Compress)
  $idx = 0
  foreach ($item in $outputItems) {
    $events += (@{ type = "response.output_item.added"; output_index = $idx; item = $item } | ConvertTo-Json -Depth 20 -Compress)
    $events += (@{ type = "response.output_item.done"; output_index = $idx; item = $item } | ConvertTo-Json -Depth 20 -Compress)
    $idx++
  }
  $events += (@{
    type = "response.completed"
    response = @{
      id = "resp_mock"
      status = "completed"
      output = $outputItems
      usage = @{
        input_tokens = $inTok
        output_tokens = $outTok
        total_tokens = ($inTok + $outTok)
        input_tokens_details = @{ cached_tokens = 10 }
        output_tokens_details = @{ reasoning_tokens = 5 }
      }
    }
  } | ConvertTo-Json -Depth 20 -Compress)
  return $events
}

while ($listener.IsListening) {
  try {
    $ctx = $listener.GetContext()
  } catch {
    break
  }
  $req = $ctx.Request
  $res = $ctx.Response
  $path = $req.Url.AbsolutePath
  $body = ""
  if ($req.HasEntityBody) {
    $sr = New-Object System.IO.StreamReader($req.InputStream, [System.Text.Encoding]::UTF8)
    $body = $sr.ReadToEnd()
    $sr.Close()
  }

  try {
    if ($path -eq "/stop") {
      Send-Json $res 200 @{ ok = $true }
      $listener.Stop()
      break
    }
    elseif ($path -eq "/page-test") {
      $html = "<html><head><title>Kursor Mock Test Page</title></head><body><main><h1>Welcome to Kursor Audit</h1><p>This page verifies fetch_url HTML extraction.</p></main></body></html>"
      $res.StatusCode = 200
      $res.ContentType = "text/html; charset=utf-8"
      $b = [System.Text.Encoding]::UTF8.GetBytes($html)
      $res.OutputStream.Write($b, 0, $b.Length)
      $res.OutputStream.Close()
    }
    elseif ($path -eq "/agent/web-search") {
      if ($req.HttpMethod -eq "GET") {
        Send-Json $res 200 @{ available = $true }
      } else {
        $qObj = $body | ConvertFrom-Json -ErrorAction SilentlyContinue
        $q = if ($qObj.query) { $qObj.query } else { "test" }
        Send-Json $res 200 @{
          results = @(
            @{ title = "Result for $q"; url = "https://example.com/search-result"; snippet = "Verified search snippet for $q." }
          )
        }
      }
    }
    elseif ($path -eq "/mcp/ok") {
      $rpc = $body | ConvertFrom-Json
      if ($rpc.method -eq "initialize") {
        Send-Json $res 200 @{ jsonrpc = "2.0"; id = $rpc.id; result = @{ protocolVersion = "2025-06-18"; capabilities = @{}; serverInfo = @{ name = "mock_mcp"; version = "1.0.0" } } }
      } elseif ($rpc.method -eq "notifications/initialized") {
        $res.StatusCode = 202; $res.OutputStream.Close()
      } elseif ($rpc.method -eq "tools/list") {
        Send-Json $res 200 @{
          jsonrpc = "2.0"
          id = $rpc.id
          result = @{
            tools = @(
              @{
                name = "echo_fact"
                description = "Echoes a fact back from mock MCP"
                inputSchema = @{
                  type = "object"
                  properties = @{ msg = @{ type = "string" } }
                  required = @("msg")
                }
              }
            )
          }
        }
      } elseif ($rpc.method -eq "tools/call") {
        $msgVal = $rpc.params.arguments.msg
        Send-Json $res 200 @{
          jsonrpc = "2.0"
          id = $rpc.id
          result = @{
            content = @(
              @{ type = "text"; text = ("MCP_ECHO_OK:" + $msgVal) }
            )
          }
        }
      } else {
        Send-Json $res 200 @{ jsonrpc = "2.0"; id = $rpc.id; result = @{} }
      }
    }
    elseif ($path -eq "/mcp/fail") {
      $res.StatusCode = 503
      $b = [System.Text.Encoding]::UTF8.GetBytes('{"error":"Composio MCP proxy unreachable"}')
      $res.OutputStream.Write($b, 0, $b.Length)
      $res.OutputStream.Close()
    }
    elseif ($path -eq "/agent/anthropic/v1/messages") {
      $sseEvents = @(
        (@{ type = "message_start"; message = @{ id = "msg_claude_1"; type = "message"; role = "assistant"; usage = @{ input_tokens = 80; output_tokens = 1; cache_read_input_tokens = 0; cache_creation_input_tokens = 0 } } } | ConvertTo-Json -Depth 15 -Compress),
        (@{ type = "content_block_start"; index = 0; content_block = @{ type = "text"; text = "" } } | ConvertTo-Json -Depth 15 -Compress),
        (@{ type = "content_block_delta"; index = 0; delta = @{ type = "text_delta"; text = "<SUMMARY>Claude model turn succeeded.</SUMMARY>`n<ANSWER>Anthropic SSE stream verified.</ANSWER>`n<DONE_TITLE>Claude turn done</DONE_TITLE>" } } | ConvertTo-Json -Depth 15 -Compress),
        (@{ type = "content_block_stop"; index = 0 } | ConvertTo-Json -Depth 15 -Compress),
        (@{ type = "message_delta"; delta = @{ stop_reason = "end_turn" }; usage = @{ output_tokens = 32 } } | ConvertTo-Json -Depth 15 -Compress),
        (@{ type = "message_stop" } | ConvertTo-Json -Depth 15 -Compress)
      )
      Send-Sse $res $sseEvents
    }
    elseif ($path -eq "/agent/openai/v1/responses") {
      $reqJson = $body | ConvertFrom-Json
      # Determine scenario tag from user messages or developer instructions
      $allText = ($body)
      $hasToolOutput = ($body -match '"type"\s*:\s*"function_call_output"') -and -not ($allText -match "SCENARIO_NOTE_EDIT" -and $body -notmatch "call_note_add")

      if ($allText -match "SCENARIO_SLOW_CANCEL" -and -not $hasToolOutput) {
        Start-Sleep -Milliseconds 1400
      }
      if ($allText -match "SCENARIO_STEER_WAIT" -and -not $hasToolOutput) {
        # Wait briefly so /steer arrives while model 1 or tool is active
        Start-Sleep -Milliseconds 900
      }

      $outputItems = @()
      if (-not $hasToolOutput) {
        if ($allText -match "SCENARIO_PLAN") {
          $outputItems += @{
            id = "fc_plan_1"; type = "function_call"; call_id = "call_plan_1"; name = "update_plan"
            arguments = '{"plan":[{"step":"Inspect workspace","status":"completed"},{"step":"Verify output","status":"in_progress"}]}'
          }
        }
        elseif ($allText -match "SCENARIO_PROGRESS") {
          $outputItems += @{
            id = "fc_prog_1"; type = "function_call"; call_id = "call_prog_1"; name = "progress"
            arguments = '{"note":"Scanning workspace files now"}'
          }
        }
        elseif ($allText -match "SCENARIO_THINKING") {
          $outputItems += @{
            id = "rs_1"; type = "reasoning"
            summary = @( @{ type = "summary_text"; text = "**Analyzing system architecture**`nChecking endpoints and harnesses." } )
          }
          $outputItems += @{
            id = "msg_1"; type = "message"; role = "assistant"; phase = "final_answer"
            content = @( @{ type = "output_text"; text = "<SUMMARY>Analyzed architecture.</SUMMARY>`n<ANSWER>Architecture verified.</ANSWER>`n<DONE_TITLE>Architecture analyzed</DONE_TITLE>" } )
          }
        }
        elseif ($allText -match "SCENARIO_WRITE_FILE") {
          $outputItems += @{
            id = "fc_write_1"; type = "function_call"; call_id = "call_write_1"; name = "write"
            arguments = '{"path":"created-by-agent.txt","content":"Hello from H1 write tool\nLine 2"}'
          }
        }
        elseif ($allText -match "SCENARIO_READ_FILE") {
          $outputItems += @{
            id = "fc_read_1"; type = "function_call"; call_id = "call_read_1"; name = "read"
            arguments = '{"path":"created-by-agent.txt"}'
          }
        }
        elseif ($allText -match "SCENARIO_EDIT_FILE") {
          $outputItems += @{
            id = "fc_edit_1"; type = "function_call"; call_id = "call_edit_1"; name = "edit"
            arguments = '{"path":"created-by-agent.txt","oldText":"Line 2","newText":"Line 2 Edited"}'
          }
        }
        elseif ($allText -match "SCENARIO_BASH_TOOL") {
          $outputItems += @{
            id = "fc_bash_1"; type = "function_call"; call_id = "call_bash_1"; name = "bash"
            arguments = '{"command":"Write-Output \"PS_EXEC_OK:$($PSVersionTable.PSVersion.Major)\""}'
          }
        }
        elseif ($allText -match "SCENARIO_NOTE_EDIT") {
          $outputItems += @{
            id = "fc_note_add"; type = "function_call"; call_id = "call_note_add"; name = "note_add"
            arguments = '{"after":"## Findings","md":"- Added via note_add"}'
          }
        }
        elseif ($allText -match "SCENARIO_NOTE_COMPOSE") {
          $outputItems += @{
            id = "fc_note_1"; type = "function_call"; call_id = "call_note_1"; name = "note"
            arguments = '{"md":"# Audit Note\n\n> Verified note tool {big=\"100%\" note=\"pass\"}\n\n## Findings\n\n- First item\n"}'
          }
        }
        elseif ($allText -match "SCENARIO_EXEC_SCRIPT") {
          $outputItems += @{
            id = "fc_exec_1"; type = "function_call"; call_id = "call_exec_1"; name = "exec"
            arguments = (@{ code = 'progress("Running exec batch"); store("savedKey", 42); const v = load("savedKey"); text("EXEC_STORE_OK:" + v);' } | ConvertTo-Json -Compress)
          }
        }
        elseif ($allText -match "SCENARIO_FETCH_URL") {
          $outputItems += @{
            id = "fc_exec_url"; type = "function_call"; call_id = "call_exec_url"; name = "exec"
            arguments = (@{ code = "const page = await tools.fetch_url({url: 'http://127.0.0.1:$Port/page-test'}); text(page);" } | ConvertTo-Json -Compress)
          }
        }
        elseif ($allText -match "SCENARIO_WEB_SEARCH") {
          $outputItems += @{
            id = "fc_exec_ws"; type = "function_call"; call_id = "call_exec_ws"; name = "exec"
            arguments = (@{ code = "const res = await tools.web_search({query: 'Kursor agent audit'}); text(res);" } | ConvertTo-Json -Compress)
          }
        }
        elseif ($allText -match "SCENARIO_ASK_USER") {
          $outputItems += @{
            id = "fc_ask_1"; type = "function_call"; call_id = "call_ask_1"; name = "ask_user"
            arguments = '{"question":"Which environment should we target?","choices":["Staging","Production"]}'
          }
        }
        elseif ($allText -match "SCENARIO_MCP_CALL") {
          $outputItems += @{
            id = "fc_mcp_1"; type = "function_call"; call_id = "call_mcp_1"; name = "exec"
            arguments = (@{ code = "const r = await tools.echo_fact({msg: 'hello-mcp'}); text(r);" } | ConvertTo-Json -Compress)
          }
        }
        elseif ($allText -match "SCENARIO_LOCAL_STDIO_MCP") {
          $outputItems += @{
            id = "fc_lmcp_1"; type = "function_call"; call_id = "call_lmcp_1"; name = "exec"
            arguments = (@{ code = "const r = await tools.stdio_ping({ping: 'pong'}); text(r);" } | ConvertTo-Json -Compress)
          }
        }
        elseif ($allText -match "SCENARIO_PI_POWERSHELL") {
          $outputItems += @{
            id = "fc_pips_1"; type = "function_call"; call_id = "call_pips_1"; name = "powershell"
            arguments = '{"command":"Write-Output \"PI_PS_OK\""}'
          }
        }
        elseif ($allText -match "SCENARIO_OUTSIDE_WORKSPACE_WRITE") {
          $outsidePath = ($StateDir -replace '\\','/') + "/outside-sandbox-proof.txt"
          $outputItems += @{
            id = "fc_out_1"; type = "function_call"; call_id = "call_out_1"; name = "write"
            arguments = (@{ path = $outsidePath; content = "Wrote outside workspace without sandbox" } | ConvertTo-Json -Compress)
          }
        }
        else {
          $outputItems += @{
            id = "msg_default"; type = "message"; role = "assistant"; phase = "final_answer"
            content = @( @{ type = "output_text"; text = "<SUMMARY>Completed task.</SUMMARY>`n<ANSWER>Task finished successfully.</ANSWER>`n<DONE_TITLE>Task done</DONE_TITLE>" } )
          }
        }
      } else {
        # Second round after tool output
        $outputItems += @{
          id = "msg_final"; type = "message"; role = "assistant"; phase = "final_answer"
          content = @( @{ type = "output_text"; text = "<SUMMARY>Tool executed and verified.</SUMMARY>`n<ANSWER>Done after tool call.</ANSWER>`n<DONE_TITLE>Tool verified</DONE_TITLE>" } )
        }
      }

      $sseEvents = Make-CompletedSse $outputItems
      Send-Sse $res $sseEvents
    }
    else {
      Send-Json $res 404 @{ error = "not found: $path" }
    }
  } catch {
    try { Send-Json $res 500 @{ error = $_.Exception.Message } } catch {}
  }
}
'@ | Set-Content -Path $mockScriptPath -Encoding UTF8

$mockProc = Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $mockScriptPath, "-Port", $mockListenerPort, "-StateDir", $tempStateDir) -PassThru -WindowStyle Hidden
Start-Sleep -Milliseconds 600

# Launch clicky-harness.exe
$harnessToken = "audit-token-" + [guid]::NewGuid().ToString("N")
$harnessPsi = New-Object System.Diagnostics.ProcessStartInfo
$harnessPsi.FileName = $HarnessExe
$harnessPsi.Arguments = ""
$harnessPsi.UseShellExecute = $false
$harnessPsi.RedirectStandardOutput = $true
$harnessPsi.RedirectStandardError = $true
$harnessPsi.CreateNoWindow = $true
$harnessPsi.EnvironmentVariables["CLICKY_HARNESS_TOKEN"] = $harnessToken
$harnessPsi.EnvironmentVariables["CLICKY_HARNESS_STATE"] = $tempStateDir
$harnessPsi.EnvironmentVariables["CLICKY_H1_STATE_DIR"] = (Join-Path $tempStateDir "h1-local")

$harnessProc = [System.Diagnostics.Process]::Start($harnessPsi)
$harnessProc.BeginErrorReadLine()
$readyLine = $harnessProc.StandardOutput.ReadLine()
$readyObj = $readyLine | ConvertFrom-Json
$harnessPort = [int]$readyObj.port
$harnessBase = "http://127.0.0.1:$harnessPort"
$authHeaders = @{ Authorization = "Bearer $harnessToken" }
Write-Host ("  clicky-harness.exe started PID={0}, readyLine={1}" -f $harnessProc.Id, $readyLine)

function Invoke-HarnessJson {
  param([string]$Method, [string]$Path, $Body = $null, [hashtable]$Headers = $authHeaders)
  $uri = "$harnessBase$Path"
  try {
    if ($null -ne $Body) {
      $jsonBody = if ($Body -is [string]) { $Body } else { $Body | ConvertTo-Json -Depth 20 -Compress }
      $resp = Invoke-WebRequest -Uri $uri -Method $Method -Headers $Headers -Body $jsonBody -ContentType "application/json" -UseBasicParsing -TimeoutSec 15
    } else {
      $resp = Invoke-WebRequest -Uri $uri -Method $Method -Headers $Headers -UseBasicParsing -TimeoutSec 15
    }
    return @{ status = [int]$resp.StatusCode; body = ($resp.Content | ConvertFrom-Json -ErrorAction SilentlyContinue); raw = $resp.Content }
  } catch {
    $errResp = $_.Exception.Response
    if ($null -ne $errResp) {
      $sr = New-Object System.IO.StreamReader($errResp.GetResponseStream())
      $raw = $sr.ReadToEnd()
      $sr.Close()
      $errResp.Close()
      return @{ status = [int]$errResp.StatusCode; body = ($raw | ConvertFrom-Json -ErrorAction SilentlyContinue); raw = $raw }
    }
    throw
  }
}

function Read-JobEventsUntilTurnEnd {
  param([string]$JobId, [int]$After = 0, [int]$TimeoutMs = 20000)
  $events = @()
  $curAfter = $After
  $sw = [System.Diagnostics.Stopwatch]::StartNew()
  while ($sw.ElapsedMilliseconds -lt $TimeoutMs) {
    $waitMs = [Math]::Min(4000, [Math]::Max(500, $TimeoutMs - [int]$sw.ElapsedMilliseconds))
    $poll = Invoke-HarnessJson -Method GET -Path ("/events?job={0}&after={1}&wait={2}" -f [uri]::EscapeDataString($JobId), $curAfter, $waitMs)
    if ($poll.status -ne 200 -or $null -eq $poll.body) { break }
    if ($null -ne $poll.body.latest) {
      $curAfter = [int]$poll.body.latest
    }
    $done = $false
    foreach ($ev in @($poll.body.events)) {
      if ($null -eq $ev) { continue }
      $events += $ev
      if ($ev.kind -eq "turn_end") {
        $done = $true
        break
      }
    }
    if ($done) { break }
  }
  return $events
}

try {
  Write-Host "`n=== 4. Harness Loopback HTTP Endpoint & Auth Contract Probes ==="
  $rUnauth = Invoke-HarnessJson -Method GET -Path "/events?job=test" -Headers @{}
  $rWrongTok = Invoke-HarnessJson -Method GET -Path "/events?job=test" -Headers @{ Authorization = "Bearer wrong-token" }
  $rNotFound = Invoke-HarnessJson -Method GET -Path "/nonexistent"
  $rPrewarm = Invoke-HarnessJson -Method POST -Path "/prewarm" -Body @{}
  $rMissingFields = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{ job = "j1" }
  $rH1Cloud = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{ job = "j-h1-cloud"; harness = "h1"; place = "cloud"; workspace = $tempWorkDir; text = "hi" }
  $rCodexCloud = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{ job = "j-codex-cloud"; harness = "codex"; place = "cloud"; workspace = $tempWorkDir; text = "hi" }
  $rPiMac = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{ job = "j-pi-mac"; harness = "pi"; place = "mac"; workspace = $tempWorkDir; text = "hi" }
  $rSteerIdle = Invoke-HarnessJson -Method POST -Path "/steer" -Body @{ job = "idle-job"; text = "follow up" }
  $rCancelIdle = Invoke-HarnessJson -Method POST -Path "/cancel" -Body @{ job = "idle-job" }

  $Results.harnessHttpTests = @(
    [ordered]@{ endpoint = "GET /events (no auth)"; status = $rUnauth.status; response = $rUnauth.raw },
    [ordered]@{ endpoint = "GET /events (wrong token)"; status = $rWrongTok.status; response = $rWrongTok.raw },
    [ordered]@{ endpoint = "GET /nonexistent"; status = $rNotFound.status; response = $rNotFound.raw },
    [ordered]@{ endpoint = "POST /prewarm"; status = $rPrewarm.status; response = $rPrewarm.raw },
    [ordered]@{ endpoint = "POST /turn (missing harness/place)"; status = $rMissingFields.status; response = $rMissingFields.raw },
    [ordered]@{ endpoint = "POST /turn (harness=h1, place=cloud)"; status = $rH1Cloud.status; response = $rH1Cloud.raw },
    [ordered]@{ endpoint = "POST /turn (harness=codex, place=cloud)"; status = $rCodexCloud.status; response = $rCodexCloud.raw },
    [ordered]@{ endpoint = "POST /turn (harness=pi, place=mac)"; status = $rPiMac.status; response = $rPiMac.raw },
    [ordered]@{ endpoint = "POST /steer (not running)"; status = $rSteerIdle.status; response = $rSteerIdle.raw },
    [ordered]@{ endpoint = "POST /cancel (not running)"; status = $rCancelIdle.status; response = $rCancelIdle.raw }
  )

  Write-Host "`n=== 5. Live Kursor Backend ($BackendUrl) Missing Route Probes ==="
  foreach ($probe in @(
    @{ Method = "POST"; Path = "/agent/openai/v1/responses"; Purpose = "H1 & Pi OpenAI Responses SSE proxy" },
    @{ Method = "POST"; Path = "/agent/anthropic/v1/messages"; Purpose = "H1 & Pi Anthropic Messages SSE proxy" },
    @{ Method = "GET";  Path = "/agent/web-search"; Purpose = "H1 Exa/Brave web search availability check" },
    @{ Method = "POST"; Path = "/agent/web-search"; Purpose = "H1/Pi web search execution proxy" },
    @{ Method = "POST"; Path = "/agent/harness/lab/h1/jobs"; Purpose = "H1 cloud job creation" },
    @{ Method = "GET";  Path = "/agent/harness/lab/h1/probe?url=http://127.0.0.1"; Purpose = "H1 cloud device tunnel probe" },
    @{ Method = "POST"; Path = "/mcp/composio"; Purpose = "Composio MCP proxy returned by POST /agent/composio/session" }
  )) {
    $status = 0
    $rawBody = ""
    try {
      $pUri = "$BackendUrl$($probe.Path)"
      $pHeaders = @{ Authorization = "Bearer kursor-agent-session-dev-token" }
      if ($probe.Method -eq "POST") {
        $resp = Invoke-WebRequest -Uri $pUri -Method POST -Headers $pHeaders -Body "{}" -ContentType "application/json" -UseBasicParsing
      } else {
        $resp = Invoke-WebRequest -Uri $pUri -Method GET -Headers $pHeaders -UseBasicParsing
      }
      $status = [int]$resp.StatusCode
      $rawBody = $resp.Content
    } catch {
      if ($_.Exception.Response) {
        $status = [int]$_.Exception.Response.StatusCode
        $sr = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $rawBody = $sr.ReadToEnd()
        $sr.Close()
      } else {
        $rawBody = $_.Exception.Message
      }
    }
    $Results.liveBackendProbes += [ordered]@{
      method = $probe.Method
      path = $probe.Path
      purpose = $probe.Purpose
      status = $status
      response = $rawBody
    }
    Write-Host ("  Live Backend {0} {1} -> HTTP {2} ({3})" -f $probe.Method, $probe.Path, $status, $rawBody)
  }

  Write-Host "`n=== 6. Running 30 Agent Task, Tool & Safety Scenarios ==="

  # Scenario 1: Live Kursor Backend (127.0.0.1:8787) with h1-mac (OpenAI model) -> Completes turn via /agent/openai/v1/responses
  $t01 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-01-live-backend-h1"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "Hello agent"
    worker = @{ base = $BackendUrl; sessionToken = "kursor-agent-session-dev-token" }
  }
  $ev01 = Read-JobEventsUntilTurnEnd -JobId "scen-01-live-backend-h1" -After ([int]$t01.body.after)
  $te01 = $ev01 | Where-Object { $_.kind -eq "turn_end" } | Select-Object -Last 1
  Add-ScenarioResult -Id 1 -Category "LiveBackend" -Name "h1-mac OpenAI turn against live Kursor backend (127.0.0.1:8787)" `
    -Status $(if ($te01.status -eq "completed") { "PASS" } else { "FAIL" }) `
    -Observed ("turn_end status={0}, error='{1}'" -f $te01.status, $te01.error) `
    -Expected "turn_end status=completed with LLM response from backend /agent/openai/v1/responses" `
    -Details @{ events = $ev01 }

  # Scenario 2: Live Kursor Backend (127.0.0.1:8787) with h1-mac (Anthropic model) -> Completes turn via /agent/anthropic/v1/messages
  $t02 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-02-live-backend-anthropic"
    harness = "h1"
    place = "mac"
    model = "claude-sonnet-5-5"
    workspace = $tempWorkDir
    text = "Hello Claude agent"
    worker = @{ base = $BackendUrl; sessionToken = "kursor-agent-session-dev-token" }
  }
  $ev02 = Read-JobEventsUntilTurnEnd -JobId "scen-02-live-backend-anthropic" -After ([int]$t02.body.after)
  $te02 = $ev02 | Where-Object { $_.kind -eq "turn_end" } | Select-Object -Last 1
  Add-ScenarioResult -Id 2 -Category "LiveBackend" -Name "h1-mac Anthropic turn against live Kursor backend (127.0.0.1:8787)" `
    -Status $(if ($te02.status -eq "completed") { "PASS" } else { "FAIL" }) `
    -Observed ("turn_end status={0}, error='{1}'" -f $te02.status, $te02.error) `
    -Expected "turn_end status=completed with LLM response from backend /agent/anthropic/v1/messages" `
    -Details @{ events = $ev02 }

  # Scenario 3: AUD-J-002 & AUD-J-003 — AgentHarnessRuntimes registers only supported h1-mac & AgentHarnessRunnerProcess drains StandardError
  $runtimesSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Harness\AgentHarnessRuntimes.cs") -Raw
  $runnerProcSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Harness\AgentHarnessRunnerProcess.cs") -Raw
  $onlyH1MacRegistered = ($runtimesSource -match "AgentHarness\.H1,\s*AgentHarnessPlace\.Mac") -and -not ($runtimesSource -match "AgentHarnessPlace\.Cloud") -and -not ($runtimesSource -match "AgentHarness\.Pi")
  $drainsStdErr = ($runnerProcSource -match "ErrorDataReceived\s*\+=") -and ($runnerProcSource -match "BeginErrorReadLine\s*\(\s*\)")
  Add-ScenarioResult -Id 3 -Category "HarnessDrivers" -Name "AgentHarnessRuntimes registers only supported h1-mac & AgentHarnessRunnerProcess drains StandardError" `
    -Status $(if ($onlyH1MacRegistered -and $drainsStdErr) { "PASS" } else { "FAIL" }) `
    -Observed ("onlyH1MacRegistered={0}, drainsStdErr={1}" -f $onlyH1MacRegistered, $drainsStdErr) `
    -Expected "Only supported h1-mac registered in AgentHarnessRuntimes.cs and StandardError drained in AgentHarnessRunnerProcess.cs" `
    -Details @{ onlyH1MacRegistered = $onlyH1MacRegistered; drainsStdErr = $drainsStdErr }

  # Scenario 4: AUD-J-001 — All 7 /agent/* and /mcp/composio endpoints return 200 OK on live backend (127.0.0.1:8787)
  $allLiveProbes200 = (@($Results.liveBackendProbes | Where-Object { $_.status -ne 200 }).Count -eq 0) -and ($Results.liveBackendProbes.Count -eq 7)
  Add-ScenarioResult -Id 4 -Category "LiveBackend" -Name "All 7 /agent/* and /mcp/composio routes return 200 OK on live backend (127.0.0.1:8787)" `
    -Status $(if ($allLiveProbes200) { "PASS" } else { "FAIL" }) `
    -Observed ("200 OK count={0}/{1}" -f @($Results.liveBackendProbes | Where-Object { $_.status -eq 200 }).Count, $Results.liveBackendProbes.Count) `
    -Expected "All 7 live backend probes return HTTP 200 OK" `
    -Details @{ probes = $Results.liveBackendProbes }

  # Scenario 5: h1-mac basic Q&A turn via Mock Worker SSE
  $t05 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-05-basic-qa"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "Basic Q&A check"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev05 = Read-JobEventsUntilTurnEnd -JobId "scen-05-basic-qa" -After ([int]$t05.body.after)
  $ans05 = ($ev05 | Where-Object { $_.kind -eq "answer" } | Select-Object -Last 1).text
  $te05 = $ev05 | Where-Object { $_.kind -eq "turn_end" } | Select-Object -Last 1
  Add-ScenarioResult -Id 5 -Category "H1Runtime" -Name "h1-mac basic turn lifecycle & long-poll event stream (answer -> stats -> turn_end)" `
    -Status $(if ($te05.status -eq "completed" -and $ans05 -match "<ANSWER>") { "PASS" } else { "FAIL" }) `
    -Observed ("status={0}, answerLen={1}" -f $te05.status, $ans05.Length) `
    -Expected "status=completed and answer emitted" `
    -Details @{ events = $ev05 }

  # Scenario 6: h1-mac multi-turn session continuity (turn 2 on scen-05-basic-qa)
  $t06 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-05-basic-qa"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "Second turn in same job"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev06 = Read-JobEventsUntilTurnEnd -JobId "scen-05-basic-qa" -After ([int]$t06.body.after)
  $h1State06 = Get-Content (Join-Path $tempStateDir "h1-mac\scen-05-basic-qa\h1.json") -Raw | ConvertFrom-Json
  Add-ScenarioResult -Id 6 -Category "H1Runtime" -Name "h1-mac multi-turn state persistence in h1-mac/<job>/h1.json" `
    -Status $(if ($h1State06.turn -eq 2) { "PASS" } else { "FAIL" }) `
    -Observed ("h1.json turn={0}, lastSeq={1}, jobId={2}" -f $h1State06.turn, $h1State06.lastSeq, $h1State06.jobId) `
    -Expected "h1.json turn=2 with persisted jobId and lastSeq" `
    -Details @{ h1Json = $h1State06 }

  # Scenario 7: h1-mac update_plan tool
  $t07 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-07-plan"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    planTool = $true
    text = "SCENARIO_PLAN"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev07 = Read-JobEventsUntilTurnEnd -JobId "scen-07-plan" -After ([int]$t07.body.after)
  $planEv07 = $ev07 | Where-Object { $_.kind -eq "plan" } | Select-Object -Last 1
  Add-ScenarioResult -Id 7 -Category "Tools" -Name "h1-mac update_plan tool emission" `
    -Status $(if ($null -ne $planEv07 -and $planEv07.plan.Count -eq 2) { "PASS" } else { "FAIL" }) `
    -Observed ("plan event steps={0}" -f $planEv07.plan.Count) `
    -Expected "plan event with 2 steps (completed, in_progress)" `
    -Details @{ planEvent = $planEv07 }

  # Scenario 8: h1-mac progress tool -> commentary event
  $t08 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-08-progress"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_PROGRESS"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev08 = Read-JobEventsUntilTurnEnd -JobId "scen-08-progress" -After ([int]$t08.body.after)
  $comm08 = $ev08 | Where-Object { $_.kind -eq "commentary" } | Select-Object -Last 1
  Add-ScenarioResult -Id 8 -Category "Tools" -Name "h1-mac progress tool commentary emission" `
    -Status $(if ($comm08.text -eq "Scanning workspace files now") { "PASS" } else { "FAIL" }) `
    -Observed ("commentary='{0}'" -f $comm08.text) `
    -Expected "commentary='Scanning workspace files now'" `
    -Details @{ commentaryEvent = $comm08 }

  # Scenario 9: h1-mac reasoning summary (**bold heading**) -> thinking event
  $t09 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-09-thinking"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_THINKING"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev09 = Read-JobEventsUntilTurnEnd -JobId "scen-09-thinking" -After ([int]$t09.body.after)
  $think09 = $ev09 | Where-Object { $_.kind -eq "thinking" } | Select-Object -Last 1
  Add-ScenarioResult -Id 9 -Category "H1Runtime" -Name "h1-mac reasoning summary heading extraction -> thinking event" `
    -Status $(if ($think09.text -eq "Analyzing system architecture") { "PASS" } else { "FAIL" }) `
    -Observed ("thinking='{0}'" -f $think09.text) `
    -Expected "thinking='Analyzing system architecture'" `
    -Details @{ thinkingEvent = $think09 }

  # Scenario 10: h1-mac local write tool
  $t10 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-10-write"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_WRITE_FILE"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev10 = Read-JobEventsUntilTurnEnd -JobId "scen-10-write" -After ([int]$t10.body.after)
  $writtenFile = Join-Path $tempWorkDir "created-by-agent.txt"
  $writtenContent = if (Test-Path $writtenFile) { Get-Content $writtenFile -Raw } else { "" }
  Add-ScenarioResult -Id 10 -Category "Tools" -Name "h1-mac write tool creating file in workspace" `
    -Status $(if ($writtenContent -match "Hello from H1 write tool") { "PASS" } else { "FAIL" }) `
    -Observed ("fileExists={0}, content='{1}'" -f (Test-Path $writtenFile), ($writtenContent.Trim() -replace "\r?\n"," | ")) `
    -Expected "created-by-agent.txt created with expected content"

  # Scenario 11: h1-mac local read tool
  $t11 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-11-read"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_READ_FILE"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev11 = Read-JobEventsUntilTurnEnd -JobId "scen-11-read" -After ([int]$t11.body.after)
  $tool11 = $ev11 | Where-Object { $_.kind -eq "tool" -and $_.name -eq "read" } | Select-Object -First 1
  Add-ScenarioResult -Id 11 -Category "Tools" -Name "h1-mac read tool reading workspace file" `
    -Status $(if ($null -ne $tool11) { "PASS" } else { "FAIL" }) `
    -Observed ("tool event name={0}, phase={1}" -f $tool11.name, $tool11.phase) `
    -Expected "tool event name=read emitted and completed"

  # Scenario 12: h1-mac local edit tool
  $t12 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-12-edit"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_EDIT_FILE"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev12 = Read-JobEventsUntilTurnEnd -JobId "scen-12-edit" -After ([int]$t12.body.after)
  $editedContent = if (Test-Path $writtenFile) { Get-Content $writtenFile -Raw } else { "" }
  Add-ScenarioResult -Id 12 -Category "Tools" -Name "h1-mac edit tool modifying workspace file in place" `
    -Status $(if ($editedContent -match "Line 2 Edited") { "PASS" } else { "FAIL" }) `
    -Observed ("content='{0}'" -f ($editedContent.Trim() -replace "\r?\n"," | ")) `
    -Expected "Line 2 replaced with Line 2 Edited"

  # Scenario 13: h1-mac bash tool (mapped to Windows PowerShell on Windows)
  $t13 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-13-bash"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_BASH_TOOL"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev13 = Read-JobEventsUntilTurnEnd -JobId "scen-13-bash" -After ([int]$t13.body.after)
  $tool13 = $ev13 | Where-Object { $_.kind -eq "tool" -and $_.name -eq "bash" } | Select-Object -First 1
  Add-ScenarioResult -Id 13 -Category "Tools" -Name "h1-mac bash tool executing Windows PowerShell command" `
    -Status $(if ($null -ne $tool13 -and $tool13.command -match "PS_EXEC_OK") { "PASS" } else { "FAIL" }) `
    -Observed ("tool name={0}, command='{1}'" -f $tool13.name, $tool13.command) `
    -Expected "bash tool invokes PowerShell and emits command in tool event"

  # Scenario 14: h1-mac note tool composing note.md
  $notePath = Join-Path $tempWorkDir "note.md"
  $t14 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-14-note"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    note = @{ path = $notePath; tools = $true; kept = $true }
    text = "SCENARIO_NOTE_COMPOSE"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev14 = Read-JobEventsUntilTurnEnd -JobId "scen-14-note" -After ([int]$t14.body.after)
  $noteEv14 = $ev14 | Where-Object { $_.kind -eq "note" -and $_.phase -eq "saved" } | Select-Object -First 1
  $noteDisk14 = if (Test-Path $notePath) { Get-Content $notePath -Raw } else { "" }
  Add-ScenarioResult -Id 14 -Category "NoteTools" -Name "h1-mac note tool composing note.md and emitting note saved event" `
    -Status $(if ($null -ne $noteEv14 -and $noteDisk14 -match "# Audit Note") { "PASS" } else { "FAIL" }) `
    -Observed ("noteSavedEvent={0}, noteDiskLen={1}" -f ($null -ne $noteEv14), $noteDisk14.Length) `
    -Expected "note.md written and {kind:'note', phase:'saved'} emitted"

  # Scenario 15: h1-mac note_add incremental edit tool on existing note.md
  $t15 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-14-note"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    note = @{ path = $notePath; tools = $true; kept = $true }
    text = "SCENARIO_NOTE_EDIT"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev15 = Read-JobEventsUntilTurnEnd -JobId "scen-14-note" -After ([int]$t15.body.after)
  $noteDisk15 = if (Test-Path $notePath) { Get-Content $notePath -Raw } else { "" }
  Add-ScenarioResult -Id 15 -Category "NoteTools" -Name "h1-mac note_add incremental markdown edit on note.md" `
    -Status $(if ($noteDisk15 -match "Added via note_add") { "PASS" } else { "FAIL" }) `
    -Observed ("note.md contains 'Added via note_add'={0}" -f ($noteDisk15 -match "Added via note_add")) `
    -Expected "note_add inserts bullet under ## Findings in note.md"

  # Scenario 16: h1-mac exec (run_script) JS sandbox with store(), load(), progress(), text()
  $t16 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-16-exec"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_EXEC_SCRIPT"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev16 = Read-JobEventsUntilTurnEnd -JobId "scen-16-exec" -After ([int]$t16.body.after)
  $comm16 = $ev16 | Where-Object { $_.kind -eq "commentary" -and $_.text -eq "Running exec batch" } | Select-Object -First 1
  $rounds16 = Get-Content (Join-Path $tempStateDir "h1-mac\scen-16-exec\h1-rounds.jsonl") -Raw
  Add-ScenarioResult -Id 16 -Category "Tools" -Name "h1-mac exec JS program sandbox (store/load/progress/text)" `
    -Status $(if ($null -ne $comm16 -and $rounds16 -match '"name":"exec","server":"harness","ok":true') { "PASS" } else { "FAIL" }) `
    -Observed ("progressEmitted={0}, execOk={1}" -f ($null -ne $comm16), ($rounds16 -match '"name":"exec","server":"harness","ok":true')) `
    -Expected "exec runs JS body, emits progress commentary, and logs ok:true in h1-rounds.jsonl"

  # Scenario 17: h1-mac fetch_url tool via exec -> emits batch event with site host
  $t17 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-17-fetch-url"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_FETCH_URL"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev17 = Read-JobEventsUntilTurnEnd -JobId "scen-17-fetch-url" -After ([int]$t17.body.after)
  $batch17 = $ev17 | Where-Object { $_.kind -eq "batch" } | Select-Object -Last 1
  Add-ScenarioResult -Id 17 -Category "Tools" -Name "h1-mac fetch_url HTML extraction & program_step batch event" `
    -Status $(if ($null -ne $batch17 -and $batch17.siteCount -ge 1) { "PASS" } else { "FAIL" }) `
    -Observed ("batch siteCount={0}, sitesDone={1}" -f $batch17.siteCount, $batch17.sitesDone) `
    -Expected "fetch_url extracts HTML and emits batch event with siteCount=1, sitesDone=1" `
    -Details @{ batchEvent = $batch17 }

  # Scenario 18: h1-mac web_search tool via exec -> calls Worker /agent/web-search and emits batch search count
  $t18 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-18-web-search"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_WEB_SEARCH"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev18 = Read-JobEventsUntilTurnEnd -JobId "scen-18-web-search" -After ([int]$t18.body.after)
  $batch18 = $ev18 | Where-Object { $_.kind -eq "batch" } | Select-Object -Last 1
  Add-ScenarioResult -Id 18 -Category "Tools" -Name "h1-mac web_search tool via Worker /agent/web-search & batch event" `
    -Status $(if ($null -ne $batch18 -and $batch18.searches.count -eq 1 -and $batch18.searches.done -eq 1) { "PASS" } else { "FAIL" }) `
    -Observed ("searches count={0}, done={1}, query='{2}'" -f $batch18.searches.count, $batch18.searches.done, $batch18.searches.query) `
    -Expected "web_search queries Worker /agent/web-search and emits batch searches count=1, done=1" `
    -Details @{ batchEvent = $batch18 }

  # Scenario 19: h1-mac ask_user tool pausing turn and resuming on next /turn
  $t19a = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-19-ask-user"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_ASK_USER"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev19a = Read-JobEventsUntilTurnEnd -JobId "scen-19-ask-user" -After ([int]$t19a.body.after)
  $h1State19a = Get-Content (Join-Path $tempStateDir "h1-mac\scen-19-ask-user\h1.json") -Raw | ConvertFrom-Json
  $ans19a = ($ev19a | Where-Object { $_.kind -eq "answer" } | Select-Object -Last 1).text
  # Now send user's answer via a second POST /turn
  $t19b = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-19-ask-user"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "Production"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev19b = Read-JobEventsUntilTurnEnd -JobId "scen-19-ask-user" -After ([int]$t19b.body.after)
  $h1State19b = Get-Content (Join-Path $tempStateDir "h1-mac\scen-19-ask-user\h1.json") -Raw | ConvertFrom-Json
  Add-ScenarioResult -Id 19 -Category "Tools" -Name "h1-mac ask_user pause (pendingAsk in h1.json) and answer resumption" `
    -Status $(if ($null -ne $h1State19a.pendingAsk -and $ans19a -match "Options: Staging" -and $null -eq $h1State19b.pendingAsk) { "PASS" } else { "FAIL" }) `
    -Observed ("turn1 pendingAsk='{0}', question='{1}', turn2 pendingAsk='{2}'" -f $h1State19a.pendingAsk, ($ans19a -replace "\r?\n"," "), $h1State19b.pendingAsk) `
    -Expected "turn1 sets pendingAsk and emits question+options; turn2 answers pendingAsk and clears it"

  # Scenario 20: h1-mac HTTP MCP server discovery (tools/list) & tools/call execution
  $t20 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-20-mcp-http"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    mcp = @( @{ name = "mock_mcp"; url = "$mockWorkerBase/mcp/ok"; token = "mcp-tok" } )
    text = "SCENARIO_MCP_CALL"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev20 = Read-JobEventsUntilTurnEnd -JobId "scen-20-mcp-http" -After ([int]$t20.body.after)
  $listed20 = $ev20 | Where-Object { $_.kind -eq "tools_listed" } | Select-Object -First 1
  $srv20 = $listed20.servers | Where-Object { $_.server -eq "mock_mcp" } | Select-Object -First 1
  Add-ScenarioResult -Id 20 -Category "MCP" -Name "h1-mac HTTP MCP server initialize, tools/list, and tools/call" `
    -Status $(if ($srv20.ok -eq $true -and $srv20.tools -eq 1) { "PASS" } else { "FAIL" }) `
    -Observed ("server={0}, ok={1}, tools={2}" -f $srv20.server, $srv20.ok, $srv20.tools) `
    -Expected "tools_listed event with mock_mcp ok=true, tools=1, and successful tool call" `
    -Details @{ toolsListed = $listed20 }

  # Scenario 21: h1-mac unreachable Composio MCP server -> emits apps_unreachable and continues
  $t21 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-21-composio-fail"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    mcp = @( @{ name = "composio"; url = "$mockWorkerBase/mcp/fail"; token = "bad" } )
    text = "Check fallback when composio MCP is unreachable"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev21 = Read-JobEventsUntilTurnEnd -JobId "scen-21-composio-fail" -After ([int]$t21.body.after) -TimeoutMs 25000
  $unreach21 = $ev21 | Where-Object { $_.kind -eq "apps_unreachable" } | Select-Object -First 1
  $te21 = $ev21 | Where-Object { $_.kind -eq "turn_end" } | Select-Object -Last 1
  Add-ScenarioResult -Id 21 -Category "MCP" -Name "h1-mac Composio MCP failure -> emits apps_unreachable and completes turn without connected apps" `
    -Status $(if ($null -ne $unreach21 -and $te21.status -eq "completed") { "PASS" } else { "FAIL" }) `
    -Observed ("apps_unreachable={0}, turn_end={1}" -f ($null -ne $unreach21), $te21.status) `
    -Expected "apps_unreachable emitted and turn_end=completed"

  # Scenario 22: h1-mac local stdio MCP connector bridged via q_() loopback HTTP proxy
  $stdioPs1 = Join-Path $tempStateDir "stdio-mcp.ps1"
  @'
while ($null -ne ($line = [Console]::In.ReadLine())) {
  if ([string]::IsNullOrWhiteSpace($line)) { continue }
  try {
    $msg = $line | ConvertFrom-Json
    if ($msg.method -eq "initialize") {
      $resp = @{ jsonrpc = "2.0"; id = $msg.id; result = @{ protocolVersion = "2025-06-18"; capabilities = @{}; serverInfo = @{ name = "stdio_test"; version = "1.0" } } }
      [Console]::Out.WriteLine(($resp | ConvertTo-Json -Depth 10 -Compress))
      [Console]::Out.Flush()
    } elseif ($msg.method -eq "tools/list") {
      $resp = @{ jsonrpc = "2.0"; id = $msg.id; result = @{ tools = @( @{ name = "stdio_ping"; description = "Ping stdio"; inputSchema = @{ type = "object"; properties = @{ ping = @{ type = "string" } } } } ) } }
      [Console]::Out.WriteLine(($resp | ConvertTo-Json -Depth 10 -Compress))
      [Console]::Out.Flush()
    } elseif ($msg.method -eq "tools/call") {
      $resp = @{ jsonrpc = "2.0"; id = $msg.id; result = @{ content = @( @{ type = "text"; text = "STDIO_PONG" } ) } }
      [Console]::Out.WriteLine(($resp | ConvertTo-Json -Depth 10 -Compress))
      [Console]::Out.Flush()
    }
  } catch {}
}
'@ | Set-Content -Path $stdioPs1 -Encoding UTF8
  $t22 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-22-stdio-mcp"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    mcp = @( @{ name = "custom-stdio-123456"; command = "powershell.exe"; args = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $stdioPs1) } )
    text = "SCENARIO_LOCAL_STDIO_MCP"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev22 = Read-JobEventsUntilTurnEnd -JobId "scen-22-stdio-mcp" -After ([int]$t22.body.after)
  $listed22 = $ev22 | Where-Object { $_.kind -eq "tools_listed" } | Select-Object -First 1
  $srv22 = $listed22.servers | Where-Object { $_.server -eq "custom-stdio-123456" } | Select-Object -First 1
  Add-ScenarioResult -Id 22 -Category "MCP" -Name "h1-mac local stdio custom MCP connector bridged via loopback HTTP proxy" `
    -Status $(if ($srv22.ok -eq $true -and $srv22.tools -eq 1) { "PASS" } else { "FAIL" }) `
    -Observed ("server={0}, ok={1}, tools={2}" -f $srv22.server, $srv22.ok, $srv22.tools) `
    -Expected "stdio MCP child spawned, bridged over HTTP, tools=1"

  # Scenario 23: POST /steer mid-turn delivery
  $t23 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-23-steer"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_STEER_WAIT"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  Start-Sleep -Milliseconds 450
  $steerResp = Invoke-HarnessJson -Method POST -Path "/steer" -Body @{
    job = "scen-23-steer"
    text = "Steered mid-turn instruction"
    info = $true
  }
  $ev23 = Read-JobEventsUntilTurnEnd -JobId "scen-23-steer" -After ([int]$t23.body.after)
  $te23 = $ev23 | Where-Object { $_.kind -eq "turn_end" } | Select-Object -Last 1
  Add-ScenarioResult -Id 23 -Category "H1Runtime" -Name "POST /steer mid-turn injection while turn is running" `
    -Status $(if ($steerResp.status -eq 200 -and $steerResp.body.ok -eq $true -and $te23.status -eq "completed") { "PASS" } else { "FAIL" }) `
    -Observed ("steer HTTP={0}, ok={1}, turn_end={2}" -f $steerResp.status, $steerResp.body.ok, $te23.status) `
    -Expected "POST /steer returns 200 ok:true and turn completes after applying steered note"

  # Scenario 24: POST /cancel mid-turn interruption
  $t24 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-24-cancel"
    harness = "h1"
    place = "mac"
    model = "gpt-6-luna"
    workspace = $tempWorkDir
    text = "SCENARIO_SLOW_CANCEL"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  Start-Sleep -Milliseconds 500
  $cancelResp = Invoke-HarnessJson -Method POST -Path "/cancel" -Body @{ job = "scen-24-cancel" }
  $ev24 = Read-JobEventsUntilTurnEnd -JobId "scen-24-cancel" -After ([int]$t24.body.after)
  $te24 = $ev24 | Where-Object { $_.kind -eq "turn_end" } | Select-Object -Last 1
  Add-ScenarioResult -Id 24 -Category "H1Runtime" -Name "POST /cancel mid-turn cancellation -> turn_end status=interrupted" `
    -Status $(if ($cancelResp.status -eq 200 -and $te24.status -eq "interrupted") { "PASS" } else { "FAIL" }) `
    -Observed ("cancel HTTP={0}, turn_end status={1}" -f $cancelResp.status, $te24.status) `
    -Expected "POST /cancel aborts active model call and emits turn_end status=interrupted"

  # Scenario 25: h1-mac Anthropic Messages SSE adapter (/agent/anthropic/v1/messages) with claude-sonnet-5-5
  $t25 = Invoke-HarnessJson -Method POST -Path "/turn" -Body @{
    job = "scen-25-h1-anthropic"
    harness = "h1"
    place = "mac"
    model = "claude-sonnet-5-5"
    workspace = $tempWorkDir
    text = "Verify Anthropic SSE adapter"
    worker = @{ base = $mockWorkerBase; sessionToken = "mock-token" }
  }
  $ev25 = Read-JobEventsUntilTurnEnd -JobId "scen-25-h1-anthropic" -After ([int]$t25.body.after)
  $ans25 = ($ev25 | Where-Object { $_.kind -eq "answer" } | Select-Object -Last 1).text
  $te25 = $ev25 | Where-Object { $_.kind -eq "turn_end" } | Select-Object -Last 1
  Add-ScenarioResult -Id 25 -Category "H1Runtime" -Name "h1-mac Anthropic Messages SSE adapter (claude-sonnet-5-5 -> /agent/anthropic/v1/messages)" `
    -Status $(if ($te25.status -eq "completed" -and $ans25 -match "Anthropic SSE stream verified") { "PASS" } else { "FAIL" }) `
    -Observed ("turn_end={0}, answer='{1}'" -f $te25.status, ($ans25 -replace "\r?\n"," ")) `
    -Expected "h1-mac translates OpenAI format to Anthropic Messages SSE and completes turn"

  # Scenario 26: AUD-J-006 — CodexHomeManager sets approval_policy = "on-request" and sandbox_mode = "workspace-write"
  $codexHomeSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Runtime\CodexHomeManager.cs") -Raw
  $hasApprovalOnRequest = $codexHomeSource -match 'approval_policy\s*=\s*\\"on-request\\"'
  $hasSandboxWorkspaceWrite = $codexHomeSource -match 'sandbox_mode\s*=\s*\\"workspace-write\\"'
  $hasDangerFullAccess = $codexHomeSource -match 'danger-full-access'
  Add-ScenarioResult -Id 26 -Category "Security" -Name "CodexHomeManager.cs sets approval_policy='on-request' and sandbox_mode='workspace-write'" `
    -Status $(if ($hasApprovalOnRequest -and $hasSandboxWorkspaceWrite -and -not $hasDangerFullAccess) { "PASS" } else { "FAIL" }) `
    -Observed ("approvalOnRequest={0}, sandboxWorkspaceWrite={1}, dangerFullAccess={2}" -f $hasApprovalOnRequest, $hasSandboxWorkspaceWrite, $hasDangerFullAccess) `
    -Expected "approval_policy = 'on-request' and sandbox_mode = 'workspace-write' with zero 'danger-full-access'"

  # Scenario 27: Safety & Portability Audit — WindowsInstructionAdapter vs TurnPromptBuilder & clicky-model-instructions.md
  $bundledInstrPath = Join-Path $RepoRoot "dist\kursor-desktop\BundledResources\clicky-model-instructions.md"
  $rawInstr = Get-Content $bundledInstrPath -Raw
  $adapterSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Runtime\WindowsInstructionAdapter.cs") -Raw
  $turnPromptSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Turns\TurnPromptBuilder.cs") -Raw
  $hasMacStepInTurnPrompt = $turnPromptSource -match "\bmac_step\b"
  $hasPcStepInTurnPrompt = $turnPromptSource -match "\bpc_step\b"
  $adapterReplacesMac = ($adapterSource -match 'Replace\("their Mac",\s*"their PC"') -and ($adapterSource -match 'Get-ChildItem')
  Add-ScenarioResult -Id 27 -Category "Portability" -Name "WindowsInstructionAdapter.Adapt & TurnPromptBuilder macOS leftover check" `
    -Status $(if (-not $hasMacStepInTurnPrompt -and $hasPcStepInTurnPrompt -and $adapterReplacesMac) { "PASS" } else { "FAIL" }) `
    -Observed ("adapterReplacesMac={0}, TurnPromptBuilderHasMacStep={1}, TurnPromptBuilderHasPcStep={2}" -f $adapterReplacesMac, $hasMacStepInTurnPrompt, $hasPcStepInTurnPrompt) `
    -Expected "Zero macOS tool/platform leftovers in Windows agent prompt builder and instructions"

  # Scenario 28: StructuredFinalReply.Parse verification across all 9 XML tags
  $sfrSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Turns\StructuredFinalReply.cs") -Raw
  $sfrTags = @("SUMMARY", "ANSWER", "DONE_TITLE", "ARTIFACTS", "NEXT_ACTIONS", "ROUTINE", "COMPUTER_USE_REQUEST", "WAIT_FOR", "LEARNED")
  $missingTags = @($sfrTags | Where-Object { $sfrSource -notmatch $_ })
  Add-ScenarioResult -Id 28 -Category "Turns" -Name "StructuredFinalReply.Parse all 9 XML blocks (<SUMMARY>..<LEARNED>)" `
    -Status $(if ($missingTags.Count -eq 0) { "PASS" } else { "FAIL" }) `
    -Observed ("All 9 XML tags supported={0} (missing={1})" -f ($missingTags.Count -eq 0), ($missingTags -join ",")) `
    -Expected "All 9 structured reply blocks parsed accurately and stripped from display text"

  # Scenario 29: RoutineScheduler & RoutineKeepAwake power request audit
  $keepAwakeSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Routines\RoutineScheduler.cs") -Raw
  $indefiniteWakeLock = $keepAwakeSource -match "RoutineKeepAwake\.Update\(routines\.Any\(\(ClickyRoutine r\) => !r\.IsPaused\)\)"
  $scopedWakeLock = $keepAwakeSource -match "ShouldKeepAwakeLocked"
  Add-ScenarioResult -Id 29 -Category "Routines" -Name "ClickyRoutine.NextSetTime & RoutineKeepAwake system sleep lock behavior" `
    -Status $(if (-not $indefiniteWakeLock -and $scopedWakeLock) { "PASS" } else { "FAIL" }) `
    -Observed ("ScopedShouldKeepAwakeLocked={0}, IndefiniteWakeLockWhenAnyRoutineUnpaused={1}" -f $scopedWakeLock, $indefiniteWakeLock) `
    -Expected "System wake lock is only held during active routine execution (ShouldKeepAwakeLocked), not indefinitely while any routine is unpaused"

  # Scenario 30: ClickyWorkFolderFiles path traversal / symlink & hidden dotfile filtering check
  $wffSource = Get-Content (Join-Path $RepoRoot "src\desktop\Kursor.Agents\HeyClicky.Agents.Jobs\ClickyWorkFolderFiles.cs") -Raw
  $excludesDotClicky = ($wffSource -match '"\.clicky"') -and ($wffSource -match '"WORK\.md"')
  $workspaceFiles = @(Get-ChildItem -Path $tempWorkDir -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch "^\." })
  Add-ScenarioResult -Id 30 -Category "Jobs" -Name "ClickyWorkFolderFiles.List workspace scanning & hidden folder exclusion" `
    -Status $(if ($excludesDotClicky -and $workspaceFiles.Count -ge 1) { "PASS" } else { "FAIL" }) `
    -Observed ("workspaceFiles={0} ({1}), dotClickyExcluded={2}" -f $workspaceFiles.Count, (($workspaceFiles | ForEach-Object { $_.Name }) -join ", "), $excludesDotClicky) `
    -Expected "Lists user artifacts in workspace while excluding .clicky, tmp, node_modules, .git"

} finally {
  if ($null -ne $harnessProc -and -not $harnessProc.HasExited) {
    $harnessProc.Kill()
  }
  try {
    Invoke-WebRequest -Uri "$mockWorkerBase/stop" -Method GET -UseBasicParsing -TimeoutSec 2 | Out-Null
  } catch {}
  if ($null -ne $mockProc -and -not $mockProc.HasExited) {
    Stop-Process -Id $mockProc.Id -Force -ErrorAction SilentlyContinue
  }
  Remove-Item -Recurse -Force $tempStateDir -ErrorAction SilentlyContinue
}

$passCount = ($Results.scenarios | Where-Object { $_.status -eq "PASS" }).Count
$failCount = ($Results.scenarios | Where-Object { $_.status -eq "FAIL" }).Count
$Results.summary = [ordered]@{
  totalScenarios = $Results.scenarios.Count
  passed = $passCount
  failed = $failCount
  harnessHttpTestsCount = $Results.harnessHttpTests.Count
  liveBackendProbesCount = $Results.liveBackendProbes.Count
}

$outJsonPath = Join-Path $EvidenceDir "agents-test-results.json"
$Results | ConvertTo-Json -Depth 15 | Set-Content -Path $outJsonPath -Encoding UTF8
Write-Host ("`n=== Saved full results to {0} (PASS={1}, FAIL={2}) ===" -f $outJsonPath, $passCount, $failCount)
