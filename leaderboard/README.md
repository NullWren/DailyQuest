# Optional leaderboard receiver

The WrenFeed desktop app remains offline until someone explicitly presses **Settings → Leaderboard upload… → Upload totals**. Its review screen shows the exact game labels and all-time daily and weekly completion counts it will send. It sends a chosen public nickname, those counts, and one private invite code. It does **not** send the journal file, dates, descriptions, subtasks, or detailed history. An ungrouped quest uses its quest name as the game label. Invite codes are encrypted for the current Windows account on the sender's PC and are not included in journal backups.

[GitHub Pages](https://nullwren.github.io/WrenFeed/) remains the public website. A small Cloudflare Worker receives uploads and stores the latest tally for each invited person in D1. The page requests public standings from that Worker. The Worker and database must be deployed before uploads work; [`docs/data/service.json`](../docs/data/service.json) has a `null` endpoint until then. Demo players appear only while no real players exist.

## Deploy the receiver

This is a one-time task for the leaderboard owner. Friends do not need Cloudflare accounts. Use a Cloudflare account with Workers and D1 enabled, plus Node.js so `npx` can run Wrangler. Cloudflare's [D1 setup guide](https://developers.cloudflare.com/d1/get-started/) covers the account and database setup.

1. In a terminal, change into `leaderboard/worker` and run `npx wrangler login`. Sign in to your own Cloudflare account. Do not put a Cloudflare token in this repository.
2. Run `npx wrangler d1 create wrenfeed-leaderboard`. Copy the returned database ID into `wrangler.toml`, replacing `REPLACE_WITH_YOUR_D1_DATABASE_ID`.
3. Run `npx wrangler d1 migrations apply wrenfeed-leaderboard --remote` to create the `invites` and `players` tables.
4. Run `npx wrangler deploy`. Note the resulting `https://…workers.dev` URL, including its final `/`.
5. Open [`docs/data/service.json`](../docs/data/service.json), set `endpoint` to that URL, and publish the change to GitHub. After Pages updates, the app can find the receiver when Upload is pressed, and the website can read real standings.

To create an invite, run [`new-invite.ps1`](new-invite.ps1) on Windows. It prints a code to share **privately with one person** and an `INSERT INTO invites ...` SQL statement. Run that SQL in Cloudflare's D1 console for the `wrenfeed-leaderboard` database. Do not commit the invite code or paste it into a public issue. Make a separate code for each person. Codes are random; D1 stores only their SHA-256 hashes. To revoke a code, set its `revoked` field to `1` in the `invites` table. Existing public totals can be removed separately from `players` if needed.

The Worker accepts one full tally per upload and replaces that person's previous tally, so retries do not double-count. It limits a code to one upload per minute. The site displays only public nicknames and totals; each player can self-report counts, so this is a friendly leaderboard rather than an anti-cheat system. Keep code ownership and any moderation of nicknames with the leaderboard owner.

## Checks

From the repository root, run `node --test leaderboard/worker/test.mjs` for receiver validation and `./test-leaderboard.ps1` on Windows for the desktop tally and encrypted invite storage. These use local test data and do not contact Cloudflare.
