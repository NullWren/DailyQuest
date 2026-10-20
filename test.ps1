$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$testExe = Join-Path $projectPath 'build\ModelTests.exe'
& (Join-Path $frameworkPath 'csc.exe') /nologo /target:exe /utf8output ('/out:' + $testExe) ('/reference:' + (Join-Path $frameworkPath 'System.Runtime.Serialization.dll')) ('/reference:' + (Join-Path $frameworkPath 'System.Web.Extensions.dll')) (Join-Path $projectPath 'src\Model.cs') (Join-Path $projectPath 'src\QuestImport.cs') (Join-Path $projectPath 'tests\ModelTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
$testPath = Join-Path $projectPath ('build\test-' + [Guid]::NewGuid().ToString('N'))
& $testExe $testPath
if ($LASTEXITCODE -ne 0) { throw 'Model tests failed.' }
