/**
 * 墨字防线后端（CloudBase 云函数 + HTTP 访问服务），照 huahua-api / caizhu-api 模板。
 *
 * 路由（POST JSON）：
 *   /login       wx code2session / anon，签发 JWT
 *   /save/pull   拉当前用户存档
 *   /save/push   上传存档（按 key 合并，updatedAt + baseRemoteUpdatedAt 防旧档回写）
 *   /health      健康检查
 *
 * 环境变量：
 *   GAME_KEY=blackrosa
 *   BLACKROSA_JWT_SECRET            必填
 *   BLACKROSA_WX_APPID / BLACKROSA_WX_SECRET   微信 code2session
 *   BLACKROSA_SAVE_MAX_BYTES / BLACKROSA_TOKEN_TTL_SEC   可选
 *
 * 集合：blackrosa_playerData（userId 唯一索引）
 */

const { handleLogin } = require('./lib/auth');
const { handlePull, handlePush } = require('./lib/save');
const { respond, parseEvent, preflight } = require('./lib/http');

const ROUTES = {
  'GET /health': async () => ({ ok: true, ts: Date.now() }),
  'POST /health': async () => ({ ok: true, ts: Date.now() }),
  'POST /login': handleLogin,
  'POST /save/pull': handlePull,
  'POST /save/push': handlePush,
};

exports.main = async (event, context) => {
  try {
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
