# WrenFeed leaderboard site

This folder is a standalone static site for GitHub Pages. It does not alter the WrenFeed desktop app, its local data, or its release ZIP. Publish the `docs/` folder from the `main` branch in the repository's **Settings → Pages**.

The first page is a visual preview. No WrenFeed data is uploaded. It reads `data/leaderboard.json` first. While that file has no players, it shows clearly labeled fictional players from `data/demo.json`. As soon as `leaderboard.json` has at least one player, the demo file is ignored entirely. A filtered game with no completions shows an empty state.

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

This schema leaves room for a future upload service. GitHub Pages is static and cannot receive submissions itself. An opt-in desktop upload would need an authenticated endpoint and a clear review of the exact counts being shared. That service is not part of this preview.

## Local preview

From the repository root, run `python -m http.server 8765 --directory docs`, then visit `http://localhost:8765/`. Opening `index.html` directly from disk will block the JSON fetch in most browsers.
