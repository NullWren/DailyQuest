$ErrorActionPreference = 'Stop'
$sourceExe = Join-Path $PSScriptRoot 'build\DailyQuest.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) { throw 'Run build.ps1 first.' }
$installFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\DailyQuest'
$installedExe = Join-Path $installFolder 'DailyQuest.exe'
New-Item -ItemType Directory -Path $installFolder -Force | Out-Null
Copy-Item -LiteralPath $sourceExe -Destination $installedExe -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src\app.ico') -Destination (Join-Path $installFolder 'DailyQuest.ico') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $installFolder 'README.md') -Force
$desktopFolder = [Environment]::GetFolderPath('DesktopDirectory')
$shortcutPath = Join-Path $desktopFolder 'Daily Quest.lnk'
$shellObject = New-Object -ComObject WScript.Shell
$shortcut = $shellObject.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExe
$shortcut.WorkingDirectory = $installFolder
$shortcut.Description = 'Your offline daily game quest log'
$shortcut.IconLocation = (Join-Path $installFolder 'DailyQuest.ico') + ',0'
$shortcut.Save()
Write-Output ('Installed app: ' + $installedExe)
Write-Output ('Desktop shortcut: ' + $shortcutPath)
