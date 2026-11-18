using System;
using System.Collections.Generic;
using UnityEngine;

namespace FaultLine
{
    public enum Kind { Level, Daily, Challenge }
    public class Hist { public Move m; public bool hinted; }
    public class Drag
    {
        public int cell = -1, axis = -1, lastStep, pointerId; public float x0, y0, off; public bool fault, blocked; public double t0;
        public Move m; public List<int[]> rows; public HashSet<int> set; public List<int[]> seams;
    }
    public class Snap { public float from, to; public double t0; public Move m; public bool hinted; }
    public class Particle { public float x, y, vx, vy, g, r, life, max, o; public Color c; }
    public class Guide { public Move m; public List<int[]> rows; public HashSet<int> set; public double t0, buzz; }
    public class Nudge { public Move m; public List<int[]> rows; public double t0; }
    public class ChalData
    {
        public string p, n; public int R, c; public bool f; public double t; public List<Move> mvs = new List<Move>(); public List<int> pns = new List<int>(), vls = new List<int>();
        public string mv, pn, vl; public string rn; public double rt = -1; // reply info
    }

    /// <summary>One puzzle attempt — port of the web build's G object and its rules.</summary>
    public class Game
    {
        public Kind kind; public int idx = -1; public bool expert; public LevelDef def; public Prepared P;
        public Board B => P.B; public int[] st; public List<Hist> history = new List<Hist>();
        public Move? hint; public string phase = "play"; public Drag drag; public Snap snap;
        public List<KeyValuePair<int, double>> flashes = new List<KeyValuePair<int, double>>();
        public bool peek, hintedAttempt; public string date; public int num; public ChalData ch;
        public double tFirst, solveMs; public float merge; public double revealT0, showT0, lastTick;
        public float[] lockD; public Dictionary<int, double> unveil = new Dictionary<int, double>();
        public double pinFlash; public Guide guide; public Nudge nudge; public double lastAct;
        public int peeksLeft = -1, goldEarned, dispGold = -1; public bool doubled, replayHalf, evX2, xFirst, nearPlayed;
        public double settleT0; public HashSet<int> settleSet; public double shakeT0; public float shakeAmp;
        public List<Particle> parts = new List<Particle>();
        public Action onHud, onReveal, onFail;

        public int Target => P.target;
        public string Pic => def.pic;
        public bool Faults => def.faults;

        public static Game ForLevel(int i)
        {
            var d = Content.Levels[i].Clone(); var g = new Game { kind = Kind.Level, idx = i, def = d, P = Engine.PrepareLevel(d) }; g.Init(); return g;
        }
        public static Game ForExpert(int i)
        {
            var b = Content.Levels[i];
            var d = new LevelDef { R = b.R, pic = b.pic, faults = true, gen = (b.gen > 0 ? b.gen : (b.moves != null ? b.moves.Count : 2)) + 2, seed = "XP-" + (b.seed ?? ("L" + i)), pins = b.pins, veils = b.veils };
            var g = new Game { kind = Kind.Level, idx = i, expert = true, def = d, P = Engine.PrepareLevel(d) }; g.Init(); return g;
        }
        public static Game ForDaily()
        {
            string date = Util.KwDate(); int num = Util.DayNum(date) - Util.DayNum("2026-10-01") + 1;
            int dn = Save.LevelsDone; int R = dn < 8 ? 2 : 3, gN = dn < 8 ? 3 : dn < 20 ? 3 : 5;
            var B = Engine.BuildBoard(R);
            var moves = Engine.GenScramble(B, "fl-" + date + ((R == 3 && gN == 5) ? "" : "-" + R + gN), gN, true);
            var keys = Content.PicKeys;
            var d = new LevelDef { R = R, faults = true, pic = keys[((num % keys.Count) + keys.Count) % keys.Count], moves = moves };
            var g = new Game { kind = Kind.Daily, date = date, num = num, def = d, P = Engine.PrepareLevel(d) }; g.Init(); return g;
        }
        public static Game ForChallenge(ChalData o)
        {
            var d = new LevelDef { R = o.R, pic = o.p, faults = o.f, moves = o.mvs };
            var g = new Game { kind = Kind.Challenge, ch = o, def = d, P = Engine.PrepareExplicit(o.R, o.f, o.mvs, o.pns, o.vls) }; g.Init(); return g;
        }
        void Init() { st = (int[])P.start.Clone(); SetGuide(); }

        public int CorrectCount() { int n = 0; for (int i = 0; i < st.Length; i++) if (st[i] == i) n++; return n; }
        public bool Solved => Engine.IsSolved(st);

        public void Restart()
        {
            tFirst = 0; solveMs = 0; merge = 0; lockD = null; parts.Clear(); nearPlayed = false; settleSet = null;
            st = (int[])P.start.Clone(); history.Clear(); hint = null; phase = "play"; drag = null; snap = null; flashes.Clear(); hintedAttempt = false; unveil.Clear();
            SetGuide(); onHud?.Invoke();
        }
        public void SetGuide()
        {
            guide = null;
            if (!(kind == Kind.Level && !expert && idx == 0 && history.Count == 0)) return;
            var m = ComputeHint(); if (!m.HasValue) return;
            var rows = Engine.MoveRows(B, m.Value); var set = new HashSet<int>(); foreach (var r in rows) foreach (var c in r) set.Add(c);
            guide = new Guide { m = m.Value, rows = rows, set = set, t0 = Util.NowMs };
        }
        public Move? ComputeHint()
        {
            var sol = Engine.FindSolution(B, st, P.moves, 2);
            if (sol != null && sol.Count > 0) return Engine.NormK(B, sol[0]);
            var all = new List<Move>(P.scramble); foreach (var h in history) all.Add(h.m);
            all.Reverse(); for (int i = 0; i < all.Count; i++) all[i] = Engine.Inv(all[i]);
            var route = Engine.SimplifyRoute(B, all);
            return route.Count > 0 ? route[0] : (Move?)null;
        }
        public void Commit(Move m, bool hinted)
        {
            m = Engine.NormK(B, m); var before = st; st = Engine.ApplyMove(B, st, m); guide = null;
            history.Add(new Hist { m = m, hinted = hinted }); if (hinted) hintedAttempt = true; hint = null;
            Sfx.Commit(m); Haptics.Do("commit");
            double now = Util.NowMs; settleSet = Engine.MovedCells(B, m); settleT0 = now; if (m.side != 0 && !Save.Comfort) { shakeT0 = now; shakeAmp = 5; }
            int homes = 0;
            foreach (var c in Engine.MovedCells(B, m))
                if (st[c] == c && before[c] != c)
                {
                    flashes.Add(new KeyValuePair<int, double>(c, now)); homes++;
                    if (P.veil.Contains(c)) { unveil[c] = now; Board_SparkAt(c, 12); App.I.Delay(.06f, Sfx.Sparkle); }
                }
            if (homes > 0) { Sfx.Home(homes); App.I.Delay(.09f, () => Haptics.Do("home")); }
            onHud?.Invoke();
            if (Solved) BeginReveal();
            else if (expert && history.Count >= Target) App.I.Delay(.52f, ExpertFail);
        }
        public Func<int, Vector2> CellPos; // provided by the board view
        public float TileSize = 20;
        void Board_SparkAt(int c, int n) { if (CellPos == null) return; var p = CellPos(c); for (int q = 0; q < n; q++) Spark(p.x + UnityEngine.Random.Range(-.5f, .5f) * TileSize, p.y + UnityEngine.Random.Range(-.5f, .5f) * TileSize, false); }
        public void Spark(float x, float y, bool silver)
        {
            if (Save.Comfort) return;
            parts.Add(new Particle { x = x, y = y, vx = UnityEngine.Random.Range(-.5f, .5f) * 55, vy = -20 - UnityEngine.Random.value * 55, g = 95, r = .7f + UnityEngine.Random.value * 1.4f, life = 380 + UnityEngine.Random.value * 320, max = 700, c = silver ? new Color(.88f, .92f, .94f) : new Color(1f, .84f, .47f), o = 1 });
        }
        public void Dust(float x, float y)
        {
            if (Save.Comfort) return;
            parts.Add(new Particle { x = x, y = y, vx = UnityEngine.Random.Range(-.5f, .5f) * 70, vy = -10 - UnityEngine.Random.value * 45, g = 70, r = .8f + UnityEngine.Random.value * 1.8f, life = 420 + UnityEngine.Random.value * 260, max = 680, c = new Color(.92f, .86f, .75f), o = .7f });
        }
        public void Undo()
        {
            if (expert || phase != "play" || drag != null || snap != null || history.Count == 0) return;
            var h = history[history.Count - 1]; history.RemoveAt(history.Count - 1); st = Engine.ApplyMove(B, st, Engine.Inv(h.m)); hint = null; SetGuide();
            Sfx.Undo(); Haptics.Do("light"); onHud?.Invoke();
        }
        public void BeginReveal()
        {
            solveMs = tFirst > 0 ? Math.Round(Util.NowMs - tFirst) : 0;
            phase = "reveal"; revealT0 = Util.NowMs; merge = 0;
            var notes = new List<int>(); foreach (var h in history) notes.Add(0);
            Sfx.Win(history.Count); Haptics.Do("win");
            onReveal?.Invoke();
        }
        void ExpertFail()
        {
            if (!expert || phase != "play" || Solved) return;
            phase = "failed"; shakeT0 = Util.NowMs; shakeAmp = 9; Sfx.Undo(); Haptics.Do("light"); onFail?.Invoke();
        }
        public bool ChalWon() { double my = solveMs, th = ch.t; return my < th || (my == th && history.Count < ch.c); }
        public int Grade()
        {
            int c = history.Count, t = Target; if (c <= t && !hintedAttempt) return 2; if (c <= t + 2) return 1; return 0;
        }
    }
}
