using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace InkLine
{
    public sealed class InkHanziComposer : EditorWindow
    {
        const int Size = 512;
        const string ArtDir = "Assets/Resources/Art";
        const string PartsDir = "Assets/Resources/Art/hanzi";

        static readonly string[] Keys = { "fire", "ice", "split", "track", "pierce", "explode", "accel", "heavy" };
        static readonly string[] Labels = { "火", "冰", "分", "瞄", "穿", "炸", "速", "重" };
        static readonly string[] Decos = { "deco_flame", "deco_crystal", "deco_fork", "deco_reticle", "deco_spear", "deco_burst", "deco_chevron", "deco_weight" };

        enum Tool { Move, Marquee, Erase }

        int _card;
        Tool _tool = Tool.Move;
        int _sel;
        float _erase = 22f;
        bool _showCard = true;
        bool _lockScale = true;
        readonly List<Piece> _parts = new List<Piece>();
        readonly List<Snap> _undo = new List<Snap>();
        Texture2D _preview;
        Vector2 _press;
        bool _drag;
        bool _marqueeOn;
        Rect _marquee;
        Vector2 _scroll;

        [MenuItem("墨字防线/汉字牌合成")]
        public static void Open()
        {
            var w = GetWindow<InkHanziComposer>("汉字牌合成");
            w.minSize = new Vector2(640, 640);
        }

        void OnEnable()
        {
            EnsurePreview();
            if (_parts.Count == 0) TryLoadCard(_card);
            Rebuild();
        }

        void OnDisable()
        {
            DisposeParts(_parts);
            for (int i = 0; i < _undo.Count; i++) DisposeParts(_undo[i].Parts);
            _undo.Clear();
            if (_preview != null) DestroyImmediate(_preview);
            _preview = null;
        }

        void OnGUI()
        {
            HandleUndoKey();

            EditorGUILayout.HelpBox(
                "导出只出字和饰，不带边框。卡框由游戏统一加。「预览卡框」只是编辑时看着方便。",
                MessageType.None);

            EditorGUI.BeginChangeCheck();
            int card = EditorGUILayout.Popup("牌", _card, Labels);
            if (card != _card)
            {
                PushUndo();
                _card = card;
                TryLoadCard(_card);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("载入字图")) PickAndAdd("字", true);
                if (GUILayout.Button("载入饰图")) PickAndAdd("饰", false);
                if (GUILayout.Button("加本牌饰图")) AddDefaultDeco();
                if (GUILayout.Button("从字体铺字")) LoadFontChar();
                if (GUILayout.Button("撤销", GUILayout.Width(56))) UndoOnce();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _tool = (Tool)GUILayout.Toolbar((int)_tool, new[] { "移动", "框选拆笔", "橡皮" });
                _showCard = GUILayout.Toggle(_showCard, "预览卡框", GUILayout.Width(80));
            }

            if (_tool == Tool.Erase)
                _erase = EditorGUILayout.Slider("橡皮", _erase, 6f, 64f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("导出到 heap_" + Keys[_card], GUILayout.Height(28)))
                    Export(Path.Combine(ArtDir, "heap_" + Keys[_card] + ".png"));
                if (GUILayout.Button("另存零件", GUILayout.Height(28)))
                    Export(EditorUtility.SaveFilePanel("另存", PartsDir, "heap_" + Keys[_card], "png"));
            }

            DrawLayers();
            DrawSelected();
            DrawCanvas();

            if (EditorGUI.EndChangeCheck()) Rebuild();
        }

        void DrawLayers()
        {
            EditorGUILayout.LabelField("层（点选后再拆笔 / 擦 / 变形）", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(92));
            for (int i = 0; i < _parts.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool on = GUILayout.Toggle(_sel == i, _parts[i].Name, "Button");
                    if (on) _sel = i;
                    if (GUILayout.Button("↑", GUILayout.Width(24)) && i > 0)
                    {
                        PushUndo();
                        Swap(i, i - 1);
                    }
                    if (GUILayout.Button("↓", GUILayout.Width(24)) && i < _parts.Count - 1)
                    {
                        PushUndo();
                        Swap(i, i + 1);
                    }
                    if (GUILayout.Button("删", GUILayout.Width(32)))
                    {
                        PushUndo();
                        DestroyImmediate(_parts[i].Tex);
                        _parts.RemoveAt(i);
                        _sel = Mathf.Clamp(_sel, 0, Mathf.Max(0, _parts.Count - 1));
                        Rebuild();
                        GUIUtility.ExitGUI();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void DrawSelected()
        {
            if (!HasSel()) return;
            var p = _parts[_sel];
            EditorGUILayout.LabelField("当前层 · " + p.Name, EditorStyles.boldLabel);
            p.Pos = EditorGUILayout.Vector2Field("位置", p.Pos);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                float sx = EditorGUILayout.Slider("横拉", p.ScaleX, 0.15f, 2.8f);
                float sy = EditorGUILayout.Slider("纵拉", p.ScaleY, 0.15f, 2.8f);
                if (EditorGUI.EndChangeCheck())
                {
                    if (_lockScale)
                    {
                        if (!Mathf.Approximately(sx, p.ScaleX)) sy = sx;
                        else sx = sy;
                    }
                    p.ScaleX = sx;
                    p.ScaleY = sy;
                }
                _lockScale = GUILayout.Toggle(_lockScale, "等比", GUILayout.Width(44));
            }
            p.Rot = EditorGUILayout.Slider("旋转", p.Rot, -180f, 180f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("左右翻")) { PushUndo(); p.FlipH = !p.FlipH; }
                if (GUILayout.Button("上下翻")) { PushUndo(); p.FlipV = !p.FlipV; }
                if (GUILayout.Button("复制层")) DuplicateSel();
            }
        }

        void DrawCanvas()
        {
            float remain = Mathf.Max(220f, position.height - 420f);
            float side = Mathf.Min(Mathf.Min(position.width - 24f, remain), 520f);
            Rect r = GUILayoutUtility.GetRect(side, side, GUILayout.ExpandWidth(true));
            r = new Rect(r.x + (r.width - side) * 0.5f, r.y, side, side);
            if (_preview != null) EditorGUI.DrawPreviewTexture(r, _preview, null, ScaleMode.StretchToFill);
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1f), Color.gray);

            Event e = Event.current;
            if (!r.Contains(e.mousePosition) && !_drag && !_marqueeOn) return;
            Vector2 canvas = GuiToCanvas(r, e.mousePosition);

            if (e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition))
            {
                PushUndo();
                _press = canvas;
                _drag = true;
                _marqueeOn = _tool == Tool.Marquee;
                if (_tool == Tool.Marquee) _marquee = new Rect(canvas.x, canvas.y, 0, 0);
                if (_tool == Tool.Erase) StampErase(canvas);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && _drag)
            {
                if (_tool == Tool.Move && HasSel())
                    _parts[_sel].Pos += canvas - _press;
                else if (_tool == Tool.Marquee)
                    _marquee = FromPress(canvas);
                else if (_tool == Tool.Erase)
                    StampErase(canvas);
                _press = canvas;
                Rebuild();
                e.Use();
            }
            else if (e.type == EventType.MouseUp && _drag)
            {
                if (_tool == Tool.Marquee && _marqueeOn) ExtractMarquee();
                _drag = false;
                _marqueeOn = false;
                Rebuild();
                e.Use();
            }

            if (_marqueeOn && _marquee.width > 2f)
            {
                Rect g = CanvasToGui(r, _marquee);
                Handles.BeginGUI();
                Handles.color = new Color(0.2f, 0.6f, 1f, 0.9f);
                Handles.DrawSolidRectangleWithOutline(g, new Color(0.2f, 0.6f, 1f, 0.12f), Color.cyan);
                Handles.EndGUI();
                Repaint();
            }
        }

        void TryLoadCard(int card)
        {
            DisposeParts(_parts);
            _parts.Clear();
            _sel = 0;
            string font = Path.Combine(PartsDir, "font_" + Keys[card] + ".png");
            string gen = Path.Combine(PartsDir, "char_" + Keys[card] + ".png");
            string path = File.Exists(gen) ? gen : font;
            if (File.Exists(path)) AddFromFile(path, Labels[card], 1f);
            Rebuild();
        }

        void LoadFontChar()
        {
            string path = Path.Combine(PartsDir, "font_" + Keys[_card] + ".png");
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("汉字牌合成", "还没有字体字图：\n" + path, "好");
                return;
            }
            PushUndo();
            DisposeParts(_parts);
            _parts.Clear();
            AddFromFile(path, Labels[_card], 1f);
            _sel = 0;
            Rebuild();
        }

        void AddDefaultDeco()
        {
            string path = Path.Combine(PartsDir, Decos[_card] + ".png");
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("汉字牌合成", "还没有饰图：\n" + path, "好");
                return;
            }
            PushUndo();
            AddFromFile(path, Labels[_card] + "饰", 0.55f);
            _sel = _parts.Count - 1;
            Rebuild();
        }

        void PickAndAdd(string name, bool replaceChar)
        {
            string path = EditorUtility.OpenFilePanel("选图", PartsDir, "png");
            if (string.IsNullOrEmpty(path)) return;
            PushUndo();
            if (replaceChar)
            {
                DisposeParts(_parts);
                _parts.Clear();
                _sel = 0;
            }
            AddFromFile(path, name, replaceChar ? 1f : 0.55f);
            if (!replaceChar) _sel = _parts.Count - 1;
            Rebuild();
        }

        void AddFromFile(string path, string name, float scale)
        {
            var tex = ReadPng(path);
            if (tex == null) return;
            _parts.Add(new Piece
            {
                Name = name,
                Tex = tex,
                Pos = new Vector2(Size * 0.5f, Size * 0.5f),
                ScaleX = scale,
                ScaleY = scale
            });
        }

        void DuplicateSel()
        {
            if (!HasSel()) return;
            PushUndo();
            var src = _parts[_sel];
            var copy = src.Clone();
            copy.Name = src.Name + "拷";
            copy.Pos += new Vector2(18f, -18f);
            _parts.Add(copy);
            _sel = _parts.Count - 1;
            Rebuild();
        }

        void Swap(int a, int b)
        {
            var t = _parts[a];
            _parts[a] = _parts[b];
            _parts[b] = t;
            _sel = b;
            Rebuild();
        }

        bool HasSel() => _sel >= 0 && _sel < _parts.Count;

        void StampErase(Vector2 canvas)
        {
            if (!HasSel()) return;
            var p = _parts[_sel];
            Color[] px = p.Tex.GetPixels();
            int w = p.Tex.width, h = p.Tex.height;
            float rad = _erase / Mathf.Max(0.05f, 0.5f * (p.ScaleX + p.ScaleY));
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                Vector2 c = LocalToCanvas(p, new Vector2(x + 0.5f, y + 0.5f));
                if ((c - canvas).sqrMagnitude > rad * rad) continue;
                int i = y * w + x;
                Color col = px[i];
                col.a = 0f;
                px[i] = col;
            }
            p.Tex.SetPixels(px);
            p.Tex.Apply(false, false);
        }

        void ExtractMarquee()
        {
            if (!HasSel()) return;
            Rect m = Norm(_marquee);
            if (m.width < 4f || m.height < 4f) return;
            var src = _parts[_sel];
            int x0 = Mathf.Clamp(Mathf.FloorToInt(m.xMin), 0, Size - 1);
            int y0 = Mathf.Clamp(Mathf.FloorToInt(m.yMin), 0, Size - 1);
            int x1 = Mathf.Clamp(Mathf.CeilToInt(m.xMax), 1, Size);
            int y1 = Mathf.Clamp(Mathf.CeilToInt(m.yMax), 1, Size);
            int nw = x1 - x0, nh = y1 - y0;
            var cut = new Texture2D(nw, nh, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            var dst = new Color[nw * nh];
            Color[] sp = src.Tex.GetPixels();
            int sw = src.Tex.width, sh = src.Tex.height;
            bool any = false;
            for (int y = 0; y < nh; y++)
            for (int x = 0; x < nw; x++)
            {
                Vector2 canvas = new Vector2(x0 + x + 0.5f, y0 + y + 0.5f);
                Vector2 local = CanvasToLocal(src, canvas);
                int lx = Mathf.FloorToInt(local.x);
                int ly = Mathf.FloorToInt(local.y);
                Color col = new Color(0, 0, 0, 0);
                if (lx >= 0 && ly >= 0 && lx < sw && ly < sh)
                {
                    int i = ly * sw + lx;
                    col = sp[i];
                    if (col.a > 0.04f)
                    {
                        any = true;
                        sp[i] = new Color(col.r, col.g, col.b, 0f);
                    }
                }
                dst[y * nw + x] = col;
            }
            if (!any)
            {
                DestroyImmediate(cut);
                return;
            }
            src.Tex.SetPixels(sp);
            src.Tex.Apply(false, false);
            cut.SetPixels(dst);
            cut.Apply(false, false);
            _parts.Add(new Piece
            {
                Name = src.Name + "笔",
                Tex = cut,
                Pos = new Vector2(x0 + nw * 0.5f, y0 + nh * 0.5f),
                ScaleX = 1f,
                ScaleY = 1f
            });
            _sel = _parts.Count - 1;
        }

        void Rebuild()
        {
            EnsurePreview();
            var px = new Color[Size * Size];
            Color paper = new Color(0.988f, 0.988f, 0.965f, 1f);
            Color ink = new Color(0.18f, 0.18f, 0.18f, 1f);
            for (int i = 0; i < px.Length; i++) px[i] = _showCard ? paper : new Color(1, 1, 1, 0);
            if (_showCard) StrokeFrame(px, ink);
            for (int i = 0; i < _parts.Count; i++) Blit(px, _parts[i]);
            _preview.SetPixels(px);
            _preview.Apply(false, false);
            Repaint();
        }

        void StrokeFrame(Color[] px, Color ink)
        {
            int m = 18, inner = 28;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                bool o = InRing(x, y, m, 4);
                bool n = InRing(x, y, inner, 3);
                if (o || n) px[y * Size + x] = ink;
            }
        }

        static bool InRing(int x, int y, int pad, int thick)
        {
            bool inside = x >= pad && y >= pad && x < Size - pad && y < Size - pad;
            bool core = x >= pad + thick && y >= pad + thick && x < Size - pad - thick && y < Size - pad - thick;
            return inside && !core;
        }

        void Blit(Color[] dst, Piece p)
        {
            if (p.Tex == null) return;
            Color[] src = p.Tex.GetPixels();
            int w = p.Tex.width, h = p.Tex.height;
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                Vector2 local = CanvasToLocal(p, new Vector2(x + 0.5f, y + 0.5f));
                Color s = Sample(src, w, h, local.x, local.y);
                if (s.a < 0.02f) continue;
                int i = y * Size + x;
                dst[i] = Blend(dst[i], s);
            }
        }

        static Color Sample(Color[] src, int w, int h, float x, float y)
        {
            if (x < 0 || y < 0 || x >= w - 1 || y >= h - 1) return new Color(0, 0, 0, 0);
            int x0 = (int)x, y0 = (int)y;
            float fx = x - x0, fy = y - y0;
            Color a = src[y0 * w + x0];
            Color b = src[y0 * w + x0 + 1];
            Color c = src[(y0 + 1) * w + x0];
            Color d = src[(y0 + 1) * w + x0 + 1];
            return Color.Lerp(Color.Lerp(a, b, fx), Color.Lerp(c, d, fx), fy);
        }

        static Color Blend(Color under, Color over)
        {
            float a = over.a + under.a * (1f - over.a);
            if (a < 0.001f) return new Color(0, 0, 0, 0);
            Color rgb = (over * over.a + under * under.a * (1f - over.a)) / a;
            rgb.a = a;
            return rgb;
        }

        static float Axis(Piece p, bool horiz) =>
            Mathf.Max(0.05f, (horiz ? p.ScaleX : p.ScaleY) * (horiz ? (p.FlipH ? -1f : 1f) : (p.FlipV ? -1f : 1f)));

        Vector2 CanvasToLocal(Piece p, Vector2 canvas)
        {
            Vector2 d = canvas - p.Pos;
            float rad = -p.Rot * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            Vector2 r = new Vector2(c * d.x - s * d.y, s * d.x + c * d.y);
            r.x /= Axis(p, true);
            r.y /= Axis(p, false);
            return new Vector2(r.x + p.Tex.width * 0.5f, r.y + p.Tex.height * 0.5f);
        }

        Vector2 LocalToCanvas(Piece p, Vector2 local)
        {
            Vector2 r = new Vector2(
                (local.x - p.Tex.width * 0.5f) * Axis(p, true),
                (local.y - p.Tex.height * 0.5f) * Axis(p, false));
            float rad = p.Rot * Mathf.Deg2Rad;
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return p.Pos + new Vector2(c * r.x - s * r.y, s * r.x + c * r.y);
        }

        Vector2 GuiToCanvas(Rect r, Vector2 gui)
        {
            float x = (gui.x - r.x) / r.width * Size;
            float y = (1f - (gui.y - r.y) / r.height) * Size;
            return new Vector2(x, y);
        }

        Rect CanvasToGui(Rect r, Rect m)
        {
            m = Norm(m);
            float x = r.x + m.xMin / Size * r.width;
            float y = r.y + (1f - m.yMax / Size) * r.height;
            return new Rect(x, y, m.width / Size * r.width, m.height / Size * r.height);
        }

        Rect FromPress(Vector2 canvas) => new Rect(_press.x, _press.y, canvas.x - _press.x, canvas.y - _press.y);

        static Rect Norm(Rect r)
        {
            if (r.width < 0) { r.x += r.width; r.width = -r.width; }
            if (r.height < 0) { r.y += r.height; r.height = -r.height; }
            return r;
        }

        void Export(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            bool was = _showCard;
            _showCard = false;
            Rebuild();
            byte[] png = _preview.EncodeToPNG();
            _showCard = was;
            Rebuild();
            if (path.StartsWith("Assets"))
            {
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path);
            }
            else File.WriteAllBytes(path, png);
            Debug.Log("汉字牌已导出 → " + path);
        }

        void EnsurePreview()
        {
            if (_preview != null && _preview.width == Size) return;
            if (_preview != null) DestroyImmediate(_preview);
            _preview = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        static Texture2D ReadPng(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!tex.LoadImage(File.ReadAllBytes(path)))
            {
                DestroyImmediate(tex);
                return null;
            }
            return tex;
        }

        void HandleUndoKey()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Z && (e.command || e.control))
            {
                UndoOnce();
                e.Use();
            }
        }

        void PushUndo()
        {
            var snap = new Snap { Sel = _sel };
            for (int i = 0; i < _parts.Count; i++) snap.Parts.Add(_parts[i].Clone());
            _undo.Add(snap);
            while (_undo.Count > 16)
            {
                DisposeParts(_undo[0].Parts);
                _undo.RemoveAt(0);
            }
        }

        void UndoOnce()
        {
            if (_undo.Count == 0) return;
            DisposeParts(_parts);
            _parts.Clear();
            var snap = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            for (int i = 0; i < snap.Parts.Count; i++) _parts.Add(snap.Parts[i]);
            _sel = Mathf.Clamp(snap.Sel, 0, Mathf.Max(0, _parts.Count - 1));
            Rebuild();
        }

        static void DisposeParts(List<Piece> parts)
        {
            if (parts == null) return;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].Tex != null) DestroyImmediate(parts[i].Tex);
            }
        }

        sealed class Snap
        {
            public int Sel;
            public readonly List<Piece> Parts = new List<Piece>();
        }

        sealed class Piece
        {
            public string Name;
            public Texture2D Tex;
            public Vector2 Pos;
            public float ScaleX = 1f;
            public float ScaleY = 1f;
            public float Rot;
            public bool FlipH;
            public bool FlipV;

            public Piece Clone()
            {
                var tex = new Texture2D(Tex.width, Tex.height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    hideFlags = HideFlags.HideAndDontSave
                };
                tex.SetPixels(Tex.GetPixels());
                tex.Apply(false, false);
                return new Piece
                {
                    Name = Name,
                    Tex = tex,
                    Pos = Pos,
                    ScaleX = ScaleX,
                    ScaleY = ScaleY,
                    Rot = Rot,
                    FlipH = FlipH,
                    FlipV = FlipV
                };
            }
        }
    }
}
