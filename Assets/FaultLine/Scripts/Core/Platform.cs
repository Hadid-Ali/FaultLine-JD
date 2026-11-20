using System;
using System.Collections.Generic;
using UnityEngine;

namespace FaultLine
{
    /// <summary>Sound effects rendered offline from the web build's synthesizer (Resources/sfx/*.ogg).</summary>
    public static class Sfx
    {
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static AudioSource[] pool; static int next;
        public static void Init(GameObject host)
        {
            pool = new AudioSource[12];
            for (int i = 0; i < pool.Length; i++) { var s = host.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0; pool[i] = s; }
        }
        static AudioClip Clip(string n)
        {
            if (clips.TryGetValue(n, out var c)) return c;
            c = Resources.Load<AudioClip>("sfx/" + n); clips[n] = c; return c;
        }
        public static void Play(string n, float vol = 1f, float pitch = 1f, float delay = 0f)
        {
            if (!Save.Sound || pool == null) return;
            var c = Clip(n); if (c == null) return;
            var s = pool[next]; next = (next + 1) % pool.Length;
            s.Stop(); s.clip = c; s.volume = vol; s.pitch = pitch;
            if (delay > 0) s.PlayDelayed(delay); else s.Play();
        }
        public static void Touch() => Play("touch");
        public static void Tick(int n = 0) => Play("tick0", 1f, (1900f + Mathf.Min(n, 12) * 110f) / 1900f);
        public static void Commit(Move m) => Play("commit_" + m.a + "_" + (m.k > 0 ? 1 : 0) + "_" + (m.side != 0 ? 1 : 0));
        public static void Home(int n) => Play("home" + Mathf.Clamp(n, 1, 3));
        public static void Coins(int n = 4) => Play(n <= 2 ? "coins2" : n <= 4 ? "coins4" : "coins6");
        public static void Coin() => Play("coin", .9f, 1f + UnityEngine.Random.Range(-.04f, .06f));
        public static void Ui() => Play("ui");
        public static void Undo() => Play("undo");
        public static void Hint() => Play("hint");
        public static void Armed() => Play("armed");
        public static void Near() => Play("near");
        public static void Sparkle() => Play("sparkle" + UnityEngine.Random.Range(0, 4));
        public static void Reward() => Play("reward");
        public static void Win(int moves) => Play(moves <= 3 ? "win3" : moves <= 5 ? "win1" : "win2");
        public static void ZoomIn() => Play("zoomIn");
        public static void ZoomOut() => Play("zoomOut");
        public static void Nudge() => Play("nudge");
        public static void Drop() => Play("drop");
        public static void Lift() => Play("lift");
        public static void Wind() => Play("wind");
        public static void Shutter() => Play("shutter");
    }

    /// <summary>Short haptic pulses (Android vibrator). No-op in the editor.</summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        static AndroidJavaObject vib; static int sdk;
        static void Ensure()
        {
            if (vib != null) return;
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var act = up.GetStatic<AndroidJavaObject>("currentActivity"))
                    vib = act.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using (var v = new AndroidJavaClass("android.os.Build$VERSION")) sdk = v.GetStatic<int>("SDK_INT");
            }
            catch { }
        }
        static void Pulse(long ms, int amp)
        {
            if (!Save.B("haptics", true) || Save.Comfort) return;
            Ensure(); if (vib == null) return;
            try
            {
                if (sdk >= 26) using (var ve = new AndroidJavaClass("android.os.VibrationEffect")) using (var e = ve.CallStatic<AndroidJavaObject>("createOneShot", ms, amp)) vib.Call("vibrate", e);
                else vib.Call("vibrate", ms);
            }
            catch { }
        }
#else
        static void Pulse(long ms, int amp) { }
#endif
        public static void Do(string kind)
        {
            switch (kind)
            {
                case "light": Pulse(10, 60); break;
                case "tick": Pulse(6, 40); break;
                case "commit": Pulse(18, 140); break;
                case "home": Pulse(24, 170); break;
                case "armed": Pulse(28, 120); break;
                case "hint": Pulse(14, 90); break;
                case "win": Pulse(60, 220); break;
                default: Pulse(10, 60); break;
            }
        }
    }

    /// <summary>Edit these before publishing.</summary>
    public static class Config
    {
        public const string BundleId = "com.sooqista.faultline";
        // Google AdMob — these are Google's public TEST IDs (always serve "Test Ad", safe to click).
        // Before release replace them with your own AdMob IDs, and set the same App ID in
        // Assets ▸ Google Mobile Ads ▸ Settings (that asset, not this constant, is what the SDK reads).
        public const string AdMobAppIdAndroid = "ca-app-pub-3940256099942544~3347511713";
        public const string AdMobBannerAndroid = "ca-app-pub-3940256099942544/6300978111";
        public const string AdMobInterstitialAndroid = "ca-app-pub-3940256099942544/1033173712";
        public const string AdMobRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
        public const string ChallengeSite = "https://fault-line-sadaa.higgsfield.app/";
        public const string PrivacyUrl = "https://faultgame.vercel.app/privacy.html";
    }
}
