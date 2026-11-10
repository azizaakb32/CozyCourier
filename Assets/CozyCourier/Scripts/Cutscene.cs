using System;
using UnityEngine;

namespace CozyCourier
{
    /// <summary>
    /// Kinosahnalar: fon, personajlar, portret, yozuv effekti.
    /// Sahna xaritadan uzoqda (x = 200 birlik) quriladi, asosiy kamera sahna vaqtida o'sha yerga ko'chadi.
    /// </summary>
    public class Cutscene : MonoBehaviour
    {
        const float STAGE_X = 200f, PORT_X = 400f, SW = 1260f, SH = 860f;
        Game G => Game.I;

        Shot[] shots;
        Action done;
        int si, li;
        float typed, age;
        int shown = -1;
        string curText = "";
        public bool Active => shots != null;

        Transform stage, portStage;
        Mesh bgMesh, animMesh, charMesh, fgMesh, portMesh;
        MeshRenderer bgR;
        readonly MB mb = new MB();
        RectTransform plateCanvas;
        UnityEngine.UI.Text plateText;
        Camera portCam;
        public RenderTexture Portrait { get; private set; }

        void Awake()
        {
            stage = new GameObject("Stage").transform;
            stage.position = new Vector3(STAGE_X, 0, 0);
            bgMesh = new Mesh(); animMesh = new Mesh(); charMesh = new Mesh(); fgMesh = new Mesh();
            bgR = Art.MeshObj(stage, "bg", bgMesh, 10);
            Art.MeshObj(stage, "anim", animMesh, 11);
            Art.MeshObj(stage, "chars", charMesh, 12);
            Art.MeshObj(stage, "fg", fgMesh, 13);

            // eshik raqami uchun kichik matn
            var go = new GameObject("Plate", typeof(RectTransform));
            go.transform.SetParent(stage, false);
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            cv.sortingOrder = 14;
            go.AddComponent<UnityEngine.UI.CanvasScaler>().dynamicPixelsPerUnit = UI.WorldDpu(SH);
            plateCanvas = (RectTransform)go.transform;
            plateCanvas.pivot = new Vector2(.5f, .5f);
            plateCanvas.sizeDelta = new Vector2(140, 54);
            plateCanvas.localScale = Vector3.one * Art.K;
            plateCanvas.localPosition = Art.W(890, 327);
            plateText = UI.Label(plateCanvas, "", 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Fill(plateText.rectTransform);
            plateText.horizontalOverflow = HorizontalWrapMode.Overflow;

            // portret
            portStage = new GameObject("PortraitStage").transform;
            portStage.position = new Vector3(PORT_X, 0, 0);
            portMesh = new Mesh();
            Art.MeshObj(portStage, "portrait", portMesh, 10);
            Portrait = new RenderTexture(256, 256, 16) { name = "Portrait" };
            var pc = new GameObject("PortraitCam");
            pc.transform.position = new Vector3(PORT_X + 96 * Art.K, -96 * Art.K, -10);
            portCam = pc.AddComponent<Camera>();
            portCam.orthographic = true;
            portCam.orthographicSize = 96 * Art.K;
            portCam.clearFlags = CameraClearFlags.SolidColor;
            portCam.backgroundColor = Art.Hex("#1d3039");
            portCam.targetTexture = Portrait;
            portCam.nearClipPlane = .1f; portCam.farClipPlane = 50f;
            portCam.enabled = false;

            stage.gameObject.SetActive(false);
            portStage.gameObject.SetActive(false);
        }

        // ========================= BOSHQARUV =========================
        public void Play(Shot[] list, Action onDone)
        {
            if (list == null || list.Length == 0) { onDone?.Invoke(); return; }
            shots = list; done = onDone; si = 0; li = 0;
            stage.gameObject.SetActive(true);
            portStage.gameObject.SetActive(true);
            G.Overlays(false);
            BeginShot();
        }

        public void Skip() { if (Active) Finish(); }

        public void Advance()
        {
            if (!Active) return;
            var sh = shots[si];
            if (sh.title == 0 && sh.lines != null && li < sh.lines.Length)
            {
                if (typed < curText.Length) { typed = curText.Length; return; }
                li++;
                if (li < sh.lines.Length) { ShowLine(); return; }
            }
            si++; li = 0;
            if (si >= shots.Length) { Finish(); return; }
            BeginShot();
        }

        void Finish()
        {
            var d = done;
            shots = null; done = null;
            stage.gameObject.SetActive(false);
            portStage.gameObject.SetActive(false);
            portCam.enabled = false;
            G.Overlays(true);
            G.hud.CutEnd();
            d?.Invoke();
        }

        void BeginShot()
        {
            age = 0;
            var sh = shots[si];
            if (sh.title != 0)
            {
                stage.gameObject.SetActive(false);
                portCam.enabled = false;
                var ch = Data.Story[Mathf.Clamp(Save.Chapter, 0, Data.Story.Length - 1)];
                string t = sh.title == 1 ? Loc.Tx(ch.title) : Loc.Tx(sh.tTitle);
                string s = sh.title == 1 ? Loc.Tx(ch.sub) : Loc.Tx(sh.tSub);
                G.hud.CutTitle(t, s);
                return;
            }
            stage.gameObject.SetActive(true);
            mb.Clear();
            CutArt.Background(mb, sh);
            mb.Fill(bgMesh);
            bool door = sh.bg == "door";
            plateCanvas.gameObject.SetActive(door);
            if (door) plateText.text = City.HomeLabel(sh.plate == 'S' ? Data.HOME_S : Data.HOME_R);
            ShowLine();
        }

        static string Fill(string s) => s.Replace("{R}", City.HomeLabel(Data.HOME_R)).Replace("{S}", City.HomeLabel(Data.HOME_S));

        void ShowLine()
        {
            var sh = shots[si];
            if (sh.lines == null || sh.lines.Length == 0) { curText = ""; G.hud.CutLine("N", ""); return; }
            var l = sh.lines[li];
            string who = l[0];
            curText = Fill(l[Mathf.Clamp(1 + Loc.L, 1, l.Length - 1)]);
            typed = 0; shown = -1;
            portCam.enabled = who != "N";
            G.hud.CutLine(who, "");
            if (who != "N" && G.sfx != null)
            {
                float f = who == "sardor" ? 880 : (who == "rustam" || who == "karim" || who == "bahodir" || who == "anvar") ? 392 : 523;
                G.sfx.Chime(f, .15f, .12f);
            }
        }

        // ========================= KADR =========================
        void Update()
        {
            if (!Active) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            age += dt;
            if (Game.AdvancePressed()) { Advance(); if (!Active) return; }
            var sh = shots[si];
            float t = Time.time;
            if (sh.title != 0) { G.hud.CutFrame(0, Mathf.Clamp01(1 - age / .5f), sh.bg); return; }

            // yozuv effekti
            if (typed < curText.Length)
            {
                typed = Mathf.Min(curText.Length, typed + dt * 42f);
                int n = Mathf.FloorToInt(typed);
                if (n != shown) { shown = n; G.hud.CutText(curText.Substring(0, n)); }
            }
            else if (shown != curText.Length) { shown = curText.Length; G.hud.CutText(curText); }

            string speaker = sh.lines != null && sh.lines.Length > 0 ? sh.lines[li][0] : "N";

            mb.Clear(); CutArt.Anim(mb, sh, t); mb.Fill(animMesh);

            mb.Clear();
            int cnt = sh.chars != null ? sh.chars.Length : 0;
            for (int i = 0; i < cnt; i++)
            {
                string id = sh.chars[i];
                float x = cnt == 1 ? SW * .7f : SW * (i + 1) / (cnt + 1) + (cnt == 2 ? (i == 1 ? 60 : -60) : 0);
                CutArt.Char(mb, id, x, sh.bg == "yard" ? 700 : 790, id == speaker, speaker != "N" && id != speaker, t);
            }
            mb.Fill(charMesh);

            mb.Clear(); CutArt.Foreground(mb, sh, t); mb.Fill(fgMesh);

            if (speaker != "N") DrawPortrait(speaker, t);

            // kamera: sahnani to'liq ko'rsatish va sekin yaqinlashish
            var cam = G.cam;
            float drift = Mathf.Min(1, age / 12f);
            float zoom = 1.02f + drift * .03f;
            float fitH = SH * Art.K / 2f, fitW = SW * Art.K / 2f / Mathf.Max(.1f, cam.aspect);
            cam.orthographicSize = Mathf.Max(fitH, fitW) / zoom;
            cam.transform.position = new Vector3(STAGE_X + (SW / 2 + drift * 10) * Art.K, -SH / 2 * Art.K, -10);

            G.hud.CutFrame(Mathf.Min(1, age / .6f), Mathf.Clamp01(1 - age / .5f), sh.bg);
        }

        void DrawPortrait(string id, float t)
        {
            if (!Data.Chars.TryGetValue(id, out var d)) return;
            mb.Clear();
            var col = Art.Hex(d.col ?? "#334455");
            mb.GradRect(-10, -10, 212, 212, Art.Shade(col, -.35f), Art.Shade(col, -.65f));
            mb.Circle(96, 90, 70, new Color(1, 1, 1, .08f), 28);
            CutArt.Char(mb, id, 96, 108 + 154 * 2.7f, true, false, t, 2.7f / d.s);
            mb.Fill(portMesh);
        }
    }
}
