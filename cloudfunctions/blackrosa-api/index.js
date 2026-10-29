/**
 * 墨字防线后端（CloudBase 云函数 + HTTP 访问服务），照 huahua-api / caizhu-api 模板。
 *
 * 路由（POST JSON）：
 *   /login       wx code2session / anon，签发 JWT
 *   /save/pull   拉当前用户存档
 *   /save/push   上传存档（按 key 合并，updatedAt + baseRemoteUpdatedAt 防旧档回写）
 *   /rank/submit 上报已通关关数和无尽最好波数（都只升不降），顺带更新昵称头像
 *   /rank/list   board=stage 通关榜 / board=endless 无尽榜，前 N 名 + 自己的名次
 *   /gameclub/daily  解密 wx.getGameClubData，回当天游戏圈发帖数
 *   /giftDeliver 微信后台消息推送：GET 验证 URL，POST 收明文 JSON/XML
 *   /wechat-gift/queryPending  当前用户待领的微信礼包
 *   /wechat-gift/markGranted   客户端入账后销单
 *   /mail/list   未领邮件
 *   /mail/claim  领取邮件，领过的不再返回
 *   /health      健康检查
 *   定时器 weeklyRankAward  每周一 00:05（北京时间）发上一周无尽榜前三名的奖励邮件
 *
 * 环境变量：
 *   GAME_KEY=blackrosa
 *   BLACKROSA_JWT_SECRET            必填
 *   BLACKROSA_WX_APPID / BLACKROSA_WX_SECRET   微信 code2session
 *   BLACKROSA_SAVE_MAX_BYTES / BLACKROSA_TOKEN_TTL_SEC   可选
 *   BLACKROSA_RANK_MAX_CLEARED（默认 108）/ BLACKROSA_RANK_MAX_ENDLESS（默认 70）
 *   BLACKROSA_RANK_BLOCKED_UIDS（逗号分隔）   可选
 *   BLACKROSA_WECHAT_PUSH_TOKEN  可选，默认 blackrosagift2026
 *
 * 集合：blackrosa_playerData（userId 唯一索引）
 *       blackrosa_rankings（userId 唯一索引；cleared desc + reachedAt asc；endless desc + endlessAt asc）
 *       blackrosa_wxSessions（userId 唯一索引）
 */

const { handleLogin } = require('./lib/auth');
const { handlePull, handlePush } = require('./lib/save');
const { handleSubmit, handleList, settleWeeklyAwards } = require('./lib/rank');
const { handleDailyPost } = require('./lib/game-club');
const {
  handleWechatPushVerify,
  handleWechatPushCallback,
  handleQueryPendingWechatGifts,
  handleMarkWechatGiftsGranted,
} = require('./lib/wechatPush');
const { handleListMail, handleClaimMail } = require('./lib/mail');
const { respond, parseEvent, preflight } = require('./lib/http');

const ROUTES = {
  'GET /health': async () => ({ ok: true, ts: Date.now() }),
  'POST /health': async () => ({ ok: true, ts: Date.now() }),
  'POST /login': handleLogin,
  'POST /save/pull': handlePull,
  'POST /save/push': handlePush,
  'POST /rank/submit': handleSubmit,
  'POST /rank/list': handleList,
  'POST /gameclub/daily': handleDailyPost,
  'GET /giftDeliver': handleWechatPushVerify,
  'POST /giftDeliver': handleWechatPushCallback,
  'POST /wechat-gift/queryPending': handleQueryPendingWechatGifts,
  'POST /wechat-gift/markGranted': handleMarkWechatGiftsGranted,
  'POST /mail/list': handleListMail,
  'POST /mail/claim': handleClaimMail,
};

function isWeeklyAwardTimer(event) {
  if (!event || event.httpMethod) return false;
  const type = String(event.Type || event.type || '');
  if (type.toLowerCase() === 'timer') return true;
  return String(event.TriggerName || event.triggerName || '') === 'weeklyRankAward';
}

exports.main = async (event, context) => {
  try {
    if (isWeeklyAwardTimer(event)) {
      const result = await settleWeeklyAwards(Date.now());
      return respond(200, { ok: true, data: result });
    }
    if (event && event.httpMethod === 'OPTIONS') {
      return preflight();
    }

    const req = parseEvent(event);
    const key = `${req.method} ${req.path}`;
    const handler = ROUTES[key];
    if (!handler) {
      return respond(404, { ok: false, code: 'NOT_FOUND', error: `no route: ${key}` });
    }

    const result = await handler(req, context);
    if (result && typeof result === 'object' && 'statusCode' in result) {
      return result;
    }
    return respond(200, { ok: true, data: result });
  } catch (error) {
    const code = error && error.code ? error.code : 'INTERNAL';
    const status = error && error.status ? error.status : 500;
    const message = (error && error.message) || String(error);
    console.error('[blackrosa-api] error:', code, message, error && error.stack);
    const out = { ok: false, code, error: message };
    if (error && error.data !== undefined) {
      out.data = error.data;
    }
    return respond(status, out);
  }
};
