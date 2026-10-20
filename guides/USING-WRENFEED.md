# Using WrenFeed

[← Back to the quick start](../README.md)

## Quests and groups

- **+ Game** adds a quest, a short description, and any number of extra lines. Enable **Checkbox** on a line to make it an individual task; leave it off for a plain note. Blank lines are ignored when saving.
- Set the optional **Game group** while editing to show related quests in one panel. For example, set both Tears of Guthix and Kingdom check-in to **OSRS**. The group heading has no checkbox; each quest keeps its own completion, reset rule, and history. Existing entries are ungrouped until you edit them. Use **+ Quest** in a group to add another quest there.
- Check a quest to complete all its tasks. Completing every checkable task automatically completes the quest. Unchecking the quest clears its tasks; unchecking one task reopens the quest.
- The **···** menu edits, reorders, or archives a quest. Archiving keeps its history. Restore archived quests in Settings.
- **History** shows a read-only snapshot for a date. Past game names, descriptions, task text, and results stay intact when you change today's list. Today is updated as you edit it.

## Reset rules

The **Weekly** tab has its own game list and recent completion history. Each weekly quest can reset on a fixed UTC weekday and time, or at **00:00 UTC on the seventh day after its last completion**. The second option suits Tears of Guthix: a completion on Sunday UTC becomes available the following Sunday at 00:00 UTC. The tab shows the next reset in your local time after completion.

Daily quests can reset at **local midnight** (the default), at a chosen **UTC time**, or a chosen **number of hours after completion** (24 hours by default). This allows tasks with server-wide resets or rolling timers to share a game group. For Forma, choose 23 hours and complete the quest when you start the next build. The window detects resets while open. If the app is closed, the next launch creates the current cycle. Unopened cycles have no recorded result and are never assumed complete.

## Settings, files, and sharing

Settings includes **always on top**, a **20–100% visibility slider**, completion animations, optional themes, and backup export/restore. Choose **Fade everything** for whole-window opacity or **Fade backgrounds only** to keep quest text clear over your game. The second mode gives text a contrast halo and reduces the extra tint on quest cards. Small buttons stay solid so they remain easy to use. Settings and other modal dialogs cannot be minimized while they block the quest log.

**Settings → Import quests** adds daily and weekly quests from one JSON file. Start with [the template and its guide](../import/README.md), which cover every field in the quest editor, or use the [copy-paste AI chat prompt](../import/AI-PROMPT.md) to turn a plain-English list into valid JSON. Review additions and skipped duplicates before saving. Importing leaves existing progress and history intact; restoring a backup remains the full-journal replacement option.

**Settings → Appearance** keeps the original design as Default. Choose **Install theme file…** to add an optional local theme, then select it in the list. [Nine themes, previews, and a custom-theme template](../themes/README.md) are supplied in the repo and shareable ZIP. Theme files are stored separately from the journal and never execute code or use the network.

**Settings → Leaderboard upload…** shows an all-time tally of completed daily and weekly quest cycles by game. Enter a nickname and your private invite code, review the labels and counts, and press **Upload totals** to share only that tally. Ungrouped quests use their quest names as game labels. The public [WrenFeed standings](https://nullwren.github.io/WrenFeed/standings.html) stay on GitHub Pages. Uploads require the separate receiver to be enabled first; until then the app explains that the service is not open. Your journal remains local, and WrenFeed makes no network request unless you press Upload.

Backups include daily and weekly quests and their history. Restoring a backup replaces the journal, after confirmation, and preserves a separate copy of the journal being replaced. You can place exported backups wherever you choose, including a synced folder.

Drag the WrenFeed title bar to move the window, drag its edges or corners to resize, or use its minimize and close buttons. Closing exits the app; it does not keep running in the tray. Window size is remembered.

Keyboard: **Ctrl+N** adds a game, **Ctrl+H** toggles History, **Ctrl+,** opens Settings, and **Esc** closes a dialog.
