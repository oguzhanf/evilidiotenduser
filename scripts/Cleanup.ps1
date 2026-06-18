<#
.SYNOPSIS
    Reverts artefacts left behind by the Evil End User simulator.

.DESCRIPTION
    Removes the reversible HKCU Run-key persistence value and the test files / staging folder /
    exported logs the simulator may have dropped into the working (Downloads) directory.

    Safe to run repeatedly; missing items are ignored.

.EXAMPLE
    pwsh ./scripts/Cleanup.ps1
    pwsh ./scripts/Cleanup.ps1 -WorkingDirectory "C:\Temp\eeu"
#>
[CmdletBinding()]
param(
    [string]$WorkingDirectory = (Join-Path $env:USERPROFILE 'Downloads')
)

Write-Host "Evil End User — cleanup" -ForegroundColor Cyan

# 1) Remove the persistence Run-key value.
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$valueName = 'EvilEndUserSimPersistence'
try {
    $existing = Get-ItemProperty -Path $runKey -Name $valueName -ErrorAction SilentlyContinue
    if ($null -ne $existing) {
        Remove-ItemProperty -Path $runKey -Name $valueName -ErrorAction Stop
        Write-Host "  Removed Run-key value '$valueName'." -ForegroundColor Green
    }
    else {
        Write-Host "  Run-key value '$valueName' not present."
    }
}
catch {
    Write-Warning "  Could not remove Run-key value: $_"
}

# 2) Remove dropped test files and exports.
$patterns = @(
    'Invoice_*.pdf.txt',
    'PaySlip_*.docx.txt',
    'Refund_Details.txt',
    'eicar*.*',
    'evilenduser-log-*.json',
    'evilenduser-log-*.csv'
)
foreach ($pattern in $patterns) {
    Get-ChildItem -Path $WorkingDirectory -Filter $pattern -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            Remove-Item $_.FullName -Force -ErrorAction Stop
            Write-Host "  Removed $($_.Name)." -ForegroundColor Green
        }
        catch {
            Write-Warning "  Could not remove $($_.Name): $_"
        }
    }
}

# 3) Remove the collection staging folder.
$staging = Join-Path $WorkingDirectory 'EvilEndUser_Staging'
if (Test-Path $staging) {
    try {
        Remove-Item $staging -Recurse -Force -ErrorAction Stop
        Write-Host "  Removed staging folder." -ForegroundColor Green
    }
    catch {
        Write-Warning "  Could not remove staging folder: $_"
    }
}

Write-Host "Cleanup complete." -ForegroundColor Cyan
