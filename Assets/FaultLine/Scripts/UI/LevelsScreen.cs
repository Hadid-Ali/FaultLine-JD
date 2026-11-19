using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    public static class Bars
    {
        public static VisualElement Top(VisualElement parent, string title, out VisualElement right, Action back = null)
        {
            var bar = U.Row(parent); U.Pad(bar, 10, 14, 4, 14); bar.style.justifyContent = Justify.SpaceBetween; bar.style.flexShrink = 0;
            var b = U.IconBtn(Save.Ar ? "back" : "back", back ?? (() => App.I.Show("menu")), bar, 44, 22);
            if (!Save.Ar) b.Q("icon").style.scale = new Scale(new Vector3(1, 1, 1)); else b.Q("icon").style.scale = new Scale(new Vector3(-1, 1, 1));
            var t = U.L(title, 17, U.Ink, U.Semi, bar); t.style.flexGrow = 1;
            right = U.Row(bar); right.style.minWidth = 48; right.style.justifyContent = Justify.FlexEnd;
            return bar;
        }
    }

    public class LevelsScreen : Scr
    {
        VisualElement right, segAll, segAlb, segX; ScrollView scroll; VisualElement content; string tab = "all";
        public override void Build()
        {
            root.style.flexDirection = FlexDirection.Column;
            Bars.Top(root, Content.T("levels"), out right);
            var segs = U.Row(root); segs.style.alignSelf = Align.Center; U.Radius(segs, 999); U.Pad(segs, 4, 4); segs.style.backgroundColor = U.Surface; U.Border(segs, 1, U.Line); segs.style.marginTop = 6;
            segAll = Seg(segs, Content.T("tabAll"), () => Open("all")); segAlb = Seg(segs, Content.T("tabAlb"), () => Open("alb")); segX = Seg(segs, Content.T("segX"), () => Open("xp"));
            scroll = new ScrollView(ScrollViewMode.Vertical); scroll.style.flexGrow = 1; scroll.verticalScrollerVisibility = ScrollerVisibility.Hidden; scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden; root.Add(scroll);
            content = scroll.contentContainer; U.Pad(content, 16, 16, 28, 16);
        }
        VisualElement Seg(VisualElement p, string t, Action a)
        {
            var s = U.E(null, p); U.Radius(s, 999); U.Pad(s, 7, 16); var l = U.L(t, 13.5f, U.Muted, U.Body, s); l.name = "label"; U.Tap(s, a); return s;
        }
        void SetSeg(VisualElement s, bool on)
        {
            s.style.backgroundImage = on ? new StyleBackground(Content.Tex("tex/g_gold")) : new StyleBackground((Texture2D)null);
            var l = s.Q<Label>("label"); l.style.color = on ? U.GoldInk : U.Muted; l.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(on ? U.Semi : U.Body));
        }
        public void Open(string t, string albId = null)
        {
            tab = t; SetSeg(segAll, t == "all"); SetSeg(segAlb, t == "alb"); SetSeg(segX, t == "xp");
            right.Clear(); var sp = U.Row(right); U.Icon("star", 13, U.Gold, sp); var sl = U.L(" " + Meta.TotalStars(), 14, U.Gold, U.Bold, sp); U.L("/" + Content.Levels.Count * 3, 11, U.Muted, U.Body, sp);
            if (Meta.Crowns() > 0) { U.Icon("crown", 14, U.Hex("#CDB6FF"), sp).style.marginLeft = 8; U.L(" " + Meta.Crowns(), 13, U.Hex("#CDB6FF"), U.Bold, sp); }
            sp.style.flexDirection = FlexDirection.Row;
            content.Clear(); scroll.scrollOffset = Vector2.zero;
            if (t == "all") Grid(content, i => true, false);
            else if (t == "xp") Expert();
            else if (albId != null) Album(albId); else Albums();
            if (App.I.cur != "levels") App.I.Show("levels");
        }
        void Grid(VisualElement parent, Func<int, bool> filter, bool expert)
        {
            var g = U.E(null, parent); g.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row; g.style.flexWrap = Wrap.Wrap; g.style.justifyContent = Justify.FlexStart;
            for (int i = 0; i < Content.Levels.Count; i++) if (filter(i)) g.Add(LvBtn(i, expert));
        }
        VisualElement LvBtn(int i, bool expert)
        {
            var lv = Content.Levels[i]; bool ev = Meta.IsEventPic(lv.pic), locked = !Meta.LvOpen(i);
            var b = U.E(null); b.style.width = Length.Percent(33.33f); b.style.alignItems = Align.Center; b.style.marginBottom = 20; U.Pad(b, 0, 7);
            var frame = expert ? U.Hex("#9C7EF0") : ev ? U.Hex("#E07AA8") : locked ? U.Line : U.Hex("#C9A050");
            var hex = new HexImage(Content.Pic(lv.pic), 100, frame);
            if (locked) hex.tint = new Color(.35f, .35f, .35f, .6f);
            b.Add(hex);
            if (ev) { var x2 = U.Badge("×2", b, Content.Tex("tex/g_pink"), U.Hex("#3a0f22")); U.Abs(x2, null, 2, 8); }
            if (Meta.Crowned(i)) { var cr = U.Icon("crown", 24, U.Hex("#F3D27A"), b); U.Abs(cr, null, -6); cr.style.alignSelf = Align.Center; cr.style.left = Length.Percent(50); cr.style.marginLeft = -12; }
            var n = U.Row(b); n.style.marginTop = 7; n.style.justifyContent = Justify.Center;
            U.L((i + 1).ToString(), 13, U.Ink, U.Semi, n); var nm = U.L(Content.PicName(lv.pic), 12, U.Muted, U.Body, n); nm.style.marginLeft = nm.style.marginRight = 4; nm.style.overflow = Overflow.Hidden; nm.style.textOverflow = TextOverflow.Ellipsis; nm.style.maxWidth = 80;
            if (locked) { U.L(Content.T("locked"), 11, U.Muted, U.Body, b).style.marginTop = 3; }
            else
            {
                var gr = U.Row(b, 0, false); gr.style.marginTop = 4; var best = Save.BestOf(i);
                for (int k = 0; k < 3; k++) { var d = U.E(null, gr); U.Size(d, 6, 6); d.style.rotate = new Rotate(new Angle(45)); d.style.marginLeft = d.style.marginRight = 2.5f; d.style.backgroundColor = best != null && best.I("grade") >= k ? U.Gold : U.Line; }
                U.Tap(b, () => { if (expert) App.I.game.StartExpert(i); else App.I.game.StartLevel(i); });
            }
            return b;
        }
        VisualElement Mosaic(Album A)
        {
            var m = U.E(null); U.Size(m, 78, 78); m.style.flexShrink = 0;
            float[][] pos = { new[] { 0f, 0f }, new[] { 39f, 0f }, new[] { 19.5f, 33f } };
            for (int k = 0; k < 3 && k < A.pics.Count; k++) { var h = new HexImage(Content.Pic(A.pics[k]), 38); U.Abs(h, pos[k][0], pos[k][1]); m.Add(h); }
            return m;
        }
        void Albums()
        {
            var E = Meta.Now(); var list = new System.Collections.Generic.List<Album>(Content.Albums); list.Sort((a, b) => (b == E.A ? 1 : 0) - (a == E.A ? 1 : 0));
            foreach (var A in list)
            {
                var st = Meta.Stats(A); var sv = Meta.AlbSave(A.id); bool ev = A == E.A;
                var c = U.Card(content, 20); c.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row; c.style.alignItems = Align.Center; U.Pad(c, 14, 14); c.style.marginBottom = 14;
                if (sv.B("m")) U.Border(c, 1.5f, U.Hex("#E2BE70")); else if (sv.B("c")) U.Border(c, 1, U.Hex("#D9B568", .55f)); else if (ev) U.Border(c, 1, U.Hex("#E87AAA", .6f));
                if (ev) c.style.backgroundColor = new Color(232 / 255f, 120 / 255f, 170 / 255f, .1f);
                c.Add(Mosaic(A));
                Info(c, A.Name, st, sv.B("m") ? Content.T("albGold") : sv.B("c") ? Content.T("albComplete") + " · " + Content.F("albProg", st.solved, st.n, st.stars) : Content.F("albProg", st.solved, st.n, st.stars) + " · " + Content.F("albReward", Meta.ALB_DONE, Meta.ALB_MASTER));
                if (ev) { var tg = U.Badge(Content.T("evTitle") + " · ×2", c, Content.Tex("tex/g_pink"), U.Hex("#3a0f22")); U.Abs(tg, null, -9, 12); }
                var id = A.id; U.Tap(c, () => Album(id));
            }
        }
        void Info(VisualElement c, string title, Meta.AlbStats st, string sub)
        {
            var inf = U.Col(c); inf.style.flexGrow = 1; inf.style.flexShrink = 1; inf.style.marginLeft = inf.style.marginRight = 14;
            var al = Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            U.L(title, 16, U.Ink, U.Semi, inf, al);
            var bar = U.E(null, inf); bar.style.height = 5; U.Radius(bar, 3); bar.style.backgroundColor = U.Line; bar.style.marginTop = 6; bar.style.marginBottom = 6; bar.style.overflow = Overflow.Hidden;
            var f = U.E(null, bar); f.style.height = 5; U.Radius(f, 3); f.style.backgroundColor = U.Gold; f.style.width = Length.Percent(st.n > 0 ? st.solved * 100f / st.n : 0); if (Save.Ar) { f.style.position = Position.Absolute; f.style.right = 0; }
            U.L(sub, 11.5f, U.Muted, U.Body, inf, al, true);
        }
        void Album(string id)
        {
            content.Clear(); scroll.scrollOffset = Vector2.zero; var A = Content.Albums.Find(a => a.id == id); var st = Meta.Stats(A); var E = Meta.Now();
            var h = U.Row(content); h.style.marginBottom = 14;
            var b = U.IconBtn("back", () => Open("alb"), h, 40, 20); if (Save.Ar) b.Q("icon").style.scale = new Scale(new Vector3(-1, 1, 1));
            Info(h, A.Name, st, (A == E.A ? Content.T("evOpen") + " · " : "") + Content.F("albProg", st.solved, st.n, st.stars));
            var idx = new System.Collections.Generic.List<int>(); foreach (var p in A.pics) idx.Add(Meta.LvlOf(p)); idx.Sort();
            var set = new System.Collections.Generic.HashSet<int>(idx); Grid(content, i => set.Contains(i), false);
        }
        void Expert()
        {
            var h = U.Card(content, 20); h.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row; h.style.alignItems = Align.Center; U.Pad(h, 14, 14); h.style.marginBottom = 16;
            U.Border(h, 1, U.Hex("#BA96FF", .45f)); h.style.backgroundColor = new Color(150 / 255f, 110 / 255f, 1, .14f);
            U.Icon("crown", 40, U.Hex("#E9CF86"), h);
            var col = U.Col(h); col.style.marginLeft = col.style.marginRight = 12; col.style.flexShrink = 1; var al = Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            U.L(Content.F("xHead", Meta.Crowns()), 16, U.Ink, U.Semi, col, al); U.L(Content.T("xRule"), 11.5f, U.Muted, U.Body, col, al, true);
            bool any = false; for (int i = 9; i < Content.Levels.Count; i++) { var bb = Save.BestOf(i); if (bb != null && bb.I("grade") == 2) any = true; }
            if (!any) { var p = U.L(Content.T("xEmpty"), 14, U.Muted, U.Body, content, TextAnchor.MiddleCenter, true); U.Pad(p, 24, 18); return; }
            Grid(content, i => { var bb = Save.BestOf(i); return i >= 9 && bb != null && bb.I("grade") == 2; }, true);
        }
        public override void OnShow() { AdsController.ShowBannerOn(root, BannerPosition.Bottom); }
        public override void OnHide() { AdsController.HideBannerOn(root); }
        public override bool Back() { return false; }
    }

    public class SettingsScreen : Scr
    {
        VisualElement list;
        public override void Build()
        {
            root.style.flexDirection = FlexDirection.Column; Bars.Top(root, Content.T("settings"), out _);
            var sv = new ScrollView(); sv.style.flexGrow = 1; sv.verticalScrollerVisibility = ScrollerVisibility.Hidden; root.Add(sv); list = sv.contentContainer; U.Pad(list, 12, 18, 24, 18);
        }
        public override void OnShow() { Refresh(); }
        void Row(string label, string hint, string value, Action onClick, U.BK kind = U.BK.Surface)
        {
            var r = U.Row(list); r.style.justifyContent = Justify.SpaceBetween; U.Pad(r, 14, 4); r.style.borderBottomWidth = 1; r.style.borderBottomColor = U.Line;
            var c = U.Col(r); c.style.flexShrink = 1; var al = Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            U.L(label, 15, U.Ink, U.Body, c, al); if (hint != null) U.L(hint, 11.5f, U.Muted, U.Body, c, al, true);
            var b = U.Btn(value, kind, onClick, r, 40); b.style.minWidth = 96;
        }
        void Refresh()
        {
            list.Clear(); string on = Content.T("on"), off = Content.T("off");
            Row(Content.T("lang"), null, Content.T("langBtn"), () => { Save.D["lang"] = Save.Ar ? "en" : "ar"; Save.Persist(); App.I.Rebuild(); });
            Row(Content.T("theme"), null, U.Dark ? Content.T("themeDark") : Content.T("themeLight"), () => { Save.D["theme"] = U.Dark ? "light" : "dark"; Save.Persist(); App.I.Rebuild(); });
            Row(Content.T("sound"), null, Save.Sound ? on : off, () => { Save.D["sound"] = !Save.Sound; Save.Persist(); Refresh(); });
            Row(Content.T("haptics"), null, Save.B("haptics", true) ? on : off, () => { Save.D["haptics"] = !Save.B("haptics", true); Save.Persist(); Haptics.Do("commit"); Refresh(); });
            Row(Content.T("liveBoard"), Content.T("liveHint"), Save.B("liveBoard", true) ? on : off, () => { Save.D["liveBoard"] = !Save.B("liveBoard", true); Save.Persist(); Refresh(); });
            Row(Content.T("comfort"), Content.T("comfortHint"), Save.Comfort ? on : off, () => { Save.D["comfort"] = !Save.Comfort; Save.Persist(); Refresh(); });
            Row(Content.T("removeAds"), null, Save.B("adsRemoved") ? on : off, () => { Save.D["adsRemoved"] = !Save.B("adsRemoved"); Save.Persist(); Refresh(); });
            Row(Content.T("reset"), null, Content.T("resetBtn"), () => App.I.Confirm(Content.T("resetTitle"), Content.T("resetWarn"), Content.T("resetBtn"), () => { Save.Reset(); App.I.Toast(Content.T("resetDone")); Sfx.Undo(); App.I.Rebuild(); }), U.BK.Red);
            var pv = U.L(Save.Ar ? "سياسة الخصوصية" : "Privacy policy", 13, U.Gold, U.Body, list); pv.style.marginTop = 22; U.Tap(pv, () => Application.OpenURL(Config.PrivacyUrl));
            var ver = U.L("Fault Line · صدع  v" + Application.version, 11, U.Muted, U.Body, list); ver.style.marginTop = 10;
        }
    }
}
