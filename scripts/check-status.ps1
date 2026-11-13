Start-Sleep 3

$p = Get-Process -Name HeyClicky -ErrorAction SilentlyContinue
if ($p) {
    Write-Host ("HeyClicky PID: " + $p.Id + " | Started: " + $p.StartTime)
} else {
    Write-Host "HeyClicky NOT running"
}

try {
    $r = Invoke-RestMethod http://127.0.0.1:8787/health
    Write-Host ("Backend health: status=" + $r.status)
} catch {
    Write-Host "Backend not reachable: $_"
}
