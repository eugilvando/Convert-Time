param([Parameter(Mandatory=$true)][string]$ExePath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName PresentationFramework
Set-Location (Split-Path $ExePath)
# Load from the installed layout to exercise the compiled WPF resource dictionaries.
$assembly=[Reflection.Assembly]::LoadFrom($ExePath)
[Windows.Application]::ResourceAssembly=$assembly
$app=[Activator]::CreateInstance($assembly.GetType('FileConverter.Application'))
$app.InitializeComponent()
$t=$assembly.GetType('FileConverter.ConvertTheme')
$t.GetMethod('Start').Invoke($null,@())
function Assert-Color($key,$expected){
 $actual=$app.FindResource($key).Color.ToString()
 if($actual -ne $expected){throw "$key : expected $expected, found $actual"}
}
foreach($mode in @(2,1)){
 $t.GetMethod('SetMode').Invoke($null,@([int]$mode))
 if($mode -eq 2){Assert-Color 'ReadyForegroundBrush' '#FFF2F2F5';Assert-Color 'Button_BackgroundBrush' '#FF303137'}
 else{Assert-Color 'ReadyForegroundBrush' '#FF202126';Assert-Color 'Button_BackgroundBrush' '#FFFFFFFF'}
 $button=[Windows.Controls.Button]::new();$button.Content='Converter';$button.Background=[Windows.Media.BrushConverter]::new().ConvertFromString('#FF7466')
 $button.Style=$app.FindResource([Windows.Controls.Button]);[void]$button.ApplyTemplate()
 $border=$button.Template.FindName('border',$button)
 if($border.Background.Color.ToString() -ne '#FFFF7466'){throw 'Cor do botão principal incorreta'}
}
$t.GetMethod('SetMode').Invoke($null,@([int]0))
