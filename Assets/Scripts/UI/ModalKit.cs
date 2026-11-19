using System;
using UnityEngine;
using UnityEngine.UI;

namespace InkLine
{
    // 全弹窗统一的 Figma @2x 切图装配与预制体加载器。
    // 面板、顶栏、暗纹星、说明暗条、斜切标签、异形签到卡、通用物品框、凹槽进度条、尖角按钮、关闭按钮与分割线
    // 全部直接取自 Figma 导出贴图，严禁使用程序化 SDF 圆角、系统描边或运行时置灰材质。
    public static class ModalKit
    {
        public const float DesignW = 720f;
        public const float DesignH = 1280f;
        public const float Ppu = 200f;

        public static readonly Color Ink = InkTheme.Hex("0D050A");
        public static readonly Color TextDark = InkTheme.Hex("1A1008");
        public static readonly Color TextBody = InkTheme.Hex("3A2A20");
        public static readonly Color TextMuted = InkTheme.Hex("6A5848");
        public static readonly Color CreamLight = InkTheme.Hex("FFF6E4");
        public static readonly Color GoldLight = InkTheme.Hex("FFE89E");
        public static readonly Color TitleGold = InkTheme.Hex("FFF6CE");

        public static readonly Vector4 BorderPanel = new Vector4(72f, 72f, 72f, 72f);
        public static readonly Vector4 BorderHead = new Vector4(64f, 0f, 64f, 0f);
        public static readonly Vector4 BorderStrip = new Vector4(56f, 0f, 56f, 0f);
        public static readonly Vector4 BorderSub18 = new Vector4(44f, 44f, 44f, 44f);
        public static readonly Vector4 BorderSub16 = new Vector4(40f, 40f, 40f, 40f);
        public static readonly Vector4 BorderPill12 = new Vector4(30f, 0f, 30f, 0f);
        public static readonly Vector4 BorderTag = new Vector4(22f, 0f, 22f, 0f);
        public static readonly Vector4 BorderProgTrack = new Vector4(34f, 0f, 34f, 0f);
        public static readonly Vector4 BorderProgFill = new Vector4(26f, 0f, 26f, 0f);
        public static readonly Vector4 BorderDivider = new Vector4(8f, 0f, 8f, 0f);

        // 烘预制时切到 AssetDatabase 里的 Sprite，确保预制体保存真实资产 GUID。
        public static Func<string, Sprite> SpriteOf;

        public static Sprite Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (SpriteOf != null)
            {
                Sprite s = SpriteOf(key);
                if (s != null) return s;
            }
            return InkSprites.Load(key);
        }

        // 预制烘出来时图没挂上，打开时按路径补。已经有图的不动。
        public static void FillSprite(Transform root, string path, string key, Vector4 border = default)
        {
            if (root == null || string.IsNullOrEmpty(key)) return;
            Transform t = string.IsNullOrEmpty(path) ? root : root.Find(path);
            var img = t != null ? t.GetComponent<Image>() : null;
            if (img == null || img.sprite != null) return;
            bool sliced = border.sqrMagnitude > 0.5f;
            Sprite s = sliced ? Load2x(key, border) : Load(key);
            if (s == null) return;
            img.sprite = s;
            img.color = Color.white;
            if (sliced) img.type = Image.Type.Sliced;
        }

        public static Sprite Load2x(string key, Vector4 border)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (SpriteOf != null)
            {
                Sprite s = SpriteOf(key);
                if (s != null) return s;
            }
            return border.sqrMagnitude > 0.5f ? InkSprites.LoadSliced(key, border, Ppu) : InkSprites.Load(key);
        }

        // 优先加载已烘焙的弹窗预制体；编辑器刚改完代码尚未重烘时无缝回退到同套布局构建函数。
        public static RectTransform SpawnPrefab(Transform layer, string prefabPath, string name, string mark,
            Action onTapBlank, Func<RectTransform> buildFallback, out RectTransform stage)
        {
            RectTransform dim = null;
            var prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab != null && HasMark(prefab.transform, mark))
            {
                var go = UnityEngine.Object.Instantiate(prefab, layer, false);
                go.name = name;
                dim = (RectTransform)go.transform;
            }
            else if (buildFallback != null)
            {
                dim = buildFallback();
                if (layer != null && dim.parent != layer) dim.SetParent(layer, false);
                dim.name = name;
            }

            UiKit.ApplyTo(dim);
            stage = dim != null ? dim.Find("stage") as RectTransform : null;
            if (stage != null)
            {
                FitStage(stage, layer as RectTransform);
                var board = stage.Find("board") as RectTransform;
                if (board != null) SettleBoard(board);
            }

            if (dim != null)
            {
                var scrim = dim.GetComponent<Image>();
                if (scrim != null) scrim.raycastTarget = true;
                var bgBtn = dim.GetComponent<Button>();
                if (onTapBlank != null)
                {
                    if (bgBtn == null) bgBtn = dim.gameObject.AddComponent<Button>();
                    bgBtn.transition = Selectable.Transition.None;
                    bgBtn.targetGraphic = scrim;
                    bgBtn.onClick.RemoveAllListeners();
                    bgBtn.onClick.AddListener(() => onTapBlank());
                }
                else if (bgBtn != null)
                {
                    bgBtn.onClick.RemoveAllListeners();
                }
            }
            RepairAdIcons(dim);
            return dim;
        }

        // 预制体里广告标经常是空引用，Image 就会画出一块白方块。打开时补上场记板。
        static void RepairAdIcons(Transform root)
        {
            if (root == null) return;
            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image img = images[i];
                if (img == null || img.gameObject.name != "ad_ico" || img.sprite != null) continue;
                img.sprite = Load("Ui/ico_ad");
            }
        }

        public static bool HasMark(Transform root, string mark)
        {
            if (root == null || string.IsNullOrEmpty(mark)) return false;
            return root.Find("stage/" + mark) != null || root.Find("stage/board/" + mark) != null;
        }

        public static void AddMark(Transform parent, string mark)
        {
            if (parent == null || string.IsNullOrEmpty(mark) || parent.Find(mark) != null) return;
            var go = new GameObject(mark, typeof(RectTransform));
            go.transform.SetParent(parent, false);
        }

        public static void SettleBoard(RectTransform board)
        {
            if (board == null) return;
            board.localScale = Vector3.one;
            board.localRotation = Quaternion.identity;
            float w = board.sizeDelta.x;
            float h = board.sizeDelta.y;
            float mx = Mathf.Round((DesignW - w) * 0.5f);
            float my = Mathf.Round((DesignH - h) * 0.5f);
            if (Mathf.Abs(board.anchorMin.x - 0.5f) < 0.01f && Mathf.Abs(board.anchorMin.y - 0.5f) < 0.01f)
            {
                board.anchoredPosition = Vector2.zero;
            }
            else if (Mathf.Abs(board.pivot.x - 0.5f) < 0.01f && Mathf.Abs(board.pivot.y - 0.5f) < 0.01f)
            {
                board.anchoredPosition = new Vector2(mx + w * 0.5f, -(my + h * 0.5f));
            }
            else
            {
                board.anchoredPosition = new Vector2(mx, -my);
            }
        }

        // 全屏暗场遮罩 + 居中等比适配的 720×1280 设计稿画布。
        public static RectTransform Stage(Transform parent, string name, float scrimAlpha, Action onTapBlank,
            out RectTransform stage)
        {
            var dimGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            if (parent != null) dimGo.transform.SetParent(parent, false);
            var dim = dimGo.GetComponent<RectTransform>();
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = Vector2.zero;
            dim.offsetMax = Vector2.zero;

            var scrim = dimGo.GetComponent<Image>();
            scrim.color = new Color(0.075f, 0.047f, 0.031f, scrimAlpha);
            scrim.raycastTarget = true;
            if (onTapBlank != null)
            {
                var btn = dimGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = scrim;
                btn.onClick.AddListener(() => onTapBlank());
            }

            var stageGo = new GameObject("stage", typeof(RectTransform));
            stageGo.transform.SetParent(dim, false);
            stage = stageGo.GetComponent<RectTransform>();
            stage.anchorMin = stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.sizeDelta = new Vector2(DesignW, DesignH);
            stage.anchoredPosition = Vector2.zero;
            FitStage(stage, parent as RectTransform);
            return dim;
        }

        public static void FitStage(RectTransform stage, RectTransform host)
        {
            if (stage == null) return;
            float cw = host != null && host.rect.width > 2f ? host.rect.width : ScreenFit.CanvasW;
            float ch = host != null && host.rect.height > 2f ? host.rect.height : ScreenFit.CanvasH;
            if (cw < 2f || ch < 2f) return;
            float topSafe = ScreenFit.CapsuleGuard > 0f ? ScreenFit.CapsuleGuard * 0.5f : 0f;
            float availH = Mathf.Max(400f, ch - topSafe - ScreenFit.BottomPad * 0.5f);
            float scale = Mathf.Min(1f, Mathf.Min(cw / DesignW, availH / DesignH));
            stage.localScale = new Vector3(scale, scale, 1f);
            stage.anchoredPosition = new Vector2(0f, -topSafe * 0.35f);
        }

        public static void TopLeft(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        // 常规奶油底弹窗主壳（复用 Figma Panel/信息面板 9:965）。
        public static RectTransform Shell(RectTransform stage, float w, float h, string title, string headIconKey,
            string stripText, string slantTone, string slantText, Action onClose,
            out Text stripLabel, out Text slantLabel, bool animate = true)
        {
            float mx = Mathf.Round((DesignW - w) * 0.5f);
            float my = Mathf.Round((DesignH - h) * 0.5f);

            var panel = Sliced(stage, "board", "Ui/modal_panel_body", BorderPanel, mx, my, w, h, true);
            ScatterMarks(panel, w, h, 96f);

            var head = Sliced(panel, "head", "Ui/modal_head_yellow", BorderHead, 0f, 0f, w, 82f, false);
            if (!string.IsNullOrEmpty(headIconKey))
            {
                var ico = Pic(head, "ico", headIconKey, 36f, 16f, 50f, 50f, true);
                ico.raycastTarget = false;
            }
            OutlinedText(head, "title", title, 32, 0f, 14f, w, 52f, Color.white, TextAnchor.MiddleCenter);

            bool showSlant = !string.IsNullOrEmpty(slantText);
            bool showStrip = stripText != null || showSlant;
            stripLabel = null;
            if (showStrip)
            {
                float stripW = showSlant ? w - 48f - 136f : w - 48f;
                var strip = Sliced(panel, "info_strip", "Ui/modal_info_strip", BorderStrip, 24f, 96f, stripW, 40f, false);
                stripLabel = SolidText(strip, "t", stripText ?? "", 16, 20f, 0f, stripW - 40f, 40f, CreamLight,
                    TextAnchor.MiddleCenter, false);
            }

            slantLabel = null;
            if (showSlant)
                SlantTag(panel, slantTone, w - 24f - 126f, 95f, slantText, out slantLabel);

            if (onClose != null)
                CloseBtn(panel, w - 50f, 0f, onClose);

            if (animate) PanelKit.Open(panel);
            return panel;
        }

        static void ScatterMarks(RectTransform panel, float w, float h, float minY)
        {
            Sprite star = Load("Ui/modal_star_mark");
            if (star == null) return;
            float[,] pts =
            {
                { 0.16f, 0.34f, 22f }, { 0.82f, 0.38f, 16f }, { 0.44f, 0.54f, 26f },
                { 0.88f, 0.66f, 15f }, { 0.20f, 0.76f, 19f }, { 0.66f, 0.88f, 22f },
            };
            for (int i = 0; i < pts.GetLength(0); i++)
            {
                float s = pts[i, 2];
                float x = pts[i, 0] * w - s * 0.5f;
                float y = Mathf.Max(minY, pts[i, 1] * h - s * 0.5f);
                if (y + s > h - 18f) continue;
                var img = Pic(panel, "mark" + i, "Ui/modal_star_mark", x, y, s, s, true);
                img.raycastTarget = false;
            }
        }

        // 全游戏弹窗共用的右上角关闭按钮：直接复用 Figma 导出的正放红底白叉切图（Ui/modal_btn_close），不再程序化画圆拼叉。
        public static Button CloseBtn(Transform parent, float x, float y, Action onClose)
        {
            const float s = 50f;
            var go = new GameObject("close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(s, s);
            rt.anchoredPosition = new Vector2(-s * 0.5f, -s * 0.5f);
            var img = go.GetComponent<Image>();
            img.sprite = Load("Ui/modal_btn_close");
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = true;
            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            if (onClose != null) btn.onClick.AddListener(() => onClose());
            return btn;
        }

        public static Button BindClose(Transform board, Action onClose)
        {
            return BindBtn(board, "close", onClose);
        }

        public static Button BindBtn(Transform root, string path, Action onClick)
        {
            if (root == null) return null;
            Transform tr = string.IsNullOrEmpty(path) ? root : root.Find(path);
            if (tr == null) return null;
            var btn = tr.GetComponent<Button>();
            if (btn == null) return null;
            btn.onClick.RemoveAllListeners();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            return btn;
        }

        public static RectTransform Divider(Transform parent, string name, float x, float y, float w, float h = 2f,
            bool dark = false)
        {
            string key = dark ? "Ui/modal_divider_dark" : "Ui/modal_divider";
            return Sliced(parent, name, key, BorderDivider, x, y, w, h, false);
        }

        public static RectTransform SlantTag(Transform parent, string tone, float x, float y, string text,
            out Text label)
        {
            string key = tone == "magenta" ? "Ui/home_tag_elite"
                : tone == "gray" ? "Ui/home_tag_gray"
                : "Ui/home_tag_normal";
            Color tc = tone == "magenta" ? Color.white : TextDark;
            var img = Pic(parent, "slant_tag", key, x, y, 126f, 45f, false);
            img.raycastTarget = false;
            label = tone == "magenta"
                ? OutlinedText(img.rectTransform, "t", text, 16, 0f, 1f, 126f, 36f, tc, TextAnchor.MiddleCenter)
                : SolidText(img.rectTransform, "t", text, 16, 0f, 1f, 126f, 36f, tc, TextAnchor.MiddleCenter, true);
            return img.rectTransform;
        }

        public static RectTransform Sliced(Transform parent, string name, string key, Vector4 border2x,
            float x, float y, float w, float h, bool raycast = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            TopLeft(rt, x, y, w, h);
            var img = go.GetComponent<Image>();
            img.sprite = Load2x(key, border2x);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            img.color = Color.white;
            img.raycastTarget = raycast;
            return rt;
        }

        public static Image Pic(Transform parent, string name, string key, float x, float y, float w, float h,
            bool preserve = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            TopLeft(go.GetComponent<RectTransform>(), x, y, w, h);
            var img = go.GetComponent<Image>();
            if (!string.IsNullOrEmpty(key)) img.sprite = Load(key);
            img.type = Image.Type.Simple;
            img.preserveAspect = preserve;
            img.color = Color.white;
            img.raycastTarget = false;
            return img;
        }

        public static RectTransform Tag(Transform parent, string tagKey, float x, float y, float w, float h,
            string text, bool darkText = false, int fontSize = 13)
        {
            var rt = Sliced(parent, "tag", tagKey, BorderTag, x, y, w, h, false);
            if (darkText)
                SolidText(rt, "t", text, fontSize, 2f, 0f, w - 4f, h, TextDark, TextAnchor.MiddleCenter, true);
            else
                OutlinedText(rt, "t", text, fontSize, 2f, 0f, w - 4f, h, Color.white, TextAnchor.MiddleCenter, 1.15f);
            return rt;
        }

        public static void SetTag(RectTransform tagRt, string tagKey, string text, bool darkText = false)
        {
            if (tagRt == null) return;
            var img = tagRt.GetComponent<Image>();
            if (img != null && !string.IsNullOrEmpty(tagKey))
                img.sprite = Load2x(tagKey, BorderTag);
            var t = tagRt.Find("t")?.GetComponent<Text>();
            if (t != null)
            {
                t.text = text ?? "";
                t.color = darkText ? TextDark : Color.white;
                var o = t.GetComponent<Outline>();
                if (darkText)
                {
                    if (o != null) o.enabled = false;
                }
                else
                {
                    if (o == null)
                    {
                        o = t.gameObject.AddComponent<Outline>();
                        o.effectColor = Ink;
                        o.effectDistance = new Vector2(1.15f, -1.15f);
                    }
                    o.enabled = true;
                }
            }
        }

        // 货币物品框：灵墨紫，金箔橙，钻石和体力黄。
        public const string CurrencyTone = "yellow";
        public const string InkTone = "purple";
        public const string FoilTone = "orange";

        public static string CurrencyFrame(string iconKey)
        {
            if (string.IsNullOrEmpty(iconKey)) return CurrencyTone;
            if (iconKey.Contains("foil")) return FoilTone;
            if (iconKey.Contains("ink")) return InkTone;
            return CurrencyTone;
        }

        // 宝箱品质底色：木(普通)=绿、银=蓝、金=紫、皇家=橙（炮台宝箱也统一用 orange）
        public static string ChestTone(ChestTier tier)
        {
            switch (tier)
            {
                case ChestTier.Royal: return "orange";
                case ChestTier.Gold: return "purple";
                case ChestTier.Silver: return "blue";
                default: return "green";
            }
        }

        // 炮台皮肤品质底色：普通=绿、高级=蓝、稀有=紫
        public static string SkinTone(SkinRarity r)
        {
            switch (r)
            {
                case SkinRarity.Rare: return "purple";
                case SkinRarity.Advanced: return "blue";
                default: return "green";
            }
        }

        public static string SlotSpriteKey(string tone, int size)
        {
            return size >= 116 ? "Ui/modal_slot_128_" + tone
                : size >= 92 ? "Ui/item_frame_" + tone
                : "Ui/modal_slot_" + size + "_" + tone;
        }

        // 复用通用方形物品框（128/86/74/68/60/52 用各自 @2x 导出的 modal_slot_*，96 用 item_frame_*，保证 2px #0D050A 描边绝对一致）。
        public static RectTransform ItemSlot(Transform parent, string tone, int size, float x, float y,
            Sprite icon, string count = null, bool forceNodes = false)
        {
            string key = SlotSpriteKey(tone, size);
            var slot = Pic(parent, "slot_" + tone, key, x, y, size, size, false);
            slot.raycastTarget = false;
            if (icon != null || forceNodes)
            {
                float s = Mathf.Round(size * 0.60f);
                float ix = Mathf.Round((size - s) * 0.5f);
                float iy = Mathf.Round((size - s) * 0.5f) - 2f;
                var art = Pic(slot.rectTransform, "icon", null, ix, iy, s, s, true);
                art.sprite = icon;
                art.raycastTarget = false;
                art.gameObject.SetActive(icon != null);
            }
            if (!string.IsNullOrEmpty(count) || forceNodes)
            {
                int fs = size <= 70 ? 16 : 18;
                float h = size <= 70 ? 22f : 25f;
                var num = OutlinedText(slot.rectTransform, "n", count ?? "", fs, 4f, size - h - 4f, size - 11f, h,
                    Color.white, TextAnchor.LowerRight, 1.25f);
                num.gameObject.SetActive(!string.IsNullOrEmpty(count));
            }
            return slot.rectTransform;
        }

        public static void SetItemSlot(RectTransform slot, string tone, int size, Sprite icon, string count)
        {
            if (slot == null) return;
            var frame = slot.GetComponent<Image>();
            if (frame != null && !string.IsNullOrEmpty(tone))
                frame.sprite = Load(SlotSpriteKey(tone, size));

            var iconTr = slot.Find("icon");
            if (icon != null)
            {
                Image art;
                if (iconTr == null)
                {
                    float s = Mathf.Round(size * 0.60f);
                    float ix = Mathf.Round((size - s) * 0.5f);
                    float iy = Mathf.Round((size - s) * 0.5f) - 2f;
                    art = Pic(slot, "icon", null, ix, iy, s, s, true);
                }
                else
                {
                    art = iconTr.GetComponent<Image>();
                }
                if (art != null)
                {
                    art.sprite = icon;
                    art.gameObject.SetActive(true);
                }
            }
            else if (iconTr != null)
            {
                iconTr.gameObject.SetActive(false);
            }

            var numTr = slot.Find("n");
            if (!string.IsNullOrEmpty(count))
            {
                Text num;
                if (numTr == null)
                {
                    int fs = size <= 70 ? 16 : 18;
                    float h = size <= 70 ? 22f : 25f;
                    num = OutlinedText(slot, "n", count, fs, 4f, size - h - 4f, size - 11f, h,
                        Color.white, TextAnchor.LowerRight, 1.25f);
                }
                else
                {
                    num = numTr.GetComponent<Text>();
                }
                if (num != null)
                {
                    num.text = count;
                    num.gameObject.SetActive(true);
                }
            }
            else if (numTr != null)
            {
                numTr.gameObject.SetActive(false);
            }
        }

        // 复用通用凹槽进度条（Item/物品卡底 16:578 + 17:7134 金色渐变填充）。
        public static RectTransform ProgressBar(Transform parent, float x, float y, float w, float h,
            float ratio, string text, out RectTransform fillRt, out Text label)
        {
            var track = Sliced(parent, "prog", "Ui/modal_prog_track", BorderProgTrack, x, y, w, h, false);
            fillRt = Sliced(track, "fill", "Ui/modal_prog_fill", BorderProgFill, 3f, 3f, w - 6f, h - 6f, false);
            SetProgress(fillRt, w, h, ratio);
            label = OutlinedText(track, "t", text ?? "", 15, 0f, 0f, w, h, Color.white, TextAnchor.MiddleCenter, 1.2f);
            return track;
        }

        public static void SetProgress(RectTransform fillRt, float trackW, float trackH, float ratio)
        {
            if (fillRt == null) return;
            ratio = Mathf.Clamp01(ratio);
            if (ratio <= 0.005f)
            {
                fillRt.gameObject.SetActive(false);
                return;
            }
            fillRt.gameObject.SetActive(true);
            float innerW = Mathf.Max(1f, trackW - 6f);
            float innerH = Mathf.Max(4f, trackH - 6f);
            float fw = Mathf.Clamp(innerW * ratio, innerH, innerW);
            TopLeft(fillRt, 3f, 3f, fw, innerH);
        }

        static int FitBtnFontSize(int baseSize, string text)
        {
            if (string.IsNullOrEmpty(text)) return baseSize;
            if (baseSize >= 24 && text.Length >= 9) return 20;
            if (baseSize >= 24 && text.Length >= 7) return 22;
            return baseSize;
        }

        // 复用原生尺寸 Button/尖角主按钮，零拉伸变形、同档位字号字体绝对统一。
        public static Button HexBtn(Transform parent, string kind, float x, float y, string text, Action onClick,
            out Text label)
        {
            GetBtnSpec(kind, out string sprKey, out float w, out float h, out float faceH, out int baseFs, out bool whiteText);
            int fs = FitBtnFontSize(baseFs, text);
            var go = new GameObject("btn_" + kind, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            TopLeft(go.GetComponent<RectTransform>(), x, y, w, h);
            var img = go.GetComponent<Image>();
            img.sprite = Load(sprKey);
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick());

            label = whiteText
                ? OutlinedText(go.transform, "t", text, fs, 8f, 0f, w - 16f, faceH, Color.white, TextAnchor.MiddleCenter)
                : SolidText(go.transform, "t", text, fs, 8f, 0f, w - 16f, faceH, Ink, TextAnchor.MiddleCenter, true);
            // 尖角按钮字号按档位锁死，不跟正文那一档放大。
            label.fontSize = fs;
            return btn;
        }

        public static void SetBtnLabel(Button btn, string kind, string text)
        {
            if (btn == null) return;
            GetBtnSpec(kind, out _, out float w, out _, out float faceH, out int baseFs, out bool whiteText);
            var label = btn.transform.Find("t")?.GetComponent<Text>();
            if (label == null) return;
            label.text = text ?? "";
            label.fontSize = FitBtnFontSize(baseFs, text);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = whiteText ? Color.white : Ink;
            TopLeft(label.rectTransform, 8f, 0f, w - 16f, faceH);
            var o = label.GetComponent<Outline>();
            if (whiteText)
            {
                if (o == null)
                {
                    o = label.gameObject.AddComponent<Outline>();
                    o.effectColor = Ink;
                    o.effectDistance = new Vector2(1.35f, -1.35f);
                }
                o.enabled = true;
            }
            else if (o != null)
            {
                o.enabled = false;
            }
        }

        // 在尖角主按钮上设置统一的「场记板看广告」小图标 + 文字整体水平居中（showAd=false 时恢复普通居中文字）。
        public static void SetAdBtnLabel(Button btn, string kind, string text, bool showAd = true, int fontSizeOverride = 0)
        {
            if (btn == null) return;
            GetBtnSpec(kind, out _, out float w, out _, out float faceH, out int baseFs, out _);
            int fs = fontSizeOverride > 0 ? fontSizeOverride : FitBtnFontSize(baseFs, text);

            var label = btn.transform.Find("t")?.GetComponent<Text>();
            if (label != null) label.fontSize = fs;

            Transform adTr = btn.transform.Find("ad_ico");
            if (!showAd)
            {
                if (adTr != null) adTr.gameObject.SetActive(false);
                if (label != null)
                {
                    label.text = text;
                    label.alignment = TextAnchor.MiddleCenter;
                    TopLeft(label.rectTransform, 8f, 0f, w - 16f, faceH);
                }
                return;
            }

            Image adImg = adTr != null ? adTr.GetComponent<Image>() : null;
            if (adImg == null)
            {
                adImg = Pic(btn.transform, "ad_ico", "Ui/ico_ad", 0f, 0f, 28f, 28f, true);
            }
            else
            {
                adTr.gameObject.SetActive(true);
                adImg.sprite = Load("Ui/ico_ad");
            }

            float icoS = Mathf.Min(32f, Mathf.Round(faceH * 0.58f));
            float tw = MeasureText(text, fs);
            const float gap = 6f;
            float total = icoS + gap + tw;
            float sx = Mathf.Max(14f, Mathf.Round((w - total) * 0.5f));
            TopLeft(adImg.rectTransform, sx, Mathf.Round((faceH - icoS) * 0.5f), icoS, icoS);

            if (label != null)
            {
                label.text = text;
                label.alignment = TextAnchor.MiddleLeft;
                TopLeft(label.rectTransform, sx + icoS + gap, 0f, Mathf.Max(40f, w - (sx + icoS + gap) - 10f), faceH);
            }
        }

        // 在尖角主按钮上设置「前缀 (-N ⚡)」体力消耗格式（不写「体力」二字）。
        public static void SetStaminaBtnLabel(Button btn, string kind, string prefix, int cost, int fontSizeOverride = 0)
        {
            if (btn == null) return;
            GetBtnSpec(kind, out _, out float w, out _, out float faceH, out int baseFs, out _);
            int fs = fontSizeOverride > 0 ? fontSizeOverride : baseFs;

            var label = btn.transform.Find("t")?.GetComponent<Text>();
            string leftTxt = prefix + " (-" + cost;
            const string rightTxt = ")";
            float lw = MeasureText(leftTxt, fs);
            float rw = MeasureText(rightTxt, fs);
            float icoS = Mathf.Round(fs * 1.18f);
            float total = lw + 3f + icoS + 2f + rw;
            float sx = Mathf.Max(14f, Mathf.Round((w - total) * 0.5f));

            if (label != null)
            {
                label.fontSize = fs;
                label.text = leftTxt;
                label.alignment = TextAnchor.MiddleLeft;
                TopLeft(label.rectTransform, sx, 0f, lw + 8f, faceH);
            }

            Transform stTr = btn.transform.Find("stam_ico");
            Image stImg = stTr != null ? stTr.GetComponent<Image>() : Pic(btn.transform, "stam_ico", "Ui/ico_stamina", 0f, 0f, icoS, icoS, true);
            TopLeft(stImg.rectTransform, sx + lw + 3f, Mathf.Round((faceH - icoS) * 0.5f), icoS, icoS);

            Transform rtTr = btn.transform.Find("t_right");
            Text rtTxt = rtTr != null ? rtTr.GetComponent<Text>() : SolidText(btn.transform, "t_right", rightTxt, fs, 0f, 0f, 24f, faceH, Ink, TextAnchor.MiddleLeft, true);
            rtTxt.fontSize = fs;
            rtTxt.text = rightTxt;
            TopLeft(rtTxt.rectTransform, sx + lw + 3f + icoS + 2f, 0f, rw + 12f, faceH);
        }

        public static float MeasureText(string s, int fontSize)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            float w = 0f;
            for (int i = 0; i < s.Length; i++)
                w += s[i] > 255 ? fontSize : fontSize * 0.58f;
            return Mathf.Round(w);
        }

        public static void SetHexBtnGray(Button btn, string activeKind, string grayKind, bool gray)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            string k = gray ? grayKind : activeKind;
            GetBtnSpec(k, out string sprKey, out _, out _, out _, out int baseFs, out bool whiteText);
            img.sprite = Load(sprKey);
            img.material = null;

            var label = btn.transform.Find("t")?.GetComponent<Text>();
            if (label != null)
            {
                label.fontSize = FitBtnFontSize(baseFs, label.text);
                label.color = whiteText ? Color.white : Ink;
                var o = label.GetComponent<Outline>();
                if (whiteText)
                {
                    if (o == null)
                    {
                        o = label.gameObject.AddComponent<Outline>();
                        o.effectColor = Ink;
                        o.effectDistance = new Vector2(1.35f, -1.35f);
                    }
                    o.enabled = true;
                }
                else if (o != null)
                {
                    o.enabled = false;
                }
            }
        }

        // 所有尖角主按钮严格按档位统一字号与字重：小 18 / 中 20 / 宽 24 / 大 24。
        static void GetBtnSpec(string kind, out string key, out float w, out float h, out float faceH,
            out int fontSize, out bool whiteText)
        {
            whiteText = false;
            switch (kind)
            {
                case "sm_yellow":
                    key = "Ui/modal_btn_sm_yellow"; w = 132f; h = 51f; faceH = 43f; fontSize = 18; return;
                case "sm_cyan":
                    key = "Ui/modal_btn_sm_cyan"; w = 132f; h = 51f; faceH = 43f; fontSize = 18; return;
                case "sm_green":
                    key = "Ui/modal_btn_sm_green"; w = 132f; h = 51f; faceH = 43f; fontSize = 18; return;
                case "sm_gray":
                    key = "Ui/modal_btn_sm_gray"; w = 132f; h = 51f; faceH = 43f; fontSize = 18; return;
                case "md_yellow":
                    key = "Ui/modal_btn_md_yellow"; w = 176f; h = 63f; faceH = 53f; fontSize = 20; return;
                case "md_cyan":
                    key = "Ui/modal_btn_md_cyan"; w = 176f; h = 63f; faceH = 53f; fontSize = 20; return;
                case "md_green":
                    key = "Ui/modal_btn_md_green"; w = 176f; h = 63f; faceH = 53f; fontSize = 20; return;
                case "md_gray":
                    key = "Ui/modal_btn_md_gray"; w = 176f; h = 63f; faceH = 53f; fontSize = 20; return;
                case "lg_yellow":
                    key = "Ui/modal_btn_lg_yellow"; w = 296f; h = 101f; faceH = 84f; fontSize = 24; return;
                case "lg_cyan":
                    key = "Ui/modal_btn_lg_cyan"; w = 296f; h = 101f; faceH = 84f; fontSize = 24; return;
                case "lg_red":
                    key = "Ui/modal_btn_lg_red"; w = 296f; h = 101f; faceH = 84f; fontSize = 24; whiteText = true; return;
                case "lg_gray":
                    key = "Ui/modal_btn_lg_gray"; w = 296f; h = 101f; faceH = 84f; fontSize = 24; return;
                case "wide_gray":
                    key = "Ui/modal_btn_wide_gray"; w = 314f; h = 80f; faceH = 66f; fontSize = 24; return;
                default: // wide_yellow
                    key = "Ui/modal_btn_wide_yellow"; w = 314f; h = 80f; faceH = 66f; fontSize = 24; return;
            }
        }

        public static Text OutlinedText(Transform parent, string name, string text, int size,
            float x, float y, float w, float h, Color color, TextAnchor align, float strokeDist = 1.35f)
        {
            var t = SolidText(parent, name, text, size, x, y, w, h, color, align, true);
            var edge = t.gameObject.AddComponent<Outline>();
            edge.effectColor = Ink;
            edge.effectDistance = new Vector2(strokeDist, -strokeDist);
            return t;
        }

        public static Text SolidText(Transform parent, string name, string text, int size,
            float x, float y, float w, float h, Color color, TextAnchor align, bool bold)
        {
            var t = UiKit.Label(parent, name, text, size, Vector2.zero, new Vector2(w, h), align, Pin.TopLeft);
            TopLeft(t.rectTransform, x, y, w, h);
            t.color = color;
            if (bold) UiKit.Bold(t);
            t.raycastTarget = false;
            return t;
        }
    }
}
