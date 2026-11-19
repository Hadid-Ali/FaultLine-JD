using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    public enum BannerPosition { Top, Bottom }

    /// <summary>
    /// AppLovin MAX ads: banner, interstitial, rewarded.
    /// Real ads need the FAULTLINE_MAX define (set by Fault Line ▸ 1 - Set up project when MAX is imported);
    /// without it a built-in mock is shown so the game is fully playable in the editor.
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

#if FAULTLINE_MAX
        static string BId => Config.MaxBannerAndroid;
        static string IId => Config.MaxInterstitialAndroid;
        static string RId => Config.MaxRewardedAndroid;
        public static bool Live => true;
        static bool sdkReady, bannerCreated;
        static Action pendingReward, pendingInter; static bool rewardEarned; static int retryR, retryI;

        public static void Init()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += cfg =>
            {
                sdkReady = true; LoadR(); LoadI();
                if (BannerVisible) PlatformShowBanner();
            };
            // rewarded
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += (id, info) => retryR = 0;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += (id, err) => { retryR++; App.I.Delay((float)Math.Pow(2, Math.Min(6, retryR)), LoadR); };
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += (id, err, info) => { LoadR(); pendingReward = null; };
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += (id, reward, info) => rewardEarned = true;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += (id, info) =>
            {
                LoadR(); var cb = pendingReward; pendingReward = null;
                if (rewardEarned && cb != null) App.I.Delay(.05f, () => { Sfx.Reward(); Haptics.Do("win"); cb(); });
                rewardEarned = false;
            };
            // interstitial
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += (id, info) => retryI = 0;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += (id, err) => { retryI++; App.I.Delay((float)Math.Pow(2, Math.Min(6, retryI)), LoadI); };
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += (id, err, info) => { LoadI(); var cb = pendingInter; pendingInter = null; cb?.Invoke(); };
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += (id, info) => { LoadI(); var cb = pendingInter; pendingInter = null; App.I.Delay(.05f, () => cb?.Invoke()); };
            MaxSdk.InitializeSdk();
        }
        static void LoadR() { MaxSdk.LoadRewardedAd(RId); }
        static void LoadI() { MaxSdk.LoadInterstitial(IId); }
        static MaxSdkBase.AdViewPosition MaxPos(BannerPosition p) { return p == BannerPosition.Top ? MaxSdkBase.AdViewPosition.TopCenter : MaxSdkBase.AdViewPosition.BottomCenter; }

        static void PlatformShowBanner()
        {
            if (!sdkReady) return; // shown once the SDK finishes initializing
            if (!bannerCreated)
            {
                MaxSdk.CreateBanner(BId, new MaxSdkBase.AdViewConfiguration(MaxPos(bannerPos)));
                MaxSdk.SetBannerBackgroundColor(BId, new Color(0, 0, 0, 0));
                bannerCreated = true;
            }
            else MaxSdk.UpdateBannerPosition(BId, MaxPos(bannerPos));
            MaxSdk.ShowBanner(BId);
        }
        static void PlatformHideBanner() { if (bannerCreated) MaxSdk.HideBanner(BId); }
        static void RefreshMock() { }

        static void PlatformInterstitial(Action onDone)
        {
            if (sdkReady && MaxSdk.IsInterstitialReady(IId)) { pendingInter = onDone; MaxSdk.ShowInterstitial(IId); }
            else onDone();
        }
        static void PlatformRewarded(Action onRewarded, string msg, string got, string no)
        {
            if (sdkReady && MaxSdk.IsRewardedAdReady(RId)) { pendingReward = onRewarded; rewardEarned = false; MarkReward(); MaxSdk.ShowRewardedAd(RId); }
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
