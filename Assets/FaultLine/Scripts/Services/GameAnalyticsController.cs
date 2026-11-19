using System.Text;
using GameAnalyticsSDK;
using UnityEngine;

namespace FaultLine
{
    /// <summary>
    /// GameAnalytics wrapper: initialization, design events and progression events.
    /// Needs Assets/Resources/GameAnalytics/Settings.asset (Window ▸ GameAnalytics ▸ Select Settings, add game key/secret).
    /// Every call is a safe no-op when the SDK isn't configured.
    /// </summary>
    public static class GameAnalyticsController
    {
        public static bool Ready { get; private set; }

        public static void Init()
        {
            if (Ready) return;
            if (Resources.Load("GameAnalytics/Settings") == null)
            {
                Debug.LogWarning("Fault Line: GameAnalytics settings not found (Window ▸ GameAnalytics ▸ Select Settings). Analytics disabled.");
                return;
            }
            if (Object.FindAnyObjectByType<GameAnalytics>() == null)
                new GameObject("GameAnalytics").AddComponent<GameAnalytics>(); // its Awake calls DontDestroyOnLoad
            try { GameAnalytics.Initialize(); Ready = true; }
            catch (System.Exception e) { Debug.LogWarning("Fault Line: GameAnalytics init failed: " + e.Message); }
        }

        /// <summary>Design event, e.g. "hint:used". Up to 5 ':'-separated parts.</summary>
        public static void DesignEvent(string eventId, float? value = null)
        {
            if (!Ready || string.IsNullOrEmpty(eventId)) return;
            var parts = eventId.Split(':'); for (int i = 0; i < parts.Length; i++) parts[i] = Clean(parts[i]);
            string id = string.Join(":", parts);
            if (value.HasValue) GameAnalytics.NewDesignEvent(id, value.Value); else GameAnalytics.NewDesignEvent(id);
        }

        public static void ProgressionEvent(GAProgressionStatus status, string p1, string p2 = null, string p3 = null, int? score = null)
        {
            if (!Ready || string.IsNullOrEmpty(p1)) return;
            p1 = Clean(p1); p2 = string.IsNullOrEmpty(p2) ? null : Clean(p2); p3 = p2 == null || string.IsNullOrEmpty(p3) ? null : Clean(p3);
            if (score.HasValue)
            {
                if (p3 != null) GameAnalytics.NewProgressionEvent(status, p1, p2, p3, score.Value);
                else if (p2 != null) GameAnalytics.NewProgressionEvent(status, p1, p2, score.Value);
                else GameAnalytics.NewProgressionEvent(status, p1, score.Value);
            }
            else
            {
                if (p3 != null) GameAnalytics.NewProgressionEvent(status, p1, p2, p3);
                else if (p2 != null) GameAnalytics.NewProgressionEvent(status, p1, p2);
                else GameAnalytics.NewProgressionEvent(status, p1);
            }
        }

        // ---- puzzle attempt lifecycle (each attempt gets exactly one Start and one Complete/Fail) ----
        public static void AttemptStart(Game g)
        {
            if (g == null) return;
            if (g.gaOpen) Progression(g, GAProgressionStatus.Fail);
            Progression(g, GAProgressionStatus.Start); g.gaOpen = true;
        }
        public static void AttemptComplete(Game g, int moves)
        {
            if (g == null || !g.gaOpen) return;
            Progression(g, GAProgressionStatus.Complete, moves); g.gaOpen = false;
        }
        public static void AttemptFail(Game g)
        {
            if (g == null || !g.gaOpen) return;
            Progression(g, GAProgressionStatus.Fail); g.gaOpen = false;
        }

        /// <summary>Progression event for a puzzle attempt: p1 = mode, p2 = level / date.</summary>
        public static void Progression(Game g, GAProgressionStatus status, int? score = null)
        {
            if (g == null) return;
            string p1, p2;
            switch (g.kind)
            {
                case Kind.Daily: p1 = "daily"; p2 = g.date; break;
                case Kind.Challenge: p1 = "challenge"; p2 = g.Pic; break;
                default: p1 = g.expert ? "expert" : "level"; p2 = "L" + (g.idx + 1).ToString("000"); break;
            }
            ProgressionEvent(status, p1, p2, null, score);
        }

        /// <summary>Keeps only characters GameAnalytics accepts: [A-Za-z0-9 -_.()!?], max 64.</summary>
        static string Clean(string s)
        {
            var b = new StringBuilder(s.Length);
            foreach (char c in s)
                if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || " -_.()!?".IndexOf(c) >= 0) b.Append(c);
            if (b.Length == 0) b.Append("unknown");
            return b.Length > 64 ? b.ToString(0, 64) : b.ToString();
        }
    }
}
