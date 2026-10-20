const board = document.querySelector('#leaderboard');
const filter = document.querySelector('#game-filter');
const statusText = document.querySelector('#board-status-text');
let players = [];
let preview = false;

function cleanData(data) {
  if (!data || data.version !== 1 || !Array.isArray(data.players)) throw new Error('Unsupported leaderboard data');
  return data.players.filter(player => player && typeof player.name === 'string' && player.name.trim())
    .map(player => ({
      name: player.name.trim().slice(0, 40),
      tag: typeof player.tag === 'string' ? player.tag.trim().slice(0, 40) : '',
      color: /^#[0-9a-f]{6}$/i.test(player.color) ? player.color : '#a8d5bb',
      games: Array.isArray(player.games) ? player.games.filter(game => game && typeof game.key === 'string' && typeof game.name === 'string')
        .map(game => ({key: game.key.trim().slice(0, 40), name: game.name.trim().slice(0, 50), daily: validCount(game.daily), weekly: validCount(game.weekly)})) : []
    }));
}

function validCount(value) { return Number.isSafeInteger(value) && value >= 0 ? value : 0; }
function total(player, key) {
  const games = key === 'all' ? player.games : player.games.filter(game => game.key === key);
  return games.reduce((sum, game) => ({daily: sum.daily + game.daily, weekly: sum.weekly + game.weekly}), {daily: 0, weekly: 0});
}
function textNode(tag, className, value) {
  const el = document.createElement(tag);
  el.className = className;
  el.textContent = value;
  return el;
}

function render() {
  const key = filter.value;
  const ranked = players.map(player => ({...player, counts: total(player, key)}))
    .filter(player => player.counts.daily + player.counts.weekly > 0)
    .sort((a, b) => (b.counts.daily + b.counts.weekly) - (a.counts.daily + a.counts.weekly) || a.name.localeCompare(b.name));
  const aggregate = ranked.reduce((sum, player) => ({daily: sum.daily + player.counts.daily, weekly: sum.weekly + player.counts.weekly}), {daily: 0, weekly: 0});
  document.querySelector('#total-quests').textContent = (aggregate.daily + aggregate.weekly).toLocaleString();
  document.querySelector('#total-daily').textContent = aggregate.daily.toLocaleString();
  document.querySelector('#total-weekly').textContent = aggregate.weekly.toLocaleString();
  board.replaceChildren();
  document.querySelector('#empty-state').hidden = ranked.length !== 0;
  ranked.forEach((player, index) => {
    const row = document.createElement('li');
    row.append(textNode('span', 'rank', String(index + 1).padStart(2, '0')));
    const avatar = textNode('span', 'player-avatar', player.name.charAt(0).toUpperCase());
    avatar.style.background = player.color;
    row.append(avatar);
    const identity = document.createElement('div');
    identity.append(textNode('span', 'player-name', player.name), textNode('span', 'player-tag', player.tag));
    row.append(identity);
    const chips = document.createElement('div');
    chips.className = 'game-chips';
    player.games.filter(game => key === 'all' || game.key === key).forEach(game => chips.append(textNode('span', 'game-chip', game.name)));
    row.append(chips);
    const score = document.createElement('div');
    score.className = 'score';
    score.append(textNode('strong', '', (player.counts.daily + player.counts.weekly).toLocaleString()), textNode('span', '', `${player.counts.daily} daily · ${player.counts.weekly} weekly`));
    row.append(score);
    board.append(row);
  });
  statusText.textContent = preview ? 'Fictional preview · no uploads' : `${ranked.length} adventurer${ranked.length === 1 ? '' : 's'} on the board`;
}

async function fetchData(path) {
  const response = await fetch(path, {cache: 'no-store'});
  if (!response.ok) throw new Error(`Could not load ${path}`);
  return cleanData(await response.json());
}

async function load() {
  try {
    const configResponse = await fetch('data/service.json', {cache: 'no-store'});
    if (!configResponse.ok) throw new Error('Could not load leaderboard configuration');
    const config = await configResponse.json();
    if (config.version !== 1) throw new Error('Unsupported leaderboard configuration');
    if (config.endpoint) {
      const endpoint = new URL(config.endpoint);
      if (endpoint.protocol !== 'https:' || !endpoint.hostname.endsWith('.workers.dev') || endpoint.pathname !== '/') throw new Error('Invalid leaderboard endpoint');
      players = await fetchData(new URL('leaderboard', endpoint).href);
    } else {
      players = await fetchData('data/leaderboard.json');
    }
    if (players.length === 0) {
      players = await fetchData('data/demo.json');
      preview = true;
      document.querySelector('#preview-notice').hidden = false;
    }
    const games = new Map();
    players.forEach(player => player.games.forEach(game => games.set(game.key, game.name)));
    [...games].sort((a, b) => a[1].localeCompare(b[1])).forEach(([key, name]) => {
      const option = document.createElement('option');
      option.value = key;
      option.textContent = name;
      filter.append(option);
    });
    render();
  } catch (error) {
    statusText.textContent = 'Standings unavailable';
    document.querySelector('#empty-state').hidden = false;
    document.querySelector('#empty-state p').textContent = 'The leaderboard data could not be loaded. Please try refreshing the page.';
    document.querySelectorAll('.stat-card strong').forEach(el => { el.textContent = '—'; });
    console.error(error);
  }
}

filter.addEventListener('change', render);
load();
