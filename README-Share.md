# Daily Quest

Daily Quest is a tiny offline game checklist. It does not need an account, internet connection, installer, or technical setup.

## Start it

1. Right-click `DailyQuest.zip` and choose **Extract All**.
2. Open the extracted `DailyQuest` folder.
3. Double-click `Start Daily Quest.cmd` (or `DailyQuest.exe`).
4. Click **+ Game** to add the games and daily tasks you want to track.

Windows may show a first-run warning because this is a small unsigned local app. Choose **More info**, then **Run anyway** if you trust the person who shared it.

## What it does

- Add a game, a short description, and optional task lines.
- Make any task line individually checkable.
- Checking a game checks all of its tasks. Completing all its tasks completes the game.
- History saves each day’s results. The daily list starts fresh at local midnight.
- **Settings** includes an optional always-on-top mode, completion animations, and backup export/restore.

Your data stays on your computer in `%LOCALAPPDATA%\DailyQuest\journal.json`. The app never sends it anywhere. You can use **Settings → Export backup** to make a copy before moving to another computer.

For the full developer notes, source, build script, and tests, see `README.md` in the source repository.
