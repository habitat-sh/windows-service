param (
    [string]$token
)

$headers = @{
  "Authorization" = "Bearer $token"
  "Content-type" = "application/json"
}

$body = @"
{
    "accountName": "chef",
    "projectSlug": "windows-service",
    "branch": "master"
}
"@

Invoke-RestMethod -Uri 'https://ci.appveyor.com/api/builds' -Headers $headers -Method Post -Body $body
