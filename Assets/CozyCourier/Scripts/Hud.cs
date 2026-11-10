using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UText = UnityEngine.UI.Text;

namespace CozyCourier
{
    /// <summary>Butun interfeys: bosh menyu, oynalar, garaj, o'yin paneli, sahna dialogi.</summary>
    public class Hud : MonoBehaviour
    {
        Game G => Game.I;

        enum View { Menu, Free, Settings, Help, Garage, Play, Pause, End, Cut }
        View view = View.Menu;
        public bool MenuIsRoot => view == View.Menu;

        RectTransform root;
        // o'yin paneli
        RectTransform left, right, pad, ordersBox, markers;
        UText tTime, tShift, tMoney, tFine, tRate, tLvl, tBag, toastTxt;
        Image xpFill, toastBg, vignette;
        float toastLife; string lastToast;
        readonly List<RectTransform> markerPool = new List<RectTransform>();

        // oynalar
        RectTransform ovMenu, ovFree, ovSettings, ovHelp, ovGarage, ovPause, ovEnd;
        UText menuWallet, menuLvl, storySub, setToast, gWallet, gLvl, gToast, endEyebrow, endTitle, endMoney, endInfo, endUnlock, endToast;
        RectTransform districtsBox, confirmBox, gItems, gTabs;
        Button endDouble, endNext;
        UText endNextLbl;
        string gTab = "up";
        bool garageFromEnd;
        int endNet, lastNet; bool endRec, endStory;

        // sahna
        RectTransform cutLayer, dialog, titleCard;
        Image barTop, barBot, fade, portBg;
        RawImage portrait;
        UText dlgWho, dlgTxt, dlgHint, titleT, titleS, titleTap;
        string cutBg = "";

        // barglar
        RectTransform leafLayer;
        readonly List<Leaf> leaves = new List<Leaf>();
        class Leaf { public RectTransform rt; public Image im; public float x, y, vx, vy, rot, vr, ph; }

        static readonly Color Dark = Art.Hex("#2a1606");

        // ========================= QURISH =========================
        public void Build()
        {
            var go = new GameObject("HUD", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 10;
            var sc = go.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1600, 900);
            sc.matchWidthOrHeight = 0.6f;
            go.AddComponent<GraphicRaycaster>();
            root = (RectTransform)go.transform;

            vignette = UI.Pic(root, Art.Vignette, new Color(.04f, .03f, .1f, .55f), "Vignette");
            UI.Fill(vignette.rectTransform);
            BuildLeaves();
            markers = UI.Node("Markers", root); UI.Fill(markers);
            BuildLeft();
            BuildRight();
            BuildPad();
            BuildToast();
            ovMenu = BuildMenu();
            ovFree = BuildFree();
            ovSettings = BuildSettings();
            ovHelp = BuildHelp();
            ovGarage = BuildGarage();
            ovPause = BuildPause();
            ovEnd = BuildEnd();
            BuildCut();
        }

        public void Rebuild()
        {
            var v = view;
            if (root != null) Destroy(root.gameObject);
            root = null; leaves.Clear(); markerPool.Clear();
            Build();
            switch (v)
            {
                case View.Free: OpenFree(); break;
                case View.Settings: OpenSettings(); break;
                case View.Help: Open(View.Help, ovHelp); break;
                case View.Garage: ShowGarage(); break;
                case View.Play: ShowPlay(); break;
                case View.Pause: ShowPause(); break;
                case View.End: ShowEnd(endNet, endRec, endStory, true); break;
                case View.Cut: ShowCut(); break;
                default: ShowMenu(); break;
            }
        }

        // ---------------------------------------------------------------- yordamchilar
        static UText Txt(Transform p, string s, int size, Color c, float h, TextAnchor a = TextAnchor.MiddleLeft, FontStyle st = FontStyle.Normal)
        {
            var t = UI.Label(p, s, size, c, a, st);
            if (h > 0) UI.LE(t, h);
            return t;
        }

        static Button Big(Transform p, string title, string sub, Color bg, Color fg, UnityEngine.Events.UnityAction a, float h = 74)
        {
            var b = UI.Btn(p, "", bg, fg, a, 26);
            UI.LE(b, h);
            var lbl = b.GetComponentInChildren<UText>();
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.supportRichText = true;
            lbl.rectTransform.offsetMin = new Vector2(22, 6);
            Color subC = fg; subC.a = .7f;
            lbl.text = "<b>" + title + "</b>" + (string.IsNullOrEmpty(sub) ? "" : "\n<size=17><color=" + UI.Hex(subC) + ">" + sub + "</color></size>");
            lbl.fontStyle = FontStyle.Normal;
            return b;
        }

        RectTransform Overlay(string name, out RectTransform card, float width, Vector2 anchor, Vector2 pivot, Vector2 pos, float dimA = .74f)
        {
            var dim = UI.Pic(root, Art.Square, Art.Hex("#0a1218", dimA), name);
            dim.raycastTarget = true;
            UI.Fill(dim.rectTransform);
            var c = UI.Box(dim.transform, Art.Hex("#17262e", .97f), "card");
            UI.Place(c.rectTransform, anchor, pivot, pos, new Vector2(width, 0));
            UI.VL(c, 28, 12);
            var fit = c.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            card = c.rectTransform;
            dim.gameObject.SetActive(false);
            return dim.rectTransform;
        }

        RectTransform Overlay(string name, out RectTransform card, float width = 640) =>
            Overlay(name, out card, width, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero);

        void LangRow(Transform parent, float h)
        {
            var row = UI.Node("langs", parent); UI.HL(row, 0, 6); UI.LE(row, h);
            string[] names = { "UZ", "RU", "EN" };
            for (int i = 0; i < 3; i++)
            {
                int li = i;
                bool on = Loc.L == i;
                UI.Btn(row, names[i], on ? UI.Fg : UI.Panel2, on ? UI.Ink : UI.Muted, () => { G.sfx.Chime(660, .15f); G.SetLang(li); }, 20);
            }
        }

        void BackBtn(Transform p, UnityEngine.Events.UnityAction a = null)
        {
            var b = UI.Btn(p, "← " + Loc.Get("back"), UI.Panel2, UI.Fg, a ?? Back, 22);
            UI.LE(b, 52);
        }

        static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject); }

        // ---------------------------------------------------------------- o'yin paneli
        UText Stat(Transform parent, string key, out Image bar)
        {
            var box = UI.Box(parent, UI.Panel2, "stat");
            var lab = UI.Label(box.transform, Loc.Get(key).ToUpper(), 15, UI.Muted, TextAnchor.UpperLeft, FontStyle.Bold);
            var lrt = lab.rectTransform; lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(1, 1); lrt.pivot = new Vector2(0, 1);
            lrt.anchoredPosition = new Vector2(12, -7); lrt.sizeDelta = new Vector2(-20, 20);
            var val = UI.Label(box.transform, "0", 24, UI.Fg, TextAnchor.LowerLeft, FontStyle.Bold);
            var vrt = val.rectTransform; vrt.anchorMin = new Vector2(0, 0); vrt.anchorMax = new Vector2(1, 0); vrt.pivot = new Vector2(0, 0);
            vrt.anchoredPosition = new Vector2(12, 8); vrt.sizeDelta = new Vector2(-20, 30);
            bar = null;
            if (key == "level")
            {
                var bg = UI.Pic(box.transform, Art.Square, new Color(1, 1, 1, .1f), "xpbg");
                UI.Place(bg.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(46, 14), new Vector2(70, 5));
                bar = UI.Pic(bg.transform, Art.Square, UI.Teal, "xp");
                bar.type = Image.Type.Filled; bar.fillMethod = Image.FillMethod.Horizontal;
                UI.Fill(bar.rectTransform);
            }
            return val;
        }

        void BuildLeft()
        {
            left = UI.Box(root, UI.Panel, "Left").rectTransform;
            UI.Place(left, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(290, 0));
            UI.VL(left, 12, 8);
            var fit = left.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Txt(left, "<color=#f4a259>Cozy</color> Courier", 30, UI.Fg, 38, TextAnchor.MiddleLeft, FontStyle.Bold);
            var row = UI.Node("buttons", left); UI.HL(row, 0, 6); UI.LE(row, 46);
            UI.Btn(row, Loc.Get("menu"), UI.Panel2, UI.Fg, () => { if (G.state == Game.St.Play) G.SetPause(true); else G.QuitToMenu(); }, 20);
            UI.Btn(row, Loc.Get("pause"), UI.Panel2, UI.Fg, () => G.SetPause(G.state == Game.St.Play), 20);
            LangRow(left, 40);

            var grid = UI.Node("stats", left);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(129, 66); g.spacing = new Vector2(8, 8);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 2;
            UI.LE(grid, 66 * 3 + 16);
            tTime = Stat(grid, "time", out _);
            tShift = Stat(grid, "shift", out _);
            tMoney = Stat(grid, "income", out _);
            tFine = Stat(grid, "fines", out _); tFine.color = UI.Soft;
            tRate = Stat(grid, "rating", out _);
            tLvl = Stat(grid, "level", out xpFill);
        }

        void BuildRight()
        {
            right = UI.Box(root, UI.Panel, "Right").rectTransform;
            UI.Place(right, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(340, 0));
            UI.VL(right, 12, 8);
            var fit = right.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Txt(right, Loc.Get("orders"), 24, UI.Fg, 32, TextAnchor.MiddleLeft, FontStyle.Bold);
            tBag = Txt(right, "", 18, UI.Muted, 24);
            ordersBox = UI.Node("orders", right);
            UI.VL(ordersBox, 0, 8);
            var fit2 = ordersBox.gameObject.AddComponent<ContentSizeFitter>(); fit2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void BuildToast()
        {
            toastBg = UI.Box(root, Art.Hex("#101b22", .92f), "Toast");
            UI.Place(toastBg.rectTransform, new Vector2(.5f, 1), new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(760, 56));
            toastTxt = UI.Label(toastBg.transform, "", 22, UI.Fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Fill(toastTxt.rectTransform, 10);
            toastBg.gameObject.SetActive(false);
        }

        void BuildPad()
        {
            // aylanadigan joystik: chap pastki yarmida istalgan joyga barmoq qo'ysangiz, o'sha yerda paydo bo'ladi
            pad = UI.Node("Joystick", root);
            pad.anchorMin = new Vector2(0, 0); pad.anchorMax = new Vector2(.45f, .62f);
            pad.offsetMin = Vector2.zero; pad.offsetMax = Vector2.zero;
            var zone = pad.gameObject.AddComponent<Image>();
            zone.sprite = Art.Square; zone.color = new Color(0, 0, 0, 0); zone.raycastTarget = true;

            var bas = UI.Pic(pad, Art.Circle, new Color(.06f, .1f, .13f, .45f), "base");
            UI.Place(bas.rectTransform, Vector2.zero, new Vector2(.5f, .5f), new Vector2(150, 150), new Vector2(200, 200));
            var ring = UI.Pic(bas.transform, Art.Circle, new Color(1, 1, 1, .1f), "ring"); UI.Fill(ring.rectTransform, 8);
            var hole = UI.Pic(bas.transform, Art.Circle, new Color(.06f, .1f, .13f, .5f), "hole"); UI.Fill(hole.rectTransform, 12);
            var knob = UI.Pic(bas.transform, Art.Circle, UI.Accent, "knob");
            UI.Place(knob.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(92, 92));
            var shine = UI.Pic(knob.transform, Art.Circle, new Color(1, 1, 1, .3f), "shine");
            UI.Place(shine.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-12, 14), new Vector2(34, 26));

            var j = pad.gameObject.AddComponent<Joystick>();
            j.Init(bas.rectTransform, knob.rectTransform, 80);
            pad.SetSiblingIndex(markers.GetSiblingIndex() + 1); // panellar joystik ustida qoladi
            pad.gameObject.SetActive(false);
        }

        void BuildLeaves()
        {
            leafLayer = UI.Node("Leaves", root); UI.Fill(leafLayer);
            string[] cols = { "#e0a03a", "#c9772f", "#d9734a", "#f2c25a", "#b5543b" };
            for (int i = 0; i < 16; i++)
            {
                var im = UI.Pic(leafLayer, Art.Circle, Art.Hex(cols[i % cols.Length], .85f), "leaf");
                var l = new Leaf { rt = im.rectTransform, im = im, x = Random.value * 1600, y = Random.value * 900, vx = 10 + Random.value * 20, vy = 18 + Random.value * 24, rot = Random.value * 360, vr = -60 + Random.value * 120, ph = Random.value * 6 };
                float s = 10 + Random.value * 8;
                UI.Place(l.rt, new Vector2(0, 1), new Vector2(.5f, .5f), Vector2.zero, new Vector2(s, s * .55f));
                leaves.Add(l);
            }
        }

        // ---------------------------------------------------------------- bosh menyu
        RectTransform BuildMenu()
        {
            var ov = Overlay("Menu", out var card, 560, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(40, 0), .35f);
            Txt(card, Loc.Get("tagline").ToUpper(), 17, UI.Gold, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            Txt(card, "Cozy <color=#f4a259>Courier</color>", 60, UI.Fg, 72, TextAnchor.MiddleLeft, FontStyle.Bold);
            Txt(card, Loc.Get("mmTag"), 19, UI.Muted, 54);
            LangRow(card, 44);
            var row = UI.Node("profile", card); UI.HL(row, 0, 10); UI.LE(row, 30);
            menuWallet = UI.Label(row, "", 20, UI.Fg, TextAnchor.MiddleLeft, FontStyle.Bold);
            menuLvl = UI.Label(row, "", 20, UI.Teal, TextAnchor.MiddleRight, FontStyle.Bold);
            var sb = Big(card, Loc.Get("story"), " ", UI.Accent, Dark, () => { G.sfx.Chime(660, .2f); G.StartStoryDay(); }, 80);
            storySub = sb.GetComponentInChildren<UText>();
            Big(card, Loc.Get("free"), Loc.Get("freeSub"), UI.Teal, Art.Hex("#06231f"), () => { G.sfx.Chime(660, .2f); OpenFree(); });
            var r2 = UI.Node("row2", card); UI.HL(r2, 0, 10); UI.LE(r2, 70);
            Big(r2, Loc.Get("garage"), Loc.Get("garageSub"), UI.Panel2, UI.Fg, () => { garageFromEnd = false; ShowGarage(); }, 70);
            Big(r2, Loc.Get("settings"), Loc.Get("settingsSub"), UI.Panel2, UI.Fg, OpenSettings, 70);
            Big(card, Loc.Get("howto"), Loc.Get("howtoSub"), UI.Panel2, UI.Fg, () => Open(View.Help, ovHelp), 66);
            return ov;
        }

        string StoryText()
        {
            int c = Save.Chapter;
            string sub = c == 0 ? Loc.Get("storyStart") : c >= 5 ? Loc.Get("storyReplay") : Loc.Get("storyCont", Loc.Tx(Data.Story[c].title) + " · " + Loc.Tx(Data.Story[c].sub));
            return "<b>" + Loc.Get("story") + "</b>\n<size=17><color=#2a1606b0>" + sub + "</color></size>";
        }

        // ---------------------------------------------------------------- erkin smena
        RectTransform BuildFree()
        {
            var ov = Overlay("Free", out var card, 600);
            Txt(card, Loc.Get("free").ToUpper(), 17, UI.Gold, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            Txt(card, Loc.Get("chooseDistrict"), 34, UI.Fg, 44, TextAnchor.MiddleLeft, FontStyle.Bold);
            districtsBox = UI.Node("districts", card);
            UI.VL(districtsBox, 0, 8);
            var fit = districtsBox.gameObject.AddComponent<ContentSizeFitter>(); fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var s = UI.Btn(card, Loc.Get("start"), UI.Accent, Dark, () => { G.sfx.Chime(660, .2f); G.StartFree(); }, 26); UI.LE(s, 64);
            BackBtn(card);
            return ov;
        }

        void OpenFree()
        {
            Open(View.Free, ovFree);
            Clear(districtsBox);
            int L = Save.Level;
            foreach (var id in Data.DistrictOrder)
            {
                var d = Data.Districts[id];
                bool locked = L < d.lvl, on = Save.District == id;
                string sub = locked ? Loc.Get("unlocksAt", d.lvl) : d.mult > 1 ? Loc.Get("payBonus", Mathf.RoundToInt((d.mult - 1) * 100)) : Loc.Get("homeD");
                string did = id;
                var b = Big(districtsBox, (on ? "● " : "") + Loc.Tx(d.name), sub, on ? UI.Fg : UI.Panel2, on ? UI.Ink : (locked ? UI.Muted : UI.Fg),
                    () => { Save.District = did; G.BuildWorld(did); G.sfx.Chime(660, .15f); OpenFree(); }, 66);
                b.interactable = !locked;
            }
        }

        // ---------------------------------------------------------------- sozlamalar
        RectTransform BuildSettings()
        {
            var ov = Overlay("Settings", out var card, 600);
            Txt(card, Loc.Get("settings").ToUpper(), 17, UI.Gold, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            Txt(card, Loc.Get("lang"), 20, UI.Muted, 26);
            LangRow(card, 46);
            Toggle(card, "music", () => Save.Music, v => Save.Music = v);
            Toggle(card, "sfx", () => Save.Sfx, v => { Save.Sfx = v; G.sfx.Chime(880, .2f); });
            Row(card, Loc.Get("resetStory"), Loc.Get("restart"), () => { Save.Chapter = 0; setToast.text = Loc.Get("storyReset"); });
            Row(card, Loc.Get("resetAll"), Loc.Get("erase"), () => confirmBox.gameObject.SetActive(true));
            var cb = UI.Box(card, Art.Hex("#3a2a24"), "confirm");
            confirmBox = cb.rectTransform;
            UI.VL(cb, 14, 8);
            var cfit = cb.gameObject.AddComponent<ContentSizeFitter>(); cfit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Txt(confirmBox, Loc.Get("resetConfirm"), 19, UI.Fg, 50);
            var cr = UI.Node("row", confirmBox); UI.HL(cr, 0, 8); UI.LE(cr, 50);
            UI.Btn(cr, Loc.Get("yesReset"), UI.Soft, Dark, () =>
            {
                Save.ResetAll();
                confirmBox.gameObject.SetActive(false);
                if (Save.District != "markaz") G.BuildWorld("markaz");
                G.ApplyLook();
                setToast.text = Loc.Get("resetDone");
            }, 20);
            UI.Btn(cr, Loc.Get("cancel"), UI.Panel2, UI.Fg, () => confirmBox.gameObject.SetActive(false), 20);
            setToast = Txt(card, "", 18, UI.Ok, 26);
            BackBtn(card);
            return ov;
        }

        void Toggle(Transform card, string key, System.Func<bool> get, System.Action<bool> set)
        {
            Button b = null;
            b = UI.Btn(card, "", UI.Panel2, UI.Fg, () => { set(!get()); Lbl(); }, 22);
            UI.LE(b, 52);
            void Lbl() { b.GetComponentInChildren<UText>().text = Loc.Get(key) + ":  <color=" + (get() ? "#8fd6a8" : "#94a9ac") + ">" + Loc.Get(get() ? "on" : "off") + "</color>"; }
            Lbl();
        }

        void Row(Transform card, string label, string btn, UnityEngine.Events.UnityAction a)
        {
            var r = UI.Node("row", card); UI.HL(r, 0, 10); UI.LE(r, 50);
            var t = UI.Label(r, label, 20, UI.Fg); UI.LE(t, -1, -1, 3);
            var b = UI.Btn(r, btn, UI.Panel2, UI.Fg, a, 19); UI.LE(b, -1, -1, 1);
        }

        void OpenSettings()
        {
            Open(View.Settings, ovSettings);
            confirmBox.gameObject.SetActive(false);
            setToast.text = "";
        }

        // ---------------------------------------------------------------- yordam
        RectTransform BuildHelp()
        {
            var ov = Overlay("Help", out var card, 760);
            Txt(card, Loc.Get("howto").ToUpper(), 17, UI.Gold, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            Txt(card, Loc.Get("helpTitle"), 34, UI.Fg, 44, TextAnchor.MiddleLeft, FontStyle.Bold);
            string[] col = { "#f4a259", "#ffd166", "#5fd08a", "#ee9583", "#46d0bf", "#c3a6e8" };
            for (int i = 0; i < Loc.Help.Length; i++)
            {
                var r = UI.Box(card, new Color(1, 1, 1, .04f), "tip");
                UI.LE(r, 66);
                var n = UI.Pic(r.transform, Art.Circle, Art.Hex(col[i % col.Length]), "num");
                UI.Place(n.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, 0), new Vector2(34, 34));
                var nt = UI.Label(n.transform, (i + 1).ToString(), 18, Art.Hex("#14222a"), TextAnchor.MiddleCenter, FontStyle.Bold); UI.Fill(nt.rectTransform);
                var t = UI.Label(r.transform, Loc.Tx(Loc.Help[i]), 18, UI.Fg);
                t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = new Vector2(62, 4); t.rectTransform.offsetMax = new Vector2(-12, -4);
            }
            Txt(card, Loc.Get("controls"), 16, UI.Muted, 44);
            BackBtn(card);
            return ov;
        }

        // ---------------------------------------------------------------- garaj
        RectTransform BuildGarage()
        {
            var ov = Overlay("Garage", out var card, 900);
            var head = UI.Node("head", card); UI.HL(head, 0, 10); UI.LE(head, 50);
            var ht = UI.Label(head, Loc.Get("garageTitle"), 32, UI.Fg, TextAnchor.MiddleLeft, FontStyle.Bold); UI.LE(ht, -1, -1, 3);
            gWallet = UI.Label(head, "", 20, Art.Hex("#ffcf8a"), TextAnchor.MiddleRight, FontStyle.Bold); UI.LE(gWallet, -1, -1, 2);
            gLvl = UI.Label(head, "", 20, UI.Teal, TextAnchor.MiddleRight, FontStyle.Bold); UI.LE(gLvl, -1, -1, 1);
            gTabs = UI.Node("tabs", card); UI.HL(gTabs, 0, 8); UI.LE(gTabs, 46);

            // aylantiriladigan ro'yxat
            var sv = UI.Box(card, new Color(0, 0, 0, .15f), "scroll");
            sv.raycastTarget = true;
            UI.LE(sv, 470);
            sv.gameObject.AddComponent<RectMask2D>();
            var sr = sv.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.movementType = ScrollRect.MovementType.Clamped; sr.scrollSensitivity = 30;
            gItems = UI.Node("items", sv.transform);
            gItems.anchorMin = new Vector2(0, 1); gItems.anchorMax = new Vector2(1, 1); gItems.pivot = new Vector2(.5f, 1);
            gItems.offsetMin = Vector2.zero; gItems.offsetMax = Vector2.zero;
            var grid = gItems.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(404, 150); grid.spacing = new Vector2(10, 10);
            grid.padding = new RectOffset(10, 10, 10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
            var gf = gItems.gameObject.AddComponent<ContentSizeFitter>(); gf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = gItems; sr.viewport = sv.rectTransform;

            gToast = Txt(card, "", 18, UI.Ok, 26);
            BackBtn(card, GarageBack);
            return ov;
        }

        void GarageBack()
        {
            if (garageFromEnd && G.state == Game.St.End) { garageFromEnd = false; view = View.End; HideAll(); ovEnd.gameObject.SetActive(true); }
            else ShowMenu();
        }

        public void ShowGarage()
        {
            Open(View.Garage, ovGarage);
            gToast.text = "";
            RenderGarage();
        }

        void RenderGarage()
        {
            gWallet.text = Loc.Money(Save.Wallet);
            gLvl.text = Loc.Get("level") + " " + Save.Level;
            Clear(gTabs);
            string[] tabs = { "up", "look", "reno" }, names = { "tabUp", "tabLook", "tabReno" };
            for (int i = 0; i < 3; i++)
            {
                string id = tabs[i]; bool on = gTab == id;
                UI.Btn(gTabs, Loc.Get(names[i]), on ? UI.Fg : UI.Panel2, on ? UI.Ink : UI.Muted, () => { gTab = id; RenderGarage(); }, 20);
            }
            Clear(gItems);
            int W = Save.Wallet, L = Save.Level;
            if (gTab == "up")
            {
                foreach (var u in Data.Upgrades)
                {
                    int lv = Save.Up(u.id), max = u.prices.Length;
                    string extra = u.id == "speed" ? " (" + Data.Speeds[lv] + (lv < max ? " → " + Data.Speeds[lv + 1] : "") + ")"
                        : u.id == "bag" ? " (" + Data.Bags[lv] + (lv < max ? " → " + Data.Bags[lv + 1] : "") + " " + Loc.Get("slots") + ")" : "";
                    var uu = u;
                    Item(Art.Hex(u.col), null, Loc.Tx(u.name), Loc.Tx(u.desc) + extra, max > 1 ? lv : -1, max,
                        lv >= max ? Loc.Get("maxed") : Loc.Money(u.prices[lv]), lv < max && W >= u.prices[lv], lv >= max ? 1 : 0,
                        () =>
                        {
                            int l2 = Save.Up(uu.id), p = uu.prices[l2];
                            if (Save.Wallet < p) return;
                            Save.Wallet -= p; Save.SetUp(uu.id, l2 + 1);
                            G.sfx.Chime(880, .3f);
                            RenderGarage(); gToast.text = Loc.Get("installed", Loc.Tx(uu.name));
                        });
                }
            }
            else if (gTab == "look")
            {
                foreach (var cat in Data.LookCats)
                    foreach (var it in Data.Looks[cat])
                    {
                        bool owned = Save.Owned(cat, it.id), on = Save.Equipped(cat) == it.id, locked = it.lvl > 0 && L < it.lvl;
                        string btn; bool can; int style;
                        if (on) { btn = Loc.Get("equipped"); can = false; style = 2; }
                        else if (owned) { btn = Loc.Get("equip"); can = true; style = 1; }
                        else if (locked) { btn = Loc.Get("atLevel", it.lvl); can = false; style = 0; }
                        else { btn = Loc.Money(it.price); can = W >= it.price; style = 0; }
                        string c = cat; var ii = it;
                        Item(Art.Hex(it.c0), it.ikat ? (Color?)null : Art.Hex(it.c1), Loc.Tx(it.name), Loc.Tx(Data.LookTitle[cat]), -1, 0, btn, can, style, () =>
                        {
                            if (Save.Owned(c, ii.id)) { Save.Equip(c, ii.id); G.sfx.Chime(660, .2f); }
                            else
                            {
                                if (Save.Wallet < ii.price) return;
                                Save.Wallet -= ii.price; Save.Own(c, ii.id); Save.Equip(c, ii.id);
                                G.sfx.Chime(880, .3f);
                                gToast.text = Loc.Get("bought", Loc.Tx(ii.name));
                            }
                            G.ApplyLook();
                            string keep = gToast.text; RenderGarage(); gToast.text = keep;
                        }, it.ikat, locked);
                    }
            }
            else
            {
                int done = 0; foreach (var r in Data.Renos) if (Save.RenoDone(r.id)) done++;
                Item(UI.Accent, null, Loc.Get("renoProg", done, Data.Renos.Length), Loc.Get("renoDesc"), done, Data.Renos.Length, null, false, 0, null);
                foreach (var r in Data.Renos)
                {
                    bool own = Save.RenoDone(r.id); var rr = r;
                    Item(own ? UI.Ok : Art.Hex("#2a434e"), null, Loc.Tx(r.name), Loc.Tx(r.desc), -1, 0,
                        own ? Loc.Get("watch") : Loc.Money(r.price), own || W >= r.price, own ? 1 : 0, () =>
                        {
                            if (!Save.RenoDone(rr.id))
                            {
                                if (Save.Wallet < rr.price) return;
                                Save.Wallet -= rr.price; Save.SetReno(rr.id);
                                G.sfx.Arp(523, 659, 784);
                            }
                            G.PlayReno(rr);
                        });
                }
            }
        }

        void Item(Color c0, Color? c1, string title, string desc, int lv, int max, string btn, bool can, int style, UnityEngine.Events.UnityAction act, bool ikat = false, bool locked = false)
        {
            var box = UI.Box(gItems, new Color(1, 1, 1, locked ? .02f : .05f), "item");
            var sw = UI.Box(box.transform, c0, "sw");
            UI.Place(sw.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, -14), new Vector2(48, 48));
            if (c1.HasValue)
            {
                var half = UI.Pic(sw.transform, Art.Tri, c1.Value, "c1");
                UI.Fill(half.rectTransform, 4);
            }
            if (ikat)
            {
                string[] ik = { "#e85d75", "#ffd166", "#3d7fd0", "#8fd6a8" };
                for (int i = 0; i < 4; i++) { var s = UI.Pic(sw.transform, Art.Square, Art.Hex(ik[i]), "ik"); UI.Place(s.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(4 + i * 10, 0), new Vector2(10, 40)); }
            }
            var t = UI.Label(box.transform, title, 20, locked ? UI.Muted : UI.Fg, TextAnchor.UpperLeft, FontStyle.Bold);
            UI.Place(t.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -14), new Vector2(318, 26));
            var d = UI.Label(box.transform, desc, 16, UI.Muted, TextAnchor.UpperLeft);
            UI.Place(d.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(76, -42), new Vector2(318, 44));
            if (lv >= 0 && max > 0)
            {
                for (int i = 0; i < max; i++)
                {
                    var p = UI.Pic(box.transform, Art.Square, i < lv ? UI.Teal : new Color(1, 1, 1, .12f), "lv");
                    UI.Place(p.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(76 + i * 34, 62), new Vector2(28, 6));
                }
            }
            if (btn == null) return;
            Color bg = style == 2 ? UI.Teal : style == 1 ? UI.Panel2 : (can ? UI.Accent : Art.Hex("#2a3a42"));
            Color fg = style == 2 ? Art.Hex("#06231f") : style == 1 ? UI.Fg : (can ? Dark : UI.Muted);
            var b = UI.Btn(box.transform, btn, bg, fg, act, 18);
            UI.Place((RectTransform)b.transform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 12), new Vector2(170, 42));
            b.interactable = can;
            var cb = b.colors; cb.disabledColor = new Color(1, 1, 1, 1); b.colors = cb;
        }

        // ---------------------------------------------------------------- pauza va yakun
        RectTransform BuildPause()
        {
            var ov = Overlay("Pause", out var card, 560);
            Txt(card, Loc.Get("pause").ToUpper(), 17, UI.Gold, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            Txt(card, Loc.Get("pauseTitle"), 34, UI.Fg, 44, TextAnchor.MiddleLeft, FontStyle.Bold);
            Txt(card, Loc.Get("pauseText"), 20, UI.Muted, 56);
            var row = UI.Node("row", card); UI.HL(row, 0, 10); UI.LE(row, 60);
            UI.Btn(row, Loc.Get("resume"), UI.Accent, Dark, () => G.SetPause(false), 24);
            UI.Btn(row, Loc.Get("menu"), UI.Panel2, UI.Fg, () => G.QuitToMenu(), 24);
            return ov;
        }

        RectTransform BuildEnd()
        {
            var ov = Overlay("End", out var card, 680);
            endEyebrow = Txt(card, "", 17, UI.Gold, 24, TextAnchor.MiddleLeft, FontStyle.Bold);
            endTitle = Txt(card, "", 32, UI.Fg, 44, TextAnchor.MiddleLeft, FontStyle.Bold);
            endMoney = Txt(card, "", 48, Art.Hex("#ffcf8a"), 60, TextAnchor.MiddleLeft, FontStyle.Bold);
            endInfo = Txt(card, "", 18, UI.Muted, 108);
            endUnlock = Txt(card, "", 18, UI.Teal, -1);
            endDouble = UI.Btn(card, "▶  " + Loc.Get("double"), UI.Gold, Dark, OnDouble, 21); UI.LE(endDouble, 54);
            endToast = Txt(card, "", 18, UI.Ok, 24);
            var row = UI.Node("row", card); UI.HL(row, 0, 10); UI.LE(row, 60);
            endNext = UI.Btn(row, "", UI.Accent, Dark, () => G.AfterEnd(), 22);
            endNextLbl = endNext.GetComponentInChildren<UText>();
            UI.Btn(row, Loc.Get("garage"), UI.Panel2, UI.Fg, () => { garageFromEnd = true; Open(View.Garage, ovGarage); gToast.text = ""; RenderGarage(); }, 22);
            UI.Btn(row, Loc.Get("menu"), UI.Panel2, UI.Fg, () => G.EndToMenu(), 22);
            return ov;
        }

        void OnDouble()
        {
            endDouble.interactable = false;
            Ads.ShowRewarded(ok =>
            {
                if (ok && lastNet > 0)
                {
                    Save.Wallet += lastNet;
                    endToast.text = Loc.Get("doubled", Loc.Money(lastNet));
                    endMoney.text = "+" + Loc.Money(lastNet * 2);
                    lastNet = 0;
                    endDouble.gameObject.SetActive(false);
                    G.sfx.Chime(988, .3f);
                }
                else
                {
                    endToast.text = Loc.Get("adFail");
                    endDouble.interactable = true;
                }
            });
        }

        // ---------------------------------------------------------------- sahna
        void BuildCut()
        {
            cutLayer = UI.Node("Cut", root); UI.Fill(cutLayer);
            var tap = UI.Pic(cutLayer, Art.Square, new Color(0, 0, 0, 0), "tap");
            tap.raycastTarget = true; UI.Fill(tap.rectTransform);
            var tb = tap.gameObject.AddComponent<Button>(); tb.transition = Selectable.Transition.None;
            tb.onClick.AddListener(() => G.cut.Advance());

            var vg = UI.Pic(cutLayer, Art.Vignette, new Color(.04f, .03f, .1f, .5f), "vig"); UI.Fill(vg.rectTransform);

            // sarlavha kartasi
            var tc = UI.Pic(cutLayer, Art.Square, Art.Hex("#0f1b22"), "title");
            titleCard = tc.rectTransform; UI.Fill(titleCard);
            var tg = UI.Pic(titleCard, Art.Vignette, new Color(0, 0, 0, .6f), "tv"); UI.Fill(tg.rectTransform);
            for (int i = 0; i < 40; i++)
            {
                var st = UI.Pic(titleCard, Art.Square, new Color(1, .82f, .4f, .25f + .3f * Art.Rnd(i + 5)), "spark");
                UI.Place(st.rectTransform, new Vector2(Art.Rnd(i), Art.Rnd(i + 99)), new Vector2(.5f, .5f), Vector2.zero, new Vector2(3, 3));
            }
            var orn = UI.Pic(titleCard, Art.Square, UI.Accent, "orn");
            UI.Place(orn.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 130), new Vector2(14, 14));
            orn.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            for (int k = -1; k <= 1; k += 2)
            {
                var ln = UI.Pic(titleCard, Art.Square, Art.A(UI.Accent, .6f), "line");
                UI.Place(ln.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(k * 95, 130), new Vector2(130, 2));
            }
            titleT = UI.Label(titleCard, "", 76, UI.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Place(titleT.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 40), new Vector2(1400, 110));
            titleS = UI.Label(titleCard, "", 32, UI.Fg, TextAnchor.UpperCenter);
            UI.Place(titleS.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, 1), new Vector2(0, -24), new Vector2(940, 160));
            titleTap = UI.Label(titleCard, Loc.Get("tapContinue"), 20, UI.Muted, TextAnchor.MiddleCenter);
            UI.Place(titleTap.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 60), new Vector2(800, 30));
            titleCard.gameObject.SetActive(false);

            // kino ramkasi
            barTop = UI.Pic(cutLayer, Art.Square, Art.Hex("#07090c"), "barTop");
            barBot = UI.Pic(cutLayer, Art.Square, Art.Hex("#07090c"), "barBot");
            foreach (var b in new[] { barTop, barBot })
            {
                b.rectTransform.anchorMin = new Vector2(0, b == barTop ? 1 : 0); b.rectTransform.anchorMax = new Vector2(1, b == barTop ? 1 : 0);
                b.rectTransform.pivot = new Vector2(.5f, b == barTop ? 1 : 0);
                b.rectTransform.anchoredPosition = Vector2.zero; b.rectTransform.sizeDelta = new Vector2(0, 0);
            }

            // dialog
            var dlg = UI.Box(cutLayer, Art.Hex("#101b22", .94f), "dialog");
            dialog = dlg.rectTransform;
            dialog.anchorMin = new Vector2(.5f, 0); dialog.anchorMax = new Vector2(.5f, 0); dialog.pivot = new Vector2(.5f, 0);
            dialog.anchoredPosition = new Vector2(0, 66); dialog.sizeDelta = new Vector2(1180, 200);
            portBg = UI.Box(dialog, UI.Panel2, "portBox");
            UI.Place(portBg.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(18, 0), new Vector2(168, 168));
            portrait = UI.Node("portrait", portBg.transform).gameObject.AddComponent<RawImage>();
            portrait.raycastTarget = false;
            UI.Fill(portrait.rectTransform, 5);
            dlgWho = UI.Label(dialog, "", 26, UI.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UI.Place(dlgWho.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(208, -18), new Vector2(900, 34));
            dlgTxt = UI.Label(dialog, "", 25, UI.Fg, TextAnchor.UpperLeft);
            UI.Place(dlgTxt.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(208, -58), new Vector2(940, 120));
            dlgHint = UI.Label(dialog, Loc.Get("tapHint"), 16, UI.Muted, TextAnchor.LowerRight);
            UI.Place(dlgHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-18, 12), new Vector2(300, 24));

            var skip = UI.Btn(cutLayer, Loc.Get("skip") + "  ⏭", Art.Hex("#101b22", .8f), UI.Fg, () => G.cut.Skip(), 18);
            UI.Place((RectTransform)skip.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-18, -60), new Vector2(240, 44));

            fade = UI.Pic(cutLayer, Art.Square, Art.Hex("#0a1016", 0), "fade");
            UI.Fill(fade.rectTransform);
            cutLayer.gameObject.SetActive(false);
        }

        public void ShowCut()
        {
            view = View.Cut;
            HideAll(); SetHud(false);
            vignette.enabled = false;
            cutLayer.gameObject.SetActive(true);
            cutLayer.SetAsLastSibling();
            dialog.gameObject.SetActive(false);
            titleCard.gameObject.SetActive(false);
            if (G.cut != null) portrait.texture = G.cut.Portrait;
        }

        public void CutLine(string who, string text)
        {
            titleCard.gameObject.SetActive(false);
            bool narr = who == "N" || !Data.Chars.ContainsKey(who);
            dialog.gameObject.SetActive(true);
            Data.Chars.TryGetValue(who, out var d);
            dlgWho.text = d != null ? Loc.Tx(d.name) : "";
            dlgWho.color = narr ? UI.Muted : Art.Hex(d.col ?? "#f4a259");
            portBg.gameObject.SetActive(!narr);
            if (!narr) portBg.color = Art.Hex(d.col ?? "#334455");
            float x0 = narr ? 30 : 208;
            dlgWho.rectTransform.anchoredPosition = new Vector2(x0, -18);
            dlgTxt.rectTransform.anchoredPosition = new Vector2(x0, -58);
            dlgTxt.rectTransform.sizeDelta = new Vector2(narr ? 1120 : 940, 120);
            dlgTxt.fontStyle = narr ? FontStyle.Italic : FontStyle.Normal;
            dlgTxt.color = narr ? Art.Hex("#d8d2c4") : UI.Fg;
            dlgTxt.text = text;
        }

        public void CutText(string s) { dlgTxt.text = s; }

        public void CutTitle(string t, string s)
        {
            dialog.gameObject.SetActive(false);
            titleCard.gameObject.SetActive(true);
            titleT.text = t; titleS.text = s;
            cutBg = "title";
        }

        public void CutFrame(float bar, float fadeA, string bg)
        {
            cutBg = titleCard.gameObject.activeSelf ? "title" : bg;
            float h = root.rect.height * 44f / 860f * bar;
            barTop.rectTransform.sizeDelta = new Vector2(0, h);
            barBot.rectTransform.sizeDelta = new Vector2(0, h);
            var c = fade.color; c.a = fadeA; fade.color = c;
            if (titleCard.gameObject.activeSelf)
            {
                float a = 1 - fadeA;
                titleT.color = Art.A(UI.Gold, a); titleS.color = Art.A(UI.Fg, a);
                titleTap.color = Art.A(UI.Muted, a * (.45f + .45f * Mathf.Sin(Time.time / .45f)));
            }
        }

        public void CutEnd()
        {
            cutLayer.gameObject.SetActive(false);
            vignette.enabled = true;
            cutBg = "";
            if (view == View.Cut) view = View.Menu;
        }

        // ========================= KO'RSATISH =========================
        void HideAll()
        {
            foreach (var o in new[] { ovMenu, ovFree, ovSettings, ovHelp, ovGarage, ovPause, ovEnd }) o.gameObject.SetActive(false);
        }

        void SetHud(bool on)
        {
            left.gameObject.SetActive(on);
            right.gameObject.SetActive(on);
            markers.gameObject.SetActive(on);
            pad.gameObject.SetActive(on);
            if (!on) toastBg.gameObject.SetActive(false);
        }

        void Open(View v, RectTransform ov)
        {
            view = v; HideAll(); SetHud(false);
            if (cutLayer != null) cutLayer.gameObject.SetActive(false);
            ov.gameObject.SetActive(true);
        }

        public void Back()
        {
            if (view == View.Garage) GarageBack();
            else if (view == View.Free || view == View.Settings || view == View.Help) ShowMenu();
            else if (view == View.End) G.EndToMenu();
        }

        public void ShowMenu()
        {
            Open(View.Menu, ovMenu);
            garageFromEnd = false;
            menuWallet.text = Loc.Get("wallet") + ": " + Loc.Money(Save.Wallet);
            int L = Save.Level, a = Save.Need(L), b = Save.Need(L + 1);
            menuLvl.text = Loc.Get("level") + " " + L + " · " + (Save.XP - a) + " / " + (b - a) + " XP";
            storySub.text = StoryText();
        }

        public void ShowPlay() { view = View.Play; HideAll(); if (cutLayer != null) cutLayer.gameObject.SetActive(false); SetHud(true); Refresh(); }
        public void ShowPause() { view = View.Pause; HideAll(); SetHud(true); ovPause.gameObject.SetActive(true); }

        public void ShowEnd(int net, bool rec, bool story) => ShowEnd(net, rec, story, false);

        void ShowEnd(int net, bool rec, bool story, bool rebuilt)
        {
            endNet = net; endRec = rec; endStory = story;
            if (!rebuilt) lastNet = net;
            Open(View.End, ovEnd);
            garageFromEnd = false;
            var ch = story && Save.Chapter < 5 ? Data.Story[Save.Chapter] : null;
            endEyebrow.text = (ch != null ? Loc.Get("dayDone", Loc.Tx(ch.title)) : Loc.Tx(G.city.D.name)).ToUpper();
            endTitle.text = ch != null ? Loc.Tx(ch.sub) : Loc.Get("shiftOver");
            int shown = rebuilt && lastNet == 0 && net > 0 ? net * 2 : net;
            endMoney.text = (shown >= 0 ? "+" : "−") + Loc.Money(Mathf.Abs(shown));
            endInfo.text = Loc.Get("endInfo", Loc.Money(G.money), Loc.Money(G.tips), Loc.Money(G.fines), G.hits, G.lates, G.reds, G.done, G.rating.ToString("0.0"), G.xpGain, Loc.Money(Save.Wallet))
                + (rec ? "  ·  <color=#46d0bf><b>" + Loc.Get("record") + "</b></color>" : "");
            var u = new List<string>();
            foreach (int L in G.lvlUps)
            {
                string ut = Game.UnlockText(L);
                u.Add("<b>" + Loc.Get("lvlUpEnd", L) + "</b> " + (ut.Length > 0 ? Loc.Get("unlocked", ut) + "." : Loc.Get("seeGarage")));
            }
            endUnlock.text = string.Join("\n", u);
            endUnlock.gameObject.SetActive(u.Count > 0);
            endNextLbl.text = story ? Loc.Get("contStory") : Loc.Get("again");
            endToast.text = "";
            endDouble.gameObject.SetActive(lastNet > 0 && Ads.RewardReady);
            endDouble.interactable = true;
        }

        // ========================= XABAR =========================
        public void Toast(string s, Color c)
        {
            if (view != View.Play) return;
            lastToast = s;
            toastTxt.text = "<color=" + UI.Hex(c) + ">●</color>  " + s;
            toastBg.gameObject.SetActive(true);
            toastLife = 2.8f;
        }

        public void ToastOnce(string s, Color c) { if (toastLife <= 0 || lastToast != s) Toast(s, c); }

        // ========================= KADR =========================
        public void Tick(float dt)
        {
            if (root == null) return;
            if (toastLife > 0)
            {
                toastLife -= dt;
                var col = toastBg.color; col.a = Mathf.Clamp01(toastLife * 2) * .92f; toastBg.color = col;
                var tc = toastTxt.color; tc.a = Mathf.Clamp01(toastLife * 2); toastTxt.color = tc;
                if (toastLife <= 0) toastBg.gameObject.SetActive(false);
            }

            // barglar: o'yinda, menyuda va ko'cha/hovli sahnalarida
            bool cutLeaves = cutBg == "street" || cutBg == "yard";
            bool showLeaves = view != View.Cut || cutLeaves;
            leafLayer.gameObject.SetActive(showLeaves);
            if (showLeaves)
            {
                var size = root.rect.size;
                float t = Time.time;
                foreach (var l in leaves)
                {
                    l.x += (l.vx + Mathf.Sin(t * .8f + l.ph) * 18) * dt;
                    l.y += l.vy * dt;
                    l.rot += l.vr * dt;
                    if (l.y > size.y + 20) { l.y = -20; l.x = Random.value * size.x; }
                    if (l.x > size.x + 20) l.x = -20;
                    l.rt.anchoredPosition = new Vector2(l.x, -l.y);
                    l.rt.localRotation = Quaternion.Euler(0, 0, l.rot);
                }
            }

            if (view == View.Play) Markers();
            else if (markers.gameObject.activeSelf) foreach (var m in markerPool) m.gameObject.SetActive(false);
        }

        void Markers()
        {
            var scale = root.localScale.x;
            int k = 0;
            var size = root.rect.size;
            float m = 48;
            foreach (var o in G.orders)
            {
                var sp = G.MapToScreen(o.Target) / scale;
                bool off = sp.x < 0 || sp.y < 0 || sp.x > size.x || sp.y > size.y;
                if (!off) continue;
                if (k >= markerPool.Count) markerPool.Add(NewMarker());
                var mk = markerPool[k++];
                mk.gameObject.SetActive(true);
                float cx = Mathf.Clamp(sp.x, m, size.x - m), cy = Mathf.Clamp(sp.y, m, size.y - m);
                mk.anchoredPosition = new Vector2(cx, cy);
                float a = Mathf.Atan2(sp.y - cy, sp.x - cx) * Mathf.Rad2Deg;
                mk.GetChild(1).GetComponent<Image>().color = o.color;
                mk.GetChild(1).GetChild(0).GetComponent<UText>().text = o.story ? "★" : o.id.ToString();
                var arrow = (RectTransform)mk.GetChild(2);
                arrow.localRotation = Quaternion.Euler(0, 0, a);
                arrow.GetChild(0).GetComponent<Image>().color = o.color;
            }
            for (int i = k; i < markerPool.Count; i++) markerPool[i].gameObject.SetActive(false);
        }

        RectTransform NewMarker()
        {
            var rt = UI.Node("marker", markers);
            UI.Place(rt, Vector2.zero, new Vector2(.5f, .5f), Vector2.zero, new Vector2(44, 44));
            var bg = UI.Pic(rt, Art.Circle, Art.Hex("#101b22", .85f), "bg"); UI.Fill(bg.rectTransform);
            var dot = UI.Pic(rt, Art.Circle, Color.white, "dot"); UI.Fill(dot.rectTransform, 6);
            var n = UI.Label(dot.transform, "", 17, Art.Hex("#14222a"), TextAnchor.MiddleCenter, FontStyle.Bold); UI.Fill(n.rectTransform);
            var arrow = UI.Node("arrow", rt);
            UI.Place(arrow, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(0, 0));
            var tri = UI.Pic(arrow, Art.Tri, Color.white, "tri");
            UI.Place(tri.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(31, 0), new Vector2(14, 14));
            tri.rectTransform.localRotation = Quaternion.Euler(0, 0, -90);
            return rt;
        }

        // ========================= YANGILASH =========================
        public void Refresh()
        {
            if (view != View.Play && view != View.Pause) return;
            if (float.IsInfinity(G.time) || G.time <= 0) tShift.text = "0:00";
            else { int s = Mathf.CeilToInt(G.time); tShift.text = (s / 60) + ":" + (s % 60).ToString("00"); }
            int mins = Mathf.RoundToInt(G.Phase * 240), hh = 17 + mins / 60, mm = mins % 60;
            tTime.text = hh + ":" + mm.ToString("00");
            tMoney.text = Loc.Num(G.money);
            tFine.text = G.fines > 0 ? "−" + Loc.Num(G.fines) : "0";
            tRate.text = G.rating.ToString("0.0") + " ★";
            int L = Save.Level, a = Save.Need(L), b = Save.Need(L + 1);
            tLvl.text = L.ToString();
            if (xpFill != null) xpFill.fillAmount = Mathf.Clamp01((Save.XP - a) / (float)(b - a));

            int inBag = 0;
            foreach (var o in G.orders) if (o.picked) inBag++;
            tBag.text = Loc.Get("bag") + ": " + inBag + " / " + G.bagMax;

            Clear(ordersBox);
            if (G.orders.Count == 0) { Txt(ordersBox, Loc.Get("waiting"), 18, UI.Muted, 60, TextAnchor.MiddleCenter); return; }
            var list = new List<Order>(G.orders);
            list.Sort((x, y) => x.story != y.story ? (x.story ? -1 : 1) : x.left.CompareTo(y.left));
            foreach (var o in list) OrderRow(o);
        }

        void OrderRow(Order o)
        {
            var row = UI.Box(ordersBox, o.story ? new Color(1, .82f, .4f, .1f) : new Color(1, 1, 1, .05f), "order");
            UI.LE(row, 70);
            var num = UI.Pic(row.transform, o.picked ? Art.Round : Art.Circle, o.color, "num");
            if (o.picked) num.type = Image.Type.Sliced;
            UI.Place(num.rectTransform, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(10, 4), new Vector2(36, 36));
            var nt = UI.Label(num.transform, o.story ? "★" : o.id.ToString(), 18, Art.Hex("#14222a"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Fill(nt.rectTransform);

            bool inf = float.IsInfinity(o.limit);
            float r = inf ? 1 : Mathf.Clamp01(o.left / o.limit);
            Color bc = o.story ? UI.Gold : o.late ? UI.Soft : r > .35f ? UI.Ok : UI.Warn;
            string chip = o.story ? Loc.Get("storyChip") : o.late ? Loc.Get("lateChip") : o.picked ? Loc.Get("drop") : Loc.Get("pick");
            Color chipC = o.story ? UI.Gold : o.late ? UI.Soft : o.picked ? UI.Accent : UI.Ok;
            string where = o.picked ? o.house.Label : Loc.Tx(o.rest.rest.name);

            var t1 = UI.Label(row.transform, "<b>" + where + "</b>  <size=13><color=" + UI.Hex(chipC) + ">" + chip + "</color></size>", 19, UI.Fg, TextAnchor.MiddleLeft);
            UI.Place(t1.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(56, -8), new Vector2(210, 26));
            t1.horizontalOverflow = HorizontalWrapMode.Overflow;
            var t2 = UI.Label(row.transform, o.DishName + " → " + o.house.Label, 15, UI.Muted, TextAnchor.MiddleLeft);
            UI.Place(t2.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(56, -34), new Vector2(220, 22));
            t2.horizontalOverflow = HorizontalWrapMode.Overflow;
            string secT = inf ? "∞" : o.late ? "+" + Mathf.CeilToInt(-o.left) + "s" : Mathf.CeilToInt(o.left) + "s";
            var sec = UI.Label(row.transform, secT, 19, bc, TextAnchor.MiddleRight, FontStyle.Bold);
            UI.Place(sec.rectTransform, new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-10, 4), new Vector2(70, 30));

            var barBg = UI.Pic(row.transform, Art.Square, new Color(1, 1, 1, .08f), "barbg");
            barBg.rectTransform.anchorMin = new Vector2(0, 0); barBg.rectTransform.anchorMax = new Vector2(1, 0);
            barBg.rectTransform.pivot = new Vector2(.5f, 0);
            barBg.rectTransform.offsetMin = new Vector2(10, 6); barBg.rectTransform.offsetMax = new Vector2(-10, 11);
            var bar = UI.Pic(barBg.transform, Art.Square, bc, "bar");
            bar.type = Image.Type.Filled; bar.fillMethod = Image.FillMethod.Horizontal;
            bar.fillAmount = o.late ? 1 : r;
            UI.Fill(bar.rectTransform);
        }
    }
}
