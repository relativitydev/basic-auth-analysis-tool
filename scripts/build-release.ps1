<#
.SYNOPSIS
	Builds the Windows release artifact: a Native AOT publish of the command-line tool from the current commit.
.DESCRIPTION
	Refuses a working tree with modified tracked files, publishes with the same command the release-evidence script uses,
	fails when the publish reports any warning, copies the published binary to the output folder as migrate-auth-<version>-<rid> (with .exe on Windows), and writes beside it the hash
	line (sha256sum format) and a small build record. It signs nothing, notarizes nothing, creates no release and no tag,
	and contacts no service other than what dotnet publish itself needs.
	The output folder must be outside the repository.
	Exit codes: 0 built, 1 build or check failed, 2 invalid arguments.
.EXAMPLE
	.\build-release.ps1 -OutputFolder C:\release-out\win-x64
#>
param(
	[Parameter(Mandatory = $true)][string]$OutputFolder,
	[string]$RuntimeIdentifier = 'win-x64',
	[string]$Repository = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$ExitFailed = 1
$ExitUsage = 2
$ProjectRelativePath = 'src/MigrateAuth.Cli/MigrateAuth.Cli.csproj'
$PublishedBinaryName = 'migrate-auth.exe'
$ArtifactBaseName = 'migrate-auth'
$WindowsRidPrefix = 'win'
$WindowsExtension = '.exe'
$VersionPattern = '<Version>([^<]+)</Version>'
$PublishLogName = 'publish.log'
$WarningPattern = 'warning [A-Za-z]+[0-9]+'
$AllowedWarningCount = 0

if (-not $Repository) { $Repository = Split-Path -Parent $PSScriptRoot }

function Get-ArtifactName {
	$projectText = Get-Content -Raw -Path (Join-Path $Repository $ProjectRelativePath)
	$versionMatch = [regex]::Match($projectText, $VersionPattern)
	if (-not $versionMatch.Success) { throw "no <Version> in $ProjectRelativePath" }
	$extension = if ($RuntimeIdentifier.StartsWith($WindowsRidPrefix, [System.StringComparison]::Ordinal)) { $WindowsExtension } else { '' }
	return "$ArtifactBaseName-$($versionMatch.Groups[1].Value.Trim())-$RuntimeIdentifier$extension"
}

function Test-OutputOutsideRepository {
	param([string]$RepositoryPath, [string]$OutputPath)
	$repositoryPrefix = $RepositoryPath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
	return -not (($OutputPath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar).StartsWith($repositoryPrefix, [System.StringComparison]::OrdinalIgnoreCase))
}

function Test-TrackedTreeClean {
	param([string]$RepositoryPath)
	$changes = & git -C $RepositoryPath status --porcelain --untracked-files=no
	return -not $changes
}

function Invoke-Publish {
	param([string]$PublishFolder, [string]$LogPath)
	$projectPath = Join-Path $Repository $ProjectRelativePath
	$previousPreference = $ErrorActionPreference
	$ErrorActionPreference = 'Continue'
	& dotnet publish $projectPath -c Release -r $RuntimeIdentifier -o $PublishFolder 2>&1 | ForEach-Object { "$_" } | Tee-Object -FilePath $LogPath | Out-Host
	$publishExit = $LASTEXITCODE
	$ErrorActionPreference = $previousPreference
	return $publishExit
}

function Get-PublishWarningCount {
	param([string]$LogPath)
	$found = Select-String -Path $LogPath -Pattern $WarningPattern -AllMatches | ForEach-Object { $_.Line.Trim() } | Sort-Object -Unique
	return @($found).Count
}

function Write-BuildRecord {
	param([string]$RecordPath, [string]$Commit, [int]$Warnings, [string]$ArtifactPath, [string]$ArtifactHash)
	$lines = @(
		"commit: $Commit",
		"runtime identifier: $RuntimeIdentifier",
		"dotnet sdk: $(& dotnet --version)",
		"publish warnings: $Warnings",
		"artifact: $(Split-Path -Leaf $ArtifactPath)",
		"artifact size (bytes): $((Get-Item $ArtifactPath).Length)",
		"sha256: $ArtifactHash"
	)
	[System.IO.File]::WriteAllText($RecordPath, (($lines -join "`n") + "`n"), (New-Object System.Text.UTF8Encoding($false)))
}

function Invoke-ReleaseBuild {
	$repositoryPath = (Resolve-Path $Repository).Path
	New-Item -ItemType Directory -Force -Path $OutputFolder | Out-Null
	$outputPath = (Resolve-Path $OutputFolder).Path
	if (-not (Test-OutputOutsideRepository -RepositoryPath $repositoryPath -OutputPath $outputPath)) {
		Write-Host 'the output folder is inside the repository'
		return $ExitUsage
	}
	if (-not (Test-TrackedTreeClean -RepositoryPath $repositoryPath)) {
		Write-Host 'the working tree has modified tracked files; commit or stash them first'
		return $ExitFailed
	}
	$commit = (& git -C $repositoryPath rev-parse HEAD).Trim()
	$workFolder = Join-Path ([System.IO.Path]::GetTempPath()) ("build-release-" + [guid]::NewGuid().ToString('N'))
	$logPath = Join-Path $outputPath $PublishLogName
	$publishExit = Invoke-Publish -PublishFolder (Join-Path $workFolder 'publish') -LogPath $logPath
	$result = $ExitFailed
	if ($publishExit -ne 0) {
		Write-Host "publish failed with exit code $publishExit"
	} else {
		$result = Complete-ReleaseBuild -PublishedPath (Join-Path (Join-Path $workFolder 'publish') $PublishedBinaryName) -OutputPath $outputPath -LogPath $logPath -Commit $commit -ArtifactName (Get-ArtifactName)
	}
	if (Test-Path $workFolder) { Remove-Item -Recurse -Force $workFolder }
	return $result
}

function Complete-ReleaseBuild {
	param([string]$PublishedPath, [string]$OutputPath, [string]$LogPath, [string]$Commit, [string]$ArtifactName)
	$warnings = Get-PublishWarningCount -LogPath $LogPath
	$result = $ExitFailed
	if ($warnings -ne $AllowedWarningCount) {
		Write-Host "the publish reported $warnings warning(s); none are allowed"
	} elseif (-not (Test-Path $PublishedPath)) {
		Write-Host "the publish did not produce $PublishedBinaryName"
	} else {
		$artifactPath = Join-Path $OutputPath $ArtifactName
		Copy-Item -Path $PublishedPath -Destination $artifactPath -Force
		$artifactHash = (Get-FileHash -Algorithm SHA256 -Path $artifactPath).Hash.ToLowerInvariant()
		[System.IO.File]::WriteAllText("$artifactPath.sha256", "$artifactHash  $ArtifactName`n", (New-Object System.Text.UTF8Encoding($false)))
		Write-BuildRecord -RecordPath (Join-Path $OutputPath "build-record-$RuntimeIdentifier.txt") -Commit $Commit -Warnings $warnings -ArtifactPath $artifactPath -ArtifactHash $artifactHash
		Write-Host "built $ArtifactName for $RuntimeIdentifier from ${Commit}: sha256 $artifactHash, $warnings warnings"
		$result = 0
	}
	return $result
}

exit (Invoke-ReleaseBuild)
