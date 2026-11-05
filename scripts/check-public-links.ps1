<#
.SYNOPSIS
	Checks that the external links of the "More information from Relativity" section of the User Guide still answer.
.DESCRIPTION
	Extracts the links of section 8 of the User Guide, requests each with HTTP GET, prints the URL, the HTTP status and the
	UTC date and time of the request, and exits non-zero when any is not a 2xx answer. It is a documentation check to run
	by hand on the day of a release; it is not part of the offline test suite and no product code makes the request.
	-ListOnly prints the extracted links and makes no request.
.EXAMPLE
	.\check-public-links.ps1 -ListOnly
#>
param(
	[string]$GuideFile = (Join-Path (Split-Path -Parent $PSScriptRoot) 'docs/public/user-guide/08-more-from-relativity.md'),
	[switch]$ListOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ExitFailed = 1
$LinkPattern = '\]\((https?://[^)\s]+)\)'
$RequestTimeoutSeconds = 30
$SuccessStatusFloor = 200
$SuccessStatusCeiling = 299
$TimestampFormat = 'yyyy-MM-dd HH:mm:ss'

function Get-GuideLinks {
	param([string]$Path)
	$content = Get-Content -Path $Path -Raw
	$links = [regex]::Matches($content, $LinkPattern) | ForEach-Object { $_.Groups[1].Value }
	return @($links | Select-Object -Unique)
}

function Get-LinkStatus {
	param([string]$Url)
	$status = 0
	try {
		$response = Invoke-WebRequest -Uri $Url -Method Get -UseBasicParsing -TimeoutSec $RequestTimeoutSeconds
		$status = [int]$response.StatusCode
	} catch [System.Net.WebException] {
		if ($_.Exception.Response) { $status = [int]$_.Exception.Response.StatusCode }
	} catch [Microsoft.PowerShell.Commands.HttpResponseException] {
		$status = [int]$_.Exception.Response.StatusCode
	}
	return $status
}

function Invoke-LinkCheck {
	if (-not (Test-Path $GuideFile)) {
		Write-Host "guide file not found: $GuideFile"
		return $ExitFailed
	}
	$links = Get-GuideLinks -Path $GuideFile
	Write-Host "$($links.Count) link(s) in $GuideFile"
	$failed = 0
	foreach ($link in $links) {
		if ($ListOnly) {
			Write-Host $link
			continue
		}
		$status = Get-LinkStatus -Url $link
		$stamp = [DateTime]::UtcNow.ToString($TimestampFormat)
		Write-Host "$link  HTTP $status  $stamp UTC"
		if ($status -lt $SuccessStatusFloor -or $status -gt $SuccessStatusCeiling) { $failed++ }
	}
	return $(if ($failed -gt 0 -or $links.Count -eq 0) { $ExitFailed } else { 0 })
}

exit (Invoke-LinkCheck)
