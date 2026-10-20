$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$testExe = Join-Path $projectPath 'build\LeaderboardTests.exe'
& (Join-Path $frameworkPath 'csc.exe') /nologo /target:exe /utf8output ('/out:' + $testExe) ('/reference:' + (Join-Path $frameworkPath 'System.Runtime.Serialization.dll')) ('/reference:' + (Join-Path $frameworkPath 'System.Web.Extensions.dll')) ('/reference:' + (Join-Path $frameworkPath 'System.Security.dll')) (Join-Path $projectPath 'src\Model.cs') (Join-Path $projectPath 'src\Leaderboard.cs') (Join-Path $projectPath 'tests\LeaderboardTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Leaderboard test compilation failed.' }
& $testExe (Join-Path $projectPath ('build\test-leaderboard-' + [Guid]::NewGuid().ToString('N')))
if ($LASTEXITCODE -ne 0) { throw 'Leaderboard tests failed.' }
