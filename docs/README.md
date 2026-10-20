# WrenFeed leaderboard site

This folder is the [WrenFeed site](https://nullwren.github.io/WrenFeed/) served by GitHub Pages from `main` → `/docs`. The home page introduces the app; [Standings](https://nullwren.github.io/WrenFeed/standings.html) has the game filter and leaderboard. The **See the standings** button opens that separate page. The site is separate from the desktop app and its local journal.

The standings page reads `data/service.json` first. It now loads live scores from the [leaderboard receiver](../leaderboard/README.md) named there. If the endpoint is ever set to `null`, it reads `data/leaderboard.json` instead. While the source has no players, it shows clearly labeled fictional players from `data/demo.json`. As soon as a real player appears, the demo file is ignored entirely. A filtered game with no completions shows an empty state.

## Standings data

`leaderboard.json` uses this small, versioned format:

```json
{
  "version": 1,
  "updatedAt": "2026-10-19T12:00:00Z",
  "players": [
    {
      "name": "Example player",
      "tag": "optional short label",
      "color": "#9ed7bd",
      "games": [
        {"key": "osrs", "name": "Old School RuneScape", "daily": 3, "weekly": 1}
      ]
    }
  ]
}
```

Each `daily` and `weekly` value is the number of **completed quest cycles** in that game, not a point score or streak. Keep `key` stable even if a game’s display `name` changes. The site totals and sorts players in the browser; it does not collect or send anything. Only add scores that players have chosen to publish. Avoid publishing local quest descriptions, detailed history, or other personal data.

GitHub Pages is static and cannot receive submissions itself. The connected receiver in [`leaderboard/`](../leaderboard/README.md) uses per-person invite codes. The desktop app shows a review of the exact counts before uploading, and keeps its local journal unchanged. The page remains a labeled preview until the first real upload.

## Local preview

From the repository root, run `python -m http.server 8765 --directory docs`, then visit `http://localhost:8765/` and use **See the standings**. Opening `standings.html` directly from disk will block the JSON fetch in most browsers.
