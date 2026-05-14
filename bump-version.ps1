param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$directoryBuildPropsPath = Join-Path $repositoryRoot 'Directory.Build.props'
$packageManifestPath = Join-Path $repositoryRoot 'src\CopilotProfileManager.WinUI\Package.appxmanifest'

if (-not (Test-Path $directoryBuildPropsPath))
{
    throw "Could not find Directory.Build.props at '$directoryBuildPropsPath'."
}

if (-not (Test-Path $packageManifestPath))
{
    throw "Could not find Package.appxmanifest at '$packageManifestPath'."
}

$normalizedVersion = $Version.Trim()
if ($normalizedVersion.StartsWith('v', [System.StringComparison]::OrdinalIgnoreCase))
{
    $normalizedVersion = $normalizedVersion.Substring(1)
}

if ($normalizedVersion -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:\.(?<revision>\d+))?$')
{
    throw "Version '$Version' must look like 1.2.3, 1.2.3.4, v1.2.3, or v1.2.3.4."
}

if (-not $matches['revision'])
{
    $normalizedVersion = "$($matches['major']).$($matches['minor']).$($matches['patch']).0"
}

function Get-Utf8EncodingForFile([string]$Path)
{
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    if ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
    {
        return [System.Text.UTF8Encoding]::new($true)
    }

    return [System.Text.UTF8Encoding]::new($false)
}

function Update-FileVersionValue(
    [string]$Path,
    [string]$Pattern,
    [scriptblock]$Replacement,
    [string]$MissingMessage)
{
    $content = [System.IO.File]::ReadAllText($Path)
    if ($content -notmatch $Pattern)
    {
        throw $MissingMessage
    }

    $updatedContent = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        $Pattern,
        $Replacement,
        [System.Text.RegularExpressions.RegexOptions]::Singleline)

    [System.IO.File]::WriteAllText($Path, $updatedContent, (Get-Utf8EncodingForFile $Path))
}

Update-FileVersionValue `
    -Path $directoryBuildPropsPath `
    -Pattern '(<AppVersion\b[^>]*>)([^<]+)(</AppVersion>)' `
    -Replacement {
        param($match)
        return $match.Groups[1].Value + $normalizedVersion + $match.Groups[3].Value
    } `
    -MissingMessage "Directory.Build.props does not contain an AppVersion element."

Update-FileVersionValue `
    -Path $packageManifestPath `
    -Pattern '(<Identity\b[\s\S]*?\bVersion=")([^"]+)(")' `
    -Replacement {
        param($match)
        return $match.Groups[1].Value + $normalizedVersion + $match.Groups[3].Value
    } `
    -MissingMessage "Package.appxmanifest does not contain an Identity Version attribute."

Write-Host "Updated app version to $normalizedVersion"
Write-Host " - $directoryBuildPropsPath"
Write-Host " - $packageManifestPath"
