$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$testExe = Join-Path $projectPath 'build\ImportTests.exe'
& (Join-Path $frameworkPath 'csc.exe') /nologo /target:exe /utf8output ('/out:' + $testExe) ('/reference:' + (Join-Path $frameworkPath 'System.Runtime.Serialization.dll')) ('/reference:' + (Join-Path $frameworkPath 'System.Web.Extensions.dll')) (Join-Path $projectPath 'src\Model.cs') (Join-Path $projectPath 'src\QuestImport.cs') (Join-Path $projectPath 'tests\ImportTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Import test compilation failed.' }
& $testExe (Join-Path $projectPath 'import\template.json')
if ($LASTEXITCODE -ne 0) { throw 'Import tests failed.' }
