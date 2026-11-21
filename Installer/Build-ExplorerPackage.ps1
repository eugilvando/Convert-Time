[CmdletBinding(DefaultParameterSetName='Production')]
param(
 [Parameter(Mandatory=$true)][string]$OutputDirectory,
 [Parameter(Mandatory=$true,ParameterSetName='Development')][switch]$LocalTest,
 [Parameter(Mandatory=$true,ParameterSetName='Production')][string]$Publisher,
 [Parameter(ParameterSetName='Production')][string]$SigningThumbprint
)
$ErrorActionPreference='Stop'
$output=[IO.Directory]::CreateDirectory($OutputDirectory).FullName
$stage=Join-Path $output ('stage-'+[guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($stage)
Copy-Item (Join-Path $PSScriptRoot 'ExplorerPackage\*') $stage -Recurse
$manifestFile=Join-Path $stage 'AppxManifest.xml'
[xml]$manifest=[IO.File]::ReadAllText($manifestFile)
if($LocalTest){$manifest.Package.Identity.Publisher='CN=Convert Time, OID.2.25.311729368913984317654407730594956997722=1';$manifest.Package.Identity.Name='ConvertTime.ExplorerMenu.LocalTest'}
else{$manifest.Package.Identity.Publisher=$Publisher}
$manifest.Save($manifestFile)
$vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
$vs=& $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if(!$vs){throw 'Instale as ferramentas C++ x64 do Visual Studio.'}
$vcvars=Join-Path $vs 'VC\Auxiliary\Build\vcvars64.bat'
$source=Join-Path $PSScriptRoot '..\Application\ExplorerCommand'
Push-Location $output
try {
  $command='call "{0}" >nul && cl /nologo /std:c++17 /EHsc /utf-8 /MT /O2 /LD "{1}\Commands.cpp" /FoCommands.obj /link /def:"{1}\Exports.def" /out:"{2}\ConvertTimeMenu.dll" /implib:ConvertTimeMenu.lib shlwapi.lib shell32.lib ole32.lib oleaut32.lib advapi32.lib runtimeobject.lib' -f $vcvars,$source,$stage
  & cmd.exe /d /c $command
  if($LASTEXITCODE){throw 'Falha na compilação da extensão.'}
} finally {Pop-Location}
$sdkRoot=Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\bin'
$sdk=Get-ChildItem $sdkRoot -Directory | Where-Object {$_.Name -match '^10\.' -and (Test-Path (Join-Path $_.FullName 'x64\makeappx.exe'))} | Sort-Object {[version]$_.Name} -Descending | Select-Object -First 1
$filename=if($LocalTest){'ConvertTime.Menu.LocalTest.msix'}else{'ConvertTime.Menu.msix'}
$package=Join-Path $output $filename
& (Join-Path $sdk.FullName 'x64\makeappx.exe') pack /d $stage /p $package /nv /o
if($LASTEXITCODE){throw 'Falha na criação do MSIX.'}
if($SigningThumbprint){
 & (Join-Path $sdk.FullName 'x64\signtool.exe') sign /fd SHA256 /sha1 $SigningThumbprint $package
 if($LASTEXITCODE){throw 'Falha ao assinar a identidade do Explorador.'}
}
Write-Output $package