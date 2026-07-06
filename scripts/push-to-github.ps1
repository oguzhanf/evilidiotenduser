<#
.SYNOPSIS
    Pushes this repository to GitHub using a Personal Access Token, without persisting the token.

.DESCRIPTION
    No credentials are stored in the repo or in git config. The token is passed only for the single
    push invocation via an in-memory HTTP Authorization header, so it never lands in the remote URL,
    the reflog, or on disk.

    The remote 'origin' should already point at the target repository
    (https://github.com/oguzhanf/evilidiotenduser.git). If it doesn't, this script sets it.

.PARAMETER Token
    A GitHub PAT. Classic: needs the 'repo' scope. Fine-grained: needs Contents: Read & Write on
    the target repository. If the repository does not exist yet, create it on GitHub first (an empty
    repo, no README) or use a token/UI to create it, then re-run.

.PARAMETER Remote
    The HTTPS URL of the GitHub repository. Defaults to the oguzhanf/evilidiotenduser repo.

.EXAMPLE
    pwsh ./scripts/push-to-github.ps1 -Token ghp_xxxxxxxxxxxxxxxxxxxx

.EXAMPLE
    $env:GITHUB_TOKEN = 'ghp_...'; pwsh ./scripts/push-to-github.ps1
#>
[CmdletBinding()]
param(
    [string]$Token = $env:GITHUB_TOKEN,
    [string]$Remote = 'https://github.com/oguzhanf/evilidiotenduser.git',
    [string]$Branch = 'main'
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Token)) {
    Write-Error "No token supplied. Pass -Token <PAT> or set `$env:GITHUB_TOKEN. The token is used only for this push and is never stored."
    exit 1
}

# Resolve a git executable (portable MinGit or system git).
$git = (Get-Command git -ErrorAction SilentlyContinue).Source
if (-not $git) {
    foreach ($c in @('C:\tools\mingit\cmd\git.exe', 'C:\Program Files\Git\cmd\git.exe')) {
        if (Test-Path $c) { $git = $c; break }
    }
}
if (-not $git) { Write-Error 'git executable not found.'; exit 1 }

# Ensure the remote is set (does not embed the token).
$existing = (& $git remote 2>$null)
if ($existing -contains 'origin') {
    & $git remote set-url origin $Remote | Out-Null
}
else {
    & $git remote add origin $Remote | Out-Null
}

# Build a Basic auth header (username is ignored by GitHub for PATs; 'x-access-token' is conventional).
$basic = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("x-access-token:$Token"))

Write-Host "Pushing '$Branch' to $Remote ..." -ForegroundColor Cyan

# The token is passed only via an in-memory -c http.extraheader for this single command.
& $git -c "http.extraheader=Authorization: Basic $basic" push -u origin $Branch
$code = $LASTEXITCODE

if ($code -eq 0) {
    Write-Host "Push complete." -ForegroundColor Green
}
else {
    Write-Warning "Push failed (exit $code). Common causes: the repository does not exist yet, or the token lacks push permission."
}

exit $code
