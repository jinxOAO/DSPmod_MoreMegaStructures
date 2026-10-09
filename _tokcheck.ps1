$ErrorActionPreference = 'Stop'
$env:PATH = "E:\Git\cmd;$env:PATH"
$res = "protocol=https`nhost=github.com`n`n" | git credential fill 2>$null
$pw = ($res | Where-Object { $_.StartsWith('password=') }).Substring(9)
$u = ($res | Where-Object { $_.StartsWith('username=') }).Substring(9)
Write-Output "username=$u token_length=$($pw.Length)"
$hdr = @{ Authorization = "token $pw"; Accept = 'application/vnd.github+json' }
try {
    $resp = Invoke-WebRequest -Uri 'https://api.github.com/user' -Headers $hdr -UseBasicParsing -TimeoutSec 30
    $json = $resp.Content | ConvertFrom-Json
    Write-Output "api_status=$($resp.StatusCode) login=$($json.login) id=$($json.id)"
    Write-Output "scopes=$($resp.Headers['X-OAuth-Scopes'])"
    Write-Output "agent=$($resp.Headers['X-Accepted-OAuth-Scopes'])"
} catch {
    Write-Output "api_error=$($_.Exception.GetType().Name): $($_.Exception.Message)"
}
