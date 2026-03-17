$ErrorActionPreference = "Stop"

$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

# Make this repo case-sensitive from Git's perspective
git config core.ignorecase false

function Get-ActualCasedRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepoRoot,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    $parts = $RelativePath -split '[\\/]'
    $current = $RepoRoot
    $actualParts = New-Object System.Collections.Generic.List[string]

    foreach ($part in $parts) {
        $children = Get-ChildItem -LiteralPath $current -Force
        $match = $children | Where-Object { $_.Name -ieq $part }

        if (-not $match) {
            return $null
        }

        if ($match -is [System.Array]) {
            $match = $match[0]
        }

        $actualParts.Add($match.Name)
        $current = Join-Path $current $match.Name
    }

    return ($actualParts -join '/')
}

$gitFiles = git ls-files
if (-not $gitFiles) {
    Write-Host "No tracked files found."
    exit 0
}

$mismatches = New-Object System.Collections.Generic.List[object]

foreach ($gitPath in $gitFiles) {
    $actualPath = Get-ActualCasedRelativePath -RepoRoot $repoRoot -RelativePath $gitPath

    if ($null -eq $actualPath) {
        Write-Warning "Tracked path not found on disk: $gitPath"
        continue
    }

    if ($gitPath -cne $actualPath) {
        $mismatches.Add([PSCustomObject]@{
            GitPath    = $gitPath
            ActualPath = $actualPath
        })
    }
}

if ($mismatches.Count -eq 0) {
    Write-Host "Git index already matches filesystem casing."
    exit 0
}

Write-Host "Found $($mismatches.Count) path casing mismatches:" -ForegroundColor Yellow
$mismatches | ForEach-Object {
    Write-Host "  $($_.GitPath)  ->  $($_.ActualPath)"
}

# Deeper paths first
$mismatches = $mismatches | Sort-Object { $_.GitPath.Length } -Descending

$counter = 0

foreach ($entry in $mismatches) {
    $oldPath = $entry.GitPath
    $newPath = $entry.ActualPath

    $parent = Split-Path $oldPath -Parent
    $leaf = Split-Path $oldPath -Leaf

    $tempLeaf = "__casefix__$counter`__$leaf"
    $tempPath = if ([string]::IsNullOrWhiteSpace($parent)) {
        $tempLeaf
    } else {
        ($parent -replace '\\','/') + "/" + $tempLeaf
    }

    Write-Host ""
    Write-Host "Updating Git index path:"
    Write-Host "  $oldPath"
    Write-Host "  -> $tempPath"
    git mv --force -- "$oldPath" "$tempPath"

    Write-Host "  $tempPath"
    Write-Host "  -> $newPath"
    git mv --force -- "$tempPath" "$newPath"

    $counter++
}

Write-Host ""
Write-Host "Done."
Write-Host "Review:"
Write-Host "  git status"
Write-Host "Commit:"
Write-Host '  git commit -m "Fix path casing to match filesystem"'