using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FaultLine
{
    /// <summary>Albums, weekly event, streak, crowns — port of the web build's depth layer.</summary>
    public static class Meta
    {
        public const int ALB_DONE = 150, ALB_MASTER = 300, EXPERT_GOLD = 60;
        public static readonly int[] STREAK_GOLD = { 20, 30, 40, 50, 60, 80, 150 };
        public static int LvlOf(string pic) { for (int i = 0; i < Content.Levels.Count; i++) if (Content.Levels[i].pic == pic) return i; return -1; }
        public static Album AlbOfPic(string pic) { foreach (var a in Content.Albums) if (a.pics.Contains(pic)) return a; return null; }
        public struct AlbStats { public int solved, master, stars, n; }
        public static AlbStats Stats(Album A)
        {
            var s = new AlbStats { n = A.pics.Count };
            foreach (var p in A.pics) { var b = Save.BestOf(LvlOf(p)); if (b != null) { s.solved++; int g = b.I("grade"); s.stars += g + 1; if (g >= 2) s.master++; } }
            return s;
        }
        public static JObj AlbSave(string id) { var al = Save.Obj("alb"); var o = al.O(id); if (o == null) { o = new JObj(); al[id] = o; } return o; }
        public struct Event { public int wk, left; public Album A; }
        public static Event Now()
        {
            int dn = Util.DayNum(Util.KwDate()); int wk = (int)Math.Floor((dn + 4) / 7.0);
            return new Event { wk = wk, A = Content.Albums[((wk % Content.Albums.Count) + Content.Albums.Count) % Content.Albums.Count], left = 7 - (((dn + 4) % 7) + 7) % 7 };
        }
        public static bool IsEventPic(string pic) { return Now().A.pics.Contains(pic); }
        public static bool LvOpen(int i) { return i + 1 <= Save.Unlocked || IsEventPic(Content.Levels[i].pic); }
        public static int StreakNow()
        {
            var s = Save.D.O("streak"); if (s == null || s.S("last") == null) return 0;
            int g = Util.DayNum(Util.KwDate()) - Util.DayNum(s.S("last")); return g <= 1 ? s.I("n") : 0;
        }
        public static bool StreakToday() { var s = Save.D.O("streak"); return s != null && s.S("last") == Util.KwDate(); }
        /// <summary>Returns gold granted (0 if already counted today).</summary>
        public static int BumpStreak()
        {
            string d = Util.KwDate(); var s = Save.D.O("streak") ?? new JObj(); if (s.S("last") == d) return 0;
            int n = (s.S("last") != null && Util.DayNum(d) - Util.DayNum(s.S("last")) == 1) ? s.I("n") + 1 : 1;
            s["n"] = (double)n; s["last"] = d; Save.D["streak"] = s; int g = STREAK_GOLD[(n - 1) % 7]; Save.Gold += g; return g;
        }
        public static int TotalStars() { int t = 0; foreach (var kv in Save.Best) t += ((JObj)kv.Value).I("grade") + 1; return t; }
        public static bool Crowned(int i) { var c = Save.D.O("crown"); return c != null && c.Has(i.ToString()); }
        public static int Crowns() { var c = Save.D.O("crown"); return c?.Count ?? 0; }
        public static int GoldByGrade(int g) => Content.KI("GOLD_BY_GRADE", g);
        public static int GoldDailyBy(int g) => Content.KI("GOLD_DAILY_BY", g);
    }

    /// <summary>WhatsApp 1v1 challenge links — byte-compatible with the web build.</summary>
    public static class Challenge
    {
        public const int CH_GOLD_WIN = 30, CH_GOLD_PLAY = 10, CH_PER_DAY = 5;
        static string B64e(string json)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }
        static string B64d(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/'); while (s.Length % 4 != 0) s += "=";
            return Encoding.UTF8.GetString(Convert.FromBase64String(s));
        }
        public static JObj Spec(Game g)
        {
            var o = new JObj();
            o["p"] = g.Pic; o["R"] = (double)g.def.R; o["f"] = g.Faults ? 1.0 : 0.0;
            var mv = new List<string>(); foreach (var m in g.P.scramble) mv.Add(m.a + "," + m.v + "," + m.side + "," + m.k); o["mv"] = string.Join(";", mv);
            var pn = new List<int>(g.P.pins); pn.Sort(); o["pn"] = string.Join(",", pn);
            var vl = new List<int>(g.P.veil); o["vl"] = string.Join(",", vl);
            return o;
        }
        public static string Url(JObj payload) { return Config.ChallengeSite + "#c=" + B64e(Json.Write(payload)); }
        public static ChalData Parse(string urlOrHash)
        {
            try
            {
                int i = urlOrHash.IndexOf("#c="); if (i < 0) return null;
                string b = urlOrHash.Substring(i + 3).Trim(); int sp = b.IndexOfAny(new[] { ' ', '\n', '\r' }); if (sp > 0) b = b.Substring(0, sp);
                var o = Json.Parse(B64d(b)) as JObj; if (o == null) return null;
                var d = new ChalData { p = o.S("p"), R = o.I("R"), f = o.D("f") != 0, t = o.D("t"), c = o.I("c"), mv = o.S("mv"), pn = o.S("pn", ""), vl = o.S("vl", "") };
                string n = o.S("n") ?? "?"; d.n = n.Length > 20 ? n.Substring(0, 20) : n; if (d.n == "") d.n = "?";
                if (!Content.PicKeys.Contains(d.p) || d.R < 2 || d.R > 4 || string.IsNullOrEmpty(d.mv) || !(d.t > 0)) return null;
                foreach (var x in d.mv.Split(';'))
                {
                    if (x == "") continue; var a = x.Split(','); if (a.Length != 4) return null;
                    var m = new Move(int.Parse(a[0]), int.Parse(a[1]), int.Parse(a[2]), int.Parse(a[3]));
                    if (m.a < 0 || m.a > 2 || Math.Abs(m.v) > d.R || m.side < -1 || m.side > 1) return null; d.mvs.Add(m);
                }
                if (d.mvs.Count == 0 || d.mvs.Count > 12) return null;
                foreach (var x in d.pn.Split(',')) if (x != "") d.pns.Add(int.Parse(x));
                foreach (var x in d.vl.Split(',')) if (x != "") d.vls.Add(int.Parse(x));
                var r = o.O("r"); if (r != null) { d.rn = r.S("n"); d.rt = r.D("t"); }
                return d;
            }
            catch { return null; }
        }
        public static JObj H2H(string n) { var h = Save.Obj("h2h"); var o = h.O(n); if (o == null) { o = new JObj(); o["w"] = 0.0; o["l"] = 0.0; h[n] = o; } return o; }
        public static bool Seen(string key)
        {
            var a = Save.Arr("chSeen"); if (a.Contains(key)) return true; a.Add(key); if (a.Count > 60) a.RemoveAt(0); return false;
        }
        public static void Send(string text, JObj payload)
        {
            string full = text + "\n" + Url(payload);
            Application.OpenURL("https://wa.me/?text=" + Uri.EscapeDataString(full));
        }
    }
}
