$ErrorActionPreference = 'Stop'
$projectPath = $PSScriptRoot
$buildScript = Join-Path $projectPath 'build.ps1'
& $buildScript
if ($LASTEXITCODE -ne 0) { throw 'WrenFeed build failed.' }

$releaseRoot = Join-Path $projectPath 'release'
$stagePath = Join-Path $releaseRoot 'WrenFeed'
$zipPath = Join-Path $releaseRoot 'WrenFeed.zip'
if (Test-Path -LiteralPath $stagePath) { Remove-Item -LiteralPath $stagePath -Recurse -Force }
if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
New-Item -ItemType Directory -Path $stagePath -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $projectPath 'build\WrenFeed.exe') -Destination (Join-Path $stagePath 'WrenFeed.exe')
Copy-Item -LiteralPath (Join-Path $projectPath 'README-Share.md') -Destination (Join-Path $stagePath 'README.md')
Copy-Item -LiteralPath (Join-Path $projectPath 'Start WrenFeed.cmd') -Destination (Join-Path $stagePath 'Start WrenFeed.cmd')
Copy-Item -LiteralPath (Join-Path $projectPath 'import') -Destination (Join-Path $stagePath 'import') -Recurse
Compress-Archive -Path (Join-Path $stagePath '*') -DestinationPath $zipPath -CompressionLevel Optimal
Write-Output ('Created shareable package: ' + $zipPath)
