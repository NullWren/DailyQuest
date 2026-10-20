const SITE_ORIGIN = 'https://nullwren.github.io';
const COLORS = ['#b5d6bd', '#e2bc8b', '#c7b7db', '#9fc7d5', '#e5b29f', '#bddba4'];

function json(value, status = 200, publicRead = false) {
  return new Response(JSON.stringify(value), {
    status,
    headers: {
      'content-type': 'application/json; charset=utf-8',
      'cache-control': publicRead ? 'public, max-age=30' : 'no-store',
      ...(publicRead ? {'access-control-allow-origin': SITE_ORIGIN, 'vary': 'Origin'} : {})
    }
  });
}

function validUpload(value) {
  if (!value || value.version !== 1 || typeof value.nickname !== 'string' || typeof value.inviteCode !== 'string' || !Array.isArray(value.games)) return null;
  const nickname = value.nickname.trim();
  if (nickname.length < 1 || nickname.length > 32 || /[\u0000-\u001f\u007f<>]/.test(nickname)) return null;
  if (!/^[A-Za-z0-9_-]{24,128}$/.test(value.inviteCode)) return null;
  if (value.games.length < 1 || value.games.length > 100) return null;
  const keys = new Set();
  const games = [];
  for (const game of value.games) {
    if (!game || typeof game.key !== 'string' || !/^[a-z0-9][a-z0-9-]{0,39}$/.test(game.key) || keys.has(game.key)) return null;
    if (typeof game.name !== 'string' || game.name.trim().length < 1 || game.name.trim().length > 50 || /[\u0000-\u001f\u007f<>]/.test(game.name)) return null;
    if (![game.daily, game.weekly].every(n => Number.isSafeInteger(n) && n >= 0 && n <= 1000000)) return null;
    keys.add(game.key);
    games.push({key: game.key, name: game.name.trim(), daily: game.daily, weekly: game.weekly});
  }
  return {nickname, inviteCode: value.inviteCode, games};
}

async function codeHash(code) {
  const bytes = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(code));
  return [...new Uint8Array(bytes)].map(byte => byte.toString(16).padStart(2, '0')).join('');
}

async function getLeaderboard(env) {
  const {results} = await env.DB.prepare('SELECT id, nickname, score_json, updated_at FROM players ORDER BY updated_at DESC LIMIT 200').all();
  const players = results.map(row => {
    const games = JSON.parse(row.score_json);
    let index = 0;
    for (const character of row.id) index = (index * 31 + character.charCodeAt(0)) % COLORS.length;
    return {name: row.nickname, tag: 'member of the flock', color: COLORS[index], games};
  });
  return json({version: 1, updatedAt: results[0]?.updated_at ?? null, players}, 200, true);
}

async function upload(request, env) {
  if (!request.headers.get('content-type')?.toLowerCase().startsWith('application/json')) return json({error: 'Expected JSON'}, 415);
  const body = await request.text();
  if (new TextEncoder().encode(body).length > 16384) return json({error: 'Upload too large'}, 413);
  let parsed;
  try { parsed = validUpload(JSON.parse(body)); } catch { return json({error: 'Invalid JSON'}, 400); }
  if (!parsed) return json({error: 'Invalid tally'}, 400);
  const digest = await codeHash(parsed.inviteCode);
  const invite = await env.DB.prepare('SELECT player_id FROM invites WHERE code_hash = ? AND revoked = 0').bind(digest).first();
  if (!invite) return json({error: 'Invite code not accepted'}, 401);
  const prior = await env.DB.prepare('SELECT updated_at FROM players WHERE id = ?').bind(invite.player_id).first();
  if (prior && Date.now() - Date.parse(prior.updated_at) < 60000) return json({error: 'Wait before uploading again'}, 429);
  const now = new Date().toISOString();
  await env.DB.prepare('INSERT INTO players (id, nickname, score_json, updated_at) VALUES (?, ?, ?, ?) ON CONFLICT(id) DO UPDATE SET nickname = excluded.nickname, score_json = excluded.score_json, updated_at = excluded.updated_at')
    .bind(invite.player_id, parsed.nickname, JSON.stringify(parsed.games), now).run();
  return json({ok: true, updatedAt: now});
}

export default {
  async fetch(request, env) {
    try {
      const url = new URL(request.url);
      if (request.method === 'GET' && url.pathname === '/leaderboard') return await getLeaderboard(env);
      if (request.method === 'POST' && url.pathname === '/upload') return await upload(request, env);
      return json({error: 'Not found'}, 404);
    } catch {
      return json({error: 'Leaderboard unavailable'}, 503);
    }
  }
};

export {validUpload, codeHash};
