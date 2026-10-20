$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerArgs = @('/nologo','/target:exe','/main:EditorTests',('/out:' + (Join-Path $projectPath 'build\EditorTests.exe')),('/resource:' + (Join-Path $projectPath 'src\Theme.xaml') + ',DailyQuest.Theme.xaml'))
foreach ($reference in @('System.dll','System.Core.dll','System.Runtime.Serialization.dll','System.Web.Extensions.dll','System.Security.dll','System.Xml.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')) { $compilerArgs += '/reference:' + (Join-Path $frameworkPath $reference) }
$compilerArgs += (Join-Path $projectPath 'src\Model.cs')
$compilerArgs += (Join-Path $projectPath 'src\QuestImport.cs')
$compilerArgs += (Join-Path $projectPath 'src\Leaderboard.cs')
$compilerArgs += (Join-Path $projectPath 'src\Themes.cs')
$compilerArgs += (Join-Path $projectPath 'src\App.cs')
$compilerArgs += (Join-Path $projectPath 'tests\EditorTests.cs')
& (Join-Path $frameworkPath 'csc.exe') @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Editor test compilation failed.' }
& (Join-Path $projectPath 'build\EditorTests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Editor tests failed.' }
