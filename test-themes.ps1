$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$compilerArgs = @('/nologo','/target:exe','/main:ThemeTests',('/out:' + (Join-Path $projectPath 'build\ThemeTests.exe')),('/resource:' + (Join-Path $projectPath 'src\Theme.xaml') + ',DailyQuest.Theme.xaml'))
foreach ($reference in @('System.dll','System.Core.dll','System.Runtime.Serialization.dll','System.Web.Extensions.dll','System.Xml.dll','System.Xaml.dll','WPF\WindowsBase.dll','WPF\PresentationCore.dll','WPF\PresentationFramework.dll')) { $compilerArgs += '/reference:' + (Join-Path $frameworkPath $reference) }
$compilerArgs += (Join-Path $projectPath 'src\Model.cs')
$compilerArgs += (Join-Path $projectPath 'src\QuestImport.cs')
$compilerArgs += (Join-Path $projectPath 'src\Themes.cs')
$compilerArgs += (Join-Path $projectPath 'src\App.cs')
$compilerArgs += (Join-Path $projectPath 'tests\ThemeTests.cs')
& (Join-Path $frameworkPath 'csc.exe') @compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Theme test compilation failed.' }
& (Join-Path $projectPath 'build\ThemeTests.exe') (Join-Path $projectPath 'themes\neon-aviary.json') (Join-Path $projectPath 'themes\adventurers-ledger.json') (Join-Path $projectPath 'themes\template.json') (Join-Path $projectPath ('build\test-theme-' + [Guid]::NewGuid().ToString('N')))
if ($LASTEXITCODE -ne 0) { throw 'Theme tests failed.' }
