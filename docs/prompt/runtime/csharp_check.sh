#!/bin/bash
# 用团结编辑器自带的 Roslyn 编译一遍脚本，做离线语法/类型检查。
# Unity MCP 连不上时靠这个提前抓编译错，省去来回开编辑器。
#   bash docs/prompt/runtime/csharp_check.sh
set -u
E=/Applications/Tuanjie/Hub/Editor/2022.3.62t8/Tuanjie.app/Contents
REPO="$(cd "$(dirname "$0")/../../.." && pwd)"
OUT=$(mktemp -d)
trap 'rm -rf "$OUT"' EXIT

# macOS 自带 bash 3.2 没有 mapfile，走 csc 的 @响应文件。
# 不要引 Managed/UnityEngine.dll 那个门面，它和拆分模块里的类型重名会报 CS0433。
for d in "$E/Managed/UnityEngine"/*.dll; do echo "-r:$d"; done > "$OUT/refs.rsp"
{
  # 同理，Managed/UnityEditor.dll 的类型和 Managed/UnityEngine/UnityEditor.*Module.dll 重名
  echo "-r:$E/NetStandard/ref/2.1.0/netstandard.dll"
  # UI 走包，不在 Managed 下，取工程已编译出来的那份
  echo "-r:$REPO/Library/ScriptAssemblies/UnityEngine.UI.dll"
} >> "$OUT/refs.rsp"

fail=0

run() {
  label="$1"; shift
  echo "=== $label ==="
  "$E/NetCoreRuntime/dotnet" "$E/DotNetSdkRoslyn/csc.dll" \
    -nologo -nostdlib+ -noconfig -target:library -langversion:9.0 \
    -define:UNITY_2022_3_OR_NEWER -warn:0 \
    -out:"$OUT/$label.dll" "@$OUT/refs.rsp" "$@"
  code=$?
  [ $code -ne 0 ] && fail=1
  echo "--- $label 退出码 $code ---"
}

find "$REPO/Assets/Scripts" -name "*.cs" > "$OUT/runtime.rsp"
run runtime "@$OUT/runtime.rsp"

find "$REPO/Assets/Editor" -name "*.cs" > "$OUT/editor.rsp"
run editor -r:"$OUT/runtime.dll" "@$OUT/editor.rsp"

# 字体是按工程字表子集化的，加了新中文不重建就会在真机上渲染成空白 ——
# 编辑器里有系统字体回落，看不出来，所以只能靠这一步拦。
echo "=== 字表覆盖 ==="
if [ -x "$REPO/.venv-mock/bin/python" ]; then
  "$REPO/.venv-mock/bin/python" "$REPO/docs/prompt/runtime/build_font.py" --check || fail=1
  # 字排不进框也是一种「显示不全」：Text 是 Wrap + Overflow，
  # 超宽会折行再顶出框，压到上下相邻的元素上。
  echo "=== 标签字宽 ==="
  "$REPO/.venv-mock/bin/python" "$REPO/docs/prompt/runtime/check_text_fit.py" || fail=1
else
  echo "跳过：缺 .venv-mock（需要 fontTools）"
fi

exit $fail
