const { getCollection, getRankingCollection } = require('./db');
const { sendMailOnce } = require('./mail');

const MAX_SAFE_INK = 500000;
const MAX_SAFE_STAMINA = 3000;
const MAX_SAFE_DIAMOND = 10000;
const MAX_SAFE_FOIL = 25000;
const MAX_SAFE_CARD_STACK = 5000;
const MAX_SAFE_SEAL_STACK = 1000;

function inspectMetaPayload(payload) {
  const out = {
    parsed: false,
    tampered: false,
    reasons: [],
    tokenToRecycle: 0,
    isFreshWipe: false,
    cleared: 0,
  };
  if (!payload || typeof payload.meta !== 'string' || !payload.meta) return out;
  let m;
  try {
    m = JSON.parse(payload.meta);
  } catch (_) {
    return out;
  }
  if (!m || typeof m !== 'object') return out;
  out.parsed = true;

  const ink = Number(m.Ink) || 0;
  const stamina = Number(m.Stamina) || 0;
  const diamond = Number(m.Diamond) || 0;
  const foil = Number(m.Foil) || 0;
  const stars = Array.isArray(m.Stars) ? m.Stars : [];
  const cleared = stars.reduce((acc, v) => acc + ((Number(v) || 0) > 0 ? 1 : 0), 0);
  out.cleared = cleared;

  if (ink > MAX_SAFE_INK) out.reasons.push(`Ink=${ink}`);
  if (stamina > MAX_SAFE_STAMINA) out.reasons.push(`Stamina=${stamina}`);
  if (diamond > MAX_SAFE_DIAMOND) out.reasons.push(`Diamond=${diamond}`);
  if (foil > MAX_SAFE_FOIL) out.reasons.push(`Foil=${foil}`);

  if (Array.isArray(m.ItemCards)) {
    const maxCard = m.ItemCards.reduce((mx, v) => Math.max(mx, Number(v) || 0), 0);
    if (maxCard > MAX_SAFE_CARD_STACK) out.reasons.push(`ItemCardsMax=${maxCard}`);
  }
  if (Array.isArray(m.SealBag)) {
    const maxSeal = m.SealBag.reduce((mx, v) => Math.max(mx, Number(v) || 0), 0);
    if (maxSeal > MAX_SAFE_SEAL_STACK) out.reasons.push(`SealBagMax=${maxSeal}`);
  }

  out.tampered = out.reasons.length > 0;

  const rawTok = Math.max(Math.floor(Number(m.Token) || 0), Math.floor(Number(m.RecycledToken) || 0));
  if (!out.tampered && rawTok > 0) {
    out.tokenToRecycle = Math.min(rawTok, 20000);
  }

  if (!out.tampered && cleared <= 1 && ink <= 500 && stamina <= 50 && diamond <= 100) {
    out.isFreshWipe = true;
  }

  return out;
}

async function notifyTamperAndBlockRank(userId, reasons, now) {
  if (!userId) return;
  const ts = now || Date.now();
  try {
    const rankCol = getRankingCollection();
    const res = await rankCol.where({ userId }).limit(1).get();
    const doc = (res && Array.isArray(res.data) && res.data[0]) || null;
    if (doc && doc._id && !doc.blocked) {
      await rankCol.doc(doc._id).update({
        blocked: true,
        blockedAt: ts,
        cheatReasons: Array.isArray(reasons) ? reasons.slice(0, 8) : [],
      });
    }
  } catch (err) {
    console.warn('[anticheat] block rank failed:', err && err.message ? err.message : err);
  }

  try {
    await sendMailOnce({
      userId,
      source: 'anticheat',
      sourceId: `tamper_shield_v1:${userId}`,
      title: '排行榜数据异常通知',
      body: '系统检测到当前存档存在异常资源数据修改，已自动屏蔽本账号的通关榜与无尽周榜排榜资格。单机闯关不受影响；如需恢复正常排榜，可在设置内重开新档。',
      rewards: {},
      allowEmpty: true,
      now: ts,
    });
  } catch (err) {
    console.warn('[anticheat] send tamper mail failed:', err && err.message ? err.message : err);
  }
}

async function unblockRankOnFreshWipe(userId, now) {
  if (!userId) return;
  const ts = now || Date.now();
  try {
    const rankCol = getRankingCollection();
    const res = await rankCol.where({ userId }).limit(1).get();
    const doc = (res && Array.isArray(res.data) && res.data[0]) || null;
    if (doc && doc._id && doc.blocked) {
      await rankCol.doc(doc._id).update({
        blocked: false,
        unblockedAt: ts,
        cleared: 0,
        endless: 0,
        weekly: 0,
        daily: 0,
      });
    }
  } catch (err) {
    console.warn('[anticheat] unblock rank failed:', err && err.message ? err.message : err);
  }
}

async function recycleTokenViaMail(userId, tokenAmount, now) {
  const amt = Math.floor(Number(tokenAmount) || 0);
  if (!userId || amt <= 0) return false;
  try {
    const state = await sendMailOnce({
      userId,
      source: 'ops',
      sourceId: `token_recycle_v1:${userId}`,
      title: '活动币折算通知',
      body: `「招财进宝」活动币已停用并按 1:1 折算为灵墨（共 ${amt} 灵墨），原聚宝盆档位已同步改为灵墨奖励，请查收附件。`,
      rewards: { ink: amt },
      now: now || Date.now(),
    });
    return state === 'sent';
  } catch (err) {
    console.warn('[anticheat] token recycle mail failed:', err && err.message ? err.message : err);
    return false;
  }
}

async function isUserCheatFlagged(userId) {
  if (!userId) return { flagged: false, reasons: [] };
  try {
    const res = await getCollection().where({ userId }).limit(1).get();
    const doc = (res && Array.isArray(res.data) && res.data[0]) || null;
    if (!doc) return { flagged: false, reasons: [] };
    if (doc.cheatFlag === true) {
      return { flagged: true, reasons: doc.cheatReasons || ['cheatFlag'] };
    }
    const check = inspectMetaPayload(doc.payload);
    if (check.tampered) {
      await getCollection().doc(doc._id).update({
        cheatFlag: true,
        cheatReasons: check.reasons,
        cheatDetectedAt: Date.now(),
      });
      return { flagged: true, reasons: check.reasons };
    }
  } catch (err) {
    console.warn('[anticheat] check user failed:', err && err.message ? err.message : err);
  }
  return { flagged: false, reasons: [] };
}

module.exports = {
  inspectMetaPayload,
  notifyTamperAndBlockRank,
  unblockRankOnFreshWipe,
  recycleTokenViaMail,
  isUserCheatFlagged,
};
