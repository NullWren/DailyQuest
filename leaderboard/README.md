# Optional leaderboard receiver

WrenFeed remains offline during normal checklist use. A player can review all-time daily and weekly completion counts by game in **Settings → Leaderboard upload…**, then explicitly upload a chosen public nickname and those totals with a private invite code. The journal, quest descriptions, subtasks, and dated history are not sent. An ungrouped quest uses its quest name as the game label.

The public site remains on [GitHub Pages](https://nullwren.github.io/WrenFeed/). This folder contains the optional Cloudflare Worker and D1 schema that receive uploads and serve standings. Until a receiver is connected, the site shows a clearly labeled demo and the app explains that uploads are unavailable.

Local checks: `node --test leaderboard/worker/test.mjs` and, on Windows, `./test-leaderboard.ps1` from the repository root.
