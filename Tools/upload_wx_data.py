#!/usr/bin/env python3
"""把这次微信包的首包 .br 上传到 MiniGameConfig 里的 CDN 目录。

只按原名新增或覆盖这一个哈希文件。不删除云上任何东西，也不动别的资源。
登录态用本机 CloudBase（~/.config/.cloudbase/auth.json），和 Cursor 里的是同一份。
"""

import hashlib
import hmac
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONFIG = ROOT / "Assets/WX-WASM-SDK-V2/Editor/MiniGameConfig.asset"
AUTH = Path.home() / ".config/.cloudbase/auth.json"
ENV_ID = "rosa-env-d7grf78r5dbd37323"
REGION = "ap-shanghai"
BUCKET = "726f-rosa-env-d7grf78r5dbd37323-1414200063"


def fail(msg):
    print(msg, file=sys.stderr)
    return 1


def cdn_base():
    for line in CONFIG.read_text(encoding="utf-8").splitlines():
        line = line.strip()
        if line.startswith("CDN:"):
            return line.split(":", 1)[1].strip()
    raise SystemExit("MiniGameConfig 里没有 CDN")


def data_file():
    webgl = ROOT / "wechat-minigame/webgl"
    game_js = (ROOT / "wechat-minigame/minigame/game.js").read_text(encoding="utf-8")
    md5 = None
    for line in game_js.splitlines():
        if "DATA_FILE_MD5" in line:
            md5 = line.split("'")[1]
            break
    if not md5:
        raise SystemExit("game.js 里没有 DATA_FILE_MD5")
    path = webgl / f"{md5}.webgl.data.unityweb.bin.br"
    if not path.is_file():
        raise SystemExit(f"找不到首包 {path.name}")
    return path


def mac_hash():
    node = uuid.getnode()
    mac = ":".join(f"{(node >> shift) & 0xFF:02x}" for shift in range(40, -1, -8))
    return hashlib.md5(mac.encode()).hexdigest()


def load_auth():
    data = json.loads(AUTH.read_text(encoding="utf-8"))
    cred = data["credential"]
    now = int(time.time() * 1000)
    # 临时密钥快过期就刷新。刷新结果写回同一份登录文件，旧文件一个都不删。
    if cred.get("tmpExpired", 0) - now < 5 * 60 * 1000:
        cred = refresh(cred)
        data["credential"] = cred
        tmp = AUTH.with_suffix(".json.tmp")
        tmp.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
        os.replace(tmp, AUTH)
    return cred


def refresh(cred):
    body = {
        "tmpSecretId": cred["tmpSecretId"],
        "tmpSecretKey": cred["tmpSecretKey"],
        "tmpToken": cred["tmpToken"],
        "tmpExpired": cred["tmpExpired"],
        "expired": cred["expired"],
        "authTime": cred["authTime"],
        "refreshToken": cred["refreshToken"],
        "uin": cred["uin"],
        "hash": mac_hash(),
        "envId": ENV_ID,
    }
    req = urllib.request.Request(
        "https://iaas.cloud.tencent.com/tcb_refresh",
        data=json.dumps(body).encode(),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=20) as resp:
            payload = json.loads(resp.read().decode())
    except urllib.error.HTTPError as err:
        detail = err.read().decode(errors="replace")[:200]
        raise SystemExit(f"云登录刷新失败 {err.code} {detail}") from err
    if payload.get("code") not in (0, None) or not payload.get("data"):
        raise SystemExit("云登录刷新失败，在 Cursor 里重新登录 CloudBase 后再打微信包")
    new = payload["data"]
    # 接口有时只回新的临时密钥，原来的长期字段留着。
    merged = dict(cred)
    merged.update(new)
    return merged


def sign(secret_id, secret_key, method, uri, headers):
    now = int(time.time())
    key_time = f"{now};{now + 3600}"
    sign_key = hmac.new(secret_key.encode(), key_time.encode(), hashlib.sha1).hexdigest()
    items = []
    for key in sorted(headers):
        val = urllib.parse.quote(str(headers[key]), safe="-_.~")
        items.append(f"{key}={val}")
    header_list = ";".join(sorted(headers))
    http_string = f"{method.lower()}\n{uri}\n\n{'&'.join(items)}\n"
    hashed = hashlib.sha1(http_string.encode()).hexdigest()
    string_to_sign = f"sha1\n{key_time}\n{hashed}\n"
    signature = hmac.new(sign_key.encode(), string_to_sign.encode(), hashlib.sha1).hexdigest()
    return (
        "q-sign-algorithm=sha1"
        f"&q-ak={secret_id}"
        f"&q-sign-time={key_time}"
        f"&q-key-time={key_time}"
        f"&q-header-list={header_list}"
        "&q-url-param-list="
        f"&q-signature={signature}"
    )


def cos(method, key, cred, body=None, content_type=None):
    host = f"{BUCKET}.cos.{REGION}.myqcloud.com"
    uri = "/" + urllib.parse.quote(key, safe="/")
    headers = {
        "host": host,
        "x-cos-security-token": cred["tmpToken"],
    }
    data = None
    if body is not None:
        data = body
        headers["content-length"] = str(len(body))
        headers["content-type"] = content_type or "application/octet-stream"
    auth = sign(cred["tmpSecretId"], cred["tmpSecretKey"], method, uri, headers)
    req_headers = {
        "Host": host,
        "Authorization": auth,
        "x-cos-security-token": cred["tmpToken"],
    }
    if body is not None:
        req_headers["Content-Length"] = headers["content-length"]
        req_headers["Content-Type"] = headers["content-type"]
    req = urllib.request.Request(
        f"https://{host}{uri}",
        data=data,
        headers=req_headers,
        method=method,
    )
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            resp.read()
            return resp.status, resp.headers
    except urllib.error.HTTPError as err:
        detail = err.read().decode(errors="replace")
        return err.code, detail


def pin_data_cdn():
    # 导出模板在 gitignore 的微信包里。启动中段会用 GameGlobal.DATA_CDN 回写配置，
    # 没先钉住就会被写成空地址。每次打包都补上这一行，不依赖那份本地模板。
    path = ROOT / "wechat-minigame/minigame/game.js"
    text = path.read_text(encoding="utf-8")
    needle = "GameGlobal.DATA_CDN = managerConfig.DATA_CDN;"
    if needle in text:
        return
    old = "GameGlobal.managerConfig = managerConfig;\n"
    if old not in text:
        raise SystemExit("game.js 里没有 managerConfig 赋值，没法钉住 CDN")
    pin = old + "// 启动中段会用 GameGlobal.DATA_CDN 回写配置。这里先钉住，避免被写成空地址。\n" + needle + "\n"
    path.write_text(text.replace(old, pin, 1), encoding="utf-8")


def main():
    pin_data_cdn()
    base = cdn_base()
    if not base.endswith("/"):
        raise SystemExit("CDN 地址需要以 / 结尾，否则客户端拼不出首包文件名")
    path = data_file()
    key = "black-rosa/StreamingAssets/" + path.name
    public = base + path.name
    cred = load_auth()
    code, headers = cos("HEAD", key, cred)
    if code == 200 and int(headers.get("Content-Length", "0")) == path.stat().st_size:
        print(f"首包已在云上，不用重传 {path.name}")
        return 0
    if code not in (200, 404):
        text = headers if isinstance(headers, str) else ""
        return fail(f"检查云上首包失败 {code} {text[:180]}")
    print(f"上传首包 {path.name} ({path.stat().st_size} bytes)")
    body = path.read_bytes()
    code, detail = cos("PUT", key, cred, body)
    if code not in (200, 201):
        text = detail if isinstance(detail, str) else ""
        return fail(f"上传首包失败 {code} {text[:180]}")
    code, headers = cos("HEAD", key, cred)
    size = int(headers.get("Content-Length", "0")) if code == 200 else -1
    if code != 200 or size != len(body):
        return fail(f"上传后大小对不上 {code} {size}")
    # 公网地址必须能匿名下到，玩家走的是这个域名，不是 COS 内网。
    req = urllib.request.Request(public, method="GET")
    req.add_header("Range", "bytes=0-15")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            got = resp.read(16)
    except urllib.error.HTTPError as err:
        return fail(f"公网地址打不开 {err.code} {public}")
    if got != body[:16]:
        return fail("公网文件和本地首包对不上")
    print(f"首包已上传 {public}")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except SystemExit as err:
        if err.code not in (0, None) and not isinstance(err.code, int):
            print(err.code, file=sys.stderr)
            sys.exit(1)
        raise
