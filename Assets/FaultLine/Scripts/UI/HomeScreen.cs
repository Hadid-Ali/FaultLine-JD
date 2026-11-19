using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    /// <summary>"My Home": restore rooms with gold, then drag-and-drop furniture (port of the web home slice).</summary>
    public class HomeScreen : Scr
    {
        string curRoom = "majlis"; int sel = -1, justPlaced = -1;
        VisualElement tabs, view, ruin, done, items, fx, amb, card, decor, catalog, itemBar, photoBtn; Label hGold, rcTitle, rcText; VisualElement rcProg, rcBtn;
        float W = 340; readonly List<Particle> parts = new List<Particle>(); VisualElement partLayer; Batch bDot;
        public override void Build()
        {
            root.style.flexDirection = FlexDirection.Column;
            VisualElement right; Bars.Top(root, Content.T("myHome"), out right); U.GoldPill(out hGold, right);
            tabs = U.Row(root); tabs.style.justifyContent = Justify.Center; U.Pad(tabs, 2, 16, 6, 16);
            var vw = U.E(null, root); vw.style.flexGrow = 1; vw.style.alignItems = Align.Center; vw.style.justifyContent = Justify.Center;
            view = U.E(null, vw); view.pickingMode = PickingMode.Position; U.Size(view, W, W); view.style.overflow = Overflow.Visible;
            vw.RegisterCallback<GeometryChangedEvent>(e => { float s = Mathf.Min(e.newRect.width - 16, e.newRect.height - 8, 560); if (s > 50 && Mathf.Abs(s - W) > 1) { W = s; U.Size(view, W, W); RenderItems(); } });
            ruin = U.E(null, view); U.Fill(ruin); done = U.E(null, view); U.Fill(done);
            amb = U.E(null, view); U.Fill(amb); items = U.E(null, view); U.Fill(items); fx = U.E(null, view); U.Fill(fx);
            partLayer = new VisualElement(); partLayer.pickingMode = PickingMode.Ignore; U.Fill(partLayer); view.Add(partLayer); bDot = new Batch(Content.Tex("tex/dot"));
            partLayer.generateVisualContent += DrawParts;
            photoBtn = U.IconBtn("camera", TakePhoto, view, 44, 22); U.Abs(photoBtn, null, null, 6, 6);
            view.RegisterCallback<PointerDownEvent>(e => { if (sel >= 0 && e.target == view) { sel = -1; RenderItems(); UpdateBar(); } });
            var panel = U.Col(root); U.Pad(panel, 6, 14, 14, 14);
            card = U.Card(panel, 22); U.Pad(card, 16, 16); card.style.alignItems = Align.Center;
            rcTitle = U.L("", 17, U.Ink, U.Semi, card); rcText = U.L("", 13.5f, U.Muted, U.Body, card, TextAnchor.MiddleCenter, true); rcText.style.marginTop = 4; rcText.style.marginBottom = 12;
            rcProg = U.E(null, card); rcProg.style.height = 5; rcProg.style.width = Length.Percent(80); U.Radius(rcProg, 3); rcProg.style.backgroundColor = U.Line; rcProg.style.marginBottom = 12; rcProg.style.overflow = Overflow.Hidden;
            var rf = U.E("fill", rcProg); rf.style.height = 5; rf.style.backgroundColor = U.Gold; if (Save.Ar) { rf.style.position = Position.Absolute; rf.style.right = 0; }
            rcBtn = U.E(null, card);
            decor = U.Col(panel);
            U.L(Content.T("placeHint"), 12.5f, U.Muted, U.Body, decor, TextAnchor.MiddleCenter, true).style.marginBottom = 8;
            itemBar = U.Row(decor); itemBar.style.justifyContent = Justify.Center; itemBar.style.marginBottom = 8;
            U.Btn(Content.T("flip"), U.BK.Surface, Flip, itemBar, 40); U.Btn(Content.T("store"), U.BK.Surface, StoreItem, itemBar, 40); U.Btn(Content.T("done"), U.BK.Primary, () => { sel = -1; RenderItems(); UpdateBar(); }, itemBar, 40); U.Gap(itemBar, 8);
            var sc = new ScrollView(ScrollViewMode.Horizontal); sc.horizontalScrollerVisibility = ScrollerVisibility.Hidden; sc.verticalScrollerVisibility = ScrollerVisibility.Hidden; decor.Add(sc);
            catalog = sc.contentContainer; catalog.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row;
        }
        public void Open() { App.I.Show("home"); }
        public override void OnShow() { sel = -1; Render(); }
        public void CheckOnboardBadge() { }
        public JObj Home()
        {
            var h = Save.D.O("home"); if (h == null) { h = new JObj(); h["rooms"] = new JObj(); h["inv"] = new JObj(); Save.D["home"] = h; }
            var rooms = h.O("rooms"); foreach (var R in Content.Rooms) if (rooms.O(R.id) == null) { var o = new JObj(); o["restored"] = false; o["items"] = new List<object>(); rooms[R.id] = o; }
            if (h.O("inv") == null) h["inv"] = new JObj(); return h;
        }
        public JObj RoomState(string id) { return Home().O("rooms").O(id); }
        Room CurR => Content.Rooms.Find(r => r.id == curRoom);
        List<object> Items => RoomState(curRoom).A("items");
        void Render()
        {
            var R = CurR; var st = RoomState(R.id); hGold.text = Save.Gold.ToString();
            tabs.Clear();
            foreach (var r in Content.Rooms)
            {
                var t = U.E(null, tabs); U.Radius(t, 999); U.Pad(t, 7, 16); U.Border(t, 1, r.id == curRoom ? U.Hex("#D9B568", .55f) : U.Line); t.style.backgroundColor = r.id == curRoom ? U.Surface : new Color(0, 0, 0, 0); t.style.marginLeft = t.style.marginRight = 4;
                U.L(r.Name, 13.5f, r.id == curRoom ? U.Ink : U.Muted, U.Body, t); var id = r.id; U.Tap(t, () => { curRoom = id; sel = -1; Render(); });
            }
            ruin.style.backgroundImage = new StyleBackground(Content.Tex("home/" + R.ruin)); done.style.backgroundImage = new StyleBackground(Content.Tex("home/" + R.done));
            bool restored = st.B("restored"); U.Show(ruin, !restored); U.Show(done, restored);
            RenderItems();
            U.Show(card, !restored); U.Show(decor, restored); U.Show(photoBtn, restored);
            if (!restored)
            {
                int have = Save.LevelsDone; bool ok = have >= R.req;
                rcTitle.text = Content.F("restoreTitle", R.Name); rcText.text = ok ? Content.T("restoreText") : Content.F("needLevels", Mathf.Min(have, R.req), R.req);
                U.Show(rcProg, !ok); rcProg.Q("fill").style.width = Length.Percent(Mathf.Min(100, have * 100f / R.req));
                rcBtn.Clear();
                if (ok)
                {
                    var b = U.Btn(Content.T("restoreBtn") + "  " + R.cost, U.BK.Primary, () => { if (Save.Gold < R.cost) { App.I.Toast(Content.F("needGold", R.cost - Save.Gold)); Haptics.Do("light"); return; } Restore(R); }, rcBtn, 50, null);
                    b.style.minWidth = 200;
                }
                else
                {
                    U.Btn(Content.T("playNext"), U.BK.Primary, () => { int i = 0; while (i < Content.Levels.Count - 1 && Save.BestOf(i) != null) i++; App.I.game.StartLevel(Mathf.Max(0, Mathf.Min(i, Save.Unlocked - 1))); }, rcBtn, 50).style.minWidth = 200;
                }
            }
            else RenderCatalog();
            UpdateBar();
        }
        static int ItemZ(Furn f, JObj it) { return f.floor ? 2 : f.wall ? 3 : 10 + Mathf.RoundToInt((float)it.D("y") * 1000); }
        void RenderItems()
        {
            items.Clear(); amb.Clear(); var st = RoomState(curRoom); if (!st.B("restored")) return;
            var R = CurR;
            if (!Save.Comfort)
            {
                var shaft = U.Img(Content.Tex("tex/glow"), W * .5f, W * .9f, amb, new Color(1, .9f, .66f, .18f)); U.Abs(shaft, W * R.winX - W * .42f, W * R.winY - W * .05f); shaft.style.rotate = new Rotate(new Angle(32));
            }
            var list = new List<KeyValuePair<int, JObj>>(); var its = st.A("items"); for (int i = 0; i < its.Count; i++) list.Add(new KeyValuePair<int, JObj>(i, (JObj)its[i]));
            list.Sort((a, b) => { var fa = Content.Furn.Find(f => f.id == a.Value.S("id")); var fb = Content.Furn.Find(f => f.id == b.Value.S("id")); return (fa == null || fb == null) ? 0 : ItemZ(fa, a.Value) - ItemZ(fb, b.Value); });
            foreach (var kv in list)
            {
                int i = kv.Key; var it = kv.Value; var f = Content.Furn.Find(x => x.id == it.S("id")); if (f == null) continue;
                var tex = Content.Tex("home/" + f.id); float w = f.w * W, h = tex != null ? w * tex.height / tex.width : w;
                var el = U.E(null, items); el.pickingMode = PickingMode.Position; U.Size(el, w, h); U.Abs(el, (float)it.D("x") * W - w / 2, (float)it.D("y") * W - h);
                var im = U.Img(tex, w, h, el); if (it.B("flip")) im.style.scale = new Scale(new Vector3(-1, 1, 1));
                if (i == sel) { im.style.unityBackgroundImageTintColor = new Color(1, .95f, .8f); el.style.scale = new Scale(new Vector3(1.04f, 1.04f, 1)); }
                if (f.id == "lantern" && !Save.Comfort) { var gl = U.Img(Content.Tex("tex/glow"), w * 2.4f, w * 2.4f, el, new Color(1, .8f, .45f, .35f)); U.Abs(gl, -w * .7f, h * .1f); gl.SendToBack(); }
                if (i == justPlaced && !Save.Comfort) { App.I.Anim(.35f, k => { float e = 1 - Mathf.Pow(1 - k, 3); el.style.translate = new Translate(0, -30 * (1 - e), 0); el.style.opacity = Mathf.Min(1, k * 3); }); }
                int idx = i; Furn ff = f;
                el.RegisterCallback<PointerDownEvent>(e => StartDrag(e, idx, el, ff, w, h));
                el.RegisterCallback<PointerMoveEvent>(e => MoveDrag(e, el, w, h));
                el.RegisterCallback<PointerUpEvent>(e => EndDrag(e, el));
                el.RegisterCallback<PointerCancelEvent>(e => EndDrag(null, el));
            }
            justPlaced = -1;
        }
        int dragIdx = -1, dragPid = -1; Vector2 dragOff; bool moved;
        void StartDrag(PointerDownEvent e, int i, VisualElement el, Furn f, float w, float h)
        {
            e.StopPropagation(); sel = i; dragIdx = i; dragPid = e.pointerId; moved = false; el.CapturePointer(e.pointerId);
            var it = (JObj)Items[i]; var p = view.WorldToLocal(e.position); dragOff = new Vector2((float)it.D("x") - p.x / W, (float)it.D("y") - p.y / W);
            Sfx.Lift(); Haptics.Do("light"); el.style.scale = new Scale(new Vector3(1.06f, 1.06f, 1)); UpdateBar(); el.BringToFront();
        }
        void MoveDrag(PointerMoveEvent e, VisualElement el, float w, float h)
        {
            if (e.pointerId != dragPid || dragIdx < 0) return; var it = (JObj)Items[dragIdx]; var p = view.WorldToLocal(e.position);
            float x = Mathf.Clamp(p.x / W + dragOff.x, .03f, .97f), y = Mathf.Clamp(p.y / W + dragOff.y, .1f, .99f); it["x"] = (double)x; it["y"] = (double)y;
            el.style.left = x * W - w / 2; el.style.top = y * W - h; moved = true;
        }
        void EndDrag(PointerUpEvent e, VisualElement el)
        {
            if (dragIdx < 0) return; if (e != null && e.pointerId != dragPid) return;
            if (el.HasPointerCapture(dragPid)) el.ReleasePointer(dragPid);
            var it = (JObj)Items[dragIdx]; if (moved) { Save.Persist(); Sfx.Drop(); Haptics.Do("tick"); Puff((float)it.D("x"), (float)it.D("y"), false); }
            dragIdx = -1; dragPid = -1; RenderItems(); UpdateBar();
        }
        void UpdateBar() { U.Show(itemBar, sel >= 0); }
        void Flip() { if (sel < 0 || sel >= Items.Count) return; var it = (JObj)Items[sel]; it["flip"] = !it.B("flip"); Save.Persist(); RenderItems(); Sfx.Tick(); }
        void StoreItem()
        {
            if (sel < 0 || sel >= Items.Count) return; var it = (JObj)Items[sel]; Items.RemoveAt(sel); var inv = Home().O("inv"); inv[it.S("id")] = (double)(inv.I(it.S("id")) + 1);
            sel = -1; Save.Persist(); RenderItems(); RenderCatalog(); UpdateBar(); Sfx.Undo(); Haptics.Do("light");
        }
        void RenderCatalog()
        {
            catalog.Clear(); var inv = Home().O("inv");
            foreach (var f in Content.Furn)
            {
                int owned = inv.I(f.id); var c = U.Card(catalog, 16); c.style.width = 92; c.style.alignItems = Align.Center; U.Pad(c, 8, 6); c.style.marginLeft = c.style.marginRight = 5; c.style.flexShrink = 0;
                if (owned == 0 && Save.Gold < f.price) c.style.opacity = .5f;
                var tex = Content.Tex("home/" + f.id); float iw = 64, ih = 64; if (tex != null) { if (tex.width > tex.height) ih = 64f * tex.height / tex.width; else iw = 64f * tex.width / tex.height; }
                var hold = U.E(null, c); U.Size(hold, 64, 64); hold.style.alignItems = Align.Center; hold.style.justifyContent = Justify.Center; U.Img(tex, iw, ih, hold);
                U.L(f.Name, 11.5f, U.Ink, U.Body, c, TextAnchor.MiddleCenter, true).style.marginTop = 4;
                var pr = U.Row(c); pr.style.marginTop = 3;
                if (owned > 0) U.L(Content.F("owned", owned), 12, U.Gold, U.Semi, pr); else { U.Coin(12, pr); U.L(" " + f.price, 12, U.Gold, U.Semi, pr); }
                var ff = f; U.Tap(c, () => Place(ff));
            }
        }
        void Place(Furn f)
        {
            var inv = Home().O("inv"); var st = RoomState(curRoom);
            if (inv.I(f.id) > 0) inv[f.id] = (double)(inv.I(f.id) - 1);
            else
            {
                if (Save.Gold < f.price) { App.I.Toast(Content.F("needGold", f.price - Save.Gold)); Haptics.Do("light"); return; }
                Save.Gold -= f.price; Sfx.Coins(3); App.I.FlyCoins(hGold.parent, view, 4);
            }
            float bx = .5f, by = f.wall ? .4f : f.floor ? .82f : .72f;
            var it = new JObj(); it["id"] = f.id; it["x"] = (double)(bx + UnityEngine.Random.Range(-.06f, .06f)); it["y"] = (double)(by + UnityEngine.Random.Range(-.03f, .03f)); it["flip"] = false;
            st.A("items").Add(it); sel = st.A("items").Count - 1; justPlaced = sel; Save.Persist();
            hGold.text = Save.Gold.ToString(); RenderItems(); RenderCatalog(); UpdateBar();
            App.I.Delay(Save.Comfort ? 0 : .3f, () => { Sfx.Drop(); Haptics.Do("commit"); Puff((float)it.D("x"), (float)it.D("y"), false); });
        }
        // ---------- restore animation ----------
        void Restore(Room R)
        {
            Save.Gold -= R.cost; Save.Persist(); hGold.text = Save.Gold.ToString(); U.Enable(rcBtn, false); App.I.FlyCoins(hGold.parent, view, 8);
            bool rm = Save.Comfort; float cx = W * .5f, cy = W * .6f, maxR = W * .95f;
            Sfx.Wind(); Haptics.Do("armed"); App.I.Delay(.45f, Sfx.Armed);
            // sand burst
            if (!rm) for (int i = 0; i < 120; i++) { float a = UnityEngine.Random.value * Mathf.PI * 2, rr = Mathf.Sqrt(UnityEngine.Random.value); var p = new Particle { x = cx + Mathf.Cos(a) * W * .3f * rr, y = cy + Mathf.Sin(a) * W * .17f * rr, vx = 60 + UnityEngine.Random.value * 150, vy = -20 - UnityEngine.Random.value * 70, g = 90, r = .8f + UnityEngine.Random.value * 2.3f, life = 950, max = 950, c = new Color(.89f, .78f, .63f), o = .8f }; parts.Add(p); }
            // circular wash reveal of the restored room
            var mask = U.E(null, fx); mask.style.overflow = Overflow.Hidden; U.Border(mask, 3, U.Hex("#F2D27A"));
            var img = U.E(null, mask); img.style.position = Position.Absolute; U.Size(img, W, W); img.style.backgroundImage = new StyleBackground(Content.Tex("home/" + R.done));
            float washStart = rm ? 0 : 1.1f, washDur = rm ? .06f : 1.65f; double lastNote = 0;
            App.I.Delay(washStart, () => App.I.Anim(washDur, k =>
            {
                float e = 1 - Mathf.Pow(1 - k, 2), rr = Mathf.Max(1, e * maxR);
                U.Abs(mask, cx - rr, cy - rr); U.Size(mask, rr * 2, rr * 2); U.Radius(mask, rr); img.style.left = -(cx - rr) - 3; img.style.top = -(cy - rr) - 3;
                mask.style.borderTopColor = mask.style.borderBottomColor = mask.style.borderLeftColor = mask.style.borderRightColor = new Color(.95f, .82f, .48f, 1 - k * .8f);
                img.style.opacity = Mathf.Clamp01((k - .05f) / .6f);
                if (!rm && Time.realtimeSinceStartupAsDouble - lastNote > .16 && k < .95f) { lastNote = Time.realtimeSinceStartupAsDouble; Sfx.Sparkle(); for (int q = 0; q < 4; q++) { float a = UnityEngine.Random.value * Mathf.PI * 2; parts.Add(new Particle { x = cx + Mathf.Cos(a) * rr, y = cy + Mathf.Sin(a) * rr, vx = 0, vy = -10, g = 0, r = 1 + UnityEngine.Random.value * 2, life = 400, max = 400, c = new Color(1, .93f, .67f), o = .9f }); } }
            }, () =>
            {
                fx.Clear(); RoomState(R.id)["restored"] = true; Save.Persist(); GameAnalyticsController.DesignEvent("room:restored:" + R.id); Sfx.Win(5); Haptics.Do("win"); App.I.Toast(Content.T("roomRestored")); Render(); Puff(.5f, .62f, true);
            }));
        }
        void Puff(float x, float y, bool gold)
        {
            if (Save.Comfort) return;
            for (int i = 0; i < (gold ? 16 : 11); i++) { float a = Mathf.PI + UnityEngine.Random.value * Mathf.PI; parts.Add(new Particle { x = (x + UnityEngine.Random.Range(-.02f, .02f)) * W, y = y * W, vx = Mathf.Cos(a) * (.06f + UnityEngine.Random.value * .12f) * W, vy = Mathf.Sin(a) * (.03f + UnityEngine.Random.value * .07f) * W, g = .06f * W, r = 1 + UnityEngine.Random.value * 2.2f, life = 450 + UnityEngine.Random.value * 350, max = 800, c = gold ? new Color(1, .84f, .47f) : new Color(.91f, .85f, .74f), o = .85f }); }
        }
        public override void Tick(double now, float dt)
        {
            for (int i = parts.Count - 1; i >= 0; i--) { var p = parts[i]; p.life -= dt; if (p.life <= 0) { parts.RemoveAt(i); continue; } p.x += p.vx * dt / 1000; p.y += p.vy * dt / 1000; p.vy += p.g * dt / 1000; p.vx *= .98f; }
            partLayer.MarkDirtyRepaint();
        }
        void DrawParts(MeshGenerationContext mgc) { foreach (var p in parts) { var c = p.c; c.a = p.life / p.max * p.o; bDot.Quad(p.x, p.y, p.r * 2.4f, p.r * 2.4f, c); } bDot.Flush(mgc); }
        // ---------- photo ----------
        void TakePhoto()
        {
            Sfx.Shutter(); Haptics.Do("commit"); Save.D["photoTaken"] = true; Save.Persist();
            var rect = view.worldBound; App.I.StartCoroutine(Capture(rect));
        }
        IEnumerator Capture(Rect panelRect)
        {
            U.Show(photoBtn, false); yield return new WaitForEndOfFrame();
            var full = ScreenCapture.CaptureScreenshotAsTexture(); U.Show(photoBtn, true);
            float k = Screen.height / App.I.root.layout.height; float top = App.I.screenLayer.layout.y;
            int x = Mathf.Clamp(Mathf.RoundToInt(panelRect.x * k), 0, full.width - 1), w = Mathf.Clamp(Mathf.RoundToInt(panelRect.width * k), 1, full.width - x);
            int yTop = Mathf.RoundToInt((panelRect.y) * k), h = Mathf.Clamp(Mathf.RoundToInt(panelRect.height * k), 1, full.height);
            int y = Mathf.Clamp(full.height - yTop - h, 0, full.height - h);
            var crop = new Texture2D(w, h, TextureFormat.RGB24, false); crop.SetPixels(full.GetPixels(x, y, w, h)); crop.Apply(); UnityEngine.Object.Destroy(full);
            VisualElement box; var o = App.I.Overlay(out box); box.style.backgroundColor = U.Hex("#F7F2E8"); box.style.alignItems = Align.Center;
            var ph = U.E(null, box); U.Size(ph, 300, 300); ph.style.backgroundImage = new StyleBackground(crop); U.Radius(ph, 6);
            U.L(CurR.Name, 30, U.Hex("#2A2420"), U.Display, box).style.marginTop = 12; U.L("صدع · FAULT LINE", 15, U.Hex("#B8862E"), U.Semi, box);
            U.L(Save.Ar ? "خذ لقطة شاشة لحفظ الصورة" : "Take a screenshot to keep it", 12.5f, U.Hex("#6B6258"), U.Body, box).style.marginTop = 8;
            var r = U.Row(box); r.style.marginTop = 14;
            U.Btn(Content.T("share"), U.BK.Wa, () => Application.OpenURL("https://wa.me/?text=" + Uri.EscapeDataString(Content.T("photoShareText") + "\n" + Config.ChallengeSite)), r, 46, "whatsapp");
            U.Btn(Content.T("close"), U.BK.Surface, () => App.I.CloseOverlay(o), r, 46); U.Gap(r, 10);
        }
        public override bool Back() { if (sel >= 0) { sel = -1; RenderItems(); UpdateBar(); return true; } return false; }
    }
}
