# WrenFeed

WrenFeed is a tiny offline game checklist for daily and weekly tasks. It does not need an account, internet connection, installer, or technical setup.

## Start it

1. Right-click `WrenFeed.zip` and choose **Extract All**.
2. Open the extracted `WrenFeed` folder.
3. Double-click `WrenFeed.exe` (or `Start WrenFeed.cmd`).
4. Click **+ Game** to add the games and daily tasks you want to track.

Windows may show a first-run warning because this is a small unsigned local app. Choose **More info**, then **Run anyway** if you trust the person who shared it.

## What it does

- Add a quest, a short description, and optional task lines. Give related quests the same optional **Game group** to show them in one panel.
- Make any task line individually checkable.
- Checking a game checks all of its tasks. Completing all its tasks completes the game.
- Each quest has its own completion and reset rule, even within a group. Daily quests can start fresh at local midnight, at a fixed UTC time, or a chosen number of hours after completion. History saves earlier results.
- The **Weekly** tab keeps a separate list. Set each game to a fixed UTC reset day and time, or to reset at 00:00 UTC seven days after completion (useful for Tears of Guthix). Recent weekly completions appear below the current list.
- **Settings** includes optional themes, an always-on-top mode, a 20–100% visibility slider, completion animations, and backup export/restore. Choose **Fade backgrounds only** to see more of your game while keeping quest text clear, or **Fade everything** to dim the whole window. Open **Install theme file…** and choose one of the two optional files in the included `themes` folder; select **Default** to restore the original look. The folder has its own README and custom-theme template.
- Drag the WrenFeed title bar to move the window and its edges or corners to resize it. The title bar also has minimize and close buttons.
- **Import quests** in Settings adds quests from a JSON file. The included `import` folder has a template and README showing daily and weekly examples. Importing does not replace your history.

Your data stays on your computer in `%LOCALAPPDATA%\DailyQuest\journal.json`. WrenFeed keeps the original Daily Quest data location so an upgrade retains your history. The app never sends it anywhere. You can use **Settings → Export backup** to make a copy before moving to another computer.

For the full developer notes, source, build script, and tests, see `README.md` in the source repository.
