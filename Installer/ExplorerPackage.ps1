param([switch]$Remove)
$ErrorActionPreference='Stop'
$package=Join-Path $PSScriptRoot 'ConvertTime.Menu.msix'
if([Environment]::OSVersion.Version.Build -lt 22000 -or !(Test-Path -LiteralPath $package)){return}
# The MSI includes this production package only after its signature is validated at build time.
$active=Get-AppxPackage -Name 'ConvertTime.ExplorerMenu'
if($Remove){if($active){Remove-AppxPackage -Package $active.PackageFullName};return}
if(!$active -or [version]$active.Version -lt [version]'2.3.1.0'){
 Add-AppxPackage -Path $package -ExternalLocation $PSScriptRoot -ErrorAction Stop
}