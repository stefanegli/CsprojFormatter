[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$RepositoryRoot,

    [Parameter(Mandatory = $true)]
    [string]$MappingFile,

    [Parameter(Mandatory = $true)]
    [string]$BareRepositoriesDirectory,

    [Parameter(Mandatory = $true)]
    [string]$WorkingRepositoriesDirectory,

    [string]$SourceRef = "HEAD",

    [string]$DefaultBranchName,

    [switch]$SkipWorkingCopies,

    [switch]$ForcePush
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (Get-Variable -Name PSNativeCommandUseErrorActionPreference -ErrorAction SilentlyContinue)
{
    $PSNativeCommandUseErrorActionPreference = $false
}

function Get-AbsolutePath
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$BasePath
    )

    if ([System.IO.Path]::IsPathRooted($Path))
    {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $BasePath $Path))
}

function Invoke-Git
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,

        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,

        [switch]$IgnoreExitCode
    )

    $stdoutPath = Join-Path $env:TEMP ("codex-git-stdout-" + [guid]::NewGuid().ToString("N") + ".log")
    $stderrPath = Join-Path $env:TEMP ("codex-git-stderr-" + [guid]::NewGuid().ToString("N") + ".log")

    try
    {
        $process = Start-Process `
            -FilePath "git" `
            -ArgumentList (@("-C", $WorkingDirectory) + $Arguments) `
            -WorkingDirectory $WorkingDirectory `
            -NoNewWindow `
            -Wait `
            -PassThru `
            -RedirectStandardOutput $stdoutPath `
            -RedirectStandardError $stderrPath

        $output = @()
        if (Test-Path -LiteralPath $stdoutPath)
        {
            $output += Get-Content -LiteralPath $stdoutPath
        }

        if (Test-Path -LiteralPath $stderrPath)
        {
            $output += Get-Content -LiteralPath $stderrPath
        }

        $exitCode = $process.ExitCode
        $script:LastGitExitCode = $exitCode
    }
    finally
    {
        if (Test-Path -LiteralPath $stdoutPath)
        {
            Remove-Item -LiteralPath $stdoutPath -Force -WhatIf:$false
        }

        if (Test-Path -LiteralPath $stderrPath)
        {
            Remove-Item -LiteralPath $stderrPath -Force -WhatIf:$false
        }
    }

    if (-not $IgnoreExitCode -and $exitCode -ne 0)
    {
        $commandText = ($Arguments | ForEach-Object {
                if ($_ -match "\s")
                {
                    '"' + $_ + '"'
                }
                else
                {
                    $_
                }
            }) -join " "
        $message = if ($output)
        {
            ($output | Out-String).Trim()
        }
        else
        {
            "git exited with code $exitCode."
        }

        throw "git -C '$WorkingDirectory' $commandText failed.`n$message"
    }

    return ,@($output)
}

function Get-GitRepositoryRoot
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProbePath
    )

    return (Invoke-Git -WorkingDirectory $ProbePath -Arguments @("rev-parse", "--show-toplevel"))[-1].Trim()
}

function Get-DefaultBranch
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot
    )

    $branchOutput = Invoke-Git -WorkingDirectory $RepositoryRoot -Arguments @("symbolic-ref", "--quiet", "--short", "HEAD") -IgnoreExitCode
    if ($script:LastGitExitCode -eq 0 -and $branchOutput.Count -gt 0)
    {
        return $branchOutput[-1].Trim()
    }

    return "main"
}

function Get-MappingValue
{
    param(
        [Parameter(Mandatory = $true)]
        [psobject]$Item,

        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $property = $Item.PSObject.Properties[$Name]
    if ($null -eq $property)
    {
        return $null
    }

    return $property.Value
}

function Resolve-RepositorySubfolder
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryRoot,

        [Parameter(Mandatory = $true)]
        [string]$RelativePath
    )

    if ([string]::IsNullOrWhiteSpace($RelativePath))
    {
        throw "SourcePath cannot be empty."
    }

    if ([System.IO.Path]::IsPathRooted($RelativePath))
    {
        throw "SourcePath '$RelativePath' must be relative to the git repository root."
    }

    $normalizedRelativePath = $RelativePath.Replace("\", "/").Trim("/")
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $RepositoryRoot ($normalizedRelativePath.Replace("/", "\"))))
    $repositoryRootPath = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $repositoryRootPrefix = $repositoryRootPath.TrimEnd("\") + "\"

    if (-not $fullPath.Equals($repositoryRootPath, [System.StringComparison]::OrdinalIgnoreCase) `
            -and -not $fullPath.StartsWith($repositoryRootPrefix, [System.StringComparison]::OrdinalIgnoreCase))
    {
        throw "SourcePath '$RelativePath' resolves outside of '$repositoryRootPath'."
    }

    if (-not (Test-Path -LiteralPath $fullPath -PathType Container))
    {
        throw "SourcePath '$normalizedRelativePath' does not exist in '$repositoryRootPath'."
    }

    return @{
        RelativePath = $normalizedRelativePath
        FullPath = $fullPath
    }
}

function Ensure-BareRepository
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$RepositoryPath,

        [Parameter(Mandatory = $true)]
        [string]$InitialBranchName
    )

    if (-not (Test-Path -LiteralPath $RepositoryPath))
    {
        $parentDirectory = Split-Path -Parent $RepositoryPath
        if (-not [string]::IsNullOrWhiteSpace($parentDirectory))
        {
            New-Item -ItemType Directory -Force -Path $parentDirectory | Out-Null
        }

        Invoke-Git -WorkingDirectory $parentDirectory -Arguments @("init", "--bare", "--initial-branch=$InitialBranchName", $RepositoryPath) | Out-Null

        return
    }

    $isBareRepository = (Invoke-Git -WorkingDirectory $RepositoryPath -Arguments @("rev-parse", "--is-bare-repository"))[-1].Trim()
    if ($isBareRepository -ne "true")
    {
        throw "'$RepositoryPath' already exists but is not a bare git repository."
    }
}

function Get-NormalizedRepositoryLocator
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Locator
    )

    $trimmedLocator = $Locator.Trim()

    if ([string]::IsNullOrWhiteSpace($trimmedLocator))
    {
        return $trimmedLocator
    }

    $uri = $null
    if ([System.Uri]::TryCreate($trimmedLocator, [System.UriKind]::Absolute, [ref]$uri))
    {
        if ($uri.IsFile)
        {
            return [System.IO.Path]::GetFullPath($uri.LocalPath).TrimEnd("\").ToLowerInvariant()
        }

        return $trimmedLocator.TrimEnd("/").ToLowerInvariant()
    }

    if ([System.IO.Path]::IsPathRooted($trimmedLocator))
    {
        return [System.IO.Path]::GetFullPath($trimmedLocator).TrimEnd("\").ToLowerInvariant()
    }

    return $trimmedLocator.TrimEnd("/").ToLowerInvariant()
}

function Sync-WorkingCopy
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$BareRepositoryPath,

        [Parameter(Mandatory = $true)]
        [string]$WorkingCopyPath,

        [Parameter(Mandatory = $true)]
        [string]$BranchName
    )

    if (-not (Test-Path -LiteralPath $WorkingCopyPath))
    {
        $parentDirectory = Split-Path -Parent $WorkingCopyPath
        if (-not [string]::IsNullOrWhiteSpace($parentDirectory))
        {
            New-Item -ItemType Directory -Force -Path $parentDirectory | Out-Null
        }

        Invoke-Git -WorkingDirectory $parentDirectory -Arguments @("clone", "--branch", $BranchName, $BareRepositoryPath, $WorkingCopyPath) | Out-Null

        return
    }

    $topLevelOutput = Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("rev-parse", "--show-toplevel") -IgnoreExitCode
    if ($script:LastGitExitCode -ne 0)
    {
        throw "'$WorkingCopyPath' already exists but is not a git working tree."
    }

    $originUrl = (Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("remote", "get-url", "origin"))[-1].Trim()
    $expectedOrigin = Get-NormalizedRepositoryLocator -Locator $BareRepositoryPath
    $actualOrigin = Get-NormalizedRepositoryLocator -Locator $originUrl

    if ($actualOrigin -ne $expectedOrigin)
    {
        throw "'$WorkingCopyPath' points to origin '$originUrl' instead of '$BareRepositoryPath'."
    }

    $statusOutput = Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("status", "--porcelain")
    if ($statusOutput.Count -gt 0)
    {
        throw "'$WorkingCopyPath' has uncommitted changes. Commit or stash them before rerunning the script."
    }

    Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("fetch", "origin", $BranchName) | Out-Null

    $branchExists = $false
    Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("show-ref", "--verify", "--quiet", "refs/heads/$BranchName") -IgnoreExitCode | Out-Null
    if ($script:LastGitExitCode -eq 0)
    {
        $branchExists = $true
    }

    if ($branchExists)
    {
        Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("checkout", $BranchName) | Out-Null
    }
    else
    {
        Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("checkout", "-b", $BranchName, "--track", "origin/$BranchName") | Out-Null
    }

    Invoke-Git -WorkingDirectory $WorkingCopyPath -Arguments @("merge", "--ff-only", "origin/$BranchName") | Out-Null
}

$scriptRepositoryPath = Get-AbsolutePath -Path (Join-Path $PSScriptRoot "..") -BasePath (Get-Location).Path
$resolvedRepositoryRoot = if ([string]::IsNullOrWhiteSpace($RepositoryRoot))
{
    Get-GitRepositoryRoot -ProbePath $scriptRepositoryPath
}
else
{
    Get-AbsolutePath -Path $RepositoryRoot -BasePath (Get-Location).Path
}

$resolvedMappingFile = Get-AbsolutePath -Path $MappingFile -BasePath (Get-Location).Path
if (-not (Test-Path -LiteralPath $resolvedMappingFile -PathType Leaf))
{
    throw "Mapping file '$resolvedMappingFile' does not exist."
}

$resolvedBareRepositoriesDirectory = Get-AbsolutePath -Path $BareRepositoriesDirectory -BasePath (Get-Location).Path
$resolvedWorkingRepositoriesDirectory = Get-AbsolutePath -Path $WorkingRepositoriesDirectory -BasePath (Get-Location).Path

$effectiveDefaultBranchName = if ([string]::IsNullOrWhiteSpace($DefaultBranchName))
{
    Get-DefaultBranch -RepositoryRoot $resolvedRepositoryRoot
}
else
{
    $DefaultBranchName.Trim()
}

$mappingItems = @((Get-Content -Raw -LiteralPath $resolvedMappingFile | ConvertFrom-Json))
if ($mappingItems.Count -eq 0)
{
    throw "Mapping file '$resolvedMappingFile' does not contain any entries."
}

$results = New-Object System.Collections.Generic.List[object]

foreach ($mappingItem in $mappingItems)
{
    $sourcePathValue = Get-MappingValue -Item $mappingItem -Name "SourcePath"
    $repositoryName = Get-MappingValue -Item $mappingItem -Name "RepositoryName"

    if ([string]::IsNullOrWhiteSpace($repositoryName))
    {
        throw "Each mapping entry must define RepositoryName."
    }

    $resolvedSourcePath = Resolve-RepositorySubfolder -RepositoryRoot $resolvedRepositoryRoot -RelativePath $sourcePathValue
    $branchName = Get-MappingValue -Item $mappingItem -Name "BranchName"
    if ([string]::IsNullOrWhiteSpace($branchName))
    {
        $branchName = $effectiveDefaultBranchName
    }

    $bareRepositoryName = Get-MappingValue -Item $mappingItem -Name "BareRepositoryName"
    if ([string]::IsNullOrWhiteSpace($bareRepositoryName))
    {
        if ($repositoryName.EndsWith(".git", [System.StringComparison]::OrdinalIgnoreCase))
        {
            $bareRepositoryName = $repositoryName
        }
        else
        {
            $bareRepositoryName = "$repositoryName.git"
        }
    }

    $workingCopyName = Get-MappingValue -Item $mappingItem -Name "WorkingCopyName"
    if ([string]::IsNullOrWhiteSpace($workingCopyName))
    {
        $workingCopyName = $repositoryName
    }

    $bareRepositoryPath = [System.IO.Path]::GetFullPath((Join-Path $resolvedBareRepositoriesDirectory $bareRepositoryName))
    $workingCopyPath = [System.IO.Path]::GetFullPath((Join-Path $resolvedWorkingRepositoriesDirectory $workingCopyName))

    Write-Host "Splitting '$($resolvedSourcePath.RelativePath)' into '$repositoryName'..."
    $splitCommit = (Invoke-Git -WorkingDirectory $resolvedRepositoryRoot -Arguments @("subtree", "split", "--prefix=$($resolvedSourcePath.RelativePath)", "--quiet", $SourceRef))[-1].Trim()
    if ($splitCommit -notmatch "^[0-9a-f]{40}$")
    {
        throw "git subtree split for '$($resolvedSourcePath.RelativePath)' did not return a commit hash."
    }

    if ($PSCmdlet.ShouldProcess($bareRepositoryPath, "Create or update bare repository"))
    {
        Ensure-BareRepository -RepositoryPath $bareRepositoryPath -InitialBranchName $branchName

        $pushArguments = @("push")
        if ($ForcePush)
        {
            $pushArguments += "--force"
        }

        $pushArguments += @($bareRepositoryPath, "$splitCommit`:refs/heads/$branchName")
        Invoke-Git -WorkingDirectory $resolvedRepositoryRoot -Arguments $pushArguments | Out-Null
        Invoke-Git -WorkingDirectory $bareRepositoryPath -Arguments @("symbolic-ref", "HEAD", "refs/heads/$branchName") | Out-Null
    }

    if (-not $SkipWorkingCopies -and $PSCmdlet.ShouldProcess($workingCopyPath, "Create or update working copy"))
    {
        Sync-WorkingCopy -BareRepositoryPath $bareRepositoryPath -WorkingCopyPath $workingCopyPath -BranchName $branchName
    }

    $results.Add([pscustomobject]@{
            RepositoryName = $repositoryName
            SourcePath = $resolvedSourcePath.RelativePath
            SplitCommit = $splitCommit
            BranchName = $branchName
            BareRepositoryPath = $bareRepositoryPath
            WorkingCopyPath = if ($SkipWorkingCopies) { $null } else { $workingCopyPath }
        })
}

$results
