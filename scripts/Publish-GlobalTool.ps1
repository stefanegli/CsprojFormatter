[CmdletBinding()]
param(
    [string]$CredentialTarget = "CsProjFormatter.NuGet.ApiKey.Prod",
    [string]$ProjectPath = "CsProjFormatter.Cli/CsProjFormatter.Cli.csproj",
    [string]$PackagesDirectory = "artifacts/packages",
    [string]$Configuration = "Release",
    [string]$NuGetSource = "https://api.nuget.org/v3/index.json",
    [switch]$SkipPack,
    [switch]$PackOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not ("CredentialManagerNative" -as [Type]))
{
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class CredentialManagerNative
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct CREDENTIAL
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool CredRead(string target, int type, int reservedFlag, out IntPtr credentialPtr);

    [DllImport("Advapi32.dll", SetLastError = true)]
    public static extern void CredFree(IntPtr cred);
}
"@
}

function Get-CredentialManagerSecret
{
    param(
        [Parameter(Mandatory = $true)]
        [string]$Target
    )

    $credentialPtr = [IntPtr]::Zero
    $credTypeGeneric = 1
    $result = [CredentialManagerNative]::CredRead($Target, $credTypeGeneric, 0, [ref]$credentialPtr)

    if (-not $result)
    {
        $win32Error = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
        throw "Credential target '$Target' was not found or could not be read (Win32 error: $win32Error)."
    }

    try
    {
        $credential = [Runtime.InteropServices.Marshal]::PtrToStructure(
            $credentialPtr,
            [Type][CredentialManagerNative+CREDENTIAL])

        if ($credential.CredentialBlobSize -le 0 -or $credential.CredentialBlob -eq [IntPtr]::Zero)
        {
            throw "Credential target '$Target' does not contain a secret."
        }

        $bytes = New-Object byte[] $credential.CredentialBlobSize
        [Runtime.InteropServices.Marshal]::Copy($credential.CredentialBlob, $bytes, 0, $credential.CredentialBlobSize)

        $secret = [Text.Encoding]::Unicode.GetString($bytes).TrimEnd([char]0)
        if ([string]::IsNullOrWhiteSpace($secret))
        {
            $secret = [Text.Encoding]::UTF8.GetString($bytes).TrimEnd([char]0)
        }

        if ([string]::IsNullOrWhiteSpace($secret))
        {
            throw "Credential target '$Target' was read but secret content is empty."
        }

        return $secret
    }
    finally
    {
        if ($credentialPtr -ne [IntPtr]::Zero)
        {
            [CredentialManagerNative]::CredFree($credentialPtr)
        }
    }
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
Push-Location $repoRoot

try
{
    $resolvedProjectPath = (Resolve-Path (Join-Path $repoRoot $ProjectPath)).Path
    $resolvedPackagesDirectory = Join-Path $repoRoot $PackagesDirectory
    New-Item -ItemType Directory -Force -Path $resolvedPackagesDirectory | Out-Null

    $dotnetHome = Join-Path $repoRoot ".dotnet"
    New-Item -ItemType Directory -Force -Path $dotnetHome | Out-Null
    $env:DOTNET_CLI_HOME = $dotnetHome
    $env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

    if (-not $SkipPack)
    {
        Write-Host "Packing tool package from '$resolvedProjectPath'..."
        & dotnet pack $resolvedProjectPath -c $Configuration -o $resolvedPackagesDirectory --nologo
        if ($LASTEXITCODE -ne 0)
        {
            throw "dotnet pack failed with exit code $LASTEXITCODE."
        }
    }
    else
    {
        Write-Host "Skipping pack step as requested."
    }

    $package = Get-ChildItem -Path $resolvedPackagesDirectory -Filter "*.nupkg" |
        Where-Object { $_.Name -notlike "*.symbols.nupkg" } |
        Sort-Object LastWriteTimeUtc -Descending |
        Select-Object -First 1

    if ($null -eq $package)
    {
        throw "No package (*.nupkg) found in '$resolvedPackagesDirectory'."
    }

    Write-Host "Selected package: $($package.FullName)"

    if ($PackOnly)
    {
        Write-Host "Pack-only mode enabled. Skipping push."
        return
    }

    $apiKey = Get-CredentialManagerSecret -Target $CredentialTarget

    Write-Host "Pushing package to '$NuGetSource'..."
    & dotnet nuget push $package.FullName --source $NuGetSource --api-key $apiKey --skip-duplicate
    if ($LASTEXITCODE -ne 0)
    {
        throw "dotnet nuget push failed with exit code $LASTEXITCODE."
    }

    Write-Host "Package push completed."
}
finally
{
    Pop-Location
}
