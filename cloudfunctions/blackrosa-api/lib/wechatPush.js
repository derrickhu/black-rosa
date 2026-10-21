const crypto = require('crypto');
const { collection } = require('./db');
const { requireUser } = require('./auth');
const { respond } = require('./http');
const { getWechatPushTokens } = require('./config');
const { sendMail } = require('./mail');

const SUCCESS = { ErrCode: 0, ErrMsg: 'Success' };
const PENDING_GIFT_COLLECTION = 'pendingWechatGifts';

async function handleWechatPushVerify(req) {
  const qs = req.query || {};
  const { signature, timestamp, nonce, echostr } = qs;
  if (!checkSignature(signature, timestamp, nonce)) {
    return respond(403, 'signature mismatch', { 'Content-Type': 'text/plain; charset=utf-8' });
  }
  return respond(200, echostr || '', { 'Content-Type': 'text/plain; charset=utf-8' });
}

async function handleWechatPushCallback(req) {
  const qs = req.query || {};
  if (!checkSignature(qs.signature, qs.timestamp, qs.nonce)) {
    return respond(403, { ErrCode: -1, ErrMsg: 'signature mismatch' });
  }

  let msg;
  try {
    msg = parsePostBody(readRawBody(req));
  } catch (error) {
    console.error('[wechatPush] parse failed:', error && error.message ? error.message : error);
    return respond(200, { ErrCode: -1, ErrMsg: 'Parse error' });
  }

  if (!msg || Object.keys(msg).length === 0) {
    return respond(200, { ErrCode: -1, ErrMsg: 'Empty message' });
  }

  const now = Date.now();
  const eventName = String(msg.Event || msg.event || '');
  await recordPushMessage(msg, eventName, now);

  if (msg.MsgType === 'event' && eventName === 'minigame_deliver_goods') {
    await recordDeliverGoods(msg.MiniGame || {}, now);
  } else if (msg.MsgType === 'event' && eventName === 'minigame_notify_msg') {
    console.log('[wechatPush] notify:', msg.Title || '', msg.Content || '');
  } else {
    console.log('[wechatPush] ignored event:', msg.MsgType || '', eventName || '');
  }

  return respond(200, SUCCESS);
}

async function handleQueryPendingWechatGifts(req) {
  const user = requireUser(req);
  const res = await collection(PENDING_GIFT_COLLECTION)
    .where({ userId: user.userId, status: 'pending' })
    .orderBy('createdAt', 'asc')
    .limit(20)
    .get();
  return { gifts: normalizeGiftRows((res && res.data) || []) };
}

async function handleMarkWechatGiftsGranted(req) {
  const user = requireUser(req);
  const body = req.body || {};
  const ids = Array.isArray(body.ids) ? body.ids.map((v) => String(v || '')).filter(Boolean) : [];
  if (ids.length === 0) return { updated: 0 };

  let updated = 0;
  const col = collection(PENDING_GIFT_COLLECTION);
  for (const id of ids) {
    try {
      const docRes = await col.doc(id).get();
      const doc = docRes && docRes.data && (Array.isArray(docRes.data) ? docRes.data[0] : docRes.data);
      if (!doc || doc.userId !== user.userId || doc.status !== 'pending') continue;
      await col.doc(id).update({ status: 'granted', grantedAt: Date.now() });
      updated++;
    } catch (error) {
      console.warn('[wechatPush] mark granted failed:', id, error && error.message ? error.message : error);
    }
  }
  return { updated };
}

function readRawBody(req) {
  let body = (req.raw && req.raw.body) || '';
  if (req.raw && req.raw.isBase64Encoded && body) {
    body = Buffer.from(body, 'base64').toString('utf8');
  }
  return body;
}

async function recordPushMessage(msg, eventName, now) {
  try {
    await collection('wechatPushMessages').add({
      msgType: msg.MsgType || '',
      event: eventName,
      openId: msg.FromUserName || msg.ToUserOpenid || (msg.MiniGame && msg.MiniGame.ToUserOpenid) || '',
      title: msg.Title || '',
      content: msg.Content || '',
      raw: msg,
      createdAt: now,
    });
  } catch (error) {
    console.warn('[wechatPush] record message failed:', error && error.message ? error.message : error);
  }
}

async function recordDeliverGoods(mini, now) {
  const orderId = String(mini.OrderId || '').trim();
  if (!orderId) return;

  const col = collection(PENDING_GIFT_COLLECTION);
  try {
    const existed = await col.where({ orderId }).limit(1).get();
    if (existed && Array.isArray(existed.data) && existed.data.length > 0) return;
  } catch (error) {
    console.warn('[wechatPush] query gift failed:', error && error.message ? error.message : error);
  }

  const openId = String(mini.ToUserOpenid || '').trim();
  const rewards = normalizeGoodsList(mini.GoodsList || []);
  if (!openId || Object.keys(rewards).length === 0) {
    console.warn('[wechatPush] gift has no known goods:', orderId);
    return;
  }
  await sendMail({
    userId: `wx:${openId}`,
    source: 'wechat',
    sourceId: orderId,
    title: '微信礼包',
    body: '礼包已送到，打开领取',
    rewards,
    now,
  });
}

function normalizeGoodsList(goodsList) {
  const rewards = {};
  const list = Array.isArray(goodsList) ? goodsList : [];
  list.forEach((item) => {
    const id = item && item.Id != null ? String(item.Id).trim() : '';
    const num = Number(item && item.Num);
    if (!id || !Number.isFinite(num) || num <= 0) return;
    const key = normalizeRewardKey(id);
    if (!key) {
      console.warn('[wechatPush] unknown goods id:', id);
      return;
    }
    rewards[key] = (rewards[key] || 0) + num;
  });
  return rewards;
}

function normalizeGiftRows(rows) {
  return rows.map((row) => {
    const rewards = row.rewards || {};
    return {
      id: row._id || row.id || '',
      orderId: row.orderId || '',
      giftId: row.giftId || '',
      ink: Number(rewards.ink) || 0,
      stamina: Number(rewards.stamina) || 0,
      diamond: Number(rewards.diamond) || 0,
      wood: Number(rewards.wood) || 0,
      silver: Number(rewards.silver) || 0,
      gold: Number(rewards.gold) || 0,
      royal: Number(rewards.royal) || 0,
      cannon: Number(rewards.cannon) || 0,
    };
  }).filter((row) => row.id && (
    row.ink > 0 || row.stamina > 0 || row.diamond > 0
    || row.wood > 0 || row.silver > 0 || row.gold > 0 || row.royal > 0 || row.cannon > 0
  ));
}

function normalizeRewardKey(id) {
  const key = String(id || '').trim();
  const lower = key.toLowerCase();
  if (lower === 'ink' || lower === 'mo' || key === '墨') return 'ink';
  if (lower === 'stamina' || lower === 'energy' || key === '体力') return 'stamina';
  if (lower === 'diamond' || lower === 'diamonds' || lower === 'gem' || key === '钻石') return 'diamond';
  if (lower === 'wood' || lower === 'chest_wood' || key === '木宝箱') return 'wood';
  if (lower === 'silver' || lower === 'chest_silver' || key === '银宝箱') return 'silver';
  if (lower === 'gold' || lower === 'chest_gold' || key === '金宝箱') return 'gold';
  if (lower === 'royal' || lower === 'chest_royal' || key === '皇家宝箱') return 'royal';
  if (lower === 'cannon' || lower === 'chest_cannon' || key === '炮台宝箱') return 'cannon';
  return '';
}

function parsePostBody(body) {
  const text = String(body || '').trim();
  if (!text) return {};
  if (text[0] === '{') return normalizeJsonMessage(JSON.parse(text));
  if (text[0] === '<') return parseXmlMessage(text);
  throw new Error('unknown body format');
}

function normalizeJsonMessage(input) {
  const msg = input || {};
  if (msg.MiniGame || !msg.minigame) return msg;
  return { ...msg, MiniGame: msg.minigame };
}

function parseXmlMessage(xml) {
  const msg = {
    ToUserName: xmlText(xml, 'ToUserName'),
    FromUserName: xmlText(xml, 'FromUserName'),
    CreateTime: xmlNumber(xml, 'CreateTime'),
    MsgType: xmlText(xml, 'MsgType'),
    Event: xmlText(xml, 'Event'),
    Title: xmlText(xml, 'Title'),
    Content: xmlText(xml, 'Content'),
  };
  const miniXml = xmlText(xml, 'MiniGame');
  if (miniXml) {
    const goodsList = [];
    const goodsReg = /<GoodsList>([\s\S]*?)<\/GoodsList>/gi;
    let match;
    while ((match = goodsReg.exec(miniXml))) {
      const itemXml = match[1];
      goodsList.push({ Id: xmlText(itemXml, 'Id'), Num: xmlNumber(itemXml, 'Num') });
    }
    msg.MiniGame = {
      OrderId: xmlText(miniXml, 'OrderId'),
      IsPreview: xmlNumber(miniXml, 'IsPreview'),
      ToUserOpenid: xmlText(miniXml, 'ToUserOpenid'),
      Zone: xmlNumber(miniXml, 'Zone'),
      GiftTypeId: xmlNumber(miniXml, 'GiftTypeId'),
      GiftId: xmlText(miniXml, 'GiftId'),
      SendTime: xmlNumber(miniXml, 'SendTime'),
      GoodsList: goodsList,
    };
  }
  return msg;
}

function xmlText(xml, tag) {
  const reg = new RegExp(`<${tag}>([\\s\\S]*?)<\\/${tag}>`, 'i');
  const match = String(xml || '').match(reg);
  if (!match) return '';
  return String(match[1] || '').replace(/^<!\[CDATA\[/, '').replace(/\]\]>$/, '').trim();
}

function xmlNumber(xml, tag) {
  const n = Number(xmlText(xml, tag));
  return Number.isFinite(n) ? n : 0;
}

function checkSignature(signature, timestamp, nonce) {
  if (!signature || !timestamp || !nonce) return false;
  return getWechatPushTokens().some((token) => {
    const hash = crypto.createHash('sha1').update([token, timestamp, nonce].sort().join('')).digest('hex');
    return hash === signature;
  });
}

module.exports = {
  handleWechatPushVerify,
  handleWechatPushCallback,
  handleQueryPendingWechatGifts,
  handleMarkWechatGiftsGranted,
};
