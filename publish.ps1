<#
.SYNOPSIS
    Publica Soldadura.App como ejecutable self-contained de archivo único para Windows x64.

.DESCRIPTION
    Genera un .exe autónomo (incluye el runtime .NET, no requiere instalación previa) en la
    carpeta publish/. Esa carpeta está en .gitignore, así que el binario no se versiona.

.EXAMPLE
    ./publish.ps1
    ./publish.ps1 -Runtime win-x64 -Output dist
#>
param(
    [string]$Runtime = "win-x64",
    [string]$Output  = "publish",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$proyecto = Join-Path $PSScriptRoot "Soldadura.App/Soldadura.App.csproj"

Write-Host "Publicando $proyecto ($Configuration, $Runtime) -> $Output" -ForegroundColor Cyan

dotnet publish $proyecto `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $Output

if ($LASTEXITCODE -eq 0) {
    $exe = Join-Path $Output "Concentrica.exe"
    Write-Host "`nListo. Ejecutable: $exe" -ForegroundColor Green
}
