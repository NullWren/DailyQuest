$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerPath = Join-Path $frameworkPath 'csc.exe'
$outputPath = Join-Path $projectPath 'build'
New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$references = @('System.dll','System.Core.dll','System.Runtime.Serialization.dll','System.Web.Extensions.dll','System.Xml.dll','System.Xaml.dll','System.Windows.Forms.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')
$compilerArgs = @('/nologo','/target:winexe','/optimize+','/utf8output',('/out:' + (Join-Path $outputPath 'WrenFeed.exe')),('/win32icon:' + (Join-Path $projectPath 'src\app.ico')),('/resource:' + (Join-Path $projectPath 'src\Theme.xaml') + ',DailyQuest.Theme.xaml'))
foreach ($reference in $references + 'System.Security.dll') { $compilerArgs += '/reference:' + (Join-Path $frameworkPath $reference) }
$compilerArgs += (Join-Path $projectPath 'src\Model.cs')
$compilerArgs += (Join-Path $projectPath 'src\QuestImport.cs')
$compilerArgs += (Join-Path $projectPath 'src\Leaderboard.cs')
$compilerArgs += (Join-Path $projectPath 'src\Themes.cs')
$compilerArgs += (Join-Path $projectPath 'src\App.cs')
& $compilerPath @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'WrenFeed compilation failed.' }
Write-Output ('Built ' + (Join-Path $outputPath 'WrenFeed.exe'))
