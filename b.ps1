$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

function Get-ActualCasedRelativePath {
    param(
        [string]$RepoRoot,
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

git ls-files | ForEach-Object {
    $actual = Get-ActualCasedRelativePath -RepoRoot $repoRoot -RelativePath $_
    if ($actual -and ($_ -cne $actual)) {
        "{0} -> {1}" -f $_, $actual
    }
}