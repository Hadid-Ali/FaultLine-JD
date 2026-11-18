using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    public abstract class Scr
    {
        public VisualElement root;
        public abstract void Build();
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void Tick(double now, float dt) { }
        public virtual bool Back() { return false; }
    }

    /// <summary>Bootstraps the whole game from code — no scene setup needed.</summary>
    public class App : MonoBehaviour
    {
        public static App I;
        UIDocument doc; PanelSettings ps; public VisualElement root, screenLayer, overLayer, flyLayer; Label toast; double toastUntil;
        public readonly Dictionary<string, Scr> screens = new Dictionary<string, Scr>(); public string cur = "menu";
        public MenuScreen menu; public LevelsScreen levels; public SettingsScreen settings; public GameScreen game; public HomeScreen home;
        Rect lastSafe; Vector2 lastPanel; bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (I != null) return;
            var go = new GameObject("FaultLine"); DontDestroyOnLoad(go); go.AddComponent<App>();
        }

        void Awake()
        {
            I = this; Application.targetFrameRate = 60; Screen.sleepTimeout = SleepTimeout.SystemSetting;
            Input_Init();
            Content.Load(); Save.Load(); U.InitFonts(); U.ApplyTheme(Save.S("theme") != "light");
            Sfx.Init(gameObject); Ads.MockShow = MockAd; Ads.Init();
            var cam = Camera.main; if (cam == null) { var cg = new GameObject("Camera"); cg.transform.SetParent(transform); cam = cg.AddComponent<Camera>(); }
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = U.Bg; cam.cullingMask = 0; cam.orthographic = true;
            ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize; ps.referenceResolution = new Vector2Int(390, 844);
            ps.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight; ps.match = (float)Screen.width / Screen.height > .62f ? 1f : 0f;
            ps.clearColor = true; ps.colorClearValue = U.Bg;
            doc = gameObject.AddComponent<UIDocument>(); doc.panelSettings = ps;
            Application.deepLinkActivated += OnDeepLink;
        }
        void Input_Init() { }

        void Start()
        {
            BuildAll(); Show("menu", false);
            if (!string.IsNullOrEmpty(Application.absoluteURL)) OnDeepLink(Application.absoluteURL);
            Delay(1.2f, CheckClipboard);
        }

        public void BuildAll()
        {
            root = doc.rootVisualElement; root.Clear();
            U.ApplyTheme(Save.S("theme") != "light"); ps.colorClearValue = U.Bg; if (Camera.main) Camera.main.backgroundColor = U.Bg;
            root.style.flexGrow = 1; root.style.backgroundImage = new StyleBackground(Content.Tex(U.Dark ? "tex/bg" : "tex/white"));
            if (!U.Dark) root.style.unityBackgroundImageTintColor = U.Bg;
            root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromFont(U.Body));
            TextSetup.Apply(root);
            screenLayer = U.E("screens", root); U.Fill(screenLayer);
            overLayer = U.E("over", root); U.Fill(overLayer);
            flyLayer = U.E("fly", root); U.Fill(flyLayer);
            toast = U.L("", 14, Color.white, U.Semi, root); U.Abs(toast, null, null, null, 110); toast.style.alignSelf = Align.Center;
            toast.style.left = 24; toast.style.right = 24; U.Radius(toast, 14); U.Pad(toast, 10, 16); toast.style.backgroundColor = new Color(.04f, .07f, .08f, .92f);
            toast.style.whiteSpace = WhiteSpace.Normal; toast.style.display = DisplayStyle.None;
            root.focusable = true;
            root.RegisterCallback<NavigationCancelEvent>(e => { if (Time.frameCount != lastBackFrame) { lastBackFrame = Time.frameCount; GoBack(); } });
            screens.Clear();
            menu = new MenuScreen(); levels = new LevelsScreen(); settings = new SettingsScreen(); game = new GameScreen(); home = new HomeScreen();
            Add("menu", menu); Add("levels", levels); Add("settings", settings); Add("game", game); Add("home", home);
            built = true; lastSafe = default; ApplySafeArea();
        }
        void Add(string id, Scr s) { s.root = U.E(id, screenLayer); U.Fill(s.root); s.root.style.display = DisplayStyle.None; s.root.pickingMode = PickingMode.Ignore; s.Build(); screens[id] = s; }
        /// <summary>Rebuild every screen (language / theme change).</summary>
        public void Rebuild() { string c = cur; Game g = game?.G; BuildAll(); if (c == "game" && g != null) { cur = "menu"; game.Resume(g); } else Show(c, false); }

        public void Show(string id, bool sound = true)
        {
            if (!screens.TryGetValue(id, out var s)) return;
            if (screens.TryGetValue(cur, out var old) && old != s) { old.OnHide(); old.root.style.display = DisplayStyle.None; }
            cur = id; s.root.style.display = DisplayStyle.Flex; s.OnShow(); root.Focus();
        }
        int lastBackFrame = -1;
        public void GoBack()
        {
            if (Time.frameCount == lastBackFrame && Time.frameCount > 0 && backHandled) return; backHandled = true; App.I.Delay(0.05f, () => backHandled = false);
            if (CloseTopOverlay()) return;
            if (screens.TryGetValue(cur, out var s) && s.Back()) return;
            if (cur != "menu") { if (cur == "game") game.Leave(); Show("menu"); }
            else Application.Quit();
        }

        // ---------- overlays ----------
        readonly List<VisualElement> overlays = new List<VisualElement>();
        public VisualElement Overlay(out VisualElement box, bool dismissable = true, Action onClose = null)
        {
            var o = U.E(null, overLayer); U.Fill(o); o.style.backgroundColor = new Color(0, 0, 0, .6f); o.pickingMode = PickingMode.Position;
            o.style.alignItems = Align.Center; o.style.justifyContent = Justify.Center; U.Pad(o, 24, 18);
            box = U.E(null, o); box.pickingMode = PickingMode.Position; U.Radius(box, 24); box.style.backgroundColor = U.Surface2; U.Border(box, 1, U.Line); U.Pad(box, 22, 20);
            box.style.width = Length.Percent(100); box.style.maxWidth = 420;
            box.RegisterCallback<ClickEvent>(e => e.StopPropagation());
            if (dismissable) o.RegisterCallback<ClickEvent>(e => { CloseOverlay(o); onClose?.Invoke(); });
            overlays.Add(o); return o;
        }
        public VisualElement Sheet(out VisualElement box)
        {
            var o = U.E(null, overLayer); U.Fill(o); o.pickingMode = PickingMode.Position; o.style.justifyContent = Justify.FlexEnd; U.Pad(o, 10, 10);
            o.style.backgroundColor = new Color(0, 0, 0, .25f);
            box = U.E(null, o); box.pickingMode = PickingMode.Position; U.Radius(box, 26); box.style.backgroundColor = U.Surface2; U.Border(box, 1, U.Line); U.Pad(box, 22, 20, 18 + SafeBottom, 20);
            overlays.Add(o); return o;
        }
        public void CloseOverlay(VisualElement o) { if (o == null) return; overlays.Remove(o); o.RemoveFromHierarchy(); }
        public void CloseAllOverlays() { foreach (var o in overlays.ToArray()) o.RemoveFromHierarchy(); overlays.Clear(); }
        bool CloseTopOverlay() { if (overlays.Count == 0) return false; var o = overlays[overlays.Count - 1]; if (o.userData as string == "locked") return true; CloseOverlay(o); return true; }
        public float SafeBottom;

        public void Toast(string s) { toast.text = s; toast.style.display = DisplayStyle.Flex; toast.BringToFront(); toastUntil = Time.realtimeSinceStartupAsDouble + 1.9; }

        public void Confirm(string title, string text, string ok, Action onOk)
        {
            VisualElement box; var o = Overlay(out box);
            U.L(title, 20, U.Ink, U.Bold, box).style.marginBottom = 8;
            var t = U.L(text, 14, U.Muted, U.Body, box, TextAnchor.MiddleCenter, true); t.style.marginBottom = 18;
            var row = U.Row(box); row.style.justifyContent = Justify.Center;
            var c = U.Btn(Content.T("cancel"), U.BK.Surface, () => CloseOverlay(o), row, 48); c.style.flexGrow = 1;
            var b = U.Btn(ok, U.BK.Red, () => { CloseOverlay(o); onOk(); }, row, 48); b.style.flexGrow = 1; U.Gap(row, 10);
        }
        void MockAd(string kind, Action onDone, string msg, string got, string no)
        {
            VisualElement box; var o = Overlay(out box, false); o.userData = "locked"; o.style.backgroundColor = new Color(0, 0, 0, .85f);
            var tag = U.L(Content.T("adLabel"), 11, U.Muted, U.Semi, box); tag.style.marginBottom = 10;
            var sp = U.Card(box, 18); sp.style.height = 220; sp.style.justifyContent = Justify.Center; sp.style.alignItems = Align.Center; U.Pad(sp, 16, 16);
            U.L(Content.T("adHead"), 22, U.Gold, U.Display, sp).style.marginBottom = 8;
            U.L(Content.T("adBody").Replace("\\n", "\n"), 13, U.Muted, U.Body, sp, TextAnchor.MiddleCenter, true);
            var bar = U.E(null, box); bar.style.height = 4; U.Radius(bar, 2); bar.style.backgroundColor = U.Line; bar.style.marginTop = 14; bar.style.marginBottom = 12;
            var fill = U.E(null, bar); fill.style.height = 4; U.Radius(fill, 2); fill.style.backgroundColor = U.Gold; fill.style.width = Length.Percent(0);
            var m = U.L(kind == "rewarded" ? (msg ?? Content.T("adRewarded")) : "", 13, U.Ink, U.Body, box, TextAnchor.MiddleCenter, true); m.style.marginBottom = 12;
            VisualElement main = null; VisualElement alt = null; float dur = kind == "rewarded" ? 5 : 4, skipAt = kind == "rewarded" ? dur : 2; bool finished = false;
            main = U.Btn(kind == "rewarded" ? Content.T("adContinue") : Content.F("adWait", Mathf.CeilToInt(skipAt)), U.BK.Primary, () =>
            {
                if (!finished && kind == "rewarded") return; CloseOverlay(o); onDone?.Invoke();
            }, box, 48);
            if (kind == "rewarded") { U.Show(main, false); alt = U.Btn(no ?? Content.T("adNo"), U.BK.Ghost, () => CloseOverlay(o), box, 44); }
            else U.Enable(main, false);
            double t0 = Time.realtimeSinceStartupAsDouble;
            Every(0.1f, () =>
            {
                if (o.parent == null) return false;
                float el = (float)(Time.realtimeSinceStartupAsDouble - t0); fill.style.width = Length.Percent(Mathf.Min(100, el / dur * 100));
                if (kind != "rewarded") { int left = Mathf.CeilToInt(skipAt - el); if (left > 0) U.SetBtnText(main, Content.F("adWait", left)); else { U.Enable(main, true); U.SetBtnText(main, Content.T("adSkip")); } }
                if (el >= dur && !finished)
                {
                    finished = true;
                    if (kind == "rewarded") { Save.D["lastReward"] = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); Save.Persist(); Sfx.Reward(); Haptics.Do("win"); m.text = got ?? Content.T("adGot"); if (alt != null) U.Show(alt, false); U.Show(main, true); }
                    return false;
                }
                return true;
            });
        }

        // ---------- timers & tweens ----------
        class Timer { public double at; public Action a; public Func<bool> every; public float period; }
        readonly List<Timer> timers = new List<Timer>();
        class Tw { public double t0; public float dur; public Action<float> f; public Action done; }
        readonly List<Tw> tweens = new List<Tw>();
        public void Delay(float sec, Action a) { timers.Add(new Timer { at = Time.realtimeSinceStartupAsDouble + sec, a = a }); }
        public void Every(float sec, Func<bool> f) { timers.Add(new Timer { at = Time.realtimeSinceStartupAsDouble + sec, every = f, period = sec }); }
        public void Anim(float dur, Action<float> f, Action done = null) { tweens.Add(new Tw { t0 = Time.realtimeSinceStartupAsDouble, dur = Mathf.Max(.001f, dur), f = f, done = done }); f(0); }
        public static float EaseOut(float t) { return 1 - Mathf.Pow(1 - t, 3); }
        public static float EaseInOut(float t) { return t < .5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2; }

        /// <summary>Coins fly from one element to another (gold reward feedback).</summary>
        public void FlyCoins(VisualElement from, VisualElement to, int n, Action<int> onEach = null, Action onDone = null)
        {
            if (from == null || to == null || Save.Comfort) { for (int i = 0; i < n; i++) onEach?.Invoke(i); onDone?.Invoke(); return; }
            var fr = from.worldBound; var tr = to.worldBound; var rb = flyLayer.worldBound;
            Vector2 f = fr.center - rb.position, t = tr.center - rb.position; int done = 0;
            for (int i = 0; i < n; i++)
            {
                int ii = i; var c = U.Coin(16, flyLayer); U.Abs(c, f.x, f.y); c.style.opacity = 0;
                Vector2 mid = new Vector2((f.x + t.x) / 2 + UnityEngine.Random.Range(-45f, 45f), Mathf.Min(f.y, t.y) - 40 - UnityEngine.Random.value * 70);
                Delay(i * .07f, () => Anim(.65f, k =>
                {
                    float e = EaseInOut(k); Vector2 a = Vector2.Lerp(f, mid, e), b = Vector2.Lerp(mid, t, e), p = Vector2.Lerp(a, b, e);
                    c.style.left = p.x - 7; c.style.top = p.y - 8; c.style.opacity = Mathf.Min(1, k * 4); float s = Mathf.Lerp(.6f, .75f, k) + (k < .45f ? k : .9f - k) * .9f; c.style.scale = new Scale(new Vector3(s, s, 1));
                }, () => { c.RemoveFromHierarchy(); onEach?.Invoke(ii); Sfx.Coin(); Haptics.Do("tick"); if (++done == n) onDone?.Invoke(); }));
            }
        }

        void Update()
        {
            if (!built) return;
            double now = Time.realtimeSinceStartupAsDouble; float dt = Mathf.Min(50f, Time.unscaledDeltaTime * 1000f);
            for (int i = timers.Count - 1; i >= 0; i--)
            {
                if (i >= timers.Count) continue; var t = timers[i]; if (now < t.at) continue;
                if (t.every != null) { bool keep; try { keep = t.every(); } catch (Exception e) { Debug.LogException(e); keep = false; } if (keep) { t.at = now + t.period; continue; } }
                timers.Remove(t); if (t.a != null) { try { t.a(); } catch (Exception e) { Debug.LogException(e); } }
            }
            for (int i = tweens.Count - 1; i >= 0; i--)
            {
                if (i >= tweens.Count) continue; var w = tweens[i]; float k = Mathf.Clamp01((float)((now - w.t0) / w.dur));
                try { w.f(k); } catch (Exception e) { Debug.LogException(e); }
                if (k >= 1) { tweens.Remove(w); try { w.done?.Invoke(); } catch (Exception e) { Debug.LogException(e); } }
            }
            if (toast.style.display == DisplayStyle.Flex && now > toastUntil) toast.style.display = DisplayStyle.None;
            if (screens.TryGetValue(cur, out var s)) { try { s.Tick(Util.NowMs, dt); } catch (Exception e) { Debug.LogException(e); } }
            ApplySafeArea();
            if (legacyInputOk) { try { if (Input.GetKeyDown(KeyCode.Escape)) GoBack(); } catch { legacyInputOk = false; } }
        }
        bool legacyInputOk = true; bool backHandled;
        void ApplySafeArea()
        {
            if (root == null || root.panel == null) return;
            var sa = Screen.safeArea; var pr = root.layout; if (pr.width < 10) return;
            if (sa == lastSafe && pr.size == lastPanel) return; lastSafe = sa; lastPanel = pr.size;
            float k = pr.height / Screen.height; float top = (Screen.height - sa.yMax) * k, bottom = sa.yMin * k;
            screenLayer.style.top = top; screenLayer.style.bottom = bottom; SafeBottom = bottom;
        }

        // ---------- challenge links ----------
        void OnDeepLink(string url) { var c = Challenge.Parse(url); if (c != null) Delay(.5f, () => menu.ShowChallengeIntro(c)); }
        void OnApplicationPause(bool paused) { if (!paused) Delay(.6f, CheckClipboard); }
        string lastClip;
        void CheckClipboard()
        {
            string clip = null; try { clip = GUIUtility.systemCopyBuffer; } catch { }
            if (string.IsNullOrEmpty(clip) || clip == lastClip || clip.IndexOf("#c=", StringComparison.Ordinal) < 0) return;
            lastClip = clip; var c = Challenge.Parse(clip); if (c == null) return;
            string key = "clip" + c.n + c.t + c.mv; var seen = Save.Arr("chSeen"); if (seen.Contains(key)) return; seen.Add(key); Save.Persist();
            if (cur == "game" && game.G != null && game.G.phase == "play") return;
            menu.ShowChallengeIntro(c);
        }
    }
}
