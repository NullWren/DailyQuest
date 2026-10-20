$ErrorActionPreference = 'Stop'
$sourceExe = Join-Path $PSScriptRoot 'build\WrenFeed.exe'
if (-not (Test-Path -LiteralPath $sourceExe)) { throw 'Run build.ps1 first.' }
$installFolder = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\WrenFeed'
$installedExe = Join-Path $installFolder 'WrenFeed.exe'
New-Item -ItemType Directory -Path $installFolder -Force | Out-Null
Copy-Item -LiteralPath $sourceExe -Destination $installedExe -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'src\app.ico') -Destination (Join-Path $installFolder 'WrenFeed.ico') -Force
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'README.md') -Destination (Join-Path $installFolder 'README.md') -Force
$desktopFolder = [Environment]::GetFolderPath('DesktopDirectory')
$shortcutPath = Join-Path $desktopFolder 'WrenFeed.lnk'
$shellObject = New-Object -ComObject WScript.Shell
$shortcut = $shellObject.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $installedExe
$shortcut.WorkingDirectory = $installFolder
$shortcut.Description = 'WrenFeed offline game quest log'
$shortcut.IconLocation = (Join-Path $installFolder 'WrenFeed.ico') + ',0'
$shortcut.Save()
$previousShortcut = Join-Path $desktopFolder 'Daily Quest.lnk'
if (Test-Path -LiteralPath $previousShortcut) {
    $previousTarget = $shellObject.CreateShortcut($previousShortcut).TargetPath
    $expectedPreviousTarget = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Programs\DailyQuest\DailyQuest.exe'
    if ([String]::Equals($previousTarget, $expectedPreviousTarget, [StringComparison]::OrdinalIgnoreCase)) { Remove-Item -LiteralPath $previousShortcut -Force }
}
Write-Output ('Installed app: ' + $installedExe)
Write-Output ('Desktop shortcut: ' + $shortcutPath)
