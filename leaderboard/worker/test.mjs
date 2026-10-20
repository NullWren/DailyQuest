import assert from 'node:assert/strict';
import {test} from 'node:test';
import {webcrypto} from 'node:crypto';
import worker, {codeHash, validUpload} from './src/worker.mjs';

globalThis.crypto ??= webcrypto;

function database(hash) {
  const players = new Map();
  return {
    prepare(sql) {
      return {
        all: async () => ({results: [...players].map(([id, player]) => ({id, ...player})).sort((a, b) => b.updated_at.localeCompare(a.updated_at))}),
        bind(...values) {
          return {
            first: async () => sql.startsWith('SELECT player_id') ? values[0] === hash ? {player_id: 'player-1'} : null : players.get(values[0]) ?? null,
            run: async () => { players.set(values[0], {nickname: values[1], score_json: values[2], updated_at: values[3]}); }
          };
        }
      };
    }
  };
}

test('receiver accepts one private code and exposes only public counts', async () => {
  const code = 'a'.repeat(32);
  const env = {DB: database(await codeHash(code))};
  const payload = {version: 1, nickname: 'Wren', inviteCode: code, games: [{key: 'osrs', name: 'OSRS', daily: 9, weekly: 2}]};
  const post = value => worker.fetch(new Request('https://example.workers.dev/upload', {method: 'POST', headers: {'content-type': 'application/json'}, body: JSON.stringify(value)}), env);
  assert.equal((await post({...payload, inviteCode: 'b'.repeat(32)})).status, 401);
  assert.equal((await post(payload)).status, 200);
  assert.equal((await post(payload)).status, 429);
  const response = await worker.fetch(new Request('https://example.workers.dev/leaderboard'), env);
  const body = await response.text();
  assert.equal(response.headers.get('access-control-allow-origin'), 'https://nullwren.github.io');
  assert.ok(!body.includes(code));
  assert.deepEqual(JSON.parse(body).players[0].games, payload.games);
});

test('receiver rejects malformed or duplicated game totals', () => {
  const payload = {version: 1, nickname: 'Wren', inviteCode: 'a'.repeat(32), games: [{key: 'osrs', name: 'OSRS', daily: 1, weekly: 1}]};
  assert.ok(validUpload(payload));
  assert.equal(validUpload({...payload, games: [...payload.games, ...payload.games]}), null);
  assert.equal(validUpload({...payload, games: [{...payload.games[0], daily: -1}]}), null);
  assert.equal(validUpload({...payload, nickname: '<script>'}), null);
});
