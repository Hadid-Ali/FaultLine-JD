using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    public class GameScreen : Scr
    {
        public Game G; BoardView board; VisualElement hud, wrap, raceBar, raceFill, muteBtn, hintBtn, peekBtn, undoBtn, restartBtn, showLayer, goldPillEl;
        Label title, picName, lesson, sCuts, sTarget, sRest, gGold, hBadge, pBadge, rcMe, rcThem, shGrade, shName, shFact, shTap;
        int lastGrade;
        public override void Build()
        {
            board = new BoardView(); root.Add(board); board.PlayRect = () => wrap != null ? new Rect(wrap.layout.x, wrap.layout.y + hud.layout.y, wrap.layout.width, wrap.layout.height) : board.contentRect;
            board.onShowcaseBegin = OnShowcaseBegin; board.onShowcaseEnd = ShowResult;
            hud = U.Col(root); U.Fill(hud); hud.pickingMode = PickingMode.Ignore;
            // top bar
            var bar = U.Row(hud); U.Pad(bar, 10, 14, 4, 14); bar.style.justifyContent = Justify.SpaceBetween;
            var left = U.Row(bar); U.IconBtn("menu", () => { Leave(); App.I.Show("menu"); }, left, 44, 22);
            var mid = U.Col(bar); mid.style.flexGrow = 1; mid.style.alignItems = Align.Center;
            title = U.L("", 17, U.Ink, U.Semi, mid); picName = U.L("", 12.5f, U.Muted, U.Body, mid);
            var rt = U.Row(bar); muteBtn = U.IconBtn(Save.Sound ? "soundOn" : "soundOff", ToggleMute, rt, 44, 22); U.IconBtn("help", Help, rt, 44, 22); U.Gap(rt, 8);
            // stats
            var stats = U.Row(hud); stats.style.justifyContent = Justify.SpaceAround; U.Pad(stats, 6, 16);
            goldPillEl = U.GoldPill(out gGold, stats);
            var c1 = U.Row(stats); U.L(Content.T("cuts") + " ", 13, U.Muted, U.Body, c1); sCuts = U.L("0", 14, U.Ink, U.Bold, c1); U.L(" / " + Content.T("target") + " ", 13, U.Muted, U.Body, c1); sTarget = U.L("0", 14, U.Ink, U.Bold, c1);
            var c2 = U.Row(stats); U.L(Content.T("restored") + " ", 13, U.Muted, U.Body, c2); sRest = U.L("", 14, U.Ink, U.Bold, c2);
            lesson = U.L("", 15, U.Ink, U.Body, hud, TextAnchor.UpperCenter, true); U.Pad(lesson, 6, 28, 0, 28); lesson.style.minHeight = 40; lesson.style.opacity = .88f;
            raceBar = U.Row(hud); U.Pad(raceBar, 2, 22); rcMe = U.L("", 13, U.Ink, U.Semi, raceBar);
            var rb = U.E(null, raceBar); rb.style.flexGrow = 1; rb.style.height = 8; U.Radius(rb, 4); rb.style.backgroundColor = U.Line; rb.style.marginLeft = rb.style.marginRight = 10; rb.style.overflow = Overflow.Hidden;
            raceFill = U.E(null, rb); raceFill.style.height = 8; U.Radius(raceFill, 4); raceFill.style.backgroundImage = new StyleBackground(Content.Tex("tex/g_race")); if (Save.Ar) { raceFill.style.position = Position.Absolute; raceFill.style.right = 0; }
            rcThem = U.L("", 13, U.Muted, U.Semi, raceBar);
            wrap = U.E(null, hud); wrap.style.flexGrow = 1;
            // controls
            var ctl = U.Row(hud); ctl.style.justifyContent = Justify.SpaceAround; U.Pad(ctl, 6, 18, 14, 18);
            hintBtn = Ctl(ctl, "hint", Content.T("hint"), OnHint, out hBadge, true);
            peekBtn = Ctl(ctl, "peek", Content.T("peek"), null, out pBadge, false);
            restartBtn = Ctl(ctl, "restart", Content.T("restart"), () => { if (G == null || G.drag != null || G.snap != null) return; App.I.CloseAllOverlays(); G.Restart(); Hud(); }, out _, false);
            undoBtn = Ctl(ctl, "undo", Content.T("undo"), () => G?.Undo(), out _, false);
            var ring = peekBtn.Q("ring");
            peekBtn.RegisterCallback<PointerDownEvent>(e => PeekDown(e.pointerId));
            peekBtn.RegisterCallback<PointerUpEvent>(e => { if (G != null) G.peek = false; });
            peekBtn.RegisterCallback<PointerLeaveEvent>(e => { if (G != null) G.peek = false; });
            peekBtn.RegisterCallback<PointerCancelEvent>(e => { if (G != null) G.peek = false; });
            // showcase captions
            showLayer = U.E(null, root); U.Fill(showLayer); showLayer.style.display = DisplayStyle.None;
            shGrade = U.L("", 30, U.Hex("#F2D68F"), U.Display, showLayer); U.Abs(shGrade, 0, 30, 0);
            shName = U.L("", 15, U.Hex("#F2E9D8", .92f), U.Semi, showLayer); U.Abs(shName, 0, 74, 0);
            shFact = U.L("", 13.5f, U.Hex("#D8D0C2", .85f), U.Body, showLayer, TextAnchor.LowerCenter, true); U.Abs(shFact, 28, null, 28, 60);
            shTap = U.L(Content.T("tapContinue"), 14, U.Hex("#E8DCC6"), U.Semi, showLayer); U.Abs(shTap, 0, null, 0, 22);
        }
        VisualElement Ctl(VisualElement p, string icon, string label, Action a, out Label badge, bool gold)
        {
            var c = U.Col(p); c.style.alignItems = Align.Center; c.style.width = 62; c.pickingMode = PickingMode.Position;
            var ring = U.E("ring", c); U.Size(ring, 52, 52); U.Radius(ring, 26); ring.style.backgroundColor = U.Surface; U.Border(ring, 1, gold ? U.Hex("#D9B568", .6f) : U.Line); ring.style.alignItems = Align.Center; ring.style.justifyContent = Justify.Center;
            U.Icon(icon, 24, gold ? U.Gold : U.Ink, ring); U.L(label, 11.5f, U.Muted, U.Body, c).style.marginTop = 6;
            var b = U.Badge("", c); U.Abs(b, null, -4, 0); badge = b.Q<Label>("label"); b.name = "badge";
            if (a != null) U.Tap(c, a, false); return c;
        }
        // ---------- start ----------
        public void StartLevel(int i) { Begin(Game.ForLevel(i)); }
        public void StartExpert(int i) { Begin(Game.ForExpert(i)); }
        public void StartDaily() { Begin(Game.ForDaily()); }
        public void StartChallenge(ChalData o) { Begin(Game.ForChallenge(o)); }
        public void Resume(Game g) { Begin(g, true); }
        void Begin(Game g, bool resume = false)
        {
            App.I.CloseAllOverlays(); G = g; G.onHud = Hud; G.onReveal = () => { }; G.onFail = ExpertFail;
            board.SetGame(g); showLayer.style.display = DisplayStyle.None; hud.style.opacity = 1;
            U.Show(raceBar, g.kind == Kind.Challenge);
            foreach (var b in new[] { hintBtn, peekBtn, undoBtn }) { b.style.opacity = g.expert ? .28f : 1; b.pickingMode = g.expert ? PickingMode.Ignore : PickingMode.Position; }
            title.style.color = g.expert ? U.Hex("#CDB6FF") : U.Ink;
            App.I.Show("game"); Hud(); Lesson();
        }
        public void Leave() { if (G != null) { board.StopLiving(); } App.I.CloseAllOverlays(); }
        bool LessonFree => G.kind == Kind.Level && G.idx < Content.KI("LESSON_FREE") && !G.expert;
        void Lesson()
        {
            picName.text = Content.PicName(G.Pic);
            title.text = G.kind == Kind.Challenge ? Content.F("chalFrom", G.ch.n) : G.kind == Kind.Daily ? Content.F("dailyT", G.num) : G.expert ? Content.F("xTitle", G.idx + 1) : Content.F("lvl", G.idx + 1);
            lesson.text = G.kind == Kind.Challenge ? Content.T("chalTouch") : G.kind == Kind.Daily ? (Save.D.O("daily").Has(G.date) ? Content.T("dailyDoneL") : Content.F("dailyGold", Meta.GoldDailyBy(2))) : G.expert ? Content.T("xRule") : Content.Lesson(G.idx);
        }
        void Hud()
        {
            if (G == null) return;
            gGold.text = (G.dispGold >= 0 ? G.dispGold : Save.Gold).ToString(); sCuts.text = G.history.Count.ToString(); sTarget.text = G.Target.ToString(); sRest.text = G.CorrectCount() + "/" + G.B.N;
            var pb = peekBtn.Q("badge"); var hb = hintBtn.Q("badge");
            if (LessonFree) { U.Show(pb, false); U.Show(hb, false); }
            else
            {
                int n = G.peeksLeft >= 0 ? G.peeksLeft : Content.KI("PEEKS"); U.Show(pb, true); pBadge.text = n > 0 ? n.ToString() : Content.T("adBadge");
                int h = Save.HintsLeft(Content.KI("FREE_HINTS")); U.Show(hb, true); hBadge.text = h > 0 ? h.ToString() : Content.T("adBadge");
            }
        }
        void ToggleMute() { Save.D["sound"] = !Save.Sound; Save.Persist(); U.SetIcon(muteBtn, Save.Sound ? "soundOn" : "soundOff"); App.I.Toast(Save.Sound ? Content.T("soundOn") : Content.T("soundOff")); }
        void Help()
        {
            VisualElement box; App.I.Sheet(out box);
            U.L(Content.T("helpTitle"), 20, U.Ink, U.Bold, box).style.marginBottom = 10;
            for (int i = 1; i <= 7; i++) { var l = U.L(Content.T("help" + i), 14, U.Muted, U.Body, box, Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, true); l.style.marginBottom = 8; }
            var o = box.parent; U.Btn(Content.T("gotIt"), U.BK.Primary, () => App.I.CloseOverlay(o), box, 50).style.marginTop = 8;
        }
        void PeekDown(int pid)
        {
            if (G == null || G.expert || G.phase != "play" || G.drag != null) return;
            if (!LessonFree)
            {
                int n = G.peeksLeft >= 0 ? G.peeksLeft : Content.KI("PEEKS");
                if (n <= 0) { var g0 = G; Ads.Rewarded(() => { if (G == g0) { G.peeksLeft = 3; Hud(); } }, Content.T("adPeek"), Content.T("adPeekGot"), Content.T("adNoReward")); return; }
                G.peeksLeft = n - 1; Hud();
            }
            G.peek = true; Sfx.Touch();
        }
        void OnHint()
        {
            if (G == null || G.expert || G.phase != "play" || G.drag != null || G.snap != null) return;
            if (G.hint.HasValue) { board.PlayHint(); return; }
            if (LessonFree) { ShowHint(); return; }
            if (Save.HintsLeft(Content.KI("FREE_HINTS")) > 0) { Save.D["hintsUsed"] = (double)(Save.I("hintsUsed") + 1); Save.Persist(); ShowHint(); Hud(); return; }
            var g0 = G; Ads.Rewarded(() => { if (G == g0) ShowHint(); });
        }
        void ShowHint() { if (G.tFirst <= 0) G.tFirst = Util.NowMs; var m = G.ComputeHint(); if (!m.HasValue) return; G.hint = m; Sfx.Hint(); Haptics.Do("hint"); App.I.Toast(Content.T("tapAgain")); }

        public override void Tick(double now, float dt)
        {
            if (G == null) return; board.Tick(now, dt);
            if (G.kind == Kind.Challenge)
            {
                double el = G.phase == "play" ? (G.tFirst > 0 ? now - G.tFirst : 0) : G.solveMs; float k = Mathf.Min(1, (float)(el / G.ch.t));
                raceFill.style.width = Length.Percent(k * 100); bool over = el > G.ch.t;
                raceFill.style.backgroundImage = new StyleBackground(Content.Tex(over ? "tex/g_red" : "tex/g_race")); rcMe.style.color = over ? U.Hex("#F0705F") : U.Ink;
                rcMe.text = Content.T("me") + " " + Util.FmtT(el); rcThem.text = G.ch.n + " " + Util.FmtT(G.ch.t);
            }
            if (G.phase == "show")
            {
                float el2 = (float)(now - G.showT0), ca = Mathf.Clamp01((el2 - 450) / 600); shGrade.style.opacity = shName.style.opacity = shFact.style.opacity = ca;
                float a = Mathf.Clamp01((el2 - 1700) / 500); shTap.style.opacity = a * (.75f + .25f * Mathf.Sin((float)now / 400));
            }
        }
        void OnShowcaseBegin()
        {
            lastGrade = G.Grade(); hud.style.opacity = 0; showLayer.style.display = DisplayStyle.Flex; showLayer.BringToFront();
            shGrade.text = Content.Grade(lastGrade); shName.text = Content.PicName(G.Pic); var f = Content.FactOf(G.Pic); shFact.text = f != null ? f.s : "";
            shGrade.style.opacity = shName.style.opacity = shFact.style.opacity = shTap.style.opacity = 0;
        }
        void ExpertFail()
        {
            VisualElement box; var o = App.I.Sheet(out box); o.userData = "locked";
            U.L(Content.T("xFailT"), 26, U.Gold, U.Display, box).style.marginBottom = 6;
            U.L(Content.T("xFailB"), 14, U.Muted, U.Body, box, TextAnchor.MiddleCenter, true).style.marginBottom = 16;
            var r = U.Row(box);
            var ex = U.Btn(Content.T("exit"), U.BK.Surface, () => { App.I.CloseOverlay(o); Leave(); App.I.levels.Open("xp"); }, r, 50); ex.style.flexGrow = 1;
            var re = U.Btn(Content.T("retry"), U.BK.Primary, () => { App.I.CloseOverlay(o); G.Restart(); }, r, 50); re.style.flexGrow = 1; U.Gap(r, 10);
        }

        // ---------- results ----------
        Label rGold; VisualElement rGoldPill, rDouble, rExtra;
        void ShowResult()
        {
            showLayer.style.display = DisplayStyle.None; hud.style.opacity = 1;
            int gr = G.Grade(); lastGrade = gr; int prevGold = Save.Gold; int earned = AwardGold(gr);
            VisualElement box; var o = App.I.Sheet(out box); o.style.backgroundColor = new Color(0, 0, 0, 0); o.userData = "locked";
            U.L(Content.Grade(gr), 34, U.Gold, U.Display, box);
            var sub = U.Row(box); sub.style.justifyContent = Justify.Center; sub.style.marginTop = 2;
            U.L(Content.T("cuts") + " " + G.history.Count + "   ·   " + Content.T("target") + " " + G.Target, 13, U.Muted, U.Body, sub);
            string note = G.hintedAttempt ? Content.T("silver") : (G.history.Count < G.Target ? Content.T("beat") : (G.replayHalf ? Content.T("replayHalf") : ""));
            if (note != "") U.L(note, 13, U.Muted, U.Body, box, TextAnchor.MiddleCenter, true).style.marginTop = 4;
            // persist outcome
            if (G.kind == Kind.Level && G.expert) { Save.Obj("crown")[G.idx.ToString()] = 1.0; }
            else if (G.kind == Kind.Level)
            {
                var b = Save.BestOf(G.idx);
                if (b == null || gr > b.I("grade") || (gr == b.I("grade") && G.history.Count < b.I("cuts"))) { var nb = new JObj(); nb["grade"] = (double)gr; nb["cuts"] = (double)G.history.Count; Save.Best[G.idx.ToString()] = nb; }
                if (G.idx < Save.Unlocked) Save.Unlocked = Mathf.Max(Save.Unlocked, Mathf.Min(Content.Levels.Count, G.idx + 2));
            }
            else if (G.kind == Kind.Daily) { var nb = new JObj(); nb["grade"] = (double)gr; nb["cuts"] = (double)G.history.Count; Save.D.O("daily")[G.date] = nb; }
            Save.Persist();
            rExtra = U.E(null, box); rExtra.style.flexDirection = FlexDirection.Row; rExtra.style.flexWrap = Wrap.Wrap; rExtra.style.justifyContent = Justify.Center; rExtra.style.marginTop = 10;
            int bonus = DepthAfterSolve();
            // gold row
            var gr2 = U.Row(box); gr2.style.justifyContent = Justify.Center; gr2.style.marginTop = 12; gr2.style.marginBottom = 14;
            rGoldPill = U.GoldPill(out rGold, gr2, true); rGold.text = "+0";
            rDouble = U.Btn(Content.T("doubleGold"), U.BK.Surface, DoubleGold, gr2, 42); U.Gap(gr2, 10);
            SetDouble();
            App.I.Delay(.35f, () => { if (earned > 0) { MenuScreen.CountUp(rGold, 0, earned, "+"); Sfx.Coins(Mathf.Min(6, 2 + earned / 10)); App.I.Delay(.75f, () => FlyGameGold(prevGold, earned)); } });
            if (bonus > 0) { int from = Save.Gold - bonus; App.I.Delay(1.7f, () => { FlyGameGold(from, bonus); Sfx.Coins(5); Haptics.Do("win"); }); }
            // buttons
            var row = U.Row(box);
            var rp = U.Btn(Content.T("replay"), U.BK.Surface, () => { App.I.CloseOverlay(o); G.Restart(); }, row, 50); rp.style.flexGrow = 1; rp.style.flexBasis = 0;
            if (Content.FactOf(G.Pic) != null) { var st = U.Btn(Content.T("aboutPic"), U.BK.Surface, Story, row, 50); st.style.flexGrow = 1; st.style.flexBasis = 0; }
            var ch = U.Btn(G.kind == Kind.Challenge ? Content.T("chalReplyBtn") : Content.T("chal"), U.BK.Wa, SendChallenge, row, 50, "whatsapp"); ch.style.flexGrow = 1.3f; ch.style.flexBasis = 0;
            U.Gap(row, 8);
            bool next = G.kind == Kind.Level && !G.expert && G.idx < Content.Levels.Count - 1 && Meta.LvOpen(G.idx + 1);
            if (next) { var nx = U.Btn(Content.T("next"), U.BK.Primary, () => { App.I.CloseOverlay(o); Next(); }, box, 54); nx.style.marginTop = 10; }
            else { var mn = U.Btn(Content.T("close"), U.BK.Ghost, () => { App.I.CloseOverlay(o); Leave(); App.I.Show("menu"); }, box, 46); mn.style.marginTop = 6; }
        }
        int AwardGold(int gr)
        {
            int earned = 0;
            if (G.kind == Kind.Daily) earned = Save.D.O("daily").Has(G.date) ? 0 : Meta.GoldDailyBy(gr);
            else if (G.kind == Kind.Challenge) { earned = Save.DayLeft("chal", Challenge.CH_PER_DAY) > 0 ? (G.ChalWon() ? Challenge.CH_GOLD_WIN : Challenge.CH_GOLD_PLAY) : 0; if (earned > 0) Save.DayUse("chal"); }
            else if (G.expert) { bool first = !Meta.Crowned(G.idx); earned = first ? Meta.EXPERT_GOLD : 15; G.xFirst = first; }
            else { bool first = Save.BestOf(G.idx) == null; earned = first ? Meta.GoldByGrade(gr) : Mathf.RoundToInt(Meta.GoldByGrade(gr) / 2f); G.replayHalf = !first; G.evX2 = Meta.IsEventPic(G.Pic); if (G.evX2) earned *= 2; }
            int prev = Save.Gold; G.goldEarned = earned; G.doubled = false; Save.Gold = prev + earned; G.dispGold = prev; Save.Persist(); Hud();
            return earned;
        }
        void Chip(string text, string kind)
        {
            var c = U.E(null, rExtra); U.Radius(c, 999); U.Pad(c, 5, 11); c.style.backgroundColor = U.Surface; c.style.marginLeft = c.style.marginRight = 3; c.style.marginBottom = 6;
            Color bc = U.Line, tc = U.Ink;
            if (kind == "hot") { bc = U.Hex("#FF9650", .6f); tc = U.Hex("#FFB27A"); } else if (kind == "ev") { bc = U.Hex("#E07AA8", .65f); tc = U.Hex("#F2A7C8"); } else if (kind == "alb") { bc = U.Hex("#D9B568", .75f); tc = U.Gold; }
            U.Border(c, 1, bc); U.L(text, 12.5f, tc, U.Semi, c);
            c.style.scale = new Scale(new Vector3(.6f, .6f, 1)); c.style.opacity = 0; int k = rExtra.childCount;
            App.I.Delay(.9f + k * .25f, () => App.I.Anim(.45f, t => { float e = 1 + 2.4f * Mathf.Pow(t - 1, 3) + 1.4f * Mathf.Pow(t - 1, 2); c.style.opacity = t; float s = Mathf.Lerp(.6f, 1, e); c.style.scale = new Scale(new Vector3(s, s, 1)); }));
        }
        int DepthAfterSolve()
        {
            int bonus = 0; int sg = Meta.BumpStreak(); if (sg > 0) { bonus += sg; Chip(Content.F("streakGot", Save.D.O("streak").I("n"), sg), "hot"); }
            if (G.kind == Kind.Challenge)
            {
                var o = G.ch; bool win = G.ChalWon(); string key = "p" + o.n + o.t + o.mv;
                if (!Challenge.Seen(key)) { var h = Challenge.H2H(o.n); if (win) h["w"] = h.D("w") + 1; else h["l"] = h.D("l") + 1; }
                var hh = Challenge.H2H(o.n);
                Chip(win ? Content.F("chalWin", Util.FmtT(G.solveMs), Util.FmtT(o.t)) : Content.F("chalLose", Util.FmtT(G.solveMs), Util.FmtT(o.t)), win ? "alb" : "hot");
                Chip(Content.F("chalRec", hh.I("w"), hh.I("l"), o.n), "ev");
            }
            if (G.kind == Kind.Level)
            {
                if (G.evX2) Chip(Content.T("evGot"), "ev");
                if (G.expert && G.xFirst) Chip(Content.F("xCrown", Meta.EXPERT_GOLD), "alb");
                var b = Save.BestOf(G.idx);
                if (!G.expert && G.idx >= 9 && b != null && b.I("grade") == 2 && !Meta.Crowned(G.idx))
                {
                    int ix = G.idx; var x = U.Btn(Content.T("xTry"), U.BK.Violet, () => { App.I.CloseAllOverlays(); StartExpert(ix); }, rExtra, 34); x.style.marginBottom = 6;
                }
                var A = Meta.AlbOfPic(G.Pic);
                if (A != null)
                {
                    var st = Meta.Stats(A); var s = Meta.AlbSave(A.id);
                    if (st.solved == st.n && !s.B("c")) { s["c"] = 1.0; Save.Gold += Meta.ALB_DONE; bonus += Meta.ALB_DONE; Chip(Content.F("albDone", Meta.ALB_DONE) + " · " + A.Name, "alb"); }
                    if (st.master == st.n && !s.B("m")) { s["m"] = 1.0; Save.Gold += Meta.ALB_MASTER; bonus += Meta.ALB_MASTER; Chip(Content.F("albMaster", Meta.ALB_MASTER), "alb"); }
                }
            }
            Save.Persist(); return bonus;
        }
        void FlyGameGold(int from, int amount)
        {
            if (G == null || amount <= 0) return; int n = Mathf.Min(12, 4 + Mathf.RoundToInt(amount / 6f));
            App.I.FlyCoins(rGoldPill, goldPillEl, n, i => { G.dispGold = Mathf.RoundToInt(from + amount * (i + 1f) / n); gGold.text = G.dispGold.ToString(); }, () => { G.dispGold = -1; gGold.text = Save.Gold.ToString(); });
        }
        void SetDouble()
        {
            if (G.goldEarned <= 0) { U.Show(rDouble, false); return; }
            if (G.doubled) { U.Enable(rDouble, false); U.SetBtnText(rDouble, Content.T("doubled")); return; }
            if (Save.DayLeft("gad", Content.KI("DOUBLE_PER_DAY")) <= 0) { U.Show(rDouble, false); return; }
            U.Show(rDouble, true); U.Enable(rDouble, true); U.SetBtnText(rDouble, Content.T("doubleGold"));
        }
        void DoubleGold()
        {
            if (G == null || G.doubled || G.goldEarned <= 0 || Save.DayLeft("gad", Content.KI("DOUBLE_PER_DAY")) <= 0) return;
            var g0 = G;
            Ads.Rewarded(() =>
            {
                if (G != g0) return; Save.DayUse("gad"); int pg = Save.Gold; Save.Gold = pg + G.goldEarned; G.doubled = true; Save.Persist();
                App.I.Delay(.7f, () => FlyGameGold(pg, G.goldEarned)); MenuScreen.CountUp(rGold, G.goldEarned, G.goldEarned * 2, "+"); Sfx.Coins(6); Haptics.Do("win"); SetDouble();
            }, Content.T("adGold"), Content.T("adGoldGot"), Content.T("adNoReward"));
        }
        void Next()
        {
            int nx = G.idx + 1; Save.D["sinceAd"] = (double)(Save.I("sinceAd") + 1); Save.Persist();
            double lr = Save.D.D("lastReward"); bool recent = lr > 0 && DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - lr < 60000;
            if (!Save.B("adsRemoved") && G.idx >= 3 && Save.I("sinceAd") >= 2 && !recent) { Save.D["sinceAd"] = 0.0; Save.Persist(); Ads.Interstitial(() => StartLevel(nx)); }
            else StartLevel(nx);
        }
        void Story()
        {
            var f = Content.FactOf(G.Pic); if (f == null) return; VisualElement box; var o = App.I.Overlay(out box);
            U.L(f.t, 22, U.Gold, U.Display, box).style.marginBottom = 10;
            var sv = new ScrollView(); sv.style.maxHeight = 360; box.Add(sv);
            U.L(f.b, 15, U.Ink, U.Body, sv.contentContainer, Save.Ar ? TextAnchor.UpperRight : TextAnchor.UpperLeft, true);
            U.Btn(Content.T("close"), U.BK.Surface, () => App.I.CloseOverlay(o), box, 46).style.marginTop = 14;
        }
        // ---------- WhatsApp challenge ----------
        void SendChallenge()
        {
            NameSheet.Need(() =>
            {
                string pn = Content.PicName(G.Pic); double ms = G.solveMs; int c = G.history.Count;
                var pay = Challenge.Spec(G); pay["t"] = Math.Round(ms); pay["c"] = (double)c; pay["n"] = Save.S("name");
                if (G.kind == Kind.Challenge)
                {
                    var o = G.ch; bool win = G.ChalWon(); var r = new JObj(); r["n"] = o.n; r["t"] = o.t; pay["r"] = r;
                    Challenge.Send(win ? Content.F("chalMsgWin", o.n, Util.FmtT(ms), Util.FmtT(o.t), pn) : Content.F("chalMsgLose", o.n, Util.FmtT(ms), Util.FmtT(o.t), pn), pay);
                }
                else Challenge.Send(Content.F("chalMsg", pn, Util.FmtT(ms), c), pay);
            });
        }
        public override bool Back()
        {
            if (G != null && G.phase == "show") { board.EndShowcase(); return true; }
            Leave(); App.I.Show("menu"); return true;
        }
        public override void OnHide() { if (G != null) G.peek = false; }
    }

    /// <summary>Asks once for the player's challenge name (native keyboard on phones).</summary>
    public static class NameSheet
    {
        public static void Need(Action cb)
        {
            if (!string.IsNullOrEmpty(Save.S("name"))) { cb(); return; }
            VisualElement box; var o = App.I.Sheet(out box);
            U.L(Content.T("chalName"), 22, U.Gold, U.Display, box);
            U.L(Content.T("chalNameHint"), 13, U.Muted, U.Body, box).style.marginBottom = 10;
            var input = U.E(null, box); input.pickingMode = PickingMode.Position; input.focusable = true; input.style.height = 52; U.Radius(input, 16); U.Border(input, 1, U.Hex("#3BD672", .7f)); input.style.backgroundColor = U.Surface; input.style.justifyContent = Justify.Center;
            var txt = U.L("", 18, U.Ink, U.Semi, input); string val = "";
            TouchScreenKeyboard kb = null;
            void Render() { txt.text = val.Length > 0 ? val : "…"; txt.style.color = val.Length > 0 ? U.Ink : U.Muted; }
            void OpenKb() { if (TouchScreenKeyboard.isSupported) kb = TouchScreenKeyboard.Open(val, TouchScreenKeyboardType.Default, false, false, false, false, "", 20); input.Focus(); }
            input.RegisterCallback<ClickEvent>(e => OpenKb());
            input.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Backspace && val.Length > 0) val = val.Substring(0, val.Length - 1);
                else if (e.character >= ' ' && val.Length < 20) val += e.character;
                Render();
            });
            Render(); App.I.Delay(.3f, OpenKb);
            App.I.Every(.1f, () => { if (o.parent == null) return false; if (kb != null) { val = kb.text ?? ""; if (val.Length > 20) val = val.Substring(0, 20); Render(); } return true; });
            var r = U.Row(box); r.style.marginTop = 14;
            var c = U.Btn(Content.T("cancel"), U.BK.Surface, () => { if (kb != null) kb.active = false; App.I.CloseOverlay(o); }, r, 50); c.style.flexGrow = 1;
            var go = U.Btn(Content.T("chalGo"), U.BK.Wa, () =>
            {
                var v = val.Trim(); if (v.Length == 0) { OpenKb(); return; }
                if (kb != null) kb.active = false; Save.D["name"] = v; Save.Persist(); App.I.CloseOverlay(o); cb();
            }, r, 50, "whatsapp"); go.style.flexGrow = 1.4f; U.Gap(r, 10);
        }
    }
}
