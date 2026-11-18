using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FaultLine
{
    public class Album { public string id, ar, en; public List<string> pics = new List<string>(); public string Name => Save.Ar ? ar : en; }
    public class Room { public string id, ar, en, ruin, done; public int cost, req; public float winX = .66f, winY = .4f; public string Name => Save.Ar ? ar : en; }
    public class Furn { public string id, ar, en; public int price; public float w; public bool floor, wall; public string Name => Save.Ar ? ar : en; }
    public class Fact { public string t, s, b; }

    /// <summary>All game text and data, extracted 1:1 from the web build (Resources/content.json).</summary>
    public static class Content
    {
        static JObj root, tAr, tEn, facts;
        public static List<string> PicKeys = new List<string>();
        static Dictionary<string, string[]> picNames = new Dictionary<string, string[]>();
        public static List<LevelDef> Levels = new List<LevelDef>();
        public static List<Album> Albums = new List<Album>();
        public static List<Room> Rooms = new List<Room>();
        public static List<Furn> Furn = new List<Furn>();
        public static List<Move> HeroSol = new List<Move>();
        public static HashSet<string> Living = new HashSet<string>();
        public static JObj K;
        public static bool Loaded;

        public static void Load()
        {
            if (Loaded) return;
            var ta = Resources.Load<TextAsset>("content");
            root = (JObj)Json.Parse(ta.text);
            var T = root.O("T"); tAr = T.O("ar"); tEn = T.O("en");
            facts = root.O("FACTS"); K = root.O("K");
            foreach (var kv in root.O("PICS")) { PicKeys.Add(kv.Key); var o = (JObj)kv.Value; picNames[kv.Key] = new[] { o.S("ar"), o.S("en") }; }
            foreach (JObj l in root.A("LEVELS")) Levels.Add(ParseLevel(l));
            foreach (JObj a in root.A("ALBUMS")) { var al = new Album { id = a.S("id"), ar = a.S("ar"), en = a.S("en") }; foreach (var p in a.A("pics")) al.pics.Add((string)p); Albums.Add(al); }
            foreach (JObj r in root.A("ROOMS")) { var w = r.A("win"); Rooms.Add(new Room { id = r.S("id"), ar = r.S("ar"), en = r.S("en"), ruin = r.S("ruin"), done = r.S("done"), cost = r.I("cost"), req = r.I("req"), winX = w != null ? (float)(double)w[0] : .66f, winY = w != null ? (float)(double)w[1] : .4f }); }
            foreach (JObj f in root.A("FURN")) Furn.Add(new Furn { id = f.S("id"), ar = f.S("ar"), en = f.S("en"), price = f.I("price"), w = (float)f.D("w"), floor = f.B("floor"), wall = f.B("wall") });
            foreach (JObj m in root.A("HERO_SOL")) HeroSol.Add(new Move(m.I("a"), m.I("v"), m.I("side"), m.I("k")));
            foreach (var kv in root.O("LIVING")) Living.Add(kv.Key);
            Loaded = true;
        }
        public static LevelDef ParseLevel(JObj o)
        {
            var d = new LevelDef { R = o.I("R"), pic = o.S("pic"), faults = o.B("faults"), gen = o.I("gen"), seed = o.S("seed"), pins = o.I("pins"), veils = o.B("veils") };
            var mv = o.A("moves"); if (mv != null) { d.moves = new List<Move>(); foreach (JObj m in mv) d.moves.Add(new Move(m.I("a"), m.I("v"), m.I("side"), m.I("k"))); }
            return d;
        }
        static JObj L => Save.Ar ? tAr : tEn;
        public static string T(string k)
        {
            var v = L[k]; if (v == null) v = tEn[k];
            if (v is string s) return s;
            if (v is JObj o) return o.S("fmt");
            return k;
        }
        /// <summary>Format a templated string: {0},{1}... are replaced in order.</summary>
        public static string F(string k, params object[] a)
        {
            string s = T(k);
            for (int i = 0; i < a.Length; i++) s = s.Replace("{" + i + "}", Str(a[i]));
            return s.Replace("\\n", "\n");
        }
        public static string Str(object o) { return o is IFormattable f ? f.ToString(null, CultureInfo.InvariantCulture) : o?.ToString() ?? ""; }
        public static List<string> TList(string k)
        {
            var o = new List<string>(); var v = L[k] as List<object>; if (v == null) return o;
            foreach (var x in v) o.Add(x as string); return o;
        }
        public static string Lesson(int i) { var l = TList("lessons"); return i >= 0 && i < l.Count ? l[i] : ""; }
        public static string Grade(int g) { var l = TList("grades"); return l[Mathf.Clamp(g, 0, l.Count - 1)]; }
        public static string PicName(string k) { return picNames.TryGetValue(k, out var n) ? n[Save.Ar ? 0 : 1] : k; }
        public static Fact FactOf(string pic)
        {
            var f = facts.O(pic); if (f == null) return null; var x = f.O(Save.Ar ? "ar" : "en"); if (x == null) return null;
            return new Fact { t = x.S("t"), s = x.S("s"), b = x.S("b") };
        }
        public static int KI(string k, int i = -1) { var v = K[k]; if (v is List<object> l) return (int)(double)l[i]; return (int)K.D(k); }

        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();
        public static Texture2D Tex(string path)
        {
            if (texCache.TryGetValue(path, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>(path); texCache[path] = t; return t;
        }
        public static Texture2D Pic(string key) { return Tex("pics/" + key); }
        public static Texture2D Icon(string key) { return Tex("icons/" + key); }
    }

    public static class Util
    {
        static readonly TimeSpan KW = TimeSpan.FromHours(3); // Asia/Kuwait, no DST
        public static DateTime KwNow => DateTime.UtcNow + KW;
        public static string KwDate() { return KwNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
        public static int DayNum(string d) { var dt = DateTime.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal); return (int)Math.Floor((dt - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays); }
        public static double NowMs => Time.realtimeSinceStartupAsDouble * 1000.0;
        public static string FmtT(double ms)
        {
            double s = Math.Max(0, ms) / 1000.0; int m = (int)Math.Floor(s / 60); double r = s - m * 60;
            return m + ":" + (r < 10 ? "0" : "") + r.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
