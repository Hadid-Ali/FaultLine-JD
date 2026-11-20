using System;
using UnityEngine;
using UnityEngine.UIElements;
#if FAULTLINE_ADMOB && !UNITY_EDITOR
using GoogleMobileAds.Api;
#endif

namespace FaultLine
{
    public enum BannerPosition { Top, Bottom }

    /// <summary>
    /// Google AdMob ads: banner, interstitial, rewarded.
    /// Real ads need the FAULTLINE_ADMOB define (set by Fault Line ▸ 1 - Set up project when the
    /// Google Mobile Ads plugin is installed) and only run on device; in the Editor (or without the
    /// define) a built-in mock is shown so the game is fully playable.
    /// </summary>
    public static class AdsController
    {
        /// <summary>Mock full-screen ad (kind, onDone, msg, got, no) — set by App.</summary>
        public static Action<string, Action, string, string, string> MockShow;

        public static bool BannerVisible { get; private set; }
        static BannerPosition bannerPos = BannerPosition.Bottom;
        static bool AdsRemoved => Save.B("adsRemoved");

        /// <summary>Standard phone banner height (50dp) converted to UI Toolkit panel units.</summary>
        public static float BannerHeight
        {
            get
            {
                float dpi = Screen.dpi > 0 ? Screen.dpi : 160f;
                float px = 50f * dpi / 160f;
                var root = App.I != null ? App.I.root : null;
                float k = root != null && root.layout.height > 10 ? root.layout.height / Screen.height : 1f;
                return px * k;
            }
        }

        public static void ShowBanner(BannerPosition pos = BannerPosition.Bottom)
        {
            if (AdsRemoved) { HideBanner(); return; }
            bool first = !BannerVisible;
            bannerPos = pos; BannerVisible = true; PlatformShowBanner();
            if (first) GameAnalyticsController.DesignEvent("ad:banner:shown");
        }

        public static void HideBanner()
        {
            if (!BannerVisible) return;
            BannerVisible = false; PlatformHideBanner();
        }

        /// <summary>Shows the banner for a screen and pads the screen so content isn't hidden under it.</summary>
        public static void ShowBannerOn(VisualElement screenRoot, BannerPosition pos = BannerPosition.Bottom)
        {
            ShowBanner(pos); Reserve(screenRoot);
            // panel layout isn't known on the very first frame — re-measure shortly after
            App.I.Delay(.1f, () => { if (!BannerVisible) return; Reserve(screenRoot); RefreshMock(); });
        }
        public static void HideBannerOn(VisualElement screenRoot)
        {
            HideBanner(); if (screenRoot == null) return;
            screenRoot.style.paddingTop = 0; screenRoot.style.paddingBottom = 0;
        }
        static void Reserve(VisualElement r)
        {
            if (r == null) return; float h = BannerVisible ? BannerHeight : 0;
            r.style.paddingBottom = bannerPos == BannerPosition.Bottom ? h : 0;
            r.style.paddingTop = bannerPos == BannerPosition.Top ? h : 0;
        }

        /// <summary>Shows an interstitial if one is ready. <paramref name="onDone"/> always runs afterwards.</summary>
        public static void ShowInterstitial(Action onDone)
        {
            GameAnalyticsController.DesignEvent("ad:interstitial");
            PlatformInterstitial(onDone ?? (() => { }));
        }

        /// <summary>Shows a rewarded ad. <paramref name="onRewarded"/> runs only if the reward was earned.</summary>
        public static void ShowRewarded(Action onRewarded, string msg = null, string got = null, string no = null)
        {
            PlatformRewarded(onRewarded, msg, got, no);
        }

        static void NotReadyToast() { App.I.Toast(Save.Ar ? "الإعلان غير جاهز بعد، حاول بعد لحظات" : "Ad not ready yet, try again in a moment"); }
        static void MarkReward() { Save.D["lastReward"] = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); Save.Persist(); }

#if FAULTLINE_ADMOB && !UNITY_EDITOR
        static string BId => Config.AdMobBannerAndroid;
        static string IId => Config.AdMobInterstitialAndroid;
        static string RId => Config.AdMobRewardedAndroid;
        public static bool Live => true;
        static bool sdkReady;
        static BannerView banner; static BannerPosition bannerBuiltFor;
        static InterstitialAd inter; static RewardedAd rewarded;
        static Action pendingReward; static bool rewardEarned; static int retryR, retryI;
        static bool loadingR, loadingI;

        public static void Init()
        {
            // our callbacks touch UI Toolkit and App.I timers — they must run on the main thread
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.Initialize(status =>
            {
                sdkReady = true; LoadR(); LoadI();
                if (BannerVisible) PlatformShowBanner();
            });
        }

        // ---- rewarded ----
        static void LoadR()
        {
            if (loadingR || (rewarded != null && rewarded.CanShowAd())) return;
            loadingR = true;
            RewardedAd.Load(RId, new AdRequest(), (ad, err) =>
            {
                loadingR = false;
                if (err != null || ad == null)
                {
                    Debug.LogWarning("[Ads] rewarded load failed: " + err);
                    retryR++; App.I.Delay((float)Math.Pow(2, Math.Min(6, retryR)), LoadR); return;
                }
                retryR = 0; rewarded = ad;
                ad.OnAdFullScreenContentFailed += e =>
                {
                    Debug.LogWarning("[Ads] rewarded show failed: " + e);
                    pendingReward = null; Drop(ref rewarded, ad); LoadR();
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    Drop(ref rewarded, ad); LoadR();
                    var cb = pendingReward; pendingReward = null;
                    if (rewardEarned && cb != null) App.I.Delay(.05f, () => { Sfx.Reward(); Haptics.Do("win"); cb(); });
                    rewardEarned = false;
                };
            });
        }

        // ---- interstitial ----
        static Action pendingInter;
        static void LoadI()
        {
            if (loadingI || (inter != null && inter.CanShowAd())) return;
            loadingI = true;
            InterstitialAd.Load(IId, new AdRequest(), (ad, err) =>
            {
                loadingI = false;
                if (err != null || ad == null)
                {
                    Debug.LogWarning("[Ads] interstitial load failed: " + err);
                    retryI++; App.I.Delay((float)Math.Pow(2, Math.Min(6, retryI)), LoadI); return;
                }
                retryI = 0; inter = ad;
                ad.OnAdFullScreenContentFailed += e =>
                {
                    Debug.LogWarning("[Ads] interstitial show failed: " + e);
                    Drop(ref inter, ad); LoadI(); var cb = pendingInter; pendingInter = null; cb?.Invoke();
                };
                ad.OnAdFullScreenContentClosed += () =>
                {
                    Drop(ref inter, ad); LoadI(); var cb = pendingInter; pendingInter = null; App.I.Delay(.05f, () => cb?.Invoke());
                };
            });
        }

        static void Drop<T>(ref T slot, T ad) where T : class
        {
            if (ReferenceEquals(slot, ad)) slot = null;
            (ad as InterstitialAd)?.Destroy(); (ad as RewardedAd)?.Destroy();
        }

        // ---- banner ----
        static AdPosition AdPos(BannerPosition p) { return p == BannerPosition.Top ? AdPosition.Top : AdPosition.Bottom; }

        static void PlatformShowBanner()
        {
            if (!sdkReady) return; // shown once the SDK finishes initializing
            if (banner != null && bannerBuiltFor != bannerPos) { banner.Destroy(); banner = null; }
            if (banner == null)
            {
                bannerBuiltFor = bannerPos;
                banner = new BannerView(BId, AdSize.Banner, AdPos(bannerPos));
                banner.OnBannerAdLoaded += () => { if (!BannerVisible) banner?.Hide(); };
                banner.OnBannerAdLoadFailed += e => Debug.LogWarning("[Ads] banner load failed: " + e);
                banner.LoadAd(new AdRequest());
            }
            banner.Show();
        }
        static void PlatformHideBanner() { banner?.Hide(); }
        static void RefreshMock() { }

        static void PlatformInterstitial(Action onDone)
        {
            if (sdkReady && inter != null && inter.CanShowAd()) { pendingInter = onDone; inter.Show(); }
            else { onDone(); if (sdkReady) LoadI(); }
        }
        static void PlatformRewarded(Action onRewarded, string msg, string got, string no)
        {
            if (sdkReady && rewarded != null && rewarded.CanShowAd())
            {
                pendingReward = onRewarded; rewardEarned = false; MarkReward();
                rewarded.Show(reward => rewardEarned = true);
            }
            else { NotReadyToast(); if (sdkReady) LoadR(); }
        }
#else
        public static bool Live => false;
        static VisualElement mockBanner;

        public static void Init() { }

        static void PlatformShowBanner()
        {
            var layer = App.I != null ? App.I.flyLayer : null; if (layer == null) return;
            if (mockBanner == null || mockBanner.parent != layer)
            {
                mockBanner = U.E("mock-banner", layer); mockBanner.pickingMode = PickingMode.Ignore;
                mockBanner.style.position = Position.Absolute; mockBanner.style.left = 0; mockBanner.style.right = 0;
                mockBanner.style.alignItems = Align.Center; mockBanner.style.justifyContent = Justify.Center;
                mockBanner.style.backgroundColor = new Color(.04f, .07f, .08f, .95f);
                U.L(Content.T("adLabel"), 11, U.Muted, U.Semi, mockBanner);
            }
            float h = BannerHeight; mockBanner.style.height = h;
            if (bannerPos == BannerPosition.Top) { mockBanner.style.top = 0; mockBanner.style.bottom = StyleKeyword.Auto; }
            else { mockBanner.style.bottom = App.I.SafeBottom; mockBanner.style.top = StyleKeyword.Auto; }
            mockBanner.style.display = DisplayStyle.Flex; mockBanner.BringToFront();
        }
        static void PlatformHideBanner() { if (mockBanner != null) mockBanner.style.display = DisplayStyle.None; }
        static void RefreshMock() { if (BannerVisible) PlatformShowBanner(); }

        static void PlatformInterstitial(Action onDone) { if (MockShow != null) MockShow("interstitial", onDone, null, null, null); else onDone(); }
        static void PlatformRewarded(Action onRewarded, string msg, string got, string no) { if (MockShow != null) MockShow("rewarded", onRewarded, msg, got, no); else NotReadyToast(); }
#endif
    }
}
