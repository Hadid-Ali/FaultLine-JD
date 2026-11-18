using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace FaultLine
{
    /// <summary>Renders and drives the puzzle board (port of the web canvas renderer + input).</summary>
    public class BoardView : VisualElement
    {
        public Game G;
        public Func<Rect> PlayRect;           // local rect of the play area (HUD gap)
        public Action onShowcaseBegin, onShowcaseEnd;
        public Label peekLabel, hintLabel;
        const float SQ3 = 1.7320508f;
        public float size, cx, cy, ox, oy, bw, bh; Vector2[] home;
        Texture2D pic; VideoPlayer vp; double liveT0; bool liveReady; string livePic;
        // layers
        class Layer { public Batch pic, vid, bevel, shadow; public Batch[] silk = new Batch[8]; }
        Layer stat, mov; Batch bGlow, bPlate, bLine, bPin, bFill, bHexLine, bDot, bArrow, bRing, bVig, bFlat, bShadowPlate;
        Texture2D tBevel, tPin, tFill, tHexLine, tDot, tGlow, tArrow, tRing, tVig; Texture2D[] tSilk = new Texture2D[8];
        // zoom state
        double zoomT0; int zoomDir; float zoomDur; float zs0, zcx0, zcy0; bool zoomActive;
        int activePointer = -1;
        public bool Interactive = true;

        public BoardView()
        {
            pickingMode = PickingMode.Position; style.position = Position.Absolute; style.left = style.top = style.right = style.bottom = 0;
            tBevel = Content.Tex("tex/bevel"); tPin = Content.Tex("tex/pin"); tFill = Content.Tex("tex/hexfill"); tHexLine = Content.Tex("tex/hexline");
            tDot = Content.Tex("tex/dot"); tGlow = Content.Tex("tex/glow"); tArrow = Content.Tex("tex/arrow"); tRing = Content.Tex("tex/ring"); tVig = Content.Tex("tex/vignette");
            for (int i = 0; i < 8; i++) tSilk[i] = Content.Tex("tex/silk" + i);
            stat = MkLayer(); mov = MkLayer();
            bGlow = new Batch(tGlow); bPlate = new Batch(null); bShadowPlate = new Batch(null); bLine = new Batch(null); bPin = new Batch(tPin); bFill = new Batch(tFill);
            bHexLine = new Batch(tHexLine); bDot = new Batch(tDot); bArrow = new Batch(tArrow); bRing = new Batch(tRing); bVig = new Batch(tVig); bFlat = new Batch(null);
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(OnDown);
            RegisterCallback<PointerMoveEvent>(OnMove);
            RegisterCallback<PointerUpEvent>(e => OnUp(e.pointerId, false));
            RegisterCallback<PointerCancelEvent>(e => OnUp(e.pointerId, true));
            peekLabel = U.L("", 24, Color.white, U.Bold, this); U.Abs(peekLabel); peekLabel.style.display = DisplayStyle.None;
            hintLabel = U.L("", 14, U.Hex("#F7DFA4"), U.Semi, this); U.Abs(hintLabel); U.Radius(hintLabel, 10); U.Pad(hintLabel, 6, 11);
            hintLabel.style.backgroundColor = new Color(6 / 255f, 10 / 255f, 12 / 255f, .86f); hintLabel.style.display = DisplayStyle.None;
        }
        Layer MkLayer() { var l = new Layer { vid = new Batch(null), bevel = new Batch(tBevel), shadow = new Batch(null) }; for (int i = 0; i < 8; i++) l.silk[i] = new Batch(tSilk[i]); return l; }

        public void SetGame(Game g)
        {
            StopLiving(); G = g; pic = Content.Pic(g.Pic); zoomActive = false; activePointer = -1;
            foreach (var l in new[] { stat, mov }) l.pic = new Batch(pic);
            g.CellPos = c => home != null && c < home.Length ? home[c] : Vector2.zero;
            if (Save.B("liveBoard", true)) StartLiving();
        }

        // ---------- living painting ----------
        public void StartLiving()
        {
            if (G == null || !Content.Living.Contains(G.Pic) || (vp != null && livePic == G.Pic)) return;
            StopLiving();
            var go = App.I.gameObject; vp = go.GetComponent<VideoPlayer>() ?? go.AddComponent<VideoPlayer>();
            vp.playOnAwake = false; vp.isLooping = true; vp.renderMode = VideoRenderMode.APIOnly; vp.audioOutputMode = VideoAudioOutputMode.None;
            vp.source = VideoSource.Url; vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, "live", G.Pic + ".mp4");
            livePic = G.Pic; liveReady = false;
            vp.prepareCompleted -= OnPrepared; vp.prepareCompleted += OnPrepared; vp.Prepare();
        }
        void OnPrepared(VideoPlayer v) { if (v != vp) return; v.Play(); }
        public void StopLiving() { if (vp != null) { vp.prepareCompleted -= OnPrepared; vp.Stop(); } liveReady = false; livePic = null; }
        Texture LiveTex()
        {
            if (vp == null || livePic == null || !vp.isPrepared || vp.texture == null) return null;
            if (!liveReady && vp.frame > 0) { liveReady = true; liveT0 = Util.NowMs; }
            if (!liveReady) return null;
            if (G.phase == "reveal") return null;
            if (G.phase == "play" && !Save.B("liveBoard", true)) return null;
            return vp.texture;
        }

        // ---------- geometry ----------
        float Fit(Rect r, int R) { return Mathf.Min((r.width - 28) / (SQ3 * (2 * R + 1)), (r.height - 20) / (3 * R + 2)); }
        void Metrics(Rect r, bool show, out float s, out float x, out float y)
        {
            int R = G.B.R; s = show ? Fit(r, R) * .97f : Mathf.Min(Fit(r, R), Fit(r, 4) * 1.8f); x = r.center.x; y = r.center.y;
        }
        public void BeginZoom(int dir, float dur) { zoomT0 = Util.NowMs; zoomDir = dir; zoomDur = dur; zoomActive = true; }
        void ComputeLayout(double now)
        {
            var full = contentRect; var play = PlayRect != null ? PlayRect() : full;
            if (play.width < 10 || play.height < 10) play = full;
            Metrics(play, false, out float s0, out float x0, out float y0);
            Metrics(full, true, out float s1, out float x1, out float y1);
            float k = 0;
            bool showing = G.phase == "show" || G.phase == "done";
            if (zoomActive)
            {
                float t = Mathf.Clamp01((float)((now - zoomT0) / zoomDur)); float e = t < .5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
                k = zoomDir > 0 ? e : 1 - e; if (t >= 1 && zoomDir < 0) zoomActive = false;
            }
            else k = showing ? 1 : 0;
            if (G.phase == "done" && !zoomActive) k = 0;
            size = Mathf.Lerp(s0, s1, k); cx = Mathf.Lerp(x0, x1, k); cy = Mathf.Lerp(y0, y1, k);
            if (G.phase == "show" && !Save.Comfort) size *= 1 + Mathf.Min(.07f, (float)((now - G.showT0 - zoomDur) / 95000.0)) * (k >= 1 ? 1 : 0);
            Dim = k;
            if (G.shakeAmp > 0) { float t = (float)((now - G.shakeT0) / 260.0); if (t < 1) { float a = G.shakeAmp * (1 - t); cx += UnityEngine.Random.Range(-.5f, .5f) * a; cy += UnityEngine.Random.Range(-.5f, .5f) * a; } else G.shakeAmp = 0; }
            int R = G.B.R; bw = SQ3 * size * (2 * R + 1); bh = size * (3 * R + 2); ox = cx - bw / 2; oy = cy - bh / 2;
            if (home == null || home.Length != G.B.N) home = new Vector2[G.B.N];
            for (int i = 0; i < G.B.N; i++) { var c = G.B.cells[i]; home[i] = new Vector2(cx + size * SQ3 * (c.q + c.r / 2f), cy + size * 1.5f * c.r); }
            G.TileSize = size;
        }
        public float Dim;
        int CellAt(Vector2 p) { int best = -1; float bd = 1e9f; for (int i = 0; i < home.Length; i++) { float d = Vector2.Distance(home[i], p); if (d < bd) { bd = d; best = i; } } return bd <= size ? best : -1; }
        float FracKey(float x, float y, int a) { float px = (x - cx) / size, py = (y - cy) / size; float q = SQ3 / 3 * px - py / 3, r = 2f / 3 * py, s = -q - r; return a == 0 ? r : a == 1 ? q : s; }

        // ---------- input ----------
        void OnDown(PointerDownEvent e)
        {
            if (G == null || !Interactive) return;
            if (G.phase != "play" || G.drag != null || G.snap != null || activePointer >= 0 || G.peek || home == null) return;
            var p = (Vector2)e.localPosition; int c = CellAt(p); if (c < 0) return;
            if (G.guide != null && !G.guide.set.Contains(c)) { G.guide.buzz = Util.NowMs; Sfx.Touch(); Haptics.Do("light"); return; }
            if (G.tFirst <= 0) G.tFirst = Util.NowMs;
            activePointer = e.pointerId; this.CapturePointer(e.pointerId);
            G.drag = new Drag { cell = c, x0 = p.x, y0 = p.y, t0 = Util.NowMs, pointerId = e.pointerId };
            G.lastAct = Util.NowMs; Sfx.Touch(); Haptics.Do("light");
        }
        void OnMove(PointerMoveEvent e)
        {
            if (G == null || e.pointerId != activePointer || G.drag == null) return;
            var D = G.drag; var p = (Vector2)e.localPosition; float dx = p.x - D.x0, dy = p.y - D.y0;
            if (D.axis < 0)
            {
                if (Mathf.Sqrt(dx * dx + dy * dy) < 10) return;
                int ba = 0; double bv = -1; for (int a = 0; a < 3; a++) { double v = Math.Abs(dx * Engine.AXD[a][0] + dy * Engine.AXD[a][1]); if (v > bv) { bv = v; ba = a; } }
                if (G.guide != null) { if (ba != G.guide.m.a) { G.guide.buzz = Util.NowMs; return; } D.fault = G.guide.m.side != 0; }
                var cell = G.B.cells[D.cell]; int vv = Engine.KeyOf(cell, ba), side = 0;
                if (D.fault) { side = vv > 0 ? 1 : vv < 0 ? -1 : (FracKey(D.x0, D.y0, ba) >= 0 ? 1 : -1); if (vv == side * G.B.R) side = 0; }
                if (G.guide != null) { side = G.guide.m.side; vv = G.guide.m.v; }
                D.axis = ba; D.m = new Move(ba, vv, side, 0); D.rows = Engine.MoveRows(G.B, D.m); D.set = new HashSet<int>(); foreach (var r in D.rows) foreach (var c in r) D.set.Add(c);
                D.seams = new List<int[]>(); foreach (var c in D.set) foreach (var n in G.B.nb[c]) if (!D.set.Contains(n) && c < n) D.seams.Add(new[] { c, n }); else if (!D.set.Contains(n)) D.seams.Add(new[] { n, c });
                D.lastStep = 0;
                foreach (var pin in G.P.pins) if (D.set.Contains(pin)) { D.blocked = true; break; }
                if (D.blocked) { G.pinFlash = Util.NowMs; Sfx.Undo(); Haptics.Do("light"); App.I.Toast(Content.T("pinBlocked")); }
            }
            D.off = (float)((dx * Engine.AXD[D.axis][0] + dy * Engine.AXD[D.axis][1]) / (SQ3 * size));
            if (D.blocked) D.off = Mathf.Clamp(D.off * .25f, -.14f, .14f);
            int st = Mathf.RoundToInt(D.off); if (st != D.lastStep) { D.lastStep = st; Sfx.Tick(Math.Abs(st)); Haptics.Do("tick"); }
        }
        void OnUp(int pid, bool cancel)
        {
            if (G == null) return;
            if (G.phase == "show" && Util.NowMs - G.showT0 > 900) { EndShowcase(); return; }
            if (pid != activePointer) return; activePointer = -1; if (this.HasPointerCapture(pid)) this.ReleasePointer(pid);
            var D = G.drag; if (D == null) return;
            if (D.axis < 0 || cancel) { G.drag = null; return; }
            int k = Mathf.RoundToInt(D.off); var m = D.m.WithK(k);
            G.snap = new Snap { from = D.off, to = (k != 0 && !Engine.IsNoop(G.B, m)) ? k : 0, t0 = Util.NowMs, m = m, hinted = G.hint.HasValue };
        }
        public void PlayHint()
        {
            if (!G.hint.HasValue) return; var m = G.hint.Value; var rows = Engine.MoveRows(G.B, m); var set = new HashSet<int>(); foreach (var r in rows) foreach (var c in r) set.Add(c);
            var seams = new List<int[]>(); foreach (var c in set) foreach (var n in G.B.nb[c]) if (!set.Contains(n)) seams.Add(new[] { c, n });
            G.drag = new Drag { axis = m.a, m = m.WithK(0), rows = rows, set = set, seams = seams, fault = m.side != 0, cell = -1 };
            G.snap = new Snap { from = 0, to = m.k, t0 = Util.NowMs, m = m, hinted = true }; G.hint = null;
        }

        // ---------- showcase ----------
        public void BeginShowcase()
        {
            if (G == null) return; G.phase = "show"; G.parts.Clear(); G.showT0 = Util.NowMs;
            BeginZoom(1, Save.Comfort ? 320 : 850); Sfx.ZoomIn(); Haptics.Do("light");
            if (vp == null || livePic != G.Pic) StartLiving(); else if (!vp.isPlaying && vp.isPrepared) vp.Play();
            onShowcaseBegin?.Invoke();
            var g0 = G; App.I.Delay(7f, () => { if (G == g0 && G.phase == "show") EndShowcase(); });
        }
        public void EndShowcase()
        {
            if (G == null || G.phase != "show") return;
            G.phase = "done"; BeginZoom(-1, Save.Comfort ? 260 : 620); Sfx.ZoomOut(); Haptics.Do("light");
            var g0 = G; App.I.Delay(Save.Comfort ? .28f : .64f, () => { if (G != g0) return; zoomActive = false; onShowcaseEnd?.Invoke(); });
        }

        // ---------- per-frame logic ----------
        public void Tick(double now, float dt)
        {
            if (G == null) return;
            if (contentRect.width < 10) return;
            ComputeLayout(now);
            // armed fault move (press & hold)
            var D = G.drag;
            if (D != null && D.axis < 0 && D.cell >= 0 && G.Faults && !D.fault && now - D.t0 > 300 && G.guide == null) { D.fault = true; Haptics.Do("armed"); Sfx.Armed(); }
            // snap animation
            if (G.snap != null)
            {
                var S = G.snap; float t = Save.Comfort ? 1 : Mathf.Min(1, (float)((now - S.t0) / 230));
                const float c1 = 1.25f, c3 = c1 + 1; float eb = 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
                if (G.drag != null) G.drag.off = S.from + (S.to - S.from) * (Save.Comfort ? 1 : eb);
                if (t >= 1) { G.snap = null; G.drag = null; if (S.to != 0) { G.Commit(S.m.WithK((int)S.to), S.hinted); SeamDust(S.m.WithK((int)S.to)); } }
            }
            // near-solved chime
            float ratio = G.CorrectCount() / (float)G.B.N;
            if (G.phase == "play" && ratio >= .9f && ratio < 1 && !G.nearPlayed) { G.nearPlayed = true; Sfx.Near(); }
            // idle nudge
            if (G.phase == "play" && !G.expert && G.kind != Kind.Challenge)
            {
                if (G.lastAct <= 0 || G.drag != null || G.snap != null || G.peek || G.hint.HasValue) G.lastAct = now;
                else if (now - G.lastAct > Content.KI("NUDGE_MS")) { var m = G.ComputeHint(); if (m.HasValue) { G.nudge = new Nudge { m = m.Value, rows = Engine.MoveRows(G.B, m.Value), t0 = now }; Sfx.Nudge(); Haptics.Do("hint"); } G.lastAct = now; }
            }
            if (G.nudge != null && (now - G.nudge.t0 > 1800 || G.phase != "play" || G.drag != null)) G.nudge = null;
            // reveal timeline
            if (G.phase == "reveal")
            {
                float t = (float)(now - G.revealT0); bool calm = Save.Comfort;
                G.merge = calm ? 1 : Mathf.Clamp01((t - 380) / 900);
                if (G.lockD == null) { float maxD = 1; foreach (var p in home) maxD = Mathf.Max(maxD, Vector2.Distance(p, new Vector2(cx, cy))); G.lockD = new float[home.Length]; for (int i = 0; i < home.Length; i++) G.lockD[i] = Vector2.Distance(home[i], new Vector2(cx, cy)) / maxD; }
                if (!calm && t < 820 + 320)
                {
                    for (int i = 0; i < home.Length; i++) { float u = (t - G.lockD[i] * 820) / 300; if (u > .3f && u < .42f && UnityEngine.Random.value < .45f) G.Spark(home[i].x + UnityEngine.Random.Range(-.5f, .5f) * size, home[i].y + UnityEngine.Random.Range(-.5f, .5f) * size, false); }
                    if (now - G.lastTick > 70) { G.lastTick = now; Sfx.Sparkle(); }
                }
                if (t > (calm ? 60 : 1480)) BeginShowcase();
            }
            // particles
            for (int i = G.parts.Count - 1; i >= 0; i--) { var p = G.parts[i]; p.life -= dt; if (p.life <= 0) { G.parts.RemoveAt(i); continue; } p.x += p.vx * dt / 1000; p.y += p.vy * dt / 1000; p.vy += p.g * dt / 1000; }
            G.flashes.RemoveAll(f => now - f.Value >= 650);
            // overlay labels
            peekLabel.style.display = G.peek ? DisplayStyle.Flex : DisplayStyle.None;
            if (G.peek) { peekLabel.text = Content.T("peek"); peekLabel.style.left = cx - 80; peekLabel.style.width = 160; peekLabel.style.top = Mathf.Max(8, oy - 40); }
            UpdateHintLabel();
            MarkDirtyRepaint();
        }
        void SeamDust(Move m)
        {
            if (Save.Comfort) return; var M = Engine.MovedCells(G.B, m); int n = 0;
            foreach (var c in M) foreach (var nb in G.B.nb[c]) if (!M.Contains(nb) && n++ < 36) { var mid = (home[c] + home[nb]) / 2; G.Dust(mid.x, mid.y); G.Dust(mid.x, mid.y); }
        }
        void UpdateHintLabel()
        {
            bool on = G.hint.HasValue && G.phase == "play" && G.drag == null && G.snap == null;
            hintLabel.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; if (!on) return;
            var m = G.hint.Value; var p = HintAnchor(m);
            hintLabel.text = m.side != 0 ? Content.F("hold", Math.Abs(m.k)) : Content.F("slide", Math.Abs(m.k));
            float w = hintLabel.resolvedStyle.width; if (float.IsNaN(w) || w < 10) w = 120;
            float lx = Mathf.Clamp(p.x - w / 2, 6, contentRect.width - w - 6), ly = p.y - size * 1.25f - 15; if (ly < 20) ly = p.y + size * 1.25f - 15;
            hintLabel.style.left = lx; hintLabel.style.top = ly;
        }
        Vector2 HintAnchor(Move m)
        {
            var row = G.B.rows[m.a][m.v]; int mid = row[(row.Length - 1) / 2]; var p = home[mid];
            if (m.side != 0 && m.v == 0) { float[][] N = { new[] { 0f, 1f }, new[] { SQ3 / 2, -.5f }, new[] { -SQ3 / 2, -.5f } }; p.x += N[m.a][0] * size * .3f * m.side; p.y += N[m.a][1] * size * .3f * m.side; }
            return p;
        }

        // ---------- drawing ----------
        Vector2 PicUV(int tile, float x, float y, float sc, float px, float py)
        {
            float S = Mathf.Max(bw, bh); var h = home[tile];
            float qx = h.x + (px - x) / sc, qy = h.y + (py - y) / sc;
            return new Vector2((qx - ox - (bw - S) / 2) / S, 1f - (qy - oy - (bh - S) / 2) / S);
        }
        void Tile(Layer L, int tile, float x, float y, float alpha, float sc, Texture live, float liveA, double now)
        {
            float r = size * .965f * sc; Color c = new Color(1, 1, 1, alpha);
            L.pic.Hex(x, y, r, c, (px, py) => PicUV(tile, x, y, sc, px, py));
            if (live != null && liveA > 0) L.vid.Hex(x, y, r, new Color(1, 1, 1, alpha * liveA), (px, py) => PicUV(tile, x, y, sc, px, py));
            if (G.P.veil.Count > 0 && G.P.veil.Contains(tile) && G.phase == "play" && !G.peek)
            {
                bool atHome = G.st[tile] == tile; float va = atHome ? 0 : 1;
                if (atHome && G.unveil.TryGetValue(tile, out double u)) va = Mathf.Max(0, 1 - (float)((now - u) / 650));
                if (va > 0) L.silk[tile % 8].Quad(x, y - (atHome ? (1 - va) * size * .5f : 0), size * sc * 115f / 128f, size * sc * 132f / 128f, new Color(1, 1, 1, alpha * va));
            }
            float mg = G.merge; if (mg < 1) L.bevel.Quad(x, y, size * sc * 115f / 128f, size * sc * 132f / 128f, new Color(1, 1, 1, alpha * (1 - mg)));
        }
        void HexTex(Batch b, float x, float y, float rScale, Color c, float rot = 0)
        {
            // hex-shaped textures are 230x264 with the hex (radius 128) centered
            b.Quad(x, y, size * rScale * 115f / 128f, size * rScale * 132f / 128f, c, rot);
        }
        void Draw(MeshGenerationContext mgc)
        {
            if (G == null || home == null || pic == null) return;
            double now = Util.NowMs; var full = contentRect;
            var live = LiveTex(); float liveA = live != null ? Mathf.Min(1, (float)((now - liveT0) / 800)) : 0;
            foreach (var l in new[] { stat, mov }) { if (l.vid.tex != live) l.vid = new Batch(live); }
            // background glow
            float ratio = G.CorrectCount() / (float)G.B.N; float glowA = G.phase == "play" ? .10f : .18f;
            if (G.phase == "play" && ratio >= .9f && ratio < 1) { float k = (ratio - .9f) / .1f; glowA = .13f + .22f * k * (.75f + .25f * Mathf.Sin((float)now / 420)); }
            float gr = Mathf.Max(bw, bh) * .78f; bGlow.Quad(cx, cy, gr, gr, new Color(217 / 255f, 181 / 255f, 104 / 255f, glowA * 1.6f)); bGlow.Flush(mgc);
            // showcase dim
            if (Dim > 0) { bFlat.Quad(full.center.x, full.center.y, full.width, full.height, new Color(3 / 255f, 7 / 255f, 9 / 255f, Dim * .84f)); bFlat.Flush(mgc); }
            // plate + shadow
            for (int i = 0; i < home.Length; i++) bShadowPlate.Hex(home[i].x, home[i].y + size * .35f, size * 1.12f, new Color(0, 0, 0, .22f));
            bShadowPlate.Flush(mgc);
            for (int i = 0; i < home.Length; i++) bPlate.Hex(home[i].x, home[i].y, size * 1.045f, U.Plate);
            bPlate.Flush(mgc);
            if (G.expert && G.phase == "play") { float a = .45f + .2f * Mathf.Sin((float)now / 700); for (int i = 0; i < home.Length; i++) HexTex(bHexLine, home[i].x, home[i].y, 1.075f / .9f, new Color(186 / 255f, 150 / 255f, 1, a)); bHexLine.Flush(mgc); }
            // tiles
            var D = G.drag; var moving = (D != null && D.axis >= 0) ? D.set : null;
            int[] view = G.peek ? Engine.Identity(G.B.N) : G.st;
            float stt = G.settleSet != null ? (float)((now - G.settleT0) / 320) : 2; if (stt >= 1) G.settleSet = null;
            float mgS = 1 + .0383f * G.merge;
            for (int c = 0; c < G.B.N; c++)
            {
                if (moving != null && moving.Contains(c) && !G.peek) continue;
                float sc = mgS; if (G.settleSet != null && stt < 1 && G.settleSet.Contains(c) && !Save.Comfort) sc *= 1 + .035f * Mathf.Cos(stt * Mathf.PI * 1.5f) * (1 - stt);
                Tile(stat, view[c], home[c].x, home[c].y, 1, sc, live, liveA, now);
            }
            FlushLayer(stat, mgc);
            if (moving != null && !G.peek)
            {
                var ax = Engine.AXD[D.axis]; float step = SQ3 * size; bool lift = !Save.Comfort;
                var copies = new List<Vector4>(); // tile, x, y, alpha
                foreach (var row in D.rows)
                {
                    int n = row.Length; var p0 = home[row[0]];
                    for (int i = 0; i < n; i++)
                    {
                        int tile = G.st[row[i]]; float t = Modf(i + D.off, n);
                        foreach (float tt in new[] { t, t - n })
                        {
                            if (tt < -1 || tt > n) continue; float outv = tt < 0 ? -tt : tt > n - 1 ? tt - (n - 1) : 0; float al = 1 - outv; if (al <= .02f) continue;
                            copies.Add(new Vector4(tile, p0.x + (float)ax[0] * step * tt, p0.y + (float)ax[1] * step * tt, al));
                        }
                    }
                }
                if (lift) { foreach (var q in copies) mov.shadow.Hex(q.y, q.z + 2 + size * .2f, size * .94f, new Color(0, 0, 0, .32f * q.w)); }
                foreach (var q in copies) Tile(mov, (int)q.x, q.y, q.z - (lift ? 3 : 0), q.w, lift ? 1.035f : 1, live, liveA, now);
                FlushLayer(mov, mgc);
                // seams
                Color sc2 = D.fault ? new Color(240 / 255f, 200 / 255f, 110 / 255f, .95f) : new Color(1, 248 / 255f, 235 / 255f, .75f);
                foreach (var s in D.seams) { var A = home[s[0]]; var Bp = home[s[1]]; var mid = (A + Bp) / 2; var d = (Bp - A).normalized; var u = new Vector2(-d.y, d.x) * size / 2; bLine.Line(mid.x - u.x, mid.y - u.y, mid.x + u.x, mid.y + u.y, D.fault ? 3 : 2, sc2); }
                bLine.Flush(mgc);
            }
            // pins
            if (G.P.pins.Count > 0 && !G.peek)
            {
                float fade = 1 - G.merge; float fl = G.pinFlash > 0 ? Mathf.Max(0, 1 - (float)((now - G.pinFlash) / 750)) : 0;
                if (fade > 0)
                {
                    foreach (var c in G.P.pins) { float pu = fl * (.5f + .5f * Mathf.Sin((float)now / 60)); var col = Color.Lerp(Color.white, new Color(1, .5f, .4f), pu); col.a = fade; HexTex(bPin, home[c].x, home[c].y, 1, col); }
                    bPin.Flush(mgc);
                }
            }
            // selected cell outline
            if (D != null && D.axis < 0 && D.cell >= 0 && !G.peek)
            {
                var p = home[D.cell]; float pu = .5f + .5f * Mathf.Sin((float)now / 110);
                HexTex(bHexLine, p.x, p.y, 1.0f, D.fault ? new Color(240 / 255f, 196 / 255f, 106 / 255f, .75f + .25f * pu) : new Color(1, 1, 1, .85f));
                if (!D.fault && G.Faults && G.guide == null) { float t = Mathf.Min(1, (float)((now - D.t0) / 300)); bRing.Quad(p.x, p.y, size * .6f * t, size * .6f * t, new Color(240 / 255f, 196 / 255f, 106 / 255f, .9f * t)); }
            }
            // home flashes
            foreach (var f in G.flashes)
            {
                var p = home[f.Key]; float t = (float)((now - f.Value) / 650);
                bFill.Quad(p.x, p.y, size * 115f / 128f, size * 132f / 128f, new Color(1, .98f, .9f, .55f * Mathf.Sin(Mathf.Clamp01(t * 1.4f) * Mathf.PI)));
                HexTex(bHexLine, p.x, p.y, .93f / .9f, new Color(240 / 255f, 196 / 255f, 106 / 255f, Mathf.Max(0, 1 - t * 1.6f) * .9f));
            }
            // guide (level 1) & nudge
            if (G.guide != null && G.phase == "play")
            {
                var gA = G.guide; float bz = gA.buzz > 0 ? Mathf.Max(0, 1 - (float)((now - gA.buzz) / 420)) : 0;
                foreach (var c in gA.set) { var p = home[c]; HexTex(bFill, p.x, p.y, 1, new Color(1, 214 / 255f, 120 / 255f, .1f + .06f * Mathf.Sin((float)now / 420) + bz * .1f)); HexTex(bHexLine, p.x, p.y, 1, new Color(246 / 255f, 207 / 255f, 116 / 255f, .9f)); }
                if (D == null && G.snap == null) Arrows(gA.m, gA.rows, (float)(now % 1500) / 1500, 2, 1f, .45f);
            }
            if (G.nudge != null)
            {
                var N = G.nudge; float t = (float)((now - N.t0) / 1800); float al = Mathf.Sin(Mathf.Min(1, t) * Mathf.PI);
                foreach (var row in N.rows) foreach (var c in row) { var p = home[c]; HexTex(bFill, p.x, p.y, 1, new Color(1, 214 / 255f, 120 / 255f, .16f * al)); HexTex(bHexLine, p.x, p.y, 1, new Color(246 / 255f, 207 / 255f, 116 / 255f, al)); }
                var rows = N.rows; if (rows.Count > 3) { int mid = rows.Count / 2; rows = rows.GetRange(mid - 1, 3); }
                Arrows(N.m, rows, t * 1.5f, 3, al, .5f);
            }
            if (G.hint.HasValue && G.phase == "play" && D == null && G.snap == null)
            {
                var m = G.hint.Value; var p = HintAnchor(m); float pulse = .5f + .5f * Mathf.Sin((float)now / 220);
                float rr = size * (.42f + .08f * pulse); var gold = new Color(240 / 255f, 196 / 255f, 106 / 255f, 1);
                bRing.Quad(p.x, p.y, rr * 1.06f, rr * 1.06f, gold);
                int sg = Math.Sign(m.k); var ax = Engine.AXD[m.a]; float L1 = SQ3 * size * 1.25f; float ex = p.x + (float)ax[0] * sg * L1, ey = p.y + (float)ax[1] * sg * L1;
                bLine.Line(p.x + (float)ax[0] * sg * size * .55f, p.y + (float)ax[1] * sg * size * .55f, ex, ey, 4, gold);
                float ang = Mathf.Atan2((float)ax[1] * sg, (float)ax[0] * sg);
                bLine.Line(ex, ey, ex - Mathf.Cos(ang - .5f) * 12, ey - Mathf.Sin(ang - .5f) * 12, 4, gold); bLine.Line(ex, ey, ex - Mathf.Cos(ang + .5f) * 12, ey - Mathf.Sin(ang + .5f) * 12, 4, gold);
            }
            // reveal ripple
            if (G.phase == "reveal" && !Save.Comfort && G.lockD != null)
            {
                float t = (float)(now - G.revealT0);
                for (int i = 0; i < home.Length; i++) { float u = (t - G.lockD[i] * 820) / 300; if (u < 0 || u > 1) continue; float a = Mathf.Sin(u * Mathf.PI); var p = home[i]; float rs = .965f + .037f * G.merge; HexTex(bFill, p.x, p.y, rs / .965f, new Color(1, .98f, .92f, a * .5f)); HexTex(bHexLine, p.x, p.y, rs / .9f, new Color(1, 1, 1, a * .85f)); }
            }
            bFill.Flush(mgc); bHexLine.Flush(mgc); bRing.Flush(mgc); bLine.Flush(mgc); bArrow.Flush(mgc);
            // particles
            foreach (var p in G.parts) { float a = p.life / p.max; var c = p.c; c.a = a * p.o; bDot.Quad(p.x, p.y, p.r * 2.2f, p.r * 2.2f, c); }
            bDot.Flush(mgc);
            // showcase flash + vignette
            if (G.phase == "show" && !Save.Comfort)
            {
                float el = (float)(now - G.showT0);
                if (el < 650) { bFlat.Quad(full.center.x, full.center.y, full.width, full.height, new Color(1, .97f, .88f, (1 - el / 650) * .45f)); bFlat.Flush(mgc); }
                bVig.Quad(full.center.x, full.center.y, full.width * .72f, full.height * .72f, new Color(1, 1, 1, .9f)); bVig.Flush(mgc);
            }
        }
        void Arrows(Move m, List<int[]> rows, float u0, int count, float alpha, float rscale)
        {
            var dir = Engine.AXD[m.a]; int sg = Math.Sign(m.k); if (sg == 0) sg = 1; float ang = Mathf.Atan2((float)dir[1] * sg, (float)dir[0] * sg);
            foreach (var row in rows)
            {
                var a = home[row[0]]; var b = home[row[row.Length - 1]]; var s0 = sg > 0 ? a : b; var s1 = sg > 0 ? b : a;
                for (int k = 0; k < count; k++) { float uu = Modf(u0 + k / (float)count, 1); var p = Vector2.Lerp(s0, s1, uu); float r = size * rscale; bArrow.Quad(p.x, p.y, r, r, new Color(1, 1, 1, alpha * Mathf.Sin(uu * Mathf.PI)), ang); }
            }
        }
        void FlushLayer(Layer L, MeshGenerationContext mgc)
        {
            L.shadow.Flush(mgc); L.pic.Flush(mgc); if (L.vid.tex != null) L.vid.Flush(mgc); else L.vid.Clear();
            for (int i = 0; i < 8; i++) L.silk[i].Flush(mgc); L.bevel.Flush(mgc);
        }
        static float Modf(float x, float n) { return ((x % n) + n) % n; }
    }
}
