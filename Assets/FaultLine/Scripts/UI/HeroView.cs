using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    /// <summary>Menu hero: the Kuwait Towers board solving itself in a loop (port of heroFrame).</summary>
    public class HeroView : VisualElement
    {
        const float SQ3 = 1.7320508f;
        Board B; List<Move> sol; int[] start, ident; List<List<int[]>> rows; Texture2D pic, bevel, dot;
        Batch bPic, bBevel, bPlate, bDot, bShine; double t0; int cycle = -1; bool burst; float mg;
        readonly List<Particle> parts = new List<Particle>(); double last;
        public HeroView()
        {
            pickingMode = PickingMode.Ignore; pic = Content.Pic("towers"); bevel = Content.Tex("tex/bevel"); dot = Content.Tex("tex/dot");
            bPic = new Batch(pic); bBevel = new Batch(bevel); bPlate = new Batch(null); bDot = new Batch(dot); bShine = new Batch(Content.Tex("tex/glow"));
            B = Engine.BuildBoard(2); sol = new List<Move>(); foreach (var m in Content.HeroSol) sol.Add(Engine.NormK(B, m));
            ident = Engine.Identity(B.N); start = ident; for (int i = sol.Count - 1; i >= 0; i--) start = Engine.ApplyMove(B, start, Engine.Inv(sol[i]));
            rows = new List<List<int[]>>(); foreach (var m in sol) rows.Add(Engine.MoveRows(B, m));
            t0 = Util.NowMs; last = t0; generateVisualContent += Draw;
        }
        public void Restart() { t0 = Util.NowMs; parts.Clear(); cycle = -1; }
        float s, cx0, cy0, ox, oy, bw, bh; Vector2[] home;
        void Geo()
        {
            var r = contentRect; s = Mathf.Min(r.width / (SQ3 * 5), r.height / 8f) * .94f; cx0 = r.width / 2; cy0 = r.height / 2;
            bw = SQ3 * s * 5; bh = s * 8; ox = cx0 - bw / 2; oy = cy0 - bh / 2;
            if (home == null) home = new Vector2[B.N];
            for (int i = 0; i < B.N; i++) { var c = B.cells[i]; home[i] = new Vector2(cx0 + s * SQ3 * (c.q + c.r / 2f), cy0 + s * 1.5f * c.r); }
        }
        Vector2 UV(int tile, float x, float y, float sc, float px, float py)
        {
            float S = Mathf.Max(bw, bh); var h = home[tile]; float qx = h.x + (px - x) / sc, qy = h.y + (py - y) / sc;
            return new Vector2((qx - ox - (bw - S) / 2) / S, 1f - (qy - oy - (bh - S) / 2) / S);
        }
        void TileAt(int tile, float x, float y, float al, float sc)
        {
            sc *= 1 + .0383f * mg; float r = s * .965f * sc;
            bPic.Hex(x, y, r, new Color(1, 1, 1, al), (px, py) => UV(tile, x, y, sc, px, py));
            if (mg < 1) bBevel.Quad(x, y, s * sc * 115f / 128f, s * sc * 132f / 128f, new Color(1, 1, 1, al * (1 - mg)));
        }
        void State(int[] st, HashSet<int> skip = null, float al = 1) { for (int c = 0; c < B.N; c++) { if (skip != null && skip.Contains(c)) continue; TileAt(st[c], home[c].x, home[c].y, al, 1); } }
        void Spark(float x, float y, bool big)
        {
            if (Save.Comfort) return;
            parts.Add(new Particle { x = x, y = y, vx = Random.Range(-.5f, .5f) * (big ? 150 : 50), vy = (big ? -40 : -15) - Random.value * (big ? 110 : 45), g = big ? 120 : 80, r = .6f + Random.value * (big ? 1.4f : .9f), life = 500 + Random.value * (big ? 700 : 300), max = big ? 1200 : 800, c = new Color(1, .89f, .55f), o = 1 });
        }
        public void Tick() { MarkDirtyRepaint(); }
        void Draw(MeshGenerationContext mgc)
        {
            if (contentRect.width < 10) return; Geo();
            double now = Util.NowMs; float dt = Mathf.Min(50, (float)(now - last)); last = now; bool calm = Save.Comfort;
            for (int i = 0; i < B.N; i++) bPlate.Hex(home[i].x, home[i].y, s * 1.04f, new Color(3 / 255f, 6 / 255f, 7 / 255f, 1));
            bPlate.Flush(mgc);
            int nM = sol.Count; float T1 = calm ? 1400 : 1000, MV = calm ? 900 : 640, GP = calm ? 420 : 260, TM = nM * (MV + GP), TS = calm ? 2000 : 1400, TH = calm ? 3600 : 2800, TR = calm ? 900 : 650, TOT = T1 + TM + TS + TH + TR;
            float el = (float)(now - t0); int cyc = Mathf.FloorToInt(el / TOT); float t = el - cyc * TOT;
            if (cyc != cycle) { cycle = cyc; burst = false; parts.Clear(); mg = 0; }
            if (t < T1) { mg = 0; State(start); }
            else if (t < T1 + TM)
            {
                float u = t - T1; int i = Mathf.Min(nM - 1, Mathf.FloorToInt(u / (MV + GP))); float v = u - i * (MV + GP);
                var st = start; for (int j = 0; j < i; j++) st = Engine.ApplyMove(B, st, sol[j]); var m = sol[i];
                if (v < MV)
                {
                    float x = v / MV, c1 = calm ? 0 : 1.3f, c3 = c1 + 1; float eb = calm ? (x < .5f ? 2 * x * x : 1 - Mathf.Pow(-2 * x + 2, 2) / 2) : 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
                    float off = m.k * eb; var set = new HashSet<int>(); foreach (var r in rows[i]) foreach (var c in r) set.Add(c);
                    mg = 0; State(st, set);
                    var ax = Engine.AXD[m.a]; float step = SQ3 * s;
                    foreach (var row in rows[i])
                    {
                        int n = row.Length; var p0 = home[row[0]];
                        for (int q = 0; q < n; q++)
                        {
                            int tile = st[row[q]]; float tt0 = ((q + off) % n + n) % n;
                            foreach (float tt in new[] { tt0, tt0 - n }) { if (tt < -1 || tt > n) continue; float outv = tt < 0 ? -tt : tt > n - 1 ? tt - (n - 1) : 0, al = 1 - outv; if (al <= .02f) continue; TileAt(tile, p0.x + (float)ax[0] * step * tt, p0.y + (float)ax[1] * step * tt - (calm ? 0 : 2), al, calm ? 1 : 1.035f); }
                        }
                    }
                }
                else { mg = 0; State(Engine.ApplyMove(B, st, m)); }
            }
            else
            {
                float fade = t > TOT - TR ? (t - (TOT - TR)) / TR : 0; float f = Mathf.Min(1, (t - T1 - TM) / TS); mg = calm ? 1 : Mathf.Min(1, f * 1.7f);
                State(ident, null, 1);
                if (f < 1 && Random.value < .5f) { var p = home[Random.Range(0, home.Length)]; Spark(p.x + Random.Range(-.5f, .5f) * s, p.y + Random.Range(-.5f, .5f) * s, false); }
                if (f >= 1 && !burst) { burst = true; for (int q = 0; q < 14; q++) { float a = Random.value * Mathf.PI * 2, rr = Random.value * s * 2; Spark(cx0 + Mathf.Cos(a) * rr, cy0 + Mathf.Sin(a) * rr, true); } }
                float ts = t - T1 - TM - TS; if (ts > 0 && t < TOT - TR && Random.value < .08f) { var p = home[Random.Range(0, home.Length)]; Spark(p.x + Random.Range(-.5f, .5f) * s, p.y + Random.Range(-.5f, .5f) * s, false); }
                if (fade > 0) { bPic.Flush(mgc); bBevel.Flush(mgc); float mg0 = mg; mg = 0; State(start, null, fade); mg = mg0; }
                if (ts > 0 && ts < 1000 && !calm) { float x2 = ts / 1000; float sx = -bw * .4f + x2 * bw * 1.8f + ox; bShine.Quad(sx, cy0, bw * .22f, bh * .6f, new Color(1, .94f, .78f, .3f * Mathf.Sin(x2 * Mathf.PI)), .5f); }
            }
            bPic.Flush(mgc); bBevel.Flush(mgc); bShine.Flush(mgc);
            for (int i = parts.Count - 1; i >= 0; i--) { var p = parts[i]; p.life -= dt; if (p.life <= 0) { parts.RemoveAt(i); continue; } p.x += p.vx * dt / 1000; p.y += p.vy * dt / 1000; p.vy += p.g * dt / 1000; var c = p.c; c.a = p.life / p.max; bDot.Quad(p.x, p.y, p.r * 2.4f, p.r * 2.4f, c); }
            bDot.Flush(mgc);
        }
    }
}
