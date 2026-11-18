using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    /// <summary>Small UI Toolkit helper layer: theme tokens, fonts, styled builders.</summary>
    public static class U
    {
        public static Font Body, Semi, Bold, Display, DisplaySemi;
        public static Color Bg, Ink, Muted, Line, Gold, Surface, Surface2, Plate, Shadow;
        public static bool Dark = true;
        public static readonly Color GoldInk = Hex("#1A1307");

        public static void InitFonts()
        {
            Body = Resources.Load<Font>("fonts/IBMPlexSansArabic-Regular");
            Semi = Resources.Load<Font>("fonts/IBMPlexSansArabic-SemiBold") ?? Body;
            Bold = Resources.Load<Font>("fonts/IBMPlexSansArabic-Bold") ?? Semi;
            Display = Resources.Load<Font>("fonts/ReemKufi-Bold") ?? Bold;
            DisplaySemi = Resources.Load<Font>("fonts/ReemKufi-SemiBold") ?? Display;
        }
        public static void ApplyTheme(bool dark)
        {
            Dark = dark;
            if (dark)
            {
                Bg = Hex("#091012"); Ink = Hex("#ECE5D8"); Muted = Hex("#8B9897"); Line = new Color(236 / 255f, 229 / 255f, 216 / 255f, .09f);
                Gold = Hex("#D9B568"); Surface = new Color(1, 1, 1, .045f); Surface2 = Hex("#162022"); Plate = Hex("#030607"); Shadow = new Color(0, 0, 0, .5f);
            }
            else
            {
                Bg = Hex("#E6E9E7"); Ink = Hex("#0E1A1C"); Muted = Hex("#5D6B6A"); Line = new Color(14 / 255f, 26 / 255f, 28 / 255f, .10f);
                Gold = Hex("#9A6F1E"); Surface = new Color(1, 1, 1, .62f); Surface2 = Hex("#F7F8F7"); Plate = Hex("#0A1113"); Shadow = new Color(10 / 255f, 20 / 255f, 22 / 255f, .18f);
            }
        }
        public static Color Hex(string h, float a = 1f) { ColorUtility.TryParseHtmlString(h, out var c); c.a = a; return c; }

        // ---------- element builders ----------
        public static VisualElement E(string cls = null, VisualElement parent = null)
        {
            var e = new VisualElement(); e.pickingMode = PickingMode.Ignore; if (cls != null) e.name = cls; parent?.Add(e); return e;
        }
        public static Label L(string text, float size = 14, Color? color = null, Font font = null, VisualElement parent = null, TextAnchor align = TextAnchor.MiddleCenter, bool wrap = false)
        {
            var l = new Label(text ?? ""); l.pickingMode = PickingMode.Ignore;
            l.style.fontSize = size; l.style.color = color ?? Ink; l.style.unityTextAlign = align;
            l.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(font ?? Body));
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0;
            l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0;
            if (wrap) { l.style.whiteSpace = WhiteSpace.Normal; }
            parent?.Add(l); return l;
        }
        public static VisualElement Row(VisualElement parent = null, float gap = 0, bool mirror = true)
        {
            var e = E(null, parent); e.style.flexDirection = (mirror && Save.Ar) ? FlexDirection.RowReverse : FlexDirection.Row; e.style.alignItems = Align.Center;
            if (gap > 0) e.userData = gap; return e;
        }
        public static VisualElement Col(VisualElement parent = null) { var e = E(null, parent); e.style.flexDirection = FlexDirection.Column; return e; }
        /// <summary>Apply a uniform gap between children (call after adding children).</summary>
        public static void Gap(VisualElement row, float gap)
        {
            bool rowDir = row.style.flexDirection.value == FlexDirection.Row || row.style.flexDirection.value == FlexDirection.RowReverse;
            int i = 0;
            foreach (var c in row.Children())
            {
                if (i++ == 0) continue;
                if (rowDir) { if (row.style.flexDirection.value == FlexDirection.RowReverse) c.style.marginRight = gap; else c.style.marginLeft = gap; }
                else c.style.marginTop = gap;
            }
        }
        public static void Radius(VisualElement e, float r) { e.style.borderTopLeftRadius = e.style.borderTopRightRadius = e.style.borderBottomLeftRadius = e.style.borderBottomRightRadius = r; }
        public static void Pad(VisualElement e, float v, float h) { e.style.paddingTop = e.style.paddingBottom = v; e.style.paddingLeft = e.style.paddingRight = h; }
        public static void Pad(VisualElement e, float t, float r, float b, float l) { e.style.paddingTop = t; e.style.paddingRight = r; e.style.paddingBottom = b; e.style.paddingLeft = l; }
        public static void Border(VisualElement e, float w, Color c)
        {
            e.style.borderTopWidth = e.style.borderBottomWidth = e.style.borderLeftWidth = e.style.borderRightWidth = w;
            e.style.borderTopColor = e.style.borderBottomColor = e.style.borderLeftColor = e.style.borderRightColor = c;
        }
        public static void Abs(VisualElement e, float? l = null, float? t = null, float? r = null, float? b = null)
        {
            e.style.position = Position.Absolute;
            if (l.HasValue) e.style.left = l.Value; if (t.HasValue) e.style.top = t.Value; if (r.HasValue) e.style.right = r.Value; if (b.HasValue) e.style.bottom = b.Value;
        }
        public static void Fill(VisualElement e) { Abs(e, 0, 0, 0, 0); }
        public static void Size(VisualElement e, float w, float h) { e.style.width = w; e.style.height = h; }
        public static void Show(VisualElement e, bool on) { e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; }
        public static VisualElement Img(Texture2D t, float w, float h, VisualElement parent = null, Color? tint = null)
        {
            var e = E(null, parent); Size(e, w, h); e.style.backgroundImage = new StyleBackground(t); e.style.flexShrink = 0;
            if (tint.HasValue) e.style.unityBackgroundImageTintColor = tint.Value; return e;
        }
        public static VisualElement Icon(string key, float s, Color tint, VisualElement parent = null) { return Img(Content.Icon(key), s, s, parent, tint); }
        public static VisualElement Coin(float s = 16, VisualElement parent = null) { return Img(Content.Icon("coin"), s * .875f, s, parent); }

        /// <summary>Tap target with press feedback.</summary>
        public static VisualElement Tap(VisualElement e, Action onClick, bool sound = true)
        {
            e.pickingMode = PickingMode.Position;
            e.RegisterCallback<PointerDownEvent>(ev => { if (e.enabledSelf) e.style.scale = new Scale(new Vector3(.96f, .96f, 1)); });
            e.RegisterCallback<PointerUpEvent>(ev => e.style.scale = new Scale(Vector3.one));
            e.RegisterCallback<PointerLeaveEvent>(ev => e.style.scale = new Scale(Vector3.one));
            e.RegisterCallback<ClickEvent>(ev => { if (!e.enabledSelf) return; if (sound) Sfx.Ui(); onClick?.Invoke(); ev.StopPropagation(); });
            return e;
        }
        public enum BK { Surface, Primary, Green, Wa, Red, Ghost, Violet }
        public static VisualElement Btn(string text, BK kind, Action onClick, VisualElement parent = null, float h = 52, string icon = null)
        {
            var b = E(null, parent); b.style.height = h; b.style.minHeight = h; Radius(b, h / 2); Pad(b, 0, 20);
            b.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row; b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center;
            Color tc = Ink; Font f = Semi;
            switch (kind)
            {
                case BK.Surface: b.style.backgroundColor = Surface; Border(b, 1, Line); break;
                case BK.Ghost: tc = Muted; f = Body; break;
                case BK.Primary: b.style.backgroundImage = new StyleBackground(Content.Tex("tex/g_gold")); tc = GoldInk; break;
                case BK.Green: b.style.backgroundImage = new StyleBackground(Content.Tex("tex/g_green")); tc = Hex("#06281C"); break;
                case BK.Wa: b.style.backgroundImage = new StyleBackground(Content.Tex("tex/g_wa")); tc = Color.white; break;
                case BK.Red: b.style.backgroundImage = new StyleBackground(Content.Tex("tex/g_red")); tc = Color.white; break;
                case BK.Violet: b.style.backgroundImage = new StyleBackground(Content.Tex("tex/g_violet")); Border(b, 1, Hex("#BA96FF", .7f)); tc = Hex("#E2D4FF"); break;
            }
            if (icon != null) { var ic = Icon(icon, 19, tc, b); if (Save.Ar) ic.style.marginLeft = 7; else ic.style.marginRight = 7; }
            var l = L(text, h >= 48 ? 16 : 14, tc, f, b); l.name = "label";
            Tap(b, onClick); return b;
        }
        public static void SetBtnText(VisualElement b, string t) { var l = b.Q<Label>("label"); if (l != null) l.text = t; }
        public static void Enable(VisualElement e, bool on) { e.SetEnabled(on); e.style.opacity = on ? 1 : .5f; }
        public static VisualElement GoldPill(out Label val, VisualElement parent = null, bool big = false)
        {
            var p = Row(parent); Radius(p, 999); Pad(p, big ? 8 : 6, big ? 18 : 13); p.style.backgroundColor = Surface; Border(p, 1, Line);
            Coin(big ? 20 : 16, p); val = L("0", big ? 20 : 15, Ink, Semi, p); if (Save.Ar) val.style.marginRight = 7; else val.style.marginLeft = 7;
            return p;
        }
        public static VisualElement IconBtn(string icon, Action onClick, VisualElement parent = null, float s = 44, float icon_s = 22)
        {
            var b = E(null, parent); Size(b, s, s); Radius(b, s / 2); b.style.backgroundColor = Surface; Border(b, 1, Line);
            b.style.alignItems = Align.Center; b.style.justifyContent = Justify.Center; b.style.flexShrink = 0;
            var ic = Icon(icon, icon_s, Ink, b); ic.name = "icon";
            Tap(b, onClick); return b;
        }
        public static void SetIcon(VisualElement iconBtn, string icon) { var ic = iconBtn.Q("icon"); if (ic != null) ic.style.backgroundImage = new StyleBackground(Content.Icon(icon)); }
        public static VisualElement Card(VisualElement parent = null, float r = 20)
        {
            var c = E(null, parent); Radius(c, r); c.style.backgroundColor = Surface; Border(c, 1, Line); return c;
        }
        public static VisualElement Badge(string text, VisualElement parent, Texture2D grad = null, Color? tc = null)
        {
            var b = E(null, parent); Radius(b, 9); Pad(b, 2, 7); b.style.backgroundImage = new StyleBackground(grad ?? Content.Tex("tex/g_gold"));
            var l = L(text, 10.5f, tc ?? GoldInk, Bold, b); l.name = "label"; return b;
        }
    }

    /// <summary>Pointy-top hexagon filled with a (cover-fit) texture — thumbnails, frames.</summary>
    public class HexImage : VisualElement
    {
        Texture tex; public Color tint = Color.white; public Color frame = new Color(0, 0, 0, 0); public float frameW = 1.5f; public float inset = 0;
        public HexImage(Texture t, float w, Color? fr = null)
        {
            tex = t; style.width = w; style.height = w / 0.866f; style.flexShrink = 0; pickingMode = PickingMode.Ignore;
            if (fr.HasValue) frame = fr.Value;
            generateVisualContent += Draw;
        }
        public void SetTexture(Texture t) { tex = t; MarkDirtyRepaint(); }
        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect; if (r.width < 2) return;
            float cx = r.width / 2, cy = r.height / 2, R = Mathf.Min(r.height / 2, r.width / Mathf.Sqrt(3)) ;
            if (frame.a > 0) Mesh2D.Hex(mgc, null, cx, cy, R, frame, null);
            float ri = frame.a > 0 ? R - frameW : R;
            if (tex != null)
            {
                float S = Mathf.Max(r.width, r.height);
                Mesh2D.Hex(mgc, tex, cx, cy, ri, tint, (x, y) => new Vector2((x - (cx - S / 2)) / S, 1f - (y - (cy - S / 2)) / S));
            }
        }
    }

    /// <summary>Low-level mesh helpers for generateVisualContent.</summary>
    public static class Mesh2D
    {
        static readonly float[] CA = new float[6], SA = new float[6];
        static Mesh2D() { for (int i = 0; i < 6; i++) { float a = Mathf.Deg2Rad * (60 * i - 30); CA[i] = Mathf.Cos(a); SA[i] = Mathf.Sin(a); } }
        public static void Hex(MeshGenerationContext mgc, Texture tex, float x, float y, float r, Color tint, Func<float, float, Vector2> uv)
        {
            var m = mgc.Allocate(7, 18, tex); var reg = m.uvRegion; var vs = new Vertex[7];
            Color32 c = tint;
            vs[0] = V(x, y, c, uv, reg);
            for (int i = 0; i < 6; i++) vs[i + 1] = V(x + r * CA[i], y + r * SA[i], c, uv, reg);
            m.SetAllVertices(vs);
            var id = new ushort[18]; for (int i = 0; i < 6; i++) { id[i * 3] = 0; id[i * 3 + 1] = (ushort)(1 + i); id[i * 3 + 2] = (ushort)(1 + (i + 1) % 6); }
            m.SetAllIndices(id);
        }
        static Vertex V(float x, float y, Color32 c, Func<float, float, Vector2> uv, Rect reg)
        {
            var v = new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = c };
            if (uv != null) { var u = uv(x, y); v.uv = new Vector2(reg.x + u.x * reg.width, reg.y + u.y * reg.height); }
            return v;
        }
        public static readonly float[] HexCos = CA, HexSin = SA;
    }

    /// <summary>Batches many textured/untextured primitives into one allocation per texture layer.</summary>
    public class Batch
    {
        public Texture tex; readonly List<Vertex> v = new List<Vertex>(256); readonly List<ushort> idx = new List<ushort>(512); Rect reg = new Rect(0, 0, 1, 1);
        public Batch(Texture t) { tex = t; }
        public void Clear() { v.Clear(); idx.Clear(); }
        public int Count => v.Count;
        Vertex Mk(float x, float y, Color32 c, float u, float w) { return new Vertex { position = new Vector3(x, y, Vertex.nearZ), tint = c, uv = new Vector2(u, w) }; }
        /// <summary>Hex polygon; uvf maps a point to texture uv (0..1).</summary>
        public void Hex(float x, float y, float r, Color c, Func<float, float, Vector2> uvf = null)
        {
            if (v.Count + 7 > 64000) return;
            ushort b = (ushort)v.Count; var u0 = uvf != null ? uvf(x, y) : Vector2.zero;
            v.Add(Mk(x, y, c, u0.x, u0.y));
            for (int i = 0; i < 6; i++) { float px = x + r * Mesh2D.HexCos[i], py = y + r * Mesh2D.HexSin[i]; var u = uvf != null ? uvf(px, py) : Vector2.zero; v.Add(Mk(px, py, c, u.x, u.y)); }
            for (int i = 0; i < 6; i++) { idx.Add(b); idx.Add((ushort)(b + 1 + i)); idx.Add((ushort)(b + 1 + (i + 1) % 6)); }
        }
        /// <summary>Axis-aligned or rotated textured quad centered at (x,y).</summary>
        public void Quad(float x, float y, float hw, float hh, Color c, float rot = 0, float u0 = 0, float v0 = 0, float u1 = 1, float v1 = 1)
        {
            if (v.Count + 4 > 64000) return;
            ushort b = (ushort)v.Count; float cs = Mathf.Cos(rot), sn = Mathf.Sin(rot);
            void P(float dx, float dy, float u, float w) { v.Add(Mk(x + dx * cs - dy * sn, y + dx * sn + dy * cs, c, u, w)); }
            P(-hw, -hh, u0, v1); P(hw, -hh, u1, v1); P(hw, hh, u1, v0); P(-hw, hh, u0, v0);
            idx.Add(b); idx.Add((ushort)(b + 1)); idx.Add((ushort)(b + 2)); idx.Add(b); idx.Add((ushort)(b + 2)); idx.Add((ushort)(b + 3));
        }
        /// <summary>Line segment as a thin quad.</summary>
        public void Line(float x0, float y0, float x1, float y1, float w, Color c)
        {
            float dx = x1 - x0, dy = y1 - y0, l = Mathf.Sqrt(dx * dx + dy * dy); if (l < .01f) return;
            Quad((x0 + x1) / 2, (y0 + y1) / 2, l / 2, w / 2, c, Mathf.Atan2(dy, dx));
        }
        public void Flush(MeshGenerationContext mgc)
        {
            if (v.Count == 0) return;
            var m = mgc.Allocate(v.Count, idx.Count, tex); var r = m.uvRegion;
            if (tex != null && (r.x != 0 || r.y != 0 || r.width != 1 || r.height != 1))
            {
                var arr = v.ToArray(); for (int i = 0; i < arr.Length; i++) arr[i].uv = new Vector2(r.x + arr[i].uv.x * r.width, r.y + arr[i].uv.y * r.height);
                m.SetAllVertices(arr);
            }
            else m.SetAllVertices(v.ToArray());
            m.SetAllIndices(idx.ToArray()); Clear();
        }
    }
}
