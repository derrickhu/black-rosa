const { collection } = require('./db');
const { requireUser } = require('./auth');

const COLLECTION = 'mails';
const GOODS = ['ink', 'stamina', 'diamond', 'wood', 'silver', 'gold', 'royal', 'cannon'];
const CHESTS = ['wood', 'silver', 'gold', 'royal', 'cannon'];
const EXPIRE_MS = 30 * 24 * 60 * 60 * 1000;

function clampReward(key, n) {
  const v = Math.floor(Number(n) || 0);
  if (v <= 0) return 0;
  if (CHESTS.indexOf(key) >= 0) return Math.min(v, 20);
  return Math.min(v, 99999);
}

function packRewards(raw) {
  const rewards = {};
  const src = raw || {};
  GOODS.forEach((key) => {
    const n = clampReward(key, src[key]);
    if (n > 0) rewards[key] = n;
  });
  return rewards;
}

function flatten(row) {
  const rewards = row.rewards || {};
  const out = {
    id: row._id || '',
    title: row.title || '',
    body: row.body || '',
    expireAt: Number(row.expireAt) || 0,
  };
  GOODS.forEach((key) => {
    out[key] = Number(rewards[key]) || 0;
  });
  return out;
}

function stillPending(row, now) {
  if (!row || row.status !== 'pending') return false;
  if (row.expireAt && Number(row.expireAt) <= now) return false;
  return true;
}

function duplicateMail(error) {
  const text = String((error && (error.code || error.message)) || error || '');
  return /dup|E11000|already exists|唯一/i.test(text);
}

// 同一来源单号只落一封。返回 sent / dup；写库失败会抛出去，方便结算重试。
async function sendMailOnce(input) {
  const userId = String((input && input.userId) || '').trim();
  const source = String((input && input.source) || 'ops');
  const sourceId = String((input && input.sourceId) || '').trim();
  if (!userId || !sourceId) return 'dup';
  const rewards = packRewards(input.rewards);
  if (Object.keys(rewards).length === 0) return 'dup';
  const dedupeKey = `${source}:${sourceId}`;
  const col = collection(COLLECTION);
  try {
    const existed = await col.where({ dedupeKey }).limit(1).get();
    if (existed && Array.isArray(existed.data) && existed.data.length > 0) return 'dup';
  } catch (error) {
    console.warn('[mail] dedupe query failed:', error && error.message ? error.message : error);
  }
  const now = input.now || Date.now();
  try {
    await col.add({
      userId,
      dedupeKey,
      source,
      title: String(input.title || '邮件').slice(0, 40),
      body: String(input.body || '').slice(0, 200),
      rewards,
      status: 'pending',
      createdAt: now,
      expireAt: now + EXPIRE_MS,
    });
    return 'sent';
  } catch (error) {
    if (duplicateMail(error)) return 'dup';
    throw error;
  }
}

// 微信礼包和以后的活动都走这里。失败只记一笔，不把调用方拖垮。
async function sendMail(input) {
  try {
    return (await sendMailOnce(input)) === 'sent';
  } catch (error) {
    console.warn('[mail] add failed:', error && error.message ? error.message : error);
    return false;
  }
}

async function handleListMail(req) {
  const user = requireUser(req);
  const now = Date.now();
  const res = await collection(COLLECTION).where({ userId: user.userId, status: 'pending' }).limit(30).get();
  const rows = ((res && res.data) || [])
    .filter((row) => stillPending(row, now))
    .sort((a, b) => (Number(b.createdAt) || 0) - (Number(a.createdAt) || 0));
  return { mails: rows.map(flatten).filter((row) => row.id) };
}

async function handleClaimMail(req) {
  const user = requireUser(req);
  const body = req.body || {};
  const ids = Array.isArray(body.ids) ? body.ids.map((v) => String(v || '')).filter(Boolean).slice(0, 30) : [];
  if (ids.length === 0) return { mails: [] };

  const now = Date.now();
  const col = collection(COLLECTION);
  const claimed = [];
  for (const id of ids) {
    try {
      const docRes = await col.doc(id).get();
      const doc = docRes && docRes.data && (Array.isArray(docRes.data) ? docRes.data[0] : docRes.data);
      if (!doc || doc.userId !== user.userId || !stillPending(doc, now)) continue;
      await col.doc(id).update({ status: 'claimed', claimedAt: now });
      claimed.push(flatten({ ...doc, _id: id }));
    } catch (error) {
      console.warn('[mail] claim failed:', id, error && error.message ? error.message : error);
    }
  }
  return { mails: claimed };
}

module.exports = {
  sendMail,
  sendMailOnce,
  handleListMail,
  handleClaimMail,
};
