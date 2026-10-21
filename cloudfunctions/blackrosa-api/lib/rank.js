/**
 * 排行榜，照 hotpot-api 的 rank.js。四张榜共用一条记录：
 *   stage   已通关关数
 *   endless 无尽模式历史最好打完几波（老客户端的「无尽榜」）
 *   weekly  无尽模式本周最好（按北京时间的周号，换周清榜）
 *   daily   每日残局当天最好（按北京时间的日期，换天清榜）
 *
 * 数据模型（collection: rankings，每人一条）：
 *   {
 *     _id,
 *     userId, platform,
 *     cleared: number,        // 已通关关数
 *     reachedAt: number (ms), // 第一次到达这个关数的时间，同分先到者排前面
 *     endless: number,        // 无尽最好成绩（波数）
 *     endlessAt: number (ms), // 第一次打到这个波数的时间
 *     week, weekly, weeklyAt, // 周号（2024-01-01 周一起算的整周数）、本周最好、达成时间
 *     day, daily, dailyAt,    // 日期 yyyymmdd、当天最好、达成时间
 *     displayName, avatarUrl,
 *     createdAt, updatedAt
 *   }
 *
 * 索引：userId 唯一；cleared desc + reachedAt asc；endless desc + endlessAt asc；
 *       week + weekly desc + weeklyAt asc；day + daily desc + dailyAt asc。
 */

const crypto = require('crypto');
const { requireUser } = require('./auth');
const { getDb, getRankingCollection, collection } = require('./db');
const { httpError } = require('./http');
const { readEnvPrefer, gameKeyUpper } = require('./config');
const { sendMailOnce } = require('./mail');

const DEFAULT_MAX_CLEARED = 90;
const DEFAULT_MAX_ENDLESS = 70;
const DEFAULT_MAX_DAILY = 20;
const DEFAULT_LIST_LIMIT = 50;
const MAX_LIST_LIMIT = 100;

// 每张榜按哪个字段排、同分看哪个时间；周榜、日榜再按 key 字段只看当期。
const BOARDS = {
  stage: { score: 'cleared', at: 'reachedAt' },
  endless: { score: 'endless', at: 'endlessAt' },
  weekly: { score: 'weekly', at: 'weeklyAt', key: 'week' },
  daily: { score: 'daily', at: 'dailyAt', key: 'day' },
};

const DAY_MS = 24 * 3600 * 1000;
const BEIJING_MS = 8 * 3600 * 1000;
const WEEK_ZERO = Date.UTC(2024, 0, 1);

// 北京时间的今天 yyyymmdd 和周号，和客户端 DayClock 同一套算法。
function serverPeriod(now) {
  const t = new Date(now + BEIJING_MS);
  const day = t.getUTCFullYear() * 10000 + (t.getUTCMonth() + 1) * 100 + t.getUTCDate();
  const midnight = Date.UTC(t.getUTCFullYear(), t.getUTCMonth(), t.getUTCDate());
  const week = Math.floor((midnight - WEEK_ZERO) / DAY_MS / 7);
  return { day, week };
}

// 跨零点那一下客户端可能还停在上一期，只收当期和上一期。
function samePeriod(value, current, previous) {
  return value === current || value === previous;
}

function previousDay(now) {
  return serverPeriod(now - DAY_MS).day;
}

function envInt(suffix, fallback) {
  const n = Number(readEnvPrefer(`${gameKeyUpper()}_${suffix}`));
  return Number.isFinite(n) && n > 0 ? Math.floor(n) : fallback;
}

// GM / 测试账号：逗号分隔的 userId，不写榜，免得一键全通关的进度挂在榜首。
function blockedUsers() {
  return new Set(
    readEnvPrefer(`${gameKeyUpper()}_RANK_BLOCKED_UIDS`)
      .split(',')
      .map((s) => s.trim())
      .filter(Boolean),
  );
}

function normalizeCleared(value) {
  const n = Number(value);
  if (!Number.isFinite(n) || n < 0) {
    throw httpError(400, 'BAD_CLEARED', 'cleared 必须为非负整数');
  }
  const cleared = Math.floor(n);
  const max = envInt('RANK_MAX_CLEARED', DEFAULT_MAX_CLEARED);
  if (cleared > max) {
    throw httpError(400, 'CLEARED_TOO_HIGH', `cleared 超出上限: ${cleared} > ${max}`);
  }
  return cleared;
}

// 老版本客户端不带 endless，当 0。
function normalizeEndless(value) {
  if (value === undefined || value === null || value === '') return 0;
  const n = Number(value);
  if (!Number.isFinite(n) || n < 0) {
    throw httpError(400, 'BAD_ENDLESS', 'endless 必须为非负整数');
  }
  const endless = Math.floor(n);
  const max = envInt('RANK_MAX_ENDLESS', DEFAULT_MAX_ENDLESS);
  if (endless > max) {
    throw httpError(400, 'ENDLESS_TOO_HIGH', `endless 超出上限: ${endless} > ${max}`);
  }
  return endless;
}

// 周榜、日榜的成绩：缺省当 0，超上限拒收。
function normalizePeriodScore(value, max, code) {
  if (value === undefined || value === null || value === '') return 0;
  const n = Number(value);
  if (!Number.isFinite(n) || n < 0) throw httpError(400, `BAD_${code}`, `${code.toLowerCase()} 必须为非负整数`);
  const score = Math.floor(n);
  if (score > max) throw httpError(400, `${code}_TOO_HIGH`, `${code.toLowerCase()} 超出上限: ${score} > ${max}`);
  return score;
}

function normalizeBoard(value) {
  return BOARDS[value] || BOARDS.stage;
}

// 周榜、日榜只看当期。
function periodScope(board, now) {
  if (!board.key) return {};
  const p = serverPeriod(now);
  return { [board.key]: board.key === 'week' ? p.week : p.day };
}

function normalizeLimit(value) {
  const n = Number(value);
  if (!Number.isFinite(n) || n <= 0) return DEFAULT_LIST_LIMIT;
  return Math.min(Math.floor(n), MAX_LIST_LIMIT);
}

function displayNameForUser(userId) {
  const digest = crypto.createHash('sha1').update(String(userId)).digest('hex');
  const suffix = String(parseInt(digest.slice(0, 8), 16) % 10000).padStart(4, '0');
  return `墨客${suffix}`;
}

function sanitizeDisplayName(value, fallback) {
  const text = String(value || '').trim().replace(/[\r\n\t]/g, ' ').slice(0, 16);
  // 微信拒绝授权时会给一个统一的「微信用户」，当没给。
  if (!text || text === '微信用户') return fallback;
  return text;
}

function sanitizeAvatarUrl(value, fallback) {
  const text = String(value || '').trim();
  if (!text || text.length > 1024 || !/^https?:\/\//i.test(text)) return fallback || '';
  return text;
}

function publicRecord(doc, userId, rank) {
  if (!doc) return null;
  const p = serverPeriod(Date.now());
  return {
    rank: rank || 0,
    cleared: Number(doc.cleared) || 0,
    endless: Number(doc.endless) || 0,
    weekly: Number(doc.week) === p.week ? Number(doc.weekly) || 0 : 0,
    daily: Number(doc.day) === p.day ? Number(doc.daily) || 0 : 0,
    displayName: doc.displayName || displayNameForUser(doc.userId || ''),
    avatarUrl: doc.avatarUrl || '',
    isMe: !!userId && doc.userId === userId,
  };
}

// 编辑器用 anon 身份联调。微信玩家看不到 anon 记录，编辑器里能看到全部，
// 这样本地调界面有数据可看，线上榜也不会混进测试号。
function audience(platform) {
  return platform === 'anon' ? {} : { platform: getDb().command.neq('anon') };
}

async function findMine(col, userId, platform, board, now) {
  const res = await col.where({ userId }).limit(1).get();
  const doc = (res && Array.isArray(res.data) && res.data[0]) || null;
  if (!doc || !(Number(doc[board.score]) > 0)) return null;
  const period = periodScope(board, now);
  if (board.key && Number(doc[board.key]) !== period[board.key]) return null;
  const _ = getDb().command;
  const score = Number(doc[board.score]);
  const at = Number(doc[board.at]) || 0;
  const scope = { ...audience(platform), ...period };
  // 周榜按波数分档：同一波都是同一个名次，和发奖一致。先到的人仍排在这一档前面。
  if (board.key === 'week') {
    const rank = await denseRank(col, scope, board, score);
    return { rank, doc };
  }
  const [ahead, tied] = await Promise.all([
    col.where({ ...scope, [board.score]: _.gt(score) }).count(),
    col.where({ ...scope, [board.score]: score, [board.at]: _.lt(at) }).count(),
  ]);
  const rank = (Number(ahead && ahead.total) || 0) + (Number(tied && tied.total) || 0) + 1;
  return { rank, doc };
}

async function denseRank(col, scope, board, score) {
  const _ = getDb().command;
  const seen = new Set();
  let skip = 0;
  while (skip < 500) {
    const res = await col
      .where({ ...scope, [board.score]: _.gt(score) })
      .orderBy(board.score, 'desc')
      .skip(skip)
      .limit(100)
      .get();
    const batch = (res && Array.isArray(res.data) && res.data) || [];
    for (const row of batch) seen.add(Math.floor(Number(row[board.score]) || 0));
    if (batch.length < 100) break;
    skip += batch.length;
  }
  return seen.size + 1;
}

function rankRows(rows, userId, board) {
  if (board.key !== 'week') return rows.map((doc, i) => publicRecord(doc, userId, i + 1));
  let rank = 0;
  let prev = null;
  return rows.map((doc) => {
    const score = Math.floor(Number(doc.weekly) || 0);
    if (prev === null || score !== prev) {
      rank += 1;
      prev = score;
    }
    return publicRecord(doc, userId, rank);
  });
}

// 无尽周榜奖励。按波数从高到低取前三档，同一波的人算同一名，都发这一档。
// 第一名炮台宝箱，第二名皇家宝箱，第三名金宝箱。编辑器账号不占名次。
const WEEKLY_PRIZES = [
  { name: '第一名', chest: '炮台宝箱', rewards: { cannon: 1 } },
  { name: '第二名', chest: '皇家宝箱', rewards: { royal: 1 } },
  { name: '第三名', chest: '金宝箱', rewards: { gold: 1 } },
];

function awardGroups(rows) {
  const blocked = blockedUsers();
  const groups = [];
  for (const doc of rows || []) {
    if (!doc || doc.platform === 'anon' || !doc.userId || blocked.has(doc.userId)) continue;
    const score = Math.floor(Number(doc.weekly) || 0);
    if (score <= 0) continue;
    const last = groups[groups.length - 1];
    if (last && last.score === score) {
      last.users.push(doc);
      continue;
    }
    if (groups.length >= WEEKLY_PRIZES.length) break;
    groups.push({ score, users: [doc] });
  }
  return groups;
}

async function loadAwardRows(week) {
  const col = getRankingCollection();
  const _ = getDb().command;
  const rows = [];
  const page = 100;
  let skip = 0;
  while (groupsComplete(rows) === false) {
    const res = await col
      .where({ week, weekly: _.gt(0) })
      .orderBy('weekly', 'desc')
      .orderBy('weeklyAt', 'asc')
      .skip(skip)
      .limit(page)
      .get();
    const batch = (res && Array.isArray(res.data) && res.data) || [];
    rows.push(...batch);
    if (batch.length < page) break;
    skip += batch.length;
    if (skip >= 2000) break;
  }
  return rows;
}

function groupsComplete(rows) {
  const groups = awardGroups(rows);
  if (groups.length < WEEKLY_PRIZES.length) return false;
  const last = groups[groups.length - 1];
  const tail = rows[rows.length - 1];
  // 最后一档可能被分页切开，下一批还是同一波就得接着读。
  return !tail || Math.floor(Number(tail.weekly) || 0) !== last.score;
}

async function awardWeek(week, now) {
  const groups = awardGroups(await loadAwardRows(week));
  let sent = 0;
  for (let i = 0; i < groups.length; i++) {
    const prize = WEEKLY_PRIZES[i];
    const tied = groups[i].users.length > 1;
    const title = tied ? `无尽周榜并列${prize.name}` : `无尽周榜${prize.name}`;
    const body = tied
      ? `上周无尽榜并列${prize.name}，${prize.chest}附上。`
      : `上周无尽榜${prize.name}，${prize.chest}附上。`;
    for (const doc of groups[i].users) {
      const state = await sendMailOnce({
        userId: doc.userId,
        source: 'rank',
        sourceId: `weekly:${week}:${doc.userId}`,
        title,
        body,
        rewards: prize.rewards,
        now,
      });
      if (state === 'sent') sent += 1;
    }
  }
  console.log('[rank] weekly award', week, 'tiers', groups.map((g) => g.users.length).join(','), 'sent', sent);
  return { week, tiers: groups.length, sent };
}

// 周一零点换周后发上一周。第一次跑只记下「从本周开始」，不补发更早的榜。
async function settleWeeklyAwards(now) {
  const due = serverPeriod(now || Date.now()).week - 1;
  if (due < 0) return { ok: true, skipped: 'none' };
  const col = collection('jobs');
  const res = await col.where({ kind: 'weekly-rank' }).limit(1).get();
  const mark = (res && Array.isArray(res.data) && res.data[0]) || null;
  if (!mark) {
    try {
      await col.add({ kind: 'weekly-rank', settledWeek: due, updatedAt: Date.now(), note: 'start' });
    } catch (error) {
      if (!/dup|E11000|already exists|唯一/i.test(String((error && error.message) || error))) throw error;
    }
    return { ok: true, primed: due };
  }
  const settled = Number(mark.settledWeek) || 0;
  if (settled >= due) return { ok: true, settledWeek: settled };
  const from = Math.max(settled + 1, due - 3);
  const weeks = [];
  for (let week = from; week <= due; week++) weeks.push(await awardWeek(week, now || Date.now()));
  await col.doc(mark._id).update({ settledWeek: due, updatedAt: Date.now() });
  return { ok: true, settledWeek: due, weeks };
}

async function settleWeeklyQuietly(now) {
  try {
    await settleWeeklyAwards(now);
  } catch (error) {
    console.warn('[rank] weekly award skipped:', error && error.message ? error.message : error);
  }
}

async function handleSubmit(req) {
  const { userId, platform } = requireUser(req);
  const body = req.body || {};
  const cleared = normalizeCleared(body.cleared);
  const endless = normalizeEndless(body.endless);
  const maxEndless = envInt('RANK_MAX_ENDLESS', DEFAULT_MAX_ENDLESS);
  const now = Date.now();
  const period = serverPeriod(now);
  // 期号对不上（改了系统时间、跨期太久）就当没报这一项。
  const week = Math.floor(Number(body.week) || 0);
  const day = Math.floor(Number(body.day) || 0);
  const weekly = samePeriod(week, period.week, period.week - 1)
    ? normalizePeriodScore(body.weekly, maxEndless, 'WEEKLY') : 0;
  const daily = samePeriod(day, period.day, previousDay(now))
    ? normalizePeriodScore(body.daily, DEFAULT_MAX_DAILY, 'DAILY') : 0;
  if (blockedUsers().has(userId)) {
    return { updated: false, reason: 'BLOCKED', record: null };
  }
  await settleWeeklyQuietly(now);

  const col = getRankingCollection();
  const res = await col.where({ userId }).limit(1).get();
  const existing = (res && Array.isArray(res.data) && res.data[0]) || null;
  const displayName = sanitizeDisplayName(
    body.displayName,
    (existing && existing.displayName) || displayNameForUser(userId),
  );
  const avatarUrl = sanitizeAvatarUrl(body.avatarUrl, (existing && existing.avatarUrl) || '');

  if (!existing) {
    if (cleared <= 0 && endless <= 0 && weekly <= 0 && daily <= 0) {
      return { updated: false, reason: 'NO_PROGRESS', record: null };
    }
    const docData = {
      userId,
      platform,
      cleared,
      reachedAt: now,
      endless,
      endlessAt: now,
      week: weekly > 0 ? week : 0,
      weekly,
      weeklyAt: now,
      day: daily > 0 ? day : 0,
      daily,
      dailyAt: now,
      displayName,
      avatarUrl,
      createdAt: now,
      updatedAt: now,
    };
    await col.add(docData);
    return { updated: true, mode: 'insert', record: publicRecord(docData, userId, 0) };
  }

  const patch = {};
  // 只升不降：清档重玩、换设备拿到旧档都不会把榜上的成绩拉下来。
  if (cleared > (Number(existing.cleared) || 0)) {
    patch.cleared = cleared;
    patch.reachedAt = now;
  }
  if (endless > (Number(existing.endless) || 0)) {
    patch.endless = endless;
    patch.endlessAt = now;
  }
  // 周榜、日榜：新的一期直接换；同一期只升不降；旧的一期不回写。
  const oldWeek = Number(existing.week) || 0;
  if (weekly > 0 && (week > oldWeek || (week === oldWeek && weekly > (Number(existing.weekly) || 0)))) {
    patch.week = week;
    patch.weekly = weekly;
    patch.weeklyAt = now;
  }
  const oldDay = Number(existing.day) || 0;
  if (daily > 0 && (day > oldDay || (day === oldDay && daily > (Number(existing.daily) || 0)))) {
    patch.day = day;
    patch.daily = daily;
    patch.dailyAt = now;
  }
  if (existing.displayName !== displayName) patch.displayName = displayName;
  if ((existing.avatarUrl || '') !== avatarUrl) patch.avatarUrl = avatarUrl;
  if (Object.keys(patch).length === 0) {
    return { updated: false, reason: 'NOT_BETTER', record: publicRecord(existing, userId, 0) };
  }
  patch.updatedAt = now;
  await col.doc(existing._id).update(patch);
  const scored = patch.cleared !== undefined || patch.endless !== undefined
    || patch.weekly !== undefined || patch.daily !== undefined;
  return {
    updated: true,
    mode: scored ? 'update' : 'profile_update',
    record: publicRecord({ ...existing, ...patch }, userId, 0),
  };
}

async function handleList(req) {
  const { userId, platform } = requireUser(req);
  const body = req.body || {};
  const limit = normalizeLimit(body.limit);
  const board = normalizeBoard(body.board);
  const now = Date.now();
  await settleWeeklyQuietly(now);
  const col = getRankingCollection();
  const res = await col
    .where({ ...audience(platform), ...periodScope(board, now), [board.score]: getDb().command.gt(0) })
    .orderBy(board.score, 'desc')
    .orderBy(board.at, 'asc')
    .limit(limit)
    .get();
  const rows = res && Array.isArray(res.data) ? res.data : [];
  const list = rankRows(rows, userId, board);
  const mine = await findMine(col, userId, platform, board, now);
  return { list, mine: mine ? publicRecord(mine.doc, userId, mine.rank) : null };
}

module.exports = {
  handleSubmit,
  handleList,
  awardGroups,
  settleWeeklyAwards,
};
