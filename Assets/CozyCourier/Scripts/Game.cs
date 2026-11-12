using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Random = UnityEngine.Random;
using Object = UnityEngine.Object;

namespace CozyCourier
{
    public class Order
    {
        public int id;
        public bool story;
        public Block rest, house;
        public int dish;
        public string[] storyDish;
        public float limit, left;
        public bool picked, late;
        public int pay;
        public Color color;
        public RectTransform pin;
        public float born;
        public Vector2 Target => picked ? house.door : rest.door;
        public string DishName => story ? Loc.Tx(storyDish) : Loc.Tx(rest.rest.dishes[Mathf.Clamp(dish, 0, rest.rest.dishes.Length - 1)]);
    }

    class Car
    {
        public Transform t;
        public MeshRenderer mr;
        public Quad glow;
        public bool h;
        public int dir, row;
        public float lane, p, v, cur, startP;
    }

    class TLight
    {
        public int c, r;
        public float off;
        public readonly List<Quad> barsH = new List<Quad>();
        public readonly List<Quad> barsV = new List<Quad>();
        public Quad[] headH, headV;
        public Quad glowH, glowV;
    }

    class Gai
    {
        public Vector2 center;
        public float alert;
        public Transform officer, baton;
        public LineRenderer zone;
        public Quad bang;
    }

    class Fx
    {
        public RectTransform rt;
        public Text t;
        public float life;
        public Vector2 p;
    }

    /// <summary>O'yinning asosiy mantiqi: smena, kuryer, buyurtmalar, mashinalar, svetofor, YHXB, kecha, hikoya oqimi.</summary>
    public class Game : MonoBehaviour
    {
        public static Game I;
        public static bool padU, padD, padL, padR;
        public static Vector2 joy;

        public const float CELL = 200f, ROAD = 60f, BS = 140f, MW = 1260f, MH = 860f;
        public const int COLS = 6, ROWS = 4;
        /// <summary>O'yin paytidagi yaqinlik (katta = yaqinroq). Menyuda xarita kengroq ko'rinadi.</summary>
        public const float ZOOM = 2.1f, MENU_ZOOM = 1.45f;
        float zoomNow = MENU_ZOOM;
        bool Playing => state == St.Play || state == St.Pause || state == St.End;
        const float STORY_SHIFT = 180f, FREE_SHIFT = 300f, GAI_R = 260f, CYCLE = 12f;
        const int FINE_CAR = 5000, FINE_LATE = 3000, FINE_RED = 8000, GIFT = 10000, MAXO = 3;
        static readonly string[] OrderCols = { "#f4a259", "#7ec8e3", "#c3a6e8", "#8fd6a8", "#ee9583" };
        public static readonly Color GOLD = new Color(1f, .82f, .4f);

        public enum St { Menu, Play, Pause, End, Cut }
        public St state = St.Menu;
        public bool storyMode;

        public City city;
        public Hud hud;
        public Sfx sfx;
        public Cutscene cut;
        public Camera cam;
        Transform worldRoot, districtRoot;
        RectTransform pinRoot;

        // smena ko'rsatkichlari
        public float time, shiftLen = FREE_SHIFT, rating = 5f;
        public int money, fines, done, lates, hits, reds, tips, xpGain;
        public readonly List<int> lvlUps = new List<int>();
        public readonly List<Order> orders = new List<Order>();
        int nextId;
        float spawnT, holdT;
        StoryOrder story;
        bool storySpawned, storyDone;
        public int speed = 140, bagMax = 3;
        bool ins, nav;
        float mult = 1f;

        // kuryer
        public Vector2 pos;
        float ang, stun, inv, bob;
        Vector2 mv;
        bool moving;
        string inX;
        Transform courier, courierBody;
        SortingGroup courierSG;
        Quad arrow, headlight, courierShadow, footL, footR, flame, flameIn;
        readonly List<Quad> bodyParts = new List<Quad>();
        readonly List<Color> bodyBase = new List<Color>();
        string vehId = "";

        // ko'rinmas rejim (reklama orqali): mashinalar va svetoforlar sezmaydi
        public const float GHOST_TIME = 45f;
        public const int GHOST_PER_SHIFT = 2;
        public float ghost;
        public int ghostUses;
        public bool Ghost => ghost > 0;

        // o'rgatish (birinchi smena) va smena yulduzlari
        public int tut = -1;
        float tutDist, tutT;
        public int lastStars;
        public readonly List<int> lvlRewards = new List<int>();

        // hamroh (mushuk / kuchukcha)
        Transform pet, petBody;
        SortingGroup petSG;
        Vector2 petPos; float petAng, petBob;
        string petId = "";
        readonly List<Quad> dust = new List<Quad>();
        readonly List<float> dustLife = new List<float>();
        int dustIdx;
        float dustT;
        LineRenderer route, route2;

        readonly List<Car> cars = new List<Car>();
        readonly List<TLight> lights = new List<TLight>();
        readonly List<Gai> gais = new List<Gai>();
        readonly List<Quad> lampGlows = new List<Quad>();
        readonly List<Quad> signGlows = new List<Quad>();
        readonly List<Color> signCols = new List<Color>();
        readonly List<Fx> fx = new List<Fx>();
        Quad nightSr, goldSr;
        float LT, uiTick;

        // ========================= ISHGA TUSHIRISH =========================
        // boshqa oqimlardan (bulut, reklama) kelgan ishlarni asosiy oqimda bajarish
        static readonly Queue<Action> mainQ = new Queue<Action>();
        public static void OnMain(Action a) { if (a == null) return; lock (mainQ) mainQ.Enqueue(a); }

        void OnApplicationPause(bool paused)
        {
            if (paused) { Notify.ScheduleAll(); Cloud.Upload(); }
            else Notify.CancelAll();
        }
        void OnApplicationQuit() { Notify.ScheduleAll(); }

        void Awake()
        {
            I = this;
            Cloud.OnImported += () =>
            {
                // bulutdan yangiroq natija keldi: ko'rinishni yangilaymiz
                if (state != St.Menu) return;
                BuildWorld(Save.District);
                ApplyLook();
                hud.Rebuild();
            };
            Daily.OnTaskDone += t => { if (hud != null && state == St.Play) { hud.Toast(Loc.Get("taskDone", Daily.Text(t)), UI.Gold); sfx.Arp(659, 784, 988); } };
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Loc.L = Mathf.Clamp(Save.Lang, 0, 2);

            cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera");
                cg.tag = "MainCamera";
                cam = cg.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = MH / ZOOM * Art.K / 2f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Art.Hex("#0c151b");
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            Art.SetupQuality(cam);
            cam.transform.position = new Vector3(MW * Art.K / 2f, -MH * Art.K / 2f, -10f);

            worldRoot = new GameObject("World").transform;
            BuildCars();
            BuildCourier();
            BuildNight();
            BuildPinCanvas();
            BuildWorld(Save.District);

            sfx = gameObject.AddComponent<Sfx>();
            EnsureEventSystem();
            hud = gameObject.AddComponent<Hud>();
            hud.Build();
            cut = gameObject.AddComponent<Cutscene>();
            Ads.Init();
            Shop.Init();
            Notify.Init();
            Cloud.Init();
            ShowMenu();
            hud.ShowSplash();
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>Mahallani (xarita, svetofor, YHXB, nurlar) qaytadan quradi.</summary>
        public void BuildWorld(string districtId)
        {
            if (!Data.Districts.ContainsKey(districtId) || Save.Level < Data.Districts[districtId].lvl) districtId = "markaz";
            if (city != null) city.Destroy();
            if (districtRoot != null) Destroy(districtRoot.gameObject);
            lights.Clear(); gais.Clear(); lampGlows.Clear(); signGlows.Clear(); signCols.Clear();
            districtRoot = new GameObject("District").transform;
            districtRoot.SetParent(worldRoot, false);
            city = new City();
            city.Build(districtRoot, Data.Districts[districtId]);
            BuildLights();
            BuildGai();
            foreach (var p in city.lampGround) lampGlows.Add(Art.Spr(districtRoot, Art.Soft, p.x, p.y, 130, 130, Color.clear, 5001, "LampGlow"));
            foreach (var s in city.signs) { signGlows.Add(Art.Spr(districtRoot, Art.Soft, s.Key.x, s.Key.y, 150, 150, Color.clear, 5001, "SignGlow")); signCols.Add(s.Value); }
        }

        void BuildLights()
        {
            var root = new GameObject("TrafficLights").transform;
            root.SetParent(districtRoot, false);
            var la = city.D.lights;
            for (int i = 0; i < la.GetLength(0); i++)
            {
                int c = la[i, 0], r = la[i, 1];
                var L = new TLight { c = c, r = r, off = ((c + r) % 2) * CYCLE / 2f };
                float x = c * CELL, y = r * CELL;
                if (c > 0) L.barsH.Add(Art.Spr(root, Art.Square, x - 3, y + 30, 4, 52, Color.white, 3));
                if (c < COLS) L.barsH.Add(Art.Spr(root, Art.Square, x + 63, y + 30, 4, 52, Color.white, 3));
                if (r > 0) L.barsV.Add(Art.Spr(root, Art.Square, x + 30, y - 3, 52, 4, Color.white, 3));
                if (r < ROWS) L.barsV.Add(Art.Spr(root, Art.Square, x + 30, y + 63, 52, 4, Color.white, 3));
                L.headV = Pole(root, x - 8, y - 8, out L.glowV);
                L.headH = Pole(root, x + 68, y + 68, out L.glowH);
                lights.Add(L);
            }
        }

        Quad[] Pole(Transform root, float px, float py, out Quad glow)
        {
            int o = 1000 + (int)py;
            Art.Spr(root, Art.Circle, px + 5, py + 2, 12, 5, new Color(.06f, .09f, .13f, .25f), o);
            Art.Spr(root, Art.Square, px, py - 13, 2.5f, 26, Art.Hex("#2c3138"), o);
            Art.Spr(root, Art.Square, px, py - 31, 8, 18, Art.Hex("#1d2126"), o + 1);
            var heads = new Quad[3];
            for (int k = 0; k < 3; k++) heads[k] = Art.Spr(root, Art.Circle, px, py - 36 + k * 5, 3.8f, 3.8f, Color.white, o + 2);
            glow = Art.Spr(root, Art.Soft, px, py - 31, 30, 30, Color.clear, 5001);
            return heads;
        }

        void BuildGai()
        {
            var root = new GameObject("Police").transform;
            root.SetParent(districtRoot, false);
            var ga = city.D.gai;
            for (int i = 0; i < ga.GetLength(0); i++)
            {
                int c = ga[i, 0], r = ga[i, 1];
                var g = new Gai { center = new Vector2(c * CELL + 30, r * CELL + 30) };
                var off = new GameObject("Officer").transform;
                off.SetParent(root, false);
                float ox = c * CELL + 62, oy = r * CELL + 62;
                off.localPosition = Art.W(ox, oy);
                var sg = off.gameObject.AddComponent<SortingGroup>();
                sg.sortingOrder = 1000 + (int)oy;
                Art.Spr(off, Art.Circle, 3, 3, 18, 9, new Color(.06f, .09f, .13f, .3f), 0);
                Art.Spr(off, Art.Square, -2.5f, -4, 3, 8, Art.Hex("#24324a"), 1);
                Art.Spr(off, Art.Square, 2.5f, -4, 3, 8, Art.Hex("#24324a"), 1);
                Art.Spr(off, Art.Square, 0, -15, 12, 15, Art.Hex("#c8e04a"), 2);
                Art.Spr(off, Art.Square, 0, -16, 12, 2, Art.Hex("#e8eef4"), 3);
                Art.Spr(off, Art.Square, 0, -11, 12, 2, Art.Hex("#e8eef4"), 3);
                Art.Spr(off, Art.Circle, 0, -26, 9, 9, Art.Hex("#d9a87e"), 4);
                Art.Spr(off, Art.Square, 0, -30, 11, 4, Art.Hex("#2f4f7f"), 5);
                var bat = new GameObject("Baton").transform;
                bat.SetParent(off, false);
                bat.localPosition = Art.W(6, -18);
                for (int k = 0; k < 4; k++) Art.Spr(bat, Art.Square, 1.75f + k * 3.5f, 0, 3.5f, 3, k % 2 == 0 ? Color.white : Art.Hex("#111111"), 6);
                g.officer = off; g.baton = bat;
                g.bang = Art.Spr(off, Art.Circle, 0, -44, 10, 10, Art.Hex("#ff5d5d"), 7, "Alert");
                g.bang.enabled = false;

                var lrGo = new GameObject("Zone");
                lrGo.transform.SetParent(root, false);
                var lr = lrGo.AddComponent<LineRenderer>();
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.positionCount = 72;
                for (int k = 0; k < 72; k++)
                {
                    float a = k / 72f * Mathf.PI * 2f;
                    lr.SetPosition(k, Art.W(g.center.x + Mathf.Cos(a) * GAI_R, g.center.y + Mathf.Sin(a) * GAI_R));
                }
                lr.widthMultiplier = 0.025f;
                lr.sharedMaterial = Art.MeshMat;
                lr.sortingOrder = 4;
                lr.startColor = lr.endColor = Color.clear;
                g.zone = lr;
                gais.Add(g);
            }
        }

        void BuildCars()
        {
            var root = new GameObject("Cars").transform;
            root.SetParent(worldRoot, false);
            string[] cols = { "#e8e4dc", "#e9c46a", "#6f9fd1", "#d07a6f", "#7f8c99", "#f2efe8", "#86bf91" };
            int[,] hc = { { 0, 1 }, { 1, -1 }, { 3, 1 }, { 4, -1 } };
            int[,] vc = { { 1, 1 }, { 4, -1 }, { 6, 1 } };
            for (int i = 0; i < hc.GetLength(0); i++)
            {
                int r = hc[i, 0], d = hc[i, 1];
                AddCar(root, true, d, r, r * CELL + 30 + (d > 0 ? 13 : -13), (i * 331) % MW, 55 + i * 7, Art.Hex(cols[i]));
            }
            for (int i = 0; i < vc.GetLength(0); i++)
            {
                int c = vc[i, 0], d = vc[i, 1];
                AddCar(root, false, d, c, c * CELL + 30 + (d > 0 ? -13 : 13), (i * 263) % MH, 50 + i * 8, Art.Hex(cols[4 + i]));
            }
        }

        void AddCar(Transform root, bool h, int dir, int row, float lane, float p, float v, Color col)
        {
            var mb = new MB();
            DrawCar(mb, h, dir, col, cars.Count % 4 == 3);
            var mr = Art.MeshObj(root, "Car", mb.Build(), 1000);
            var car = new Car { t = mr.transform, mr = mr, h = h, dir = dir, row = row, lane = lane, p = p, startP = p, v = v, cur = v };
            car.glow = Art.Spr(root, Art.Soft, 0, 0, 60, 60, Color.clear, 5001, "Headlight");
            cars.Add(car);
        }

        /// <summary>
        /// Mashina chizmasi (2.5D): g'ildiraklar, qavariq kuzov, oynali salon, tom, faralar.
        /// u — mashina uzunligi bo'ylab (old tomon +u), v — eni bo'ylab, z — balandlik.
        /// van = true bo'lsa, Damas kabi uzun salonli kichik avtobus.
        /// </summary>
        static void DrawCar(MB mb, bool h, int dir, Color col, bool van)
        {
            const float L = 38, W = 20;
            float s = dir > 0 ? 1 : -1;
            // u,v,z -> xarita nuqtasi (to'g'ri to'rtburchak uchun)
            void RR(float u0, float u1, float v0, float v1, float z, float r, Color c)
            {
                float a0 = Mathf.Min(u0 * s, u1 * s), a1 = Mathf.Max(u0 * s, u1 * s);
                float x0, y0, w, hh;
                if (h) { x0 = a0; w = a1 - a0; y0 = v0; hh = v1 - v0; }
                else { y0 = a0; hh = a1 - a0; x0 = v0; w = v1 - v0; }
                mb.RRect(x0 + z * Art.OX, y0 + z * Art.OY, w, hh, Mathf.Min(r, Mathf.Min(w, hh) / 2), c);
            }
            void Stack(float u0, float u1, float v0, float v1, float z0, float z1, float r, Color c)
            {
                for (float z = z0; z < z1; z += 1f) RR(u0, u1, v0, v1, z, r, c);
            }

            var dark = Art.Shade(col, -.38f);
            var side = Art.Shade(col, -.2f);
            var glass = Art.Hex("#2b3d50");
            var glassHi = Art.Hex("#4f6f8c");

            // soya
            RR(-L / 2 + 2, L / 2 + 3, -W / 2 + 2, W / 2 + 4, 0, 7, new Color(.05f, .07f, .1f, .32f));
            // g'ildiraklar
            var tyre = Art.Hex("#16191c");
            foreach (float wu in new[] { -L / 2 + 8, L / 2 - 9 })
                foreach (float wv in new[] { -W / 2 - 1.2f, W / 2 - 4.8f })
                    Stack(wu - 4, wu + 4, wv, wv + 6, 0, 4, 2.5f, tyre);
            // kuzov yon devorlari va ustki qismi
            Stack(-L / 2, L / 2, -W / 2, W / 2, 2, 7, 6, dark);
            RR(-L / 2, L / 2, -W / 2, W / 2, 7, 6, side);
            RR(-L / 2 + .5f, L / 2 - .5f, -W / 2 + .5f, W / 2 - 1.5f, 8, 6, col);
            // yaltiroq chiziq (kuzov yon tomoni bo'ylab)
            RR(-L / 2 + 5, L / 2 - 6, -W / 2 + 1.5f, -W / 2 + 3.5f, 8.5f, 1, Art.Shade(col, .28f));
            // faralar va orqa chiroqlar
            var hl = Art.Hex("#fff3c4"); var tl = Art.Hex("#d9453a");
            RR(L / 2 - 3, L / 2 - .5f, -W / 2 + 2, -W / 2 + 6, 7.5f, 1.5f, hl);
            RR(L / 2 - 3, L / 2 - .5f, W / 2 - 6, W / 2 - 2, 7.5f, 1.5f, hl);
            RR(-L / 2 + .5f, -L / 2 + 2.5f, -W / 2 + 2, -W / 2 + 6, 7.5f, 1, tl);
            RR(-L / 2 + .5f, -L / 2 + 2.5f, W / 2 - 6, W / 2 - 2, 7.5f, 1, tl);

            // salon: oynalar (devor) + tom
            float c0 = van ? -L / 2 + 3 : -L / 2 + 9, c1 = van ? L / 2 - 8 : L / 2 - 12;
            Stack(c0, c1, -W / 2 + 2, W / 2 - 2, 8, 13, 4, glass);
            RR(c0, c1, -W / 2 + 2, W / 2 - 2, 13, 4, glassHi);           // old va orqa oyna ko'rinadi
            RR(c0 + (van ? 2 : 3.5f), c1 - (van ? 2.5f : 4.5f), -W / 2 + 2.5f, W / 2 - 2.5f, 13.5f, 3, Art.Shade(col, .1f)); // tom
            RR(c0 + 5, c1 - 7, -W / 2 + 4, -W / 2 + 6, 14, 1, new Color(1, 1, 1, .28f)); // tomdagi yaltiroq
            if (van)
            {
                // Damas: yon oynalar chizig'i
                for (int k = 0; k < 3; k++) RR(c0 + 3 + k * 8, c0 + 9 + k * 8, -W / 2 + 2, -W / 2 + 3, 10.5f, .5f, glassHi);
            }
        }

        void BuildCourier()
        {
            courier = new GameObject("Courier").transform;
            courier.SetParent(worldRoot, false);
            courierSG = courier.gameObject.AddComponent<SortingGroup>();
            courierShadow = Art.Spr(courier, Art.Circle, 4, 5, 36, 20, new Color(.06f, .09f, .13f, .32f), 0, "Shadow");
            courierBody = new GameObject("Body").transform;
            courierBody.SetParent(courier, false);
            courierBody.localPosition = Art.W(0, -6);
            ApplyLook();

            var dustRoot = new GameObject("Dust").transform;
            dustRoot.SetParent(worldRoot, false);
            for (int i = 0; i < 16; i++) { dust.Add(Art.Spr(dustRoot, Art.Circle, 0, 0, 8, 8, Color.clear, 999)); dustLife.Add(0); }

            arrow = Art.Spr(worldRoot, Art.Tri, 0, 0, 18, 18, Color.white, 6000, "Arrow");
            headlight = Art.Spr(worldRoot, Art.Soft, 0, 0, 80, 80, Color.clear, 5001, "CourierLight");

            var rg = new GameObject("Route");
            rg.transform.SetParent(worldRoot, false);
            route = rg.AddComponent<LineRenderer>();
            route.useWorldSpace = false;
            route.widthMultiplier = 0.04f;
            route.sharedMaterial = Art.MeshMat;
            route.sortingOrder = 5;
            route.numCornerVertices = 4;
            route.numCapVertices = 4;
            route.positionCount = 0;
            var rg2 = new GameObject("Route2");
            rg2.transform.SetParent(worldRoot, false);
            route2 = rg2.AddComponent<LineRenderer>();
            route2.useWorldSpace = false;
            route2.widthMultiplier = 0.035f;
            route2.sharedMaterial = Art.MeshMat;
            route2.sortingOrder = 5;
            route2.numCornerVertices = 3;
            route2.positionCount = 0;
        }

        /// <summary>Garajda tanlangan transport va ranglarni kuryerga qo'llaydi (tanani qaytadan chizadi).</summary>
        public void ApplyLook()
        {
            vehId = Save.Vehicle;
            if (!Save.OwnedVeh(vehId)) { vehId = "foot"; Save.Vehicle = "foot"; }
            speed = Data.Veh(vehId).speed;
            BuildBody();
            BuildPet();
        }

        Quad P(Sprite sp, float x, float y, float w, float h, Color c, int order, string name = "part")
        {
            var q = Art.Spr(courierBody, sp, x, y, w, h, c, order, name);
            bodyParts.Add(q); bodyBase.Add(c);
            return q;
        }

        /// <summary>Kuryer tanasi: x — oldinga (harakat yo'nalishi), y — eniga. Har bir transport o'zicha.</summary>
        void BuildBody()
        {
            if (courierBody == null) return;
            for (int i = courierBody.childCount - 1; i >= 0; i--) Destroy(courierBody.GetChild(i).gameObject);
            bodyParts.Clear(); bodyBase.Clear();
            footL = footR = flame = flameIn = null;

            var sc = Data.Look("scooter", Save.Equipped("scooter"));
            var hc = Data.Look("helmet", Save.Equipped("helmet"));
            var bg = Data.Look("bag", Save.Equipped("bag"));
            Color c0 = Art.Hex(sc.c0), c1 = Art.Hex(sc.c1), h0 = Art.Hex(hc.c0), h1 = Art.Hex(hc.c1), b0 = Art.Hex(bg.c0), b1 = Art.Hex(bg.c1);
            var tyre = Art.Hex("#1e1e1e");
            var jacket = Art.Shade(c1, -.1f);
            var skin = Art.Hex("#e0a782");
            string[] ik = { "#e85d75", "#ffd166", "#3d7fd0", "#8fd6a8" };

            void Bag(float x, float w, float h, int order)
            {
                P(Art.Square, x, 0, w, h, b1, order, "Bag");
                P(Art.Square, x, -h / 2 + 2, w, 4, b0, order + 1);
                if (bg.ikat) for (int i = 0; i < 4; i++) P(Art.Square, x, -h / 2 + 2 + i * (h / 4), w, h / 4, Art.Hex(ik[i]), order + 1, "Ikat");
            }
            void Helmet(float x, float d, int order)
            {
                P(Art.Circle, x, 0, d, d, h0, order, "Helmet");
                P(Art.Circle, x + d * .25f, 0, d * .45f, d * .45f, Art.Hex("#2b3a44"), order + 1, "Visor");
            }

            float shW = 36, shH = 20, shX = 4, shY = 5;
            switch (vehId)
            {
                case "foot":
                    footL = P(Art.Square, 0, -3.5f, 6, 4, Art.Hex("#2a2422"), 1, "FootL");
                    footR = P(Art.Square, 0, 3.5f, 6, 4, Art.Hex("#2a2422"), 1, "FootR");
                    P(Art.Circle, -1, 0, 10, 17, jacket, 3, "Body");
                    Bag(-6.5f, 8, 12, 4);
                    P(Art.Circle, 1.5f, 0, 9, 9, skin, 6, "Head");
                    P(Art.Circle, 1, 0, 9, 9, h0, 7, "Cap");
                    P(Art.Square, 5, 0, 3, 7, h1, 7, "Peak");
                    shW = 16; shH = 14; shX = 2; shY = 4;
                    break;
                case "bike":
                    P(Art.Square, 12, 0, 10, 3, tyre, 1); P(Art.Square, -12, 0, 10, 3, tyre, 1);
                    P(Art.Square, 0, 0, 22, 2.6f, c1, 2, "Frame");
                    P(Art.Square, 9, 0, 2.2f, 13, Art.Hex("#3a3a3a"), 3, "Bar");
                    P(Art.Circle, 13.5f, 0, 3.4f, 3.4f, Art.Hex("#fff6c8"), 3);
                    Bag(-12, 11, 11, 4);
                    P(Art.Circle, -1, 0, 9, 15, jacket, 6, "Rider");
                    Helmet(1.5f, 10, 7);
                    shW = 32; shH = 12; shX = 3; shY = 4;
                    break;
                case "car":
                    foreach (float wx in new[] { -10.5f, 10.5f }) foreach (float wy in new[] { -10.5f, 10.5f }) P(Art.Square, wx, wy, 8, 4, tyre, 1);
                    P(Art.Square, 0, 0, 36, 21, Art.Shade(c1, -.25f), 2, "Under");
                    P(Art.Square, .5f, -.5f, 34, 19, c1, 3, "Body");
                    P(Art.Square, 11, 0, 9, 15, Art.Shade(c1, .12f), 4, "Hood");
                    P(Art.Square, 5, 0, 5, 16, Art.Hex("#2b3d50"), 4, "Glass");
                    P(Art.Square, -3, 0, 12, 16, c0, 4, "Roof");
                    P(Art.Square, -10.5f, 0, 3, 15, Art.Hex("#2b3d50"), 4, "Rear");
                    P(Art.Circle, 17, -6, 3.4f, 3.4f, Art.Hex("#fff6c8"), 5); P(Art.Circle, 17, 6, 3.4f, 3.4f, Art.Hex("#fff6c8"), 5);
                    P(Art.Square, -17.3f, -6, 1.6f, 4, Art.Hex("#d9453a"), 5); P(Art.Square, -17.3f, 6, 1.6f, 4, Art.Hex("#d9453a"), 5);
                    Bag(-3, 10, 12, 6); // tomdagi yetkazish qutisi
                    shW = 42; shH = 26; shX = 4; shY = 6;
                    break;
                case "rocket":
                    flame = P(Art.Circle, -21, 0, 16, 9, Art.Hex("#ff9f43"), 1, "Flame");
                    flameIn = P(Art.Circle, -19, 0, 9, 5, Art.Hex("#ffe08a"), 2, "FlameIn");
                    P(Art.Square, -12, -8, 9, 5, Art.Hex("#c0443a"), 2, "Fin"); P(Art.Square, -12, 8, 9, 5, Art.Hex("#c0443a"), 2, "Fin");
                    P(Art.Circle, 0, 0, 36, 15, Art.Hex("#f4f1ea"), 3, "Hull");
                    P(Art.Circle, -1, 2.5f, 30, 8, Art.Hex("#d9d4ca"), 3, "HullShade");
                    var nose = P(Art.Tri, 18, 0, 10, 11, Art.Hex("#e85d4a"), 4, "Nose");
                    P(Art.Square, -8, 0, 4, 13, Art.Hex("#e85d4a"), 4, "Stripe");
                    P(Art.Circle, 6, 0, 9, 9, Art.Hex("#7ec8e3"), 5, "Window");
                    P(Art.Circle, 5.5f, 0, 5.5f, 5.5f, h0, 6, "Pilot");
                    P(Art.Circle, 8, -2, 2.5f, 2.5f, new Color(1, 1, 1, .7f), 7);
                    shW = 34; shH = 12; shX = 8; shY = 14;
                    break;
                default: // skuter
                    P(Art.Square, 13.5f, 0, 9, 7, tyre, 1); P(Art.Square, -13.5f, 0, 9, 7, tyre, 1);
                    P(Art.Square, 1, 0, 26, 14, c1, 2, "Scooter");
                    P(Art.Square, 1, -3, 22, 4, c0, 3);
                    P(Art.Square, 12, 0, 4, 12, Art.Hex("#d8eeea"), 3);
                    P(Art.Circle, 15, 0, 4.4f, 4.4f, Art.Hex("#fff6c8"), 4);
                    P(Art.Square, -1, 0, 10, 16, jacket, 4, "Rider");
                    Bag(-15, 16, 16, 5);
                    Helmet(2, 13, 7);
                    break;
            }
            courierShadow.transform.localPosition = Art.W(shX, shY);
            courierShadow.transform.localScale = new Vector3(shW * Art.K, shH * Art.K, 1);
        }

        /// <summary>Oyoqlar qadami, raketa alangasi, ko'rinmas rejimdagi shaffoflik.</summary>
        void AnimateBody(bool moving)
        {
            if (footL != null)
            {
                float st = moving ? Mathf.Sin(bob * .8f) * 4.5f : 0;
                footL.transform.localPosition = Art.W(st, -3.5f);
                footR.transform.localPosition = Art.W(-st, 3.5f);
            }
            if (flame != null)
            {
                float f = moving ? 1f + Mathf.Sin(Time.time * 40f) * .18f : .55f;
                flame.transform.localScale = new Vector3(16 * f * Art.K, 9 * (0.8f + .2f * f) * Art.K, 1);
                flame.transform.localPosition = Art.W(-14 - 7 * f, 0);
                flameIn.transform.localScale = new Vector3(9 * f * Art.K, 5 * Art.K, 1);
                flameIn.transform.localPosition = Art.W(-14 - 4.5f * f, 0);
            }
            float a = 1f;
            if (Ghost)
            {
                a = .38f + .14f * Mathf.Sin(Time.time * 6f);
                if (ghost < 5f && Mathf.FloorToInt(Time.time * 6f) % 2 == 0) a = .75f; // tugashidan oldin ogohlantirish
            }
            for (int i = 0; i < bodyParts.Count; i++)
            {
                var b = bodyBase[i];
                bodyParts[i].color = Ghost ? new Color(Mathf.Lerp(b.r, .7f, .35f), Mathf.Lerp(b.g, .9f, .35f), Mathf.Lerp(b.b, 1f, .35f), b.a * a) : b;
            }
            var shc = courierShadow.color; shc.a = Ghost ? .12f : .32f; courierShadow.color = shc;
        }

        // ---------------------------------------------------------------- hamroh
        void BuildPet()
        {
            if (pet == null)
            {
                pet = new GameObject("Pet").transform;
                pet.SetParent(worldRoot, false);
                petSG = pet.gameObject.AddComponent<SortingGroup>();
                petBody = new GameObject("PetBody").transform;
                petBody.SetParent(pet, false);
            }
            petId = Save.Equipped("pet");
            for (int i = petBody.childCount - 1; i >= 0; i--) Destroy(petBody.GetChild(i).gameObject);
            for (int i = pet.childCount - 1; i >= 0; i--) if (pet.GetChild(i) != petBody) Destroy(pet.GetChild(i).gameObject);
            pet.gameObject.SetActive(petId != "none");
            if (petId == "none") return;
            var lk = Data.Look("pet", petId);
            Color c0 = Art.Hex(lk.c0), c1 = Art.Hex(lk.c1);
            bool dog = petId == "dog";
            Art.Spr(pet, Art.Circle, 1, 3, dog ? 20 : 16, dog ? 11 : 9, new Color(.06f, .09f, .13f, .28f), 0, "Shadow");
            // dum
            var tail = Art.Spr(petBody, Art.Square, dog ? -10 : -11, 0, dog ? 6 : 10, 2.4f, c1, 1, "Tail");
            tail.transform.localRotation = Quaternion.Euler(0, 0, dog ? 0 : 25);
            Art.Spr(petBody, Art.Circle, -1, 0, dog ? 17 : 14, dog ? 10 : 8.5f, c0, 2, "Body");
            if (petId == "cat") for (int i = 0; i < 3; i++) Art.Spr(petBody, Art.Square, -5 + i * 3.5f, 0, 1.4f, 7, c1, 3, "Stripe");
            Art.Spr(petBody, Art.Circle, dog ? 8 : 7, 0, dog ? 9 : 8.5f, dog ? 8.5f : 8.5f, c0, 4, "Head");
            if (dog)
            {
                Art.Spr(petBody, Art.Circle, 7, -4.5f, 4, 5.5f, c1, 5, "Ear"); Art.Spr(petBody, Art.Circle, 7, 4.5f, 4, 5.5f, c1, 5, "Ear");
                Art.Spr(petBody, Art.Circle, 12.5f, 0, 3, 3, Art.Hex("#2a1c18"), 5, "Nose");
            }
            else
            {
                var e1 = Art.Spr(petBody, Art.Tri, 6, -3.6f, 4, 3.6f, c1, 5, "Ear"); e1.transform.localRotation = Quaternion.Euler(0, 0, 70);
                var e2 = Art.Spr(petBody, Art.Tri, 6, 3.6f, 4, 3.6f, c1, 5, "Ear"); e2.transform.localRotation = Quaternion.Euler(0, 0, -70);
                Art.Spr(petBody, Art.Circle, 11, 0, 1.6f, 1.6f, Art.Hex("#e88a8a"), 5, "Nose");
            }
        }

        void UpdatePet(float dt, bool moving)
        {
            if (pet == null || petId == "none" || !pet.gameObject.activeSelf) return;
            // kuryer orqasidan ergashadi
            var back = pos - new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * (vehId == "car" ? 30 : 24) + new Vector2(-Mathf.Sin(ang), Mathf.Cos(ang)) * 4;
            var d = back - petPos;
            if (d.sqrMagnitude > 140 * 140) petPos = back; // juda uzoqlashsa (masalan, smena boshida) yoniga o'tadi
            petPos += d * Mathf.Min(1, dt * 7f);
            bool pm = d.magnitude > 2f;
            if (pm) petAng = Mathf.LerpAngle(petAng * Mathf.Rad2Deg, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg, Mathf.Min(1, dt * 10)) * Mathf.Deg2Rad;
            petBob += dt * (pm ? 18f : 3f);
            float hop = pm ? Mathf.Abs(Mathf.Sin(petBob)) * 2.2f : 0;
            pet.localPosition = Art.W(petPos.x, petPos.y - hop);
            petBody.localRotation = Quaternion.Euler(0, 0, -petAng * Mathf.Rad2Deg);
            petSG.sortingOrder = 1000 + (int)petPos.y;
        }

        /// <summary>Ko'rinmas rejimni yoqish: reklama ko'rilgandan keyin (yoki «Reklamasiz» bo'lsa darhol).</summary>
        public void TryGhost()
        {
            if (state != St.Play || Ghost || ghostUses >= GHOST_PER_SHIFT) return;
            SetPause(true);
            Ads.ShowRewarded(ok =>
            {
                if (state == St.Pause) SetPause(false);
                if (!ok) { hud.Toast(Loc.Get("adFail"), UI.Warn); return; }
                ghost = GHOST_TIME; ghostUses++; inv = 0;
                Daily.Track("ghost");
                sfx.Arp(659, 880, 1175);
                hud.Toast(Loc.Get("ghostOn"), UI.Teal);
            });
        }

        void BuildNight()
        {
            goldSr = Art.Spr(cam.transform, Art.Square, 0, 0, 1, 1, Color.clear, 4999, "Golden");
            nightSr = Art.Spr(cam.transform, Art.Square, 0, 0, 1, 1, Color.clear, 5000, "Night");
            goldSr.transform.localPosition = new Vector3(0, 0, 10);
            nightSr.transform.localPosition = new Vector3(0, 0, 10);
        }

        /// <summary>Kechki/tungi qatlamlarni yoqadi yoki o'chiradi (sahna vaqtida o'chiq).</summary>
        public void Overlays(bool on)
        {
            if (nightSr != null) nightSr.enabled = on;
            if (goldSr != null) goldSr.enabled = on;
        }

        void BuildPinCanvas()
        {
            var go = new GameObject("Pins", typeof(RectTransform));
            go.transform.SetParent(worldRoot, false);
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            cv.sortingOrder = 6000;
            go.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = UI.WorldDpu(MH / ZOOM);
            pinRoot = (RectTransform)go.transform;
            pinRoot.pivot = new Vector2(0, 1);
            pinRoot.sizeDelta = new Vector2(MW, MH);
            pinRoot.localScale = Vector3.one * Art.K;
            pinRoot.localPosition = Vector3.zero;
        }

        // ========================= OQIM =========================
        public void ShowMenu()
        {
            state = St.Menu;
            Weather.Clear();
            storyMode = false;
            ClearOrders();
            courier.gameObject.SetActive(false);
            arrow.enabled = false;
            route.positionCount = 0;
            if (city == null || city.D.id != Save.District) BuildWorld(Save.District);
            hud.ShowMenu();
        }

        public void StartStoryDay()
        {
            if (Save.Chapter >= 5) Save.Chapter = 0;
            storyMode = true;
            BuildWorld("markaz");
            PlayCut(Data.Story[Save.Chapter].intro, StartShift);
        }

        public void StartFree()
        {
            storyMode = false;
            string d = Save.District;
            BuildWorld(d);
            var dist = Data.Districts[city.D.id];
            if (dist.intro != null && !Save.Seen(dist.id)) { Save.SetSeen(dist.id); PlayCut(dist.intro, StartShift); }
            else StartShift();
        }

        public void StartShift()
        {
            ClearOrders();
            var ch = storyMode && Save.Chapter < 5 ? Data.Story[Save.Chapter] : null;
            story = ch != null ? ch.story : null;
            storySpawned = storyDone = false;
            time = shiftLen = ch != null ? STORY_SHIFT : FREE_SHIFT;
            money = fines = done = lates = hits = reds = tips = xpGain = 0;
            rating = 5f; holdT = 0;
            lvlUps.Clear();
            nextId = 1; spawnT = 2f;
            speed = Data.Veh(Save.Vehicle).speed;
            ghost = 0; ghostUses = 0;
            lvlRewards.Clear();
            tut = Save.TutDone ? -1 : 0; tutDist = 0; tutT = 0;
            bagMax = Data.Bags[Mathf.Clamp(Save.Up("bag"), 0, Data.Bags.Length - 1)];
            ins = Save.Up("ins") > 0; nav = Save.Up("nav") > 0;
            Weather.Roll();
            mult = city.D.mult * Weather.PayBonus;
            pos = new Vector2(3 * CELL + 30, 2 * CELL + 30);
            ang = 0; stun = 0; inv = 0; mv = Vector2.zero; inX = "3,2";
            foreach (var c in cars) { c.p = c.startP; c.cur = c.v; }
            ApplyLook();
            courier.gameObject.SetActive(true);
            petPos = pos - new Vector2(24, 0);
            state = St.Play;
            hud.ShowPlay();
            if (Weather.Navruz) hud.Toast(Loc.Get("navruz"), UI.Gold);
            else if (Weather.NewYear) hud.Toast(Loc.Get("newyear"), UI.Gold);
            else if (Weather.LocKey != null) hud.Toast(Loc.Get(Weather.LocKey), UI.Teal);
            SnapCamera();
        }

        public void SetPause(bool v)
        {
            if (state != St.Play && state != St.Pause) return;
            state = v ? St.Pause : St.Play;
            padU = padD = padL = padR = false; joy = Vector2.zero;
            if (v) hud.ShowPause(); else hud.ShowPlay();
        }

        public void QuitToMenu()
        {
            if (state == St.Play || state == St.Pause) Bank();
            ShowMenu();
        }

        public void SetLang(int l)
        {
            Loc.L = Mathf.Clamp(l, 0, 2);
            Save.Lang = Loc.L;
            city.RefreshLabels();
            hud.Rebuild();
        }

        int Bank()
        {
            int net = money - fines;
            Save.Wallet = Save.Wallet + net;
            return net;
        }

        void EndShift()
        {
            state = St.End;
            // yulduzlar: smena uzunligiga qarab yetkazilgan buyurtmalar soni
            float k = shiftLen / FREE_SHIFT;
            lastStars = done >= Mathf.CeilToInt(10 * k) ? 3 : done >= Mathf.CeilToInt(6 * k) ? 2 : done >= Mathf.CeilToInt(3 * k) ? 1 : 0;
            Daily.Track("shift");
            if (fines == 0 && done > 0) Daily.Track("clean");
            if (lastStars >= 3) Daily.Track("stars3");
            if (tut >= 0) { tut = -1; Save.TutDone = true; }
            int net = Bank();
            Save.PlayTime = Save.PlayTime + Mathf.RoundToInt(shiftLen);
            Save.TotalDone = Save.TotalDone + done;
            Save.TotalEarned = Save.TotalEarned + Mathf.Max(0, money);
            Cloud.AfterShift(done, fines, lastStars);
            Notify.AskPermission();
            bool rec = net > Save.Best && net > 0;
            if (rec) Save.Best = net;
            ClearOrders();
            arrow.enabled = false;
            route.positionCount = 0;
            hud.ShowEnd(net, rec, storyMode);
        }

        /// <summary>Natijalar oynasidagi "davom etish" tugmasi.</summary>
        public void AfterEnd()
        {
            Ads.MaybeInterstitial(() =>
            {
                if (!storyMode) { StartFree(); return; }
                PlayCut(Data.Story[Save.Chapter].outro, () =>
                {
                    Save.Chapter = Save.Chapter + 1;
                    if (Save.Chapter >= 5) ShowMenu(); else StartStoryDay();
                });
            });
        }

        /// <summary>Natijalar oynasidan bosh menyuga.</summary>
        public void EndToMenu()
        {
            Ads.MaybeInterstitial(() =>
            {
                if (!storyMode) { ShowMenu(); return; }
                PlayCut(Data.Story[Save.Chapter].outro, () => { Save.Chapter = Save.Chapter + 1; ShowMenu(); });
            });
        }

        public void PlayCut(Shot[] shots, Action done)
        {
            var prev = state;
            state = St.Cut;
            padU = padD = padL = padR = false; joy = Vector2.zero;
            arrow.enabled = false;
            hud.ShowCut();
            cut.Play(shots, () =>
            {
                SnapCamera();
                if (prev == St.Play) { state = St.Play; hud.ShowPlay(); }
                done?.Invoke();
            });
        }

        public void PlayReno(Reno r)
        {
            var lines = new List<string[]>(r.lines);
            bool all = true;
            foreach (var x in Data.Renos) if (!Save.RenoDone(x.id)) all = false;
            if (all) lines.Add(Data.RenoAll);
            var shot = new Shot { bg = r.id == "sign" ? "street" : "office", chars = new[] { "jasur", "karim" }, lines = lines.ToArray() };
            bool fromEnd = state == St.End;
            PlayCut(new[] { shot }, () => { state = fromEnd ? St.End : St.Menu; hud.ShowGarage(); });
        }

        void ClearOrders()
        {
            foreach (var o in orders) if (o.pin != null) Destroy(o.pin.gameObject);
            orders.Clear();
            foreach (var f in fx) if (f.rt != null) Destroy(f.rt.gameObject);
            fx.Clear();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && state == St.Play) SetPause(true);
        }

        public static string UnlockText(int L)
        {
            var u = new List<string>();
            foreach (var e in Data.Extra) if (e.lvl == L) u.Add(Loc.Get("restU", Loc.Tx(e.name)));
            foreach (var id in Data.DistrictOrder) { var d = Data.Districts[id]; if (d.lvl == L && L > 1) u.Add(Loc.Get("distU", Loc.Tx(d.name))); }
            foreach (var v in Data.Vehicles) if (v.lvl == L && !v.iap && L > 0) u.Add(Loc.Get("vehU", Loc.Tx(v.name)));
            foreach (var cat in Data.LookCats) foreach (var it in Data.Looks[cat]) if (it.lvl == L) u.Add(Loc.Tx(it.name) + " (" + Loc.Tx(Data.LookTitle[cat]).ToLower() + ")");
            return string.Join(", ", u);
        }

        // ========================= KADR =========================
        void Update()
        {
            while (true)
            {
                Action job = null;
                lock (mainQ) { if (mainQ.Count > 0) job = mainQ.Dequeue(); }
                if (job == null) break;
                try { job(); } catch (Exception e) { Debug.LogException(e); }
            }
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (BackPressed())
            {
                if (state == St.Play) SetPause(true);
                else if (state == St.Pause) SetPause(false);
                else if (state == St.Cut) cut.Skip();
                else if (state == St.Menu) { if (hud.MenuIsRoot) Application.Quit(); else hud.Back(); }
                else if (state == St.End) hud.Back();
            }
            if (state == St.Cut) { hud.Tick(dt); return; }
            if (state != St.Pause) LT += dt;
            if (state == St.Play) Tick(dt);
            else if (state != St.Pause) StepCars(dt, false);

            UpdateVisuals(dt);
            UpdateCamera(dt);
            uiTick -= dt;
            if (uiTick <= 0) { uiTick = .2f; hud.Refresh(); }
            hud.Tick(dt);
        }

        Vector2 ReadMove()
        {
            float x = 0, y = 0;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.rightArrowKey.isPressed || k.dKey.isPressed) x += 1;
                if (k.leftArrowKey.isPressed || k.aKey.isPressed) x -= 1;
                if (k.downArrowKey.isPressed || k.sKey.isPressed) y += 1;
                if (k.upArrowKey.isPressed || k.wKey.isPressed) y -= 1;
            }
#else
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y += 1;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y -= 1;
#endif
            if (padR) x += 1;
            if (padL) x -= 1;
            if (padD) y += 1;
            if (padU) y -= 1;
            if (joy.sqrMagnitude > .04f && x == 0 && y == 0)
            {
                // joystik: asosiy yo'nalish + kuchli bo'lsa ikkinchi yo'nalish (chorrahada burilish uchun)
                float ax = Mathf.Abs(joy.x), ay = Mathf.Abs(joy.y);
                if (ax >= ay) { x = Mathf.Sign(joy.x); if (ay > .45f) y = Mathf.Sign(joy.y); }
                else { y = Mathf.Sign(joy.y); if (ax > .45f) x = Mathf.Sign(joy.x); }
            }
            return new Vector2(Mathf.Clamp(x, -1, 1), Mathf.Clamp(y, -1, 1));
        }

        static bool BackPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.pKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool AdvancePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
        }

        static bool OnRoad(float x, float y)
        {
            if (x < 12 || y < 12 || x > MW - 12 || y > MH - 12) return false;
            float mx = x % CELL, my = y % CELL;
            return (mx >= 12 && mx <= 48) || (my >= 12 && my <= 48);
        }

        // ---------------------------------------------------------------- bitta qatorda harakat
        const float LANE = 13f;   // yo'l o'rtasidan o'ng qatorgacha masofa (mashinalar ham shu qatorda yuradi)
        const float SNAP = 17f;   // chorrahada burilish uchun ruxsat etilgan chetlanish
        bool axisH = true;        // hozir gorizontal yo'ldami

        /// <summary>
        /// Kuryer faqat yo'lning o'ng qatori bo'ylab yuradi (mashinalar kabi).
        /// Burilish faqat chorrahada; yon tomonga bosilsa, eng yaqin chorrahagacha avtomatik boradi.
        /// </summary>
        void LaneMove(Vector2 m, float sp)
        {
            float hy = Mathf.Round((pos.y - 30) / CELL) * CELL + 30;   // eng yaqin gorizontal yo'l o'qi
            float vx = Mathf.Round((pos.x - 30) / CELL) * CELL + 30;   // eng yaqin vertikal yo'l o'qi
            bool onH = Mathf.Abs(pos.y - hy) <= SNAP, onV = Mathf.Abs(pos.x - vx) <= SNAP;
            bool wantH = Mathf.Abs(m.x) > .1f, wantV = Mathf.Abs(m.y) > .1f;

            // ikkala yo'nalish bosilsa: chorrahada yangi yo'nalishga buriladi, aks holda hozirgisida davom etadi
            bool goH;
            if (wantH && wantV) goH = axisH ? !(onV) : onH;
            else goH = wantH;
            if (wantH && wantV && goH && !onH) goH = false;
            if (wantH && wantV && !goH && !onV) goH = true;

            if (goH)
            {
                if (!onH)
                {
                    // vertikal yo'ldamiz: eng yaqin chorrahagacha boramiz
                    float d = hy - pos.y;
                    pos.y += Mathf.Sign(d) * Mathf.Min(sp, Mathf.Abs(d));
                    EaseTo(ref pos.x, vx + (d > 0 ? -LANE : LANE), sp);
                    axisH = false;
                    return;
                }
                float dir = Mathf.Sign(m.x);
                pos.x = Mathf.Clamp(pos.x + dir * sp, 30 - LANE, MW - 30 + LANE);
                EaseTo(ref pos.y, hy + (dir > 0 ? LANE : -LANE), sp * .9f);
                axisH = true;
            }
            else
            {
                if (!onV)
                {
                    float d = vx - pos.x;
                    pos.x += Mathf.Sign(d) * Mathf.Min(sp, Mathf.Abs(d));
                    EaseTo(ref pos.y, hy + (d > 0 ? LANE : -LANE), sp);
                    axisH = true;
                    return;
                }
                float dir = Mathf.Sign(m.y);
                pos.y = Mathf.Clamp(pos.y + dir * sp, 30 - LANE, MH - 30 + LANE);
                EaseTo(ref pos.x, vx + (dir > 0 ? -LANE : LANE), sp * .9f);
                axisH = false;
            }
        }

        static void EaseTo(ref float v, float target, float step)
        {
            float d = target - v;
            v += Mathf.Sign(d) * Mathf.Min(step, Mathf.Abs(d));
        }

        void Tick(float dt)
        {
            time = Mathf.Max(0, time - dt);
            bool storyPending = story != null && !storyDone;
            if (time <= 0)
            {
                if (!storyPending) { EndShift(); return; }
                holdT -= dt;
                if (holdT <= 0) { holdT = 7; hud.Toast(Loc.Get("hold"), GOLD); }
            }
            else
            {
                spawnT -= dt;
                if (spawnT <= 0) { SpawnOrder(); spawnT = 8f + Random.value * 5f - (bagMax - 3) * 1.2f; }
            }
            if (story != null && !storySpawned && shiftLen - time >= 10) SpawnStory();
            if (stun > 0) stun -= dt;
            if (ghost > 0)
            {
                ghost -= dt;
                if (ghost <= 0) { ghost = 0; hud.Toast(Loc.Get("ghostEnd"), UI.Muted); sfx.Chime(392, .3f); }
            }
            if (inv > 0) inv -= dt;
            foreach (var g in gais) if (g.alert > 0) g.alert -= dt;

            // harakat
            var m = ReadMove();
            moving = false;
            if (stun <= 0 && m.sqrMagnitude > 0.01f)
            {
                float sp = speed * dt;
                var old = pos;
                LaneMove(m, sp);
                if (pos != old)
                {
                    moving = true;
                    if (tut == 0) { tutDist += (pos - old).magnitude; if (tutDist > 90) tut = 1; }
                    mv = pos - old;
                    float ta = Mathf.Atan2(mv.y, mv.x);
                    float da = Mathf.DeltaAngle(ang * Mathf.Rad2Deg, ta * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                    ang += da * Mathf.Min(1f, dt * 14f);
                }
                bob += dt * 14f;
            }

            if (tut == 3) { tutT += dt; if (tutT > 6f) { tut = -1; Save.TutDone = true; } }
            CheckRedLight();
            if (state != St.Play) return;
            StepCars(dt, true);

            // buyurtmalar
            int inBag = 0;
            foreach (var o in orders) if (o.picked && !o.story) inBag++;
            for (int i = orders.Count - 1; i >= 0; i--)
            {
                var o = orders[i];
                o.left -= dt;
                if (o.left <= 0 && !o.late)
                {
                    o.late = true; lates++; rating = Mathf.Max(1, rating - .2f);
                    hud.Toast(Loc.Get("late", o.id), UI.Warn);
                }
                if (!o.picked && Vector2.Distance(pos, o.rest.door) < 30)
                {
                    if (o.story || inBag < bagMax)
                    {
                        o.picked = true; if (!o.story) inBag++;
                        if (tut >= 0 && tut <= 1) tut = 2;
                        sfx.Chime(659, .4f);
                        AddFx(o.rest.door + new Vector2(0, -30), Loc.Get("picked"), Color.white);
                    }
                    else hud.ToastOnce(Loc.Get("bagFull"), UI.Warn);
                }
                else if (o.picked && Vector2.Distance(pos, o.house.door) < 30)
                {
                    int tip = 0, fine = 0, xp = 10;
                    if (o.story) { tip = GIFT; xp = 30; }
                    else if (o.late) { fine = Fine(FINE_LATE); fines += fine; xp = 6; }
                    else
                    {
                        float r = o.left / o.limit;
                        tip = r > .5f ? Mathf.RoundToInt(o.pay * .3f / 500f) * 500 : r > .25f ? Mathf.RoundToInt(o.pay * .15f / 500f) * 500 : 0;
                        if (tip > 0) xp = 15;
                        rating = Mathf.Min(5, rating + .1f);
                    }
                    money += o.pay + tip; tips += tip; done++;
                    Daily.Track("deliver"); Daily.Track("earn", o.pay + tip);
                    if (tip > 0 && !o.story) Daily.Track("tip");
                    if (tut >= 0 && tut <= 2) { tut = 3; tutT = 0; }
                    sfx.Chime(784, .35f); sfx.Arp(1047);
                    string s = "+" + Loc.Num(o.pay + tip) + (o.story ? Loc.Get("withGift") : tip > 0 ? Loc.Get("withTip") : "") + (fine > 0 ? "  (−" + Loc.Num(fine) + ")" : "");
                    AddFx(o.house.door + new Vector2(0, -34), s, o.story ? GOLD : fine > 0 ? UI.Warn : UI.Ok);
                    GainXp(xp, o.house.door);
                    if (o.pin != null) Destroy(o.pin.gameObject);
                    orders.RemoveAt(i);
                    if (o.story)
                    {
                        storyDone = true; rating = Mathf.Min(5, rating + .2f);
                        PlayCut(Data.Story[Save.Chapter].mid, null);
                        return;
                    }
                }
            }
        }

        int Fine(int amount) => Mathf.RoundToInt(amount * (ins ? .5f : 1f));

        void GainXp(int n, Vector2 at)
        {
            int before = Save.Level;
            Save.XP = Save.XP + n;
            xpGain += n;
            AddFx(at + new Vector2(0, -54), "+" + n + " XP", UI.Teal);
            int after = Save.Level;
            if (after > before)
            {
                int reward = 0;
                for (int L = before + 1; L <= after; L++) { lvlUps.Add(L); int r = Save.LevelReward(L); lvlRewards.Add(r); reward += r; }
                Save.Wallet += reward;
                string u = UnlockText(after);
                hud.Toast(Loc.Get("newLevel", after) + " · " + Loc.Get("lvlReward", Loc.Money(reward)) + (u.Length > 0 ? " " + Loc.Get("unlocked", u) : ""), UI.Teal);
                sfx.Arp(523, 659, 784, 1047);
            }
        }

        void SpawnOrder()
        {
            int regular = 0;
            foreach (var o in orders) if (!o.story) regular++;
            if (regular >= MAXO + (bagMax - 3)) return;
            var busy = new HashSet<Block>();
            foreach (var o in orders) busy.Add(o.house);
            var free = new List<Block>();
            foreach (var hb in city.houses)
            {
                if (busy.Contains(hb) || Vector2.Distance(hb.door, pos) <= 120) continue;
                if (storyMode && (hb.key == Data.HOME_R || hb.key == Data.HOME_S)) continue;
                free.Add(hb);
            }
            if (free.Count == 0) return;
            var house = free[Random.Range(0, free.Count)];
            var rest = city.rests[Random.Range(0, city.rests.Count)];
            float d1 = Mathf.Abs(pos.x - rest.door.x) + Mathf.Abs(pos.y - rest.door.y);
            float d2 = Mathf.Abs(rest.door.x - house.door.x) + Mathf.Abs(rest.door.y - house.door.y);
            var ord = new Order
            {
                id = nextId++,
                rest = rest,
                house = house,
                dish = Random.Range(0, rest.rest.dishes.Length),
                limit = Mathf.Round((d1 + d2) / 140f * 2.8f + 28f),
                pay = Mathf.RoundToInt((10000 + d2 / 100f * 1800) * mult / 500f) * 500,
                born = Time.time
            };
            ord.left = ord.limit;
            ord.color = Art.Hex(OrderCols[(ord.id - 1) % OrderCols.Length]);
            ord.pin = MakePin(ord);
            orders.Add(ord);
            sfx.Chime(784, .5f);
            hud.Toast(Loc.Get("newOrder", ord.id, Loc.Tx(rest.rest.name)), UI.Accent);
        }

        void SpawnStory()
        {
            var rest = city.ByKey(story.rest);
            var house = city.ByKey(story.house);
            storySpawned = true;
            if (rest == null || house == null || rest.type != "rest") return;
            float d2 = Mathf.Abs(rest.door.x - house.door.x) + Mathf.Abs(rest.door.y - house.door.y);
            var ord = new Order
            {
                id = 0, story = true, rest = rest, house = house, storyDish = story.dish,
                limit = float.PositiveInfinity, left = float.PositiveInfinity,
                pay = Mathf.RoundToInt((10000 + d2 / 100f * 1800) / 500f) * 500,
                color = GOLD, born = Time.time
            };
            ord.pin = MakePin(ord);
            orders.Add(ord);
            sfx.Chime(659, .6f); sfx.Arp(988);
            hud.Toast(Loc.Get("storyOrder", Loc.Tx(rest.rest.name), house.Label), GOLD);
        }

        RectTransform MakePin(Order o)
        {
            var rt = UI.Node("Pin", pinRoot);
            UI.Place(rt, new Vector2(0, 1), new Vector2(.5f, .5f), Vector2.zero, new Vector2(34, 34));
            var stem = UI.Pic(rt, Art.Square, o.color, "stem");
            UI.Place(stem.rectTransform, new Vector2(.5f, 0), new Vector2(.5f, 1), new Vector2(0, 2), new Vector2(3, 20));
            if (o.story)
            {
                var glow = UI.Pic(rt, Art.Soft, new Color(1, .82f, .4f, .6f), "glow");
                UI.Place(glow.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(70, 70));
                var st = UI.Pic(rt, Art.Star, Color.white, "starOutline");
                UI.Place(st.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(42, 42));
                var st2 = UI.Pic(rt, Art.Star, GOLD, "star");
                UI.Place(st2.rectTransform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(34, 34));
                return rt;
            }
            var outer = UI.Pic(rt, Art.Circle, Color.white, "outer");
            UI.Fill(outer.rectTransform);
            var inner = UI.Pic(rt, Art.Circle, o.color, "inner");
            UI.Fill(inner.rectTransform, 3);
            var t = UI.Label(rt, o.id.ToString(), 15, Art.Hex("#14222a"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UI.Fill(t.rectTransform);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return rt;
        }

        /// <summary>Olingan buyurtma belgisini uy shakliga (yumaloq kvadrat) o'zgartiradi.</summary>
        void PinToHouse(Order o)
        {
            if (o.story || o.pin == null) return;
            var outer = o.pin.Find("outer");
            var inner = o.pin.Find("inner");
            if (outer == null || inner == null) return;
            foreach (var im in new[] { outer.GetComponent<Image>(), inner.GetComponent<Image>() })
            {
                im.sprite = Art.Round; im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = 3f;
            }
            o.pin.name = "PinHouse";
        }

        void AddFx(Vector2 at, string s, Color c)
        {
            var t = UI.Label(pinRoot, s, 15, c, TextAnchor.MiddleCenter, FontStyle.Bold);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = Art.Hex("#14222a");
            ol.effectDistance = new Vector2(1.5f, -1.5f);
            UI.Place(t.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(at.x, -at.y), new Vector2(240, 24));
            fx.Add(new Fx { rt = t.rectTransform, t = t, life = 1.8f, p = at });
        }

        // ========================= SVETOFOR =========================
        char HS(TLight L) { float k = Mathf.Repeat(LT + L.off, CYCLE); return k < 5 ? 'g' : k < 6 ? 'y' : 'r'; }
        char VS(TLight L) { float k = Mathf.Repeat(LT + L.off, CYCLE); return (k >= 6 && k < 11) ? 'g' : k >= 11 ? 'y' : 'r'; }
        TLight LightAt(int c, int r) { foreach (var L in lights) if (L.c == c && L.r == r) return L; return null; }

        void CheckRedLight()
        {
            int c = Mathf.FloorToInt(pos.x / CELL), r = Mathf.FloorToInt(pos.y / CELL);
            bool inSq = pos.x - c * CELL < 60 && pos.y - r * CELL < 60;
            string key = inSq ? c + "," + r : null;
            if (key != null && key != inX && !Ghost)
            {
                var L = LightAt(c, r);
                if (L != null && mv.sqrMagnitude > 0)
                {
                    bool horiz = Mathf.Abs(mv.x) >= Mathf.Abs(mv.y);
                    if ((horiz ? HS(L) : VS(L)) == 'r')
                    {
                        reds++;
                        var lc = new Vector2(c * CELL + 30, r * CELL + 30);
                        Gai g = null;
                        foreach (var gg in gais) if (Vector2.Distance(gg.center, lc) <= GAI_R) { g = gg; break; }
                        if (g != null)
                        {
                            int fine = Fine(FINE_RED);
                            fines += fine; g.alert = 2f; sfx.Whistle();
                            hud.Toast(Loc.Get("redGai", Loc.Money(fine)), UI.Soft);
                            AddFx(pos + new Vector2(0, -30), "−" + Loc.Num(fine), UI.Soft);
                            if (!Save.GaiMet) { Save.GaiMet = true; inX = key; PlayCut(Data.GaiCut, null); return; }
                        }
                        else hud.Toast(Loc.Get("redFree"), UI.Warn);
                    }
                }
            }
            inX = key;
        }

        void StepCars(float dt, bool collide)
        {
            foreach (var c in cars)
            {
                float target = c.v, front = c.p + c.dir * 18f;
                int ci; float d;
                if (c.dir > 0) { ci = Mathf.CeilToInt((front - 2) / CELL); d = ci * CELL - front; }
                else { ci = Mathf.FloorToInt((front + 2 - 60) / CELL); d = front - (ci * CELL + 60); }
                var L = c.h ? LightAt(ci, c.row) : LightAt(c.row, ci);
                if (L != null && d > -2 && d < 40)
                {
                    char s = c.h ? HS(L) : VS(L);
                    if (s == 'r' || (s == 'y' && d > 10)) target = c.v * Mathf.Clamp01((d - 3) / 30f);
                }
                c.cur += (target - c.cur) * Mathf.Min(1f, dt * 4f);
                c.p += c.dir * c.cur * dt;
                float lim = c.h ? MW : MH;
                if (c.p > lim + 30) c.p = -30;
                if (c.p < -30) c.p = lim + 30;
                if (!collide) continue;
                float cx = c.h ? c.p : c.lane, cy = c.h ? c.lane : c.p, hw = c.h ? 18 : 10, hh = c.h ? 10 : 18;
                if (!Ghost && inv <= 0 && c.cur > 8 && Mathf.Abs(cx - pos.x) < hw + 8 && Mathf.Abs(cy - pos.y) < hh + 8)
                {
                    int fine = Fine(FINE_CAR);
                    stun = ins ? .3f : .6f; inv = 2.5f; hits++; fines += fine;
                    sfx.Chime(220, .5f);
                    hud.Toast(Loc.Get("fineCar", Loc.Money(fine)), UI.Soft);
                    AddFx(pos + new Vector2(0, -30), "−" + Loc.Num(fine), UI.Soft);
                }
            }
        }

        // ========================= KO'RINISH =========================
        public float Phase => (state == St.Play || state == St.Pause) ? Mathf.Clamp01(1f - time / shiftLen) : .62f;

        static Color LightCol(char s) => s == 'g' ? Art.Hex("#5fd08a") : s == 'y' ? Art.Hex("#f2c14e") : Art.Hex("#ff5d5d");

        public Order Urgent()
        {
            Order u = null;
            int inBag = 0;
            foreach (var o in orders) if (o.picked && !o.story) inBag++;
            foreach (var o in orders)
            {
                if (!(o.picked || inBag < bagMax || o.story)) continue;
                float key = o.story ? 1e9f : o.left, uk = u == null ? float.MaxValue : (u.story ? 1e9f : u.left);
                if (u == null || key < uk) u = o;
            }
            return u;
        }

        void UpdateVisuals(float dt)
        {
            float p = Phase;
            float night = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.3f, 1f, p)) * .62f;
            float golden = Mathf.Max(0, 1 - Mathf.Abs(p - .15f) / .35f);
            float nk = night / .62f;

            foreach (var L in lights)
            {
                char h = HS(L), v = VS(L);
                foreach (var b in L.barsH) b.color = Art.A(LightCol(h), .9f);
                foreach (var b in L.barsV) b.color = Art.A(LightCol(v), .9f);
                SetHeads(L.headV, v); SetHeads(L.headH, h);
                L.glowV.color = Art.A(LightCol(v), .5f * nk);
                L.glowH.color = Art.A(LightCol(h), .5f * nk);
            }

            foreach (var c in cars)
            {
                float x = c.h ? c.p : c.lane, y = c.h ? c.lane : c.p;
                c.t.localPosition = Art.W(x, y);
                c.mr.sortingOrder = 1000 + (int)y;
                c.glow.transform.localPosition = Art.W(c.h ? x + c.dir * 26 : x, c.h ? y : y + c.dir * 26);
                c.glow.color = new Color(1, .94f, .75f, .4f * nk);
            }

            bool playing = state == St.Play || state == St.Pause;
            foreach (var g in gais)
            {
                g.baton.localRotation = Quaternion.Euler(0, 0, g.alert > 0 ? 60 + Mathf.Sin(Time.time * 12f) * 18f : -20f);
                g.bang.enabled = g.alert > 0;
                float d = playing ? Vector2.Distance(pos, g.center) : 9999;
                float a = Mathf.Clamp01(1 - (d - GAI_R) / 160f) * .4f;
                g.zone.startColor = g.zone.endColor = new Color(.55f, .77f, 1f, a);
            }

            if (pet != null) { bool pa = petId != "none" && courier.gameObject.activeSelf; if (pet.gameObject.activeSelf != pa) pet.gameObject.SetActive(pa); }
            // kuryer
            if (courier.gameObject.activeSelf)
            {
                float bobY = moving ? Mathf.Sin(bob) * 1.1f : 0;
                courier.localPosition = Art.W(pos.x, pos.y + bobY);
                float wob = stun > 0 ? Mathf.Sin(Time.time * 16f) * 17f : 0;
                courierBody.localRotation = Quaternion.Euler(0, 0, -ang * Mathf.Rad2Deg + wob);
                courierSG.sortingOrder = 1000 + (int)pos.y;
                bool blink = inv > 0 && Mathf.FloorToInt(Time.time * 8f) % 2 == 0;
                courierBody.gameObject.SetActive(!blink);
                AnimateBody(moving);
                UpdatePet(dt, moving);
                headlight.transform.localPosition = Art.W(pos.x + Mathf.Cos(ang) * 34, pos.y + Mathf.Sin(ang) * 34);
                headlight.color = new Color(1, .94f, .8f, .45f * nk);
                // chang izi
                dustT -= dt;
                if (moving && dustT <= 0 && state == St.Play && vehId != "foot")
                {
                    dustT = .05f;
                    var s = dust[dustIdx];
                    s.transform.localPosition = Art.W(pos.x - Mathf.Cos(ang) * 14, pos.y - Mathf.Sin(ang) * 14);
                    dustLife[dustIdx] = .5f;
                    dustIdx = (dustIdx + 1) % dust.Count;
                }
            }
            else headlight.color = Color.clear;
            for (int i = 0; i < dust.Count; i++)
            {
                if (dustLife[i] <= 0) { dust[i].color = Color.clear; continue; }
                dustLife[i] -= dt;
                float l = Mathf.Max(0, dustLife[i]);
                float sz = (4 + (.5f - l) * 10) * 2;
                dust[i].transform.localScale = new Vector3(sz * Art.K, sz * Art.K, 1);
                dust[i].color = vehId == "rocket" ? new Color(1f, .62f + .3f * l, .3f, l * .55f) : new Color(.94f, .92f, .86f, l * .35f);
            }

            // kecha
            float vh = cam.orthographicSize * 2f, vw = vh * cam.aspect;
            nightSr.transform.localScale = new Vector3(vw + 1, vh + 1, 1);
            goldSr.transform.localScale = nightSr.transform.localScale;
            nightSr.color = new Color(14 / 255f, 18 / 255f, 44 / 255f, night);
            goldSr.color = new Color(1f, .59f, .27f, .13f * golden);
            foreach (var g in lampGlows) g.color = new Color(1f, .82f, .52f, .5f * nk);
            for (int i = 0; i < signGlows.Count; i++) signGlows[i].color = Art.A(signCols[i], .35f * nk);
            if (city.windowGlow != null) city.windowGlow.sharedMaterial.color = new Color(1, 1, 1, Mathf.Min(1, nk * 1.2f));

            // belgilar, strelka, navigator
            foreach (var o in orders)
            {
                if (o.picked && o.pin != null && o.pin.name == "Pin") PinToHouse(o);
                var tgt = o.Target;
                float pulse = (Mathf.Sin(Time.time * 2.4f) + 1) / 2;
                float age = Mathf.Clamp01((Time.time - o.born) / .4f);
                float pop = age < 1 ? 1 + Mathf.Sin(age * Mathf.PI) * .35f : 1;
                o.pin.anchoredPosition = new Vector2(tgt.x, -(tgt.y - 36 - pulse * 4));
                o.pin.localScale = Vector3.one * pop;
                if (o.story) o.pin.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 1.4f) * 8f);
            }
            var urgent = state == St.Play ? Urgent() : null;
            if (urgent != null)
            {
                var tgt = urgent.Target;
                var path = FindPath(pos, tgt, nav);
                // strelka yo'l bo'ylab keyingi burilishni ko'rsatadi (to'g'ri chiziq bo'ylab emas)
                Vector2 next = path.Count > 1 ? path[1] : tgt;
                if (path.Count > 2 && Vector2.Distance(pos, next) < 10) next = path[2];
                float dist = Vector2.Distance(pos, tgt);
                arrow.enabled = dist > 40;
                float a = Mathf.Atan2(next.y - pos.y, next.x - pos.x);
                float ap = 1 + Mathf.Sin(Time.time * 6f) * .12f;
                arrow.transform.localPosition = Art.W(pos.x + Mathf.Cos(a) * 38, pos.y - 6 + Mathf.Sin(a) * 38);
                arrow.transform.localRotation = Quaternion.Euler(0, 0, -a * Mathf.Rad2Deg);
                arrow.transform.localScale = new Vector3(22 * Art.K * ap, 22 * Art.K * ap, 1);
                arrow.color = urgent.color;

                if (nav)
                {
                    // sotib olingan navigator: butun yo'l, YHXB postlarini aylanib o'tadi, oqib turuvchi chiziq
                    SetRoute(route, path, path.Count, Art.A(urgent.color, .55f + .25f * Mathf.Sin(Time.time * 3f)), .07f);
                    Order second = null;
                    foreach (var o in orders)
                        if (o != urgent && (o.picked || o.story) && (second == null || o.left < second.left)) second = o;
                    if (second != null) SetRoute(route2, FindPath(pos, second.Target, true), 99, Art.A(second.color, .3f), .035f);
                    else route2.positionCount = 0;
                }
                else
                {
                    // oddiy navigator: faqat keyingi 2 burilishgacha xira chiziq
                    SetRoute(route, path, 3, Art.A(urgent.color, .35f), .035f);
                    route2.positionCount = 0;
                }
            }
            else { arrow.enabled = false; route.positionCount = 0; route2.positionCount = 0; }

            for (int i = fx.Count - 1; i >= 0; i--)
            {
                var f = fx[i];
                f.life -= dt;
                if (f.life <= 0) { Destroy(f.rt.gameObject); fx.RemoveAt(i); continue; }
                f.rt.anchoredPosition = new Vector2(f.p.x, -(f.p.y - (1.8f - f.life) * 22f));
                f.t.color = Art.A(f.t.color, Mathf.Min(1, f.life));
            }
        }

        // ---------------------------------------------------------------- yo'l topish
        static readonly float[] dDist = new float[(COLS + 1) * (ROWS + 1)];
        static readonly int[] prev = new int[(COLS + 1) * (ROWS + 1)];
        static readonly bool[] doneN = new bool[(COLS + 1) * (ROWS + 1)];
        readonly List<Vector2> pathBuf = new List<Vector2>();
        static Vector2 NodePos(int i) => new Vector2((i % (COLS + 1)) * CELL + 30, (i / (COLS + 1)) * CELL + 30);
        static int NodeId(int c, int r) => Mathf.Clamp(r, 0, ROWS) * (COLS + 1) + Mathf.Clamp(c, 0, COLS);

        /// <summary>Nuqta qaysi yo'l bo'lagida turibdi: bo'lak uchlaridagi ikki chorraha.</summary>
        static void Ends(Vector2 p, out int a, out int b)
        {
            var n = Node(p);
            int c = Mathf.RoundToInt((n.x - 30) / CELL), r = Mathf.RoundToInt((n.y - 30) / CELL);
            if (Mathf.Abs(p.y - n.y) <= Mathf.Abs(p.x - n.x))
            {   // gorizontal yo'l
                int c0 = Mathf.FloorToInt((p.x - 30) / CELL);
                a = NodeId(c0, r); b = NodeId(c0 + 1, r);
            }
            else
            {
                int r0 = Mathf.FloorToInt((p.y - 30) / CELL);
                a = NodeId(c, r0); b = NodeId(c, r0 + 1);
            }
        }

        /// <summary>
        /// Chorrahalar bo'yicha eng qisqa yo'l (Dijkstra). avoidGai = true bo'lsa, YHXB postlaridan qochadi.
        /// </summary>
        List<Vector2> FindPath(Vector2 from, Vector2 to, bool avoidGai)
        {
            pathBuf.Clear();
            pathBuf.Add(from);
            Ends(from, out int sa, out int sb);
            Ends(to, out int ta, out int tb);
            if ((sa == ta && sb == tb) || (sa == tb && sb == ta)) { pathBuf.Add(to); return pathBuf; }
            int N = dDist.Length;
            for (int i = 0; i < N; i++) { dDist[i] = float.MaxValue; prev[i] = -1; doneN[i] = false; }
            dDist[sa] = Vector2.Distance(from, NodePos(sa));
            dDist[sb] = Mathf.Min(dDist[sb], Vector2.Distance(from, NodePos(sb)));
            for (int it = 0; it < N; it++)
            {
                int u = -1; float best = float.MaxValue;
                for (int i = 0; i < N; i++) if (!doneN[i] && dDist[i] < best) { best = dDist[i]; u = i; }
                if (u < 0) break;
                doneN[u] = true;
                int uc = u % (COLS + 1), ur = u / (COLS + 1);
                for (int k = 0; k < 4; k++)
                {
                    int vc = uc + (k == 0 ? 1 : k == 1 ? -1 : 0), vr = ur + (k == 2 ? 1 : k == 3 ? -1 : 0);
                    if (vc < 0 || vr < 0 || vc > COLS || vr > ROWS) continue;
                    int v = vr * (COLS + 1) + vc;
                    float w = CELL;
                    if (avoidGai) foreach (var g in gais) if (Vector2.Distance(g.center, NodePos(v)) < 40) w += 700;
                    if (dDist[u] + w < dDist[v]) { dDist[v] = dDist[u] + w; prev[v] = u; }
                }
            }
            float ca = dDist[ta] + Vector2.Distance(NodePos(ta), to), cb = dDist[tb] + Vector2.Distance(NodePos(tb), to);
            int end = ca <= cb ? ta : tb;
            int start = pathBuf.Count;
            for (int n = end; n >= 0; n = prev[n]) pathBuf.Insert(start, NodePos(n));
            pathBuf.Add(to);
            return pathBuf;
        }

        static void SetRoute(LineRenderer lr, List<Vector2> pts, int maxPts, Color col, float width)
        {
            int n = Mathf.Min(pts.Count, maxPts);
            lr.positionCount = n;
            for (int i = 0; i < n; i++) lr.SetPosition(i, Art.W(pts[i].x, pts[i].y));
            lr.startColor = col;
            lr.endColor = maxPts < pts.Count ? Art.A(col, 0) : col;
            lr.widthMultiplier = width;
        }

        static Vector2 Node(Vector2 p) => new Vector2(Mathf.Round((p.x - 30) / CELL) * CELL + 30, Mathf.Round((p.y - 30) / CELL) * CELL + 30);

        static void SetHeads(Quad[] heads, char s)
        {
            heads[0].color = s == 'r' ? LightCol('r') : new Color(1, 1, 1, .12f);
            heads[1].color = s == 'y' ? LightCol('y') : new Color(1, 1, 1, .12f);
            heads[2].color = s == 'g' ? LightCol('g') : new Color(1, 1, 1, .12f);
        }

        /// <summary>Xarita nuqtasining ekrandagi joyi (chekka ko'rsatkichlar uchun).</summary>
        public Vector3 MapToScreen(Vector2 p) => cam.WorldToScreenPoint(Art.W(p.x, p.y));

        void UpdateCamera(float dt)
        {
            zoomNow = Mathf.Lerp(zoomNow, Playing ? ZOOM : MENU_ZOOM, Mathf.Min(1, dt * 2.5f));
            cam.orthographicSize = MH / zoomNow * Art.K / 2f;
            float vh = cam.orthographicSize * 2f, vw = vh * cam.aspect;
            float mw = MW * Art.K, mh = MH * Art.K;
            Vector2 target;
            if (state == St.Play || state == St.Pause || state == St.End) target = new Vector2(pos.x * Art.K, -pos.y * Art.K);
            else
            {
                float t = Time.time;
                target = new Vector2(mw / 2 + Mathf.Sin(t / 9f) * Mathf.Max(0, mw - vw) / 2, -mh / 2 + Mathf.Sin(t / 6.5f) * Mathf.Max(0, mh - vh) / 2);
            }
            target.x = vw >= mw ? mw / 2 : Mathf.Clamp(target.x, vw / 2, mw - vw / 2);
            target.y = vh >= mh ? -mh / 2 : Mathf.Clamp(target.y, -mh + vh / 2, -vh / 2);
            float k = state == St.Menu ? Mathf.Min(1, dt * 1.5f) : Mathf.Min(1, dt * 5f);
            var cp = cam.transform.position;
            cam.transform.position = new Vector3(Mathf.Lerp(cp.x, target.x, k), Mathf.Lerp(cp.y, target.y, k), -10f);
        }

        public void SnapCamera()
        {
            zoomNow = Playing ? ZOOM : MENU_ZOOM;
            cam.orthographicSize = MH / zoomNow * Art.K / 2f;
            float vh = cam.orthographicSize * 2f, vw = vh * cam.aspect;
            float mw = MW * Art.K, mh = MH * Art.K;
            float x = vw >= mw ? mw / 2 : Mathf.Clamp(pos.x * Art.K, vw / 2, mw - vw / 2);
            float y = vh >= mh ? -mh / 2 : Mathf.Clamp(-pos.y * Art.K, -mh + vh / 2, -vh / 2);
            cam.transform.position = new Vector3(x, y, -10f);
        }
    }
}
