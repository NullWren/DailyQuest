# Daily Quest

Daily Quest is a tiny offline game checklist for daily and weekly tasks. It does not need an account, internet connection, installer, or technical setup.

## Start it

1. Right-click `DailyQuest.zip` and choose **Extract All**.
2. Open the extracted `DailyQuest` folder.
3. Double-click `DailyQuest.exe` (or `Start Daily Quest.cmd`).
4. Click **+ Game** to add the games and daily tasks you want to track.

Windows may show a first-run warning because this is a small unsigned local app. Choose **More info**, then **Run anyway** if you trust the person who shared it.

## What it does

- Add a quest, a short description, and optional task lines. Give related quests the same optional **Game group** to show them in one panel.
- Make any task line individually checkable.
- Checking a game checks all of its tasks. Completing all its tasks completes the game.
- Each quest has its own completion and reset rule, even within a group. Daily quests can start fresh at local midnight or at a fixed UTC time. History saves earlier results.
- The **Weekly** tab keeps a separate list. Set each game to a fixed UTC reset day and time, or to reset at 00:00 UTC seven days after completion (useful for Tears of Guthix). Recent weekly completions appear below the current list.
- **Settings** includes an optional always-on-top mode, completion animations, and backup export/restore.

Your data stays on your computer in `%LOCALAPPDATA%\DailyQuest\journal.json`. The app never sends it anywhere. You can use **Settings → Export backup** to make a copy before moving to another computer.

For the full developer notes, source, build script, and tests, see `README.md` in the source repository.
