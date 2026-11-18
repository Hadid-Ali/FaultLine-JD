using System.Collections.Generic;
using UnityEngine;

namespace FaultLine
{
    /// <summary>Player save — same shape as the web build's localStorage save, kept in PlayerPrefs as JSON.</summary>
    public static class Save
    {
        const string KEY = "faultline_v1";
        public static JObj D = new JObj();
        public static bool Ar => S("lang", "ar") == "ar";

        public static void Load()
        {
            D = null;
            try { var s = PlayerPrefs.GetString(KEY, ""); if (!string.IsNullOrEmpty(s)) D = Json.Parse(s) as JObj; } catch { D = null; }
            if (D == null) D = new JObj();
            if (D.O("best") == null) D["best"] = new JObj();
            if (D["unlocked"] == null) D["unlocked"] = 1.0;
            if (D["lang"] == null) D["lang"] = "ar";
            if (D["sound"] == null) D["sound"] = true;
            if (D["haptics"] == null) D["haptics"] = true;
            if (D["comfort"] == null) D["comfort"] = false;
            if (D.O("daily") == null) D["daily"] = new JObj();
            if (D["sinceAd"] == null) D["sinceAd"] = 0.0;
        }
        public static void Persist() { try { PlayerPrefs.SetString(KEY, Json.Write(D)); PlayerPrefs.Save(); } catch { } }
        public static void Reset() { var lang = S("lang", "ar"); PlayerPrefs.DeleteKey(KEY); Load(); D["lang"] = lang; Persist(); }

        public static string S(string k, string d = null) { return D.S(k, d); }
        public static int I(string k, int d = 0) { return D.I(k, d); }
        public static bool B(string k, bool d = false) { return D.B(k, d); }
        public static void Set(string k, object v) { D[k] = v is int i ? (double)i : v; }

        public static int Gold { get => I("gold"); set => D["gold"] = (double)value; }
        public static int Unlocked { get => Mathf.Max(1, I("unlocked", 1)); set => D["unlocked"] = (double)value; }
        public static JObj Best => D.O("best");
        public static JObj BestOf(int i) { return Best.O(i.ToString()); }
        public static int LevelsDone => Best.Count;
        public static bool Sound => B("sound", true);
        public static bool Comfort => B("comfort");
        public static JObj Obj(string k) { var o = D.O(k); if (o == null) { o = new JObj(); D[k] = o; } return o; }
        public static List<object> Arr(string k) { var a = D.A(k); if (a == null) { a = new List<object>(); D[k] = a; } return a; }

        // per-day counters (web: dayLeft/dayUse)
        public static int DayLeft(string k, int lim) { var d = Util.KwDate(); if (S(k + "Day") != d) { D[k + "Day"] = d; D[k + "Used"] = 0.0; } return Mathf.Max(0, lim - I(k + "Used")); }
        public static void DayUse(string k) { D[k + "Used"] = (double)(I(k + "Used") + 1); Persist(); }
        public static int HintsLeft(int free) { var d = Util.KwDate(); if (S("hintDay") != d) { D["hintDay"] = d; D["hintsUsed"] = 0.0; Persist(); } return Mathf.Max(0, free - I("hintsUsed")); }
    }
}
