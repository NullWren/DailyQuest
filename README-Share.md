# WrenFeed

WrenFeed is a tiny offline game checklist for daily and weekly tasks. The checklist does not need an account, internet connection, installer, or technical setup. An optional leaderboard upload is available when its owner enables the separate service.

## Start it

1. Right-click `WrenFeed.zip` and choose **Extract All**.
2. Open the extracted `WrenFeed` folder and double-click `WrenFeed.exe`.
3. Click **+ Game** to add the games and daily tasks you want to track.

Keep the extracted folder in a convenient place. For a desktop shortcut, right-click `WrenFeed.exe` and choose **Show more options → Send to → Desktop (create shortcut)**. Keep the folder in place so the shortcut continues to work. To update the app later, close it and replace this extracted folder with the new one; your list and history are stored separately.

Windows may show a first-run warning because this is a small unsigned local app. Choose **More info**, then **Run anyway** if you trust the person who shared it.

## What it does

- Add a quest, a short description, and optional task lines. Give related quests the same optional **Game group** to show them in one panel.
- Make any task line individually checkable.
- Checking a game checks all of its tasks. Completing all its tasks completes the game.
- Each quest has its own completion and reset rule, even within a group. Daily quests can start fresh at local midnight, at a fixed UTC time, or a chosen number of hours after completion. History saves earlier results.
- The **Weekly** tab keeps a separate list. Set each game to a fixed UTC reset day and time, or to reset at 00:00 UTC seven days after completion (useful for Tears of Guthix). Recent weekly completions appear below the current list.
- **Settings** includes optional themes, an always-on-top mode, a 20–100% visibility slider, completion animations, and backup export/restore. Choose **Fade backgrounds only** to see more of your game while keeping quest text clear, or **Fade everything** to dim the whole window. Open **Install theme file…** and choose one of the eight optional files in the included `themes` folder; select **Default** to restore the original look. The folder has previews, its own README, and a custom-theme template.
- Drag the WrenFeed title bar to move the window and its edges or corners to resize it. The title bar also has minimize and close buttons.
- **Import quests** in Settings adds quests from a JSON file. The included `import` folder has a template, instructions, and a copy-paste AI chat prompt for turning your own task list into an import file. Importing does not replace your history.
- **Leaderboard upload…** in Settings shows your completed daily and weekly totals by game. If the leaderboard owner gives you a private invite code, you can choose a public nickname, review the totals, and press **Upload totals**. This is the only feature that uses the internet. If the service is not open yet, WrenFeed will tell you.

Your journal stays on your computer in `%LOCALAPPDATA%\DailyQuest\journal.json`. WrenFeed keeps the original Daily Quest data location so an upgrade retains your history. Optional uploads share only the public nickname and game totals shown in the review screen, plus the private invite code needed to accept them. You can use **Settings → Export backup** to make a copy before moving to another computer.

For the full developer notes, source, build script, and tests, see `README.md` in the source repository.
