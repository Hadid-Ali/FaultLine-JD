// Deterministic puzzle engine — a 1:1 port of the web build's ENGINE block.
// Scrambles generated here match the web game exactly, so challenge links and
// daily puzzles are identical on both. Pure C#, no Unity dependency.
using System;
using System.Collections.Generic;

namespace FaultLine
{
    public struct Move
    {
        public int a, v, side, k;
        public Move(int a, int v, int side, int k) { this.a = a; this.v = v; this.side = side; this.k = k; }
        public Move WithK(int nk) { return new Move(a, v, side, nk); }
        public override string ToString() { return a + "," + v + "," + side + "," + k; }
    }

    public class Cell { public int q, r, s, i; }

    public class Board
    {
        public int R, N;
        public List<Cell> cells = new List<Cell>();
        public Dictionary<long, int> idx = new Dictionary<long, int>();
        public Dictionary<int, int[]>[] rows = new Dictionary<int, int[]>[3];
        public int[][] nb;
    }

    public class LegalMove
    {
        public Move m; public HashSet<int> cells;
    }

    public class LevelDef
    {
        public int R; public string pic; public bool faults; public int gen; public string seed;
        public List<Move> moves; public int pins; public bool veils;
        public LevelDef Clone() { var d = (LevelDef)MemberwiseClone(); d.moves = moves == null ? null : new List<Move>(moves); return d; }
    }

    public class Prepared
    {
        public Board B; public int[] start; public List<LegalMove> moves; public int target;
        public HashSet<int> pins = new HashSet<int>(); public HashSet<int> veil = new HashSet<int>();
        public List<Move> scramble;
    }

    public static class Engine
    {
        public static readonly double SQ3 = Math.Sqrt(3);
        static readonly int[][] DIRS = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 }, new[] { 1, -1 }, new[] { -1, 1 } };
        public static readonly double[][] AXD = { new[] { 1.0, 0.0 }, new[] { 0.5, SQ3 / 2 }, new[] { 0.5, -SQ3 / 2 } };
        public static int KeyOf(Cell c, int a) { return a == 0 ? c.r : a == 1 ? c.q : c.s; }
        static int ParOf(Cell c, int a) { return a == 1 ? c.r : c.q; }
        static long K(int q, int r) { return ((long)q << 32) ^ (uint)r; }

        static readonly Dictionary<int, Board> cache = new Dictionary<int, Board>();
        public static Board BuildBoard(int R)
        {
            if (cache.TryGetValue(R, out var cb)) return cb;
            var B = new Board { R = R };
            for (int r = -R; r <= R; r++) for (int q = -R; q <= R; q++)
                {
                    int s = -q - r; if (Math.Abs(s) > R) continue;
                    B.idx[K(q, r)] = B.cells.Count; B.cells.Add(new Cell { q = q, r = r, s = s, i = B.cells.Count });
                }
            for (int a = 0; a < 3; a++)
            {
                var m = new Dictionary<int, List<int>>();
                foreach (var c in B.cells) { int k = KeyOf(c, a); if (!m.ContainsKey(k)) m[k] = new List<int>(); m[k].Add(c.i); }
                var d = new Dictionary<int, int[]>();
                foreach (var kv in m) { var arr = kv.Value; int aa = a; arr.Sort((x, y) => ParOf(B.cells[x], aa) - ParOf(B.cells[y], aa)); d[kv.Key] = arr.ToArray(); }
                B.rows[a] = d;
            }
            B.N = B.cells.Count;
            B.nb = new int[B.N][];
            for (int i = 0; i < B.N; i++)
            {
                var c = B.cells[i]; var l = new List<int>();
                foreach (var dd in DIRS) if (B.idx.TryGetValue(K(c.q + dd[0], c.r + dd[1]), out int j)) l.Add(j);
                B.nb[i] = l.ToArray();
            }
            cache[R] = B; return B;
        }
        public static List<int[]> MoveRows(Board B, Move m)
        {
            var o = new List<int[]>();
            if (m.side == 0) { o.Add(B.rows[m.a][m.v]); return o; }
            for (int k = m.v; Math.Abs(k) <= B.R; k += m.side) o.Add(B.rows[m.a][k]);
            return o;
        }
        public static int Modk(int k, int n) { return ((k % n) + n) % n; }
        public static int[] ApplyMove(Board B, int[] st, Move m)
        {
            var ns = (int[])st.Clone();
            foreach (var row in MoveRows(B, m))
            {
                int n = row.Length, kk = Modk(m.k, n); if (kk == 0) continue;
                for (int i = 0; i < n; i++) ns[row[(i + kk) % n]] = st[row[i]];
            }
            return ns;
        }
        public static bool IsNoop(Board B, Move m) { foreach (var row in MoveRows(B, m)) if (Modk(m.k, row.Length) != 0) return false; return true; }
        public static Move Inv(Move m) { return new Move(m.a, m.v, m.side, -m.k); }
        public static bool IsSolved(int[] st) { for (int i = 0; i < st.Length; i++) if (st[i] != i) return false; return true; }
        public static HashSet<int> MovedCells(Board B, Move m)
        {
            var s = new HashSet<int>();
            foreach (var row in MoveRows(B, m)) if (Modk(m.k, row.Length) != 0) foreach (var c in row) s.Add(c);
            return s;
        }
        public static Move NormK(Board B, Move m)
        {
            if (m.side != 0) return m;
            int n = B.rows[m.a][m.v].Length; int k = Modk(m.k, n); if (k * 2 > n) k -= n;
            return m.WithK(k);
        }
        public static List<LegalMove> AllMoves(Board B, bool faults)
        {
            var o = new List<Move>(); int R = B.R;
            for (int a = 0; a < 3; a++) for (int v = -R; v <= R; v++)
                {
                    int n = B.rows[a][v].Length;
                    for (int k = 1; k < n; k++) o.Add(new Move(a, v, 0, k * 2 <= n ? k : k - n));
                    if (!faults) continue;
                    int[] sides = v > 0 ? new[] { 1 } : v < 0 ? new[] { -1 } : new[] { 1, -1 };
                    foreach (int side in sides)
                    {
                        if (v == side * R) continue;
                        int L = 0; foreach (var r in MoveRows(B, new Move(a, v, side, 0))) L = Math.Max(L, r.Length);
                        for (int k = -(L - 1); k <= L - 1; k++) { if (k == 0) continue; var m = new Move(a, v, side, k); if (!IsNoop(B, m)) o.Add(m); }
                    }
                }
            var res = new List<LegalMove>(o.Count);
            foreach (var m in o) res.Add(new LegalMove { m = m, cells = MovedCells(B, m) });
            return res;
        }
        static List<int> WrongOf(int[] st) { var w = new List<int>(); for (int i = 0; i < st.Length; i++) if (st[i] != i) w.Add(i); return w; }
        static bool Covers(LegalMove m, List<int> w) { foreach (var c in w) if (!m.cells.Contains(c)) return false; return true; }
        public static List<Move> FindSolution(Board B, int[] st, List<LegalMove> moves, int maxDepth)
        {
            var w0 = WrongOf(st); if (w0.Count == 0) return new List<Move>();
            foreach (var m in moves) if (Covers(m, w0) && IsSolved(ApplyMove(B, st, m.m))) return new List<Move> { m.m };
            if (maxDepth < 2) return null;
            List<Move> best = null; int bestCost = 1000000000;
            foreach (var m1 in moves)
            {
                var s1 = ApplyMove(B, st, m1.m); var w1 = WrongOf(s1);
                foreach (var m2 in moves)
                {
                    if (!Covers(m2, w1)) continue;
                    if (IsSolved(ApplyMove(B, s1, m2.m))) { int c = Math.Abs(m1.m.k) + Math.Abs(m2.m.k); if (c < bestCost) { bestCost = c; best = new List<Move> { m1.m, m2.m }; } }
                }
            }
            return best;
        }
        public static List<Move> SimplifyRoute(Board B, List<Move> list)
        {
            var o = new List<Move>();
            foreach (var m0 in list)
            {
                if (o.Count > 0) { var last = o[o.Count - 1]; if (last.a == m0.a && last.v == m0.v && last.side == m0.side) { last.k += m0.k; if (IsNoop(B, last)) o.RemoveAt(o.Count - 1); else o[o.Count - 1] = last; continue; } }
                if (!IsNoop(B, m0)) o.Add(m0);
            }
            for (int i = 0; i < o.Count; i++) o[i] = NormK(B, o[i]);
            return o;
        }
        // mulberry32 exactly as the web build
        public class Rng
        {
            uint seed; public Rng(uint s) { seed = s; }
            public double Next()
            {
                seed = unchecked(seed + 0x6D2B79F5u);
                uint t = unchecked((seed ^ (seed >> 15)) * (1u | seed));
                t = unchecked((t + (t ^ (t >> 7)) * (61u | t)) ^ t);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }
        public static uint HashStr(string s)
        {
            uint h = 2166136261;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                h ^= c; h = unchecked(h * 16777619u);
                if (char.IsHighSurrogate(c) && i + 1 < s.Length) i++; // JS for..of iterates code points, charCodeAt(0) = high surrogate
            }
            return h;
        }
        public static List<Move> GenScramble(Board B, string seedStr, int count, bool faults, List<LegalMove> legal = null)
        {
            var r = new Rng(HashStr(seedStr)); var moves = legal ?? AllMoves(B, faults); var seq = new List<Move>();
            var st = Identity(B.N); int guard = 0;
            while (seq.Count < count && guard++ < 500)
            {
                var m = moves[(int)Math.Floor(r.Next() * moves.Count)].m;
                if (seq.Count > 0) { var last = seq[seq.Count - 1]; if (last.a == m.a && last.v == m.v) continue; }
                seq.Add(m); st = ApplyMove(B, st, m);
            }
            if (IsSolved(st)) return GenScramble(B, seedStr + "x", count, faults, legal);
            return seq;
        }
        public static int[] Identity(int n) { var a = new int[n]; for (int i = 0; i < n; i++) a[i] = i; return a; }
        public static List<int> PickCells(Board B, string seed, int n, List<int> pool = null)
        {
            var r = new Rng(HashStr(seed)); var src = pool ?? new List<int>(Identity(B.N)); var o = new List<int>(); int g = 0;
            int lim = Math.Min(n, src.Count);
            while (o.Count < lim && g++ < 800) { int c = src[(int)Math.Floor(r.Next() * src.Count)]; if (!o.Contains(c)) o.Add(c); }
            return o;
        }
        static int JsRound(double x) { return (int)Math.Floor(x + 0.5); }
        public static Prepared PrepareLevel(LevelDef def)
        {
            var B = BuildBoard(def.R);
            var pins = def.pins > 0 ? PickCells(B, (def.seed ?? "pin") + "#pin", def.pins) : new List<int>();
            var legal = new List<LegalMove>();
            foreach (var m in AllMoves(B, def.faults)) { bool ok = true; foreach (var p in pins) if (m.cells.Contains(p)) { ok = false; break; } if (ok) legal.Add(m); }
            if (def.gen > 0 && def.moves == null) def.moves = GenScramble(B, def.seed, def.gen, def.faults, legal);
            var st = Identity(B.N); foreach (var m in def.moves) st = ApplyMove(B, st, m);
            var wrongT = new List<int>(); for (int c = 0; c < st.Length; c++) if (st[c] != c) wrongT.Add(st[c]);
            var veil = def.veils ? PickCells(B, (def.seed ?? "veil") + "#veil", Math.Min(14, JsRound(wrongT.Count * .42)), wrongT) : new List<int>();
            int target = def.moves.Count;
            if (target > 1 && target <= 3) { var s = FindSolution(B, st, legal, Math.Min(2, target - 1)); if (s != null) target = s.Count; }
            var P = new Prepared { B = B, start = st, moves = legal, target = target, scramble = def.moves };
            foreach (var p in pins) P.pins.Add(p); foreach (var v in veil) P.veil.Add(v);
            return P;
        }
        // challenge payload: explicit scramble + pins + veil
        public static Prepared PrepareExplicit(int R, bool faults, List<Move> mv, List<int> pins, List<int> veil)
        {
            var B = BuildBoard(R);
            var legal = new List<LegalMove>();
            foreach (var m in AllMoves(B, faults)) { bool ok = true; foreach (var p in pins) if (m.cells.Contains(p)) { ok = false; break; } if (ok) legal.Add(m); }
            var st = Identity(B.N); foreach (var m in mv) st = ApplyMove(B, st, m);
            int target = mv.Count;
            if (target > 1 && target <= 3) { var s = FindSolution(B, st, legal, Math.Min(2, target - 1)); if (s != null) target = s.Count; }
            var P = new Prepared { B = B, start = st, moves = legal, target = target, scramble = mv };
            foreach (var p in pins) if (p >= 0 && p < B.N) P.pins.Add(p);
            foreach (var v in veil) if (v >= 0 && v < B.N) P.veil.Add(v);
            return P;
        }
    }
}
