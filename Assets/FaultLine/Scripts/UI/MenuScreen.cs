using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FaultLine
{
    public class MenuScreen : Scr
    {
        Label gold, chestLbl, chestLeft, pcSub, homeSub, dailySub, levelsSub, skN, skSub, evKick, evName, evSub;
        VisualElement chest, pcThumbHolder, pcProg, dailyBadge, strip, streakCard, heroWrap, logo; HeroView hero; HexImage pcThumb, evThumb; Label dailyBadgeL;
        public override void Build()
        {
            var r = root; r.pickingMode = PickingMode.Ignore; r.style.alignItems = Align.Center; U.Pad(r, 8, 22, 14, 22);
            // motes
            var motes = U.E(null, r); U.Fill(motes);
            for (int i = 0; i < 14; i++) { var m = U.Img(Content.Tex("tex/dot"), 5, 5, motes, new Color(.94f, .8f, .5f, .8f)); U.Abs(m, UnityEngine.Random.value * 390, 900); m.userData = new Vector3(UnityEngine.Random.value * 380, UnityEngine.Random.Range(14f, 28f), UnityEngine.Random.value * 30); }
            moteLayer = motes;
            // gold bar
            var bar = U.Row(r); bar.style.width = Length.Percent(100); bar.style.justifyContent = Justify.SpaceBetween; bar.style.marginBottom = 4;
            U.GoldPill(out gold, bar);
            var right = U.Row(bar);
            var lang = U.E(null, right); lang.style.flexDirection = FlexDirection.Row; lang.style.alignItems = Align.Center; U.Radius(lang, 999); U.Pad(lang, 7, 12); U.Border(lang, 1, U.Hex("#D9B568", .5f));
            U.Icon("globe", 18, U.Gold, lang); var ll = U.L(Save.Ar ? "EN" : "ع", 14, U.Gold, U.Bold, lang); ll.style.marginLeft = 6;
            U.Tap(lang, () => { Save.D["lang"] = Save.Ar ? "en" : "ar"; Save.Persist(); App.I.Rebuild(); });
            chest = U.E(null, right); chest.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row; chest.style.alignItems = Align.Center; U.Radius(chest, 999); U.Pad(chest, 7, 12); U.Border(chest, 1, U.Hex("#D9B568", .5f));
            U.Coin(16, chest); chestLbl = U.L("", 14, U.Gold, U.Semi, chest); chestLbl.style.marginLeft = chestLbl.style.marginRight = 6; chestLeft = U.L("", 12, U.Muted, U.Body, chest);
            U.Tap(chest, OnChest);
            var set = U.IconBtn("gear", () => App.I.Show("settings"), right, 40, 20);
            U.Gap(right, 8);
            // hero
            heroWrap = U.E(null, r); heroWrap.style.marginTop = 10; heroWrap.style.width = 230; heroWrap.style.height = 213; heroWrap.style.flexShrink = 0;
            var halo = U.Img(Content.Tex("tex/glow"), 260, 260, heroWrap, new Color(217 / 255f, 181 / 255f, 104 / 255f, .22f)); U.Abs(halo, 0, 0, 0, 0); halo.style.width = StyleKeyword.Auto; halo.style.height = StyleKeyword.Auto; halo.style.scale = new Scale(new Vector3(1.15f, 1.2f, 1));
            hero = new HeroView(); U.Fill(hero); heroWrap.Add(hero);
            // logo
            var lt = Content.Tex("tex/logo"); logo = U.Img(lt, 220, 220f * lt.height / lt.width, r); logo.style.marginTop = 4;
            var tag = U.L(Content.T("tag"), 15, U.Muted, U.Body, r); tag.style.marginTop = 4;
            // buttons
            var btns = U.Col(r); btns.style.width = Length.Percent(100); btns.style.maxWidth = 350; btns.style.marginTop = 14;
            // play card
            var pc = U.E(null, btns); pc.style.height = 80; U.Radius(pc, 24); pc.style.backgroundImage = new StyleBackground(Content.Tex("tex/g_green"));
            pc.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row; pc.style.alignItems = Align.Center; U.Pad(pc, 10, 14, 16, 14); pc.style.overflow = Overflow.Hidden;
            pcThumbHolder = U.E(null, pc); pcThumb = new HexImage(null, 52, new Color(5 / 255f, 40 / 255f, 28 / 255f, .25f)); pcThumbHolder.Add(pcThumb);
            var tx = U.Col(pc); tx.style.flexGrow = 1; tx.style.marginLeft = tx.style.marginRight = 12;
            U.L(Content.T("play"), 20, U.Hex("#06281C"), U.Bold, tx, Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            pcSub = U.L("", 12.5f, U.Hex("#06281C", .78f), U.Body, tx, Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft);
            var go = U.E(null, pc); U.Size(go, 42, 42); U.Radius(go, 21); go.style.backgroundColor = U.Hex("#05281C", .9f); go.style.alignItems = Align.Center; go.style.justifyContent = Justify.Center;
            U.Icon("play", 18, U.Hex("#9BF0CA"), go);
            var prog = U.E(null, pc); U.Abs(prog, 16, null, 16, 7); prog.style.height = 3; U.Radius(prog, 2); prog.style.backgroundColor = U.Hex("#05281C", .18f);
            pcProg = U.E(null, prog); pcProg.style.height = 3; U.Radius(pcProg, 2); pcProg.style.backgroundColor = U.Hex("#05281C", .6f);
            if (Save.Ar) { pcProg.style.position = Position.Absolute; pcProg.style.right = 0; }
            U.Tap(pc, () => App.I.game.StartLevel(Mathf.Min(Save.Unlocked, Content.Levels.Count) - 1));
            // streak + event strip
            strip = U.Row(btns); strip.style.marginTop = 12;
            streakCard = SCard(strip, out skN, out skSub, null, out _); var fl = U.Icon("flame", 30, Color.white); streakCard.Insert(0, fl); flame = fl;
            streakCard.style.flexGrow = 1; streakCard.style.flexBasis = 0;
            var ev = SCard(strip, out evName, out evSub, Content.T("evTitle") + " · ×2", out evKick); ev.style.flexGrow = 1.3f; ev.style.flexBasis = 0;
            ev.style.backgroundColor = new Color(232 / 255f, 120 / 255f, 170 / 255f, .14f); U.Border(ev, 1, new Color(232 / 255f, 120 / 255f, 170 / 255f, .5f));
            evThumb = new HexImage(null, 36); ev.Insert(0, evThumb);
            U.Gap(strip, 10);
            U.Tap(streakCard, () => App.I.Toast(Content.T("streakInfo")));
            U.Tap(ev, () => App.I.levels.Open("alb", Meta.Now().A.id));
            // tiles
            var tiles = U.Row(btns); tiles.style.marginTop = 12;
            Tile(tiles, "home", Content.T("myHome"), out homeSub, () => App.I.home.Open());
            var dt = Tile(tiles, "daily", Content.T("daily"), out dailySub, () => App.I.game.StartDaily());
            dailyBadge = U.Badge("", dt); U.Abs(dailyBadge, null, -7, -3); dailyBadgeL = dailyBadge.Q<Label>("label");
            Tile(tiles, "levels", Content.T("levels"), out levelsSub, () => App.I.levels.Open("all"));
            U.Gap(tiles, 10);
            root.RegisterCallback<GeometryChangedEvent>(e => Fit(e.newRect.height));
        }
        float lastFitH = -1;
        void Fit(float h)
        {
            if (h < 100 || Mathf.Abs(h - lastFitH) < 2) return; lastFitH = h;
            float need = 760, k = Mathf.Clamp((h - (need - 342)) / 342f, .45f, 1f); // shrink hero+logo on short screens
            heroWrap.style.width = 230 * k; heroWrap.style.height = 213 * k; var lt = Content.Tex("tex/logo"); logo.style.width = 220 * Mathf.Max(.7f, k); logo.style.height = 220 * Mathf.Max(.7f, k) * lt.height / lt.width;
            heroWrap.style.marginTop = 10 * k;
        }
        VisualElement moteLayer, flame;
        VisualElement SCard(VisualElement parent, out Label main, out Label sub, string kick, out Label kickL)
        {
            var c = U.Card(parent, 18); c.style.flexDirection = Save.Ar ? FlexDirection.RowReverse : FlexDirection.Row; c.style.alignItems = Align.Center; U.Pad(c, 10, 12); c.style.minHeight = 64; c.style.overflow = Overflow.Hidden;
            var col = U.Col(c); col.style.flexGrow = 1; col.style.flexShrink = 1; col.style.marginLeft = col.style.marginRight = 9;
            var al = Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            kickL = null; if (kick != null) { kickL = U.L(kick, 10, U.Hex("#F2A7C8"), U.Bold, col, al); }
            main = U.L("", 14, U.Ink, U.Bold, col, al); main.style.overflow = Overflow.Hidden; main.style.textOverflow = TextOverflow.Ellipsis;
            sub = U.L("", 11, U.Muted, U.Body, col, al); sub.style.overflow = Overflow.Hidden; sub.style.textOverflow = TextOverflow.Ellipsis;
            return c;
        }
        VisualElement Tile(VisualElement parent, string icon, string title, out Label sub, Action onClick)
        {
            var t = U.Card(parent, 20); t.style.flexGrow = 1; t.style.flexBasis = 0; t.style.alignItems = Align.Center; U.Pad(t, 12, 6, 10, 6); t.style.minHeight = 98;
            U.Icon(icon, 26, U.Gold, t).style.marginBottom = 4;
            U.L(title, 13.5f, U.Ink, U.Semi, t);
            sub = U.L("", 11, U.Muted, U.Body, t, TextAnchor.MiddleCenter, true);
            U.Tap(t, onClick); return t;
        }
        void OnChest()
        {
            int lim = Content.KI("CHEST_PER_DAY"); if (Save.DayLeft("chest", lim) <= 0) { App.I.Toast(Content.T("chestTomorrow")); return; }
            AdsController.ShowRewarded(() =>
            {
                GameAnalyticsController.DesignEvent("chest:rewarded", Content.KI("GOLD_CHEST"));
                Save.DayUse("chest"); int from = Save.Gold; Save.Gold = from + Content.KI("GOLD_CHEST"); Save.Persist(); Refresh(); CountUp(gold, from, Save.Gold); Sfx.Coins(5); Haptics.Do("win");
            }, Content.T("adChest"), Content.T("adChestGot"), Content.T("adNoReward"));
        }
        public static void CountUp(Label l, int from, int to, string prefix = "")
        {
            App.I.Anim(.65f, k => { l.text = prefix + Mathf.RoundToInt(from + (to - from) * App.EaseOut(k)); });
        }
        public override void OnShow() { Refresh(); hero.Restart(); App.I.Delay(.3f, App.I.home.CheckOnboardBadge); AdsController.ShowBannerOn(root, BannerPosition.Bottom); }
        public override void OnHide() { AdsController.HideBannerOn(root); }
        public void Refresh()
        {
            gold.text = Save.Gold.ToString();
            int left = Save.DayLeft("chest", Content.KI("CHEST_PER_DAY")); U.Enable(chest, left > 0); chestLbl.text = Content.T("chest"); chestLeft.text = left > 0 ? Content.F("chestLeft", left) : Content.T("chestTomorrow");
            int i = Mathf.Min(Save.Unlocked, Content.Levels.Count) - 1; string pic = Content.Levels[i].pic; int done = Save.LevelsDone;
            pcThumb.SetTexture(Content.Pic(pic)); pcSub.text = Content.F("pcSub", i + 1, Content.PicName(pic));
            pcProg.style.width = Length.Percent(Mathf.Round(done * 100f / Content.Levels.Count));
            var R = Content.Rooms[0]; var st = App.I.home.RoomState(R.id);
            homeSub.text = st.B("restored") ? Content.T("tHomeDecor") : (done >= R.req ? Content.F("tHomeReady", R.cost) : Content.F("tHomeLock", done, R.req));
            bool dDone = Save.D.O("daily").Has(Util.KwDate()); int mx = Meta.GoldDailyBy(2);
            dailySub.text = dDone ? Content.T("tDailyDone") : Content.F("tDailyReward", mx); dailyBadgeL.text = dDone ? "" : "+" + mx; U.Show(dailyBadge, !dDone);
            levelsSub.text = done + "/" + Content.Levels.Count;
            U.Show(strip, done >= 3);
            int n = Meta.StreakNow(); bool today = Meta.StreakToday();
            skN.text = n > 0 ? (n == 1 && !Save.Ar ? "1 day" : Content.F("streakN", n)) : Content.T("streak0");
            skSub.text = today ? Content.T("streakDone") : Content.F("streakNext", Meta.STREAK_GOLD[n % 7]);
            flame.style.opacity = n > 0 ? 1 : .45f; U.Border(streakCard, 1, today ? new Color(1, .59f, .31f, .5f) : U.Line);
            var E = Meta.Now(); var s = Meta.Stats(E.A);
            evThumb.SetTexture(Content.Pic(E.A.pics[(E.wk * 5 % E.A.pics.Count + E.A.pics.Count) % E.A.pics.Count])); evName.text = E.A.Name;
            evSub.text = E.left == 1 ? Content.F("evSubLast", E.left, s.solved, s.n) : Content.F("evSub", E.left, s.solved, s.n);
        }
        public override void Tick(double now, float dt)
        {
            hero.Tick();
            float y = Mathf.Sin((float)now / 1000f * (Mathf.PI * 2 / 6.5f)) * -7; heroWrap.style.translate = new Translate(0, y, 0);
            float h = root.layout.height; if (h < 10) return;
            foreach (var m in moteLayer.Children()) { var d = (Vector3)m.userData; float yy = h + 10 - (float)((now / 1000f * d.y + d.z * 40) % (h + 40)); m.style.left = d.x; m.style.top = yy; }
        }
        // ---------- incoming challenge ----------
        public void ShowChallengeIntro(ChalData o)
        {
            if (App.I.cur == "game" && App.I.game.G != null && App.I.game.G.phase == "play") { App.I.game.Leave(); App.I.Show("menu"); }
            VisualElement box; var ov = App.I.Overlay(out box);
            var row = U.Row(box); row.style.marginBottom = 16;
            row.Add(new HexImage(Content.Pic(o.p), 86));
            var col = U.Col(row); col.style.marginLeft = col.style.marginRight = 14; col.style.flexShrink = 1;
            var al = Save.Ar ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            U.L(Content.F("chalFrom", o.n), 22, U.Gold, U.Display, col, al);
            U.L(Content.PicName(o.p), 16, U.Ink, U.Semi, col, al);
            U.L(Content.F("chalBeat", Util.FmtT(o.t), o.c), 13, U.Muted, U.Body, col, al, true);
            string rec = "";
            if (o.rn != null && Save.S("name") != null && o.rn == Save.S("name"))
            {
                bool theyWon = o.t < o.rt; string key = "r" + o.n + o.t + o.mv;
                if (!Challenge.Seen(key)) { var h = Challenge.H2H(o.n); if (theyWon) h["l"] = h.D("l") + 1; else h["w"] = h.D("w") + 1; Save.Persist(); }
                rec = (theyWon ? Content.F("chalReplied", o.n) : Content.F("chalRepliedLose", o.n)) + " · ";
            }
            var hh = Save.D.O("h2h")?.O(o.n); if (hh != null) rec += Content.F("chalRec", hh.I("w"), hh.I("l"), o.n);
            if (rec != "") U.L(rec, 13, U.Gold, U.Body, col, al, true);
            var br = U.Row(box);
            var later = U.Btn(Content.T("chalLater"), U.BK.Surface, () => App.I.CloseOverlay(ov), br, 48); later.style.flexGrow = 1;
            var start = U.Btn(Content.T("chalStart"), U.BK.Primary, () => { App.I.CloseOverlay(ov); App.I.game.StartChallenge(o); }, br, 48); start.style.flexGrow = 1;
            U.Gap(br, 10);
        }
    }
}
