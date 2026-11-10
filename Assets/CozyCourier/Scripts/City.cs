using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CozyCourier
{
    public class Block
    {
        public int c, r;
        public string key;
        public float x0, y0;
        public int side;
        public Vector2 door;
        public string type;      // "rest", "park", "house", "bazar"
        public RestDef rest;
        public Color color;
        public int no;
        public float h;
        public Color wall;
        public Vector2 labelPos;
        public Text label;
        public RectTransform plate;
        public string Label => Loc.House(no);
    }

    /// <summary>Mahalla xaritasi: yo'llar, binolar, restoranlar, bog'lar, fonarlar va yozuvlar.</summary>
    public class City
    {
        const float CELL = Game.CELL, ROAD = Game.ROAD, BS = Game.BS;

        public District D;
        public readonly List<Block> blocks = new List<Block>();
        public readonly List<Block> rests = new List<Block>();
        public readonly List<Block> houses = new List<Block>();
        public readonly List<Vector2> lampGround = new List<Vector2>();
        public readonly List<KeyValuePair<Vector2, Color>> signs = new List<KeyValuePair<Vector2, Color>>();
        public MeshRenderer windowGlow;
        public Transform root;
        RectTransform labelRoot;
        Text bazarLabel;

        public Block ByKey(string k)
        {
            foreach (var b in blocks) if (b.key == k) return b;
            return null;
        }

        /// <summary>Markaz mahallasidagi uy raqami (hikoyadagi {R} va {S} uchun).</summary>
        public static string HomeLabel(string key)
        {
            var M = Data.Districts["markaz"];
            int level = Save.Level, n = 0;
            for (int r = 0; r < Game.ROWS; r++)
                for (int c = 0; c < Game.COLS; c++)
                {
                    string k = c + "," + r;
                    int ei = System.Array.IndexOf(M.extra, k);
                    bool extraRest = ei >= 0 && ei < Data.Extra.Length && level >= Data.Extra[ei].lvl;
                    if (!M.rests.ContainsKey(k) && System.Array.IndexOf(M.parks, k) < 0 && !extraRest) n++;
                    if (k == key) return Loc.House(M.Num(n));
                }
            return "";
        }

        public void Build(Transform parent, District d)
        {
            D = d;
            root = new GameObject("City " + d.id).transform;
            root.SetParent(parent, false);
            int level = Save.Level;
            int houseNo = 1;
            for (int r = 0; r < Game.ROWS; r++)
                for (int c = 0; c < Game.COLS; c++)
                {
                    var b = new Block { c = c, r = r, key = c + "," + r, x0 = c * CELL + ROAD, y0 = r * CELL + ROAD };
                    b.side = (c * 7 + r * 3 + 1) % 4;
                    float x0 = b.x0, y0 = b.y0;
                    switch (b.side)
                    {
                        case 0: b.door = new Vector2(x0 + BS / 2, y0 - 30); break;
                        case 1: b.door = new Vector2(x0 + BS + 30, y0 + BS / 2); break;
                        case 2: b.door = new Vector2(x0 + BS / 2, y0 + BS + 30); break;
                        default: b.door = new Vector2(x0 - 30, y0 + BS / 2); break;
                    }
                    int ei = System.Array.IndexOf(d.extra, b.key);
                    RestDef rd = null;
                    if (d.rests.TryGetValue(b.key, out var r0)) rd = r0;
                    else if (ei >= 0 && ei < Data.Extra.Length && level >= Data.Extra[ei].lvl) rd = Data.Extra[ei];
                    if (rd != null) { b.type = "rest"; b.rest = rd; b.color = Art.Hex(rd.color); }
                    else if (System.Array.IndexOf(d.parks, b.key) >= 0) b.type = "park";
                    else if (d.marks.TryGetValue(b.key, out var mk)) b.type = mk;
                    else
                    {
                        b.type = "house";
                        b.no = d.Num(houseNo); houseNo++;
                        b.wall = Art.Hex(d.walls[(c + r * 2) % d.walls.Length]);
                        b.h = d.heights[(c * 3 + r) % d.heights.Length];
                    }
                    blocks.Add(b);
                }
            foreach (var b in blocks)
            {
                if (b.type == "rest") rests.Add(b);
                else if (b.type == "house") houses.Add(b);
            }
            var glow = new MB();
            BuildGround();
            foreach (var b in blocks) BuildTall(b, glow);
            BuildGaiBooths();
            var gm = Art.NewMat();
            windowGlow = Art.MeshObj(root, "WindowGlow", glow.Build(), 5002, gm);
            gm.color = new Color(1, 1, 1, 0);
            BuildLabels();
            RefreshLabels();
        }

        public void Destroy()
        {
            if (root != null) Object.Destroy(root.gameObject);
        }

        bool Eski => D.id == "eski";

        void BuildGround()
        {
            var mb = new MB();
            mb.Rect(-600, -400, Game.MW + 1200, Game.MH + 800, Art.Hex("#0c151b"));
            mb.Rect(0, 0, Game.MW, Game.MH, Eski ? Art.Hex("#615d57") : D.id == "chilonzor" ? Art.Hex("#4f5862") : Art.Hex("#535c65"));
            var rnd = new System.Random(7);
            if (Eski)
            {
                var seam = new Color(0, 0, 0, .12f);
                for (float y = 0; y < Game.MH; y += 12) mb.Rect(0, y, Game.MW, 1, seam);
                for (int i = 0; i < 2600; i++) { float y = Mathf.Floor((float)rnd.NextDouble() * Game.MH / 12) * 12; mb.Rect((float)rnd.NextDouble() * Game.MW, y, 1, 12, new Color(0, 0, 0, .1f)); }
            }
            for (int i = 0; i < 1600; i++)
            {
                float x = (float)rnd.NextDouble() * Game.MW, y = (float)rnd.NextDouble() * Game.MH;
                mb.Rect(x, y, 1.8f, 1.8f, i % 2 == 0 ? new Color(1, 1, 1, .04f) : new Color(0, 0, 0, .06f));
            }
            if (!Eski)
            {
                var dash = new Color(.96f, .87f, .55f, .6f);
                for (int r = 0; r <= Game.ROWS; r++)
                    for (int c = 0; c < Game.COLS; c++)
                    {
                        float y = r * CELL + 30, end = (c + 1) * CELL - 6;
                        for (float x = c * CELL + 66; x < end; x += 30) mb.Rect(x, y - 1.1f, Mathf.Min(16, end - x), 2.2f, dash);
                    }
                for (int c = 0; c <= Game.COLS; c++)
                    for (int r = 0; r < Game.ROWS; r++)
                    {
                        float x = c * CELL + 30, end = (r + 1) * CELL - 6;
                        for (float y = r * CELL + 66; y < end; y += 30) mb.Rect(x - 1.1f, y, 2.2f, Mathf.Min(16, end - y), dash);
                    }
            }
            var zebra = new Color(1, 1, 1, .55f);
            for (int r = 0; r <= Game.ROWS; r++)
                for (int c = 0; c <= Game.COLS; c++)
                {
                    float x = c * CELL, y = r * CELL;
                    for (int i = 0; i < 5; i++)
                    {
                        if (c < Game.COLS) mb.Rect(x + 64, y + 5 + i * 11, 12, 6, zebra);
                        if (c > 0) mb.Rect(x - 16, y + 5 + i * 11, 12, 6, zebra);
                        if (r < Game.ROWS) mb.Rect(x + 5 + i * 11, y + 64, 6, 12, zebra);
                        if (r > 0) mb.Rect(x + 5 + i * 11, y - 16, 6, 12, zebra);
                    }
                }
            for (int i = 0; i < 10; i++)
            {
                float x = (float)rnd.NextDouble() * (Game.MW - 200) + 100, y = Mathf.Round((float)rnd.NextDouble() * Game.ROWS) * CELL + 18;
                mb.Circle(x, y, 6, Art.Hex("#3d444b"));
            }
            foreach (var b in blocks)
            {
                float x0 = b.x0, y0 = b.y0;
                mb.Rect(x0 - 1, y0 + 2, BS + 3, BS + 3, new Color(0, 0, 0, .25f));
                mb.RRect(x0, y0, BS, BS, 10, Art.Hex("#a99c82"));
                mb.RRect(x0 + 2, y0 + 2, BS - 4, BS - 4, 8, Eski ? Art.Hex("#e8d4b2") : Art.Hex("#e6dcc5"));
                var tile = new Color(.59f, .53f, .43f, .25f);
                for (float k = 12; k < BS; k += 12)
                {
                    mb.Rect(x0 + k, y0 + 3, 1, 8, tile); mb.Rect(x0 + k, y0 + BS - 11, 1, 8, tile);
                    mb.Rect(x0 + 3, y0 + k, 8, 1, tile); mb.Rect(x0 + BS - 11, y0 + k, 8, 1, tile);
                }
                if (b.type == "park")
                {
                    float cx = x0 + BS / 2, cy = y0 + BS / 2;
                    mb.RRect(x0 + 10, y0 + 10, BS - 20, BS - 20, 8, Art.Hex("#8fbf78"));
                    mb.Ellipse(cx, cy, 44, 32, Art.Hex("#ddd2b6"));
                    mb.Rect(cx - 5, y0 + 10, 10, BS - 20, Art.Hex("#ddd2b6"));
                    mb.Rect(x0 + 10, cy - 5, BS - 20, 10, Art.Hex("#ddd2b6"));
                    mb.Ellipse(cx, cy + 1, 28, 18, Art.Hex("#b4a98f"));
                    mb.Ellipse(cx, cy, 24, 15, Art.Hex("#6fa6cc"));
                    mb.Ellipse(cx - 6, cy - 4, 8, 3, new Color(1, 1, 1, .35f));
                    for (int i = 0; i < 40; i++) mb.Rect(x0 + 14 + (float)rnd.NextDouble() * (BS - 28), y0 + 14 + (float)rnd.NextDouble() * (BS - 28), 2, 3, new Color(.24f, .39f, .2f, .35f));
                    string[] fc = { "#ee9583", "#ffd166", "#c3a6e8" };
                    for (int i = 0; i < 8; i++)
                    {
                        mb.Circle(x0 + 30 + (float)rnd.NextDouble() * 16 - 8, y0 + BS / 2 - 24 + (float)rnd.NextDouble() * 16 - 8, 2.5f, Art.Hex(fc[i % 3]));
                        mb.Circle(x0 + BS - 30 + (float)rnd.NextDouble() * 16 - 8, y0 + BS / 2 + 24 + (float)rnd.NextDouble() * 16 - 8, 2.5f, Art.Hex(fc[(i + 1) % 3]));
                    }
                }
                else if (b.type == "bazar")
                {
                    mb.RRect(x0 + 8, y0 + 8, BS - 16, BS - 16, 6, Art.Hex("#e0cba3"));
                    var ln = new Color(.47f, .35f, .24f, .2f);
                    for (float k = 16; k < BS - 8; k += 16) { mb.Rect(x0 + 8, y0 + k, BS - 16, 1, ln); mb.Rect(x0 + k, y0 + 8, 1, BS - 16, ln); }
                }
                else
                {
                    mb.RRect(x0 + 12, y0 + 12, BS - 24, BS - 24, 6, Art.Hex("#bfd19d"));
                    for (int i = 0; i < 24; i++) mb.Rect(x0 + 16 + (float)rnd.NextDouble() * (BS - 32), y0 + 16 + (float)rnd.NextDouble() * (BS - 32), 2, 3, new Color(.27f, .43f, .24f, .3f));
                    var dc = b.type == "rest" ? b.color : Art.Hex("#bdb19a");
                    const float dw = 24;
                    switch (b.side)
                    {
                        case 0: mb.Rect(x0 + BS / 2 - dw / 2, y0, dw, 14, dc); break;
                        case 1: mb.Rect(x0 + BS - 14, y0 + BS / 2 - dw / 2, 14, dw, dc); break;
                        case 2: mb.Rect(x0 + BS / 2 - dw / 2, y0 + BS - 14, dw, 14, dc); break;
                        default: mb.Rect(x0, y0 + BS / 2 - dw / 2, 14, dw, dc); break;
                    }
                }
            }
            for (int i = 0; i < D.gai.GetLength(0); i++)
            {
                float gx = D.gai[i, 0] * CELL + 68, gy = D.gai[i, 1] * CELL + 68;
                mb.RRect(gx - 4, gy - 4, 40, 40, 6, new Color(.18f, .31f, .5f, .25f));
            }
            Art.MeshObj(root, "Ground", mb.Build(), 0);
        }

        // ================================================================ chizish yordamchilari
        // Old devor (janub): u — chapdan, z — yerdan balandlik (piksel)
        static Vector2 F(float x, float y, float d, float u, float z) => new Vector2(x + u + z * Art.OX, y + d + z * Art.OY);
        // Yon devor (sharq): t — yuqoridan pastga, z — balandlik
        static Vector2 E(float x, float y, float w, float t, float z) => new Vector2(x + w + z * Art.OX, y + t + z * Art.OY);

        static void FRect(MB mb, float x, float y, float d, float u0, float z0, float u1, float z1, Color c) =>
            mb.Quad(F(x, y, d, u0, z0), F(x, y, d, u1, z0), F(x, y, d, u1, z1), F(x, y, d, u0, z1), c);
        static void ERect(MB mb, float x, float y, float w, float t0, float z0, float t1, float z1, Color c) =>
            mb.Quad(E(x, y, w, t0, z0), E(x, y, w, t1, z0), E(x, y, w, t1, z1), E(x, y, w, t0, z1), c);

        /// <summary>Old devordagi deraza: rom, oyna, yorug' bo'lsa parda va nur.</summary>
        static void Window(MB mb, MB glow, float x, float y, float d, float u, float z, float ww, float wh, Color frame, bool lit, int seed)
        {
            FRect(mb, x, y, d, u - 1.5f, z - 1.5f, u + ww + 1.5f, z + wh + 1.5f, frame);
            var gl = lit ? Art.Hex("#ffe1a1") : Art.Hex("#5a6f84");
            FRect(mb, x, y, d, u, z, u + ww, z + wh, gl);
            if (lit)
            {
                // parda
                var cur = (seed % 3) switch { 0 => Art.Hex("#e8a05c"), 1 => Art.Hex("#d9734a"), _ => Art.Hex("#f2c25a") };
                FRect(mb, x, y, d, u, z, u + ww * .28f, z + wh, Art.A(cur, .85f));
                FRect(mb, x, y, d, u + ww * .72f, z, u + ww, z + wh, Art.A(cur, .85f));
                var a = F(x, y, d, u, z); var b2 = F(x, y, d, u + ww, z + wh);
                glow.Quad(F(x, y, d, u, z), F(x, y, d, u + ww, z), F(x, y, d, u + ww, z + wh), F(x, y, d, u, z + wh), Art.Hex("#ffd48a"));
                glow.Circle((a.x + b2.x) / 2, (a.y + b2.y) / 2, ww * 1.4f, new Color(1f, .78f, .47f, .16f), 10);
            }
            else FRect(mb, x, y, d, u + 1, z + wh * .55f, u + ww * .45f, z + wh - 1, new Color(1, 1, 1, .22f)); // aks
            FRect(mb, x, y, d, u + ww / 2 - .5f, z, u + ww / 2 + .5f, z + wh, frame); // o'rta rom
        }

        /// <summary>Yon devordagi deraza (qiya).</summary>
        static void SideWindow(MB mb, float x, float y, float w, float t, float z, float tw, float wh, Color frame, bool lit)
        {
            ERect(mb, x, y, w, t - 1, z - 1, t + tw + 1, z + wh + 1, frame);
            ERect(mb, x, y, w, t, z, t + tw, z + wh, lit ? Art.Hex("#f0c98a") : Art.Hex("#4b5d70"));
        }

        /// <summary>O'zbekcha naqshli bezak chizig'i (girih uslubida rombchalar).</summary>
        static void Ornament(MB mb, float x, float y, float d, float u0, float u1, float z, Color band, Color a, Color b)
        {
            FRect(mb, x, y, d, u0, z, u1, z + 6, band);
            int n = Mathf.Max(1, Mathf.FloorToInt((u1 - u0) / 7f));
            float step = (u1 - u0) / n;
            for (int i = 0; i < n; i++)
            {
                float cu = u0 + step * (i + .5f);
                mb.Poly(new[] { F(x, y, d, cu, z + .8f), F(x, y, d, cu + 2.6f, z + 3), F(x, y, d, cu, z + 5.2f), F(x, y, d, cu - 2.6f, z + 3) }, i % 2 == 0 ? a : b);
            }
        }

        static void Planter(MB mb, float px, float py, int seed)
        {
            mb.Ellipse(px + 3, py + 2, 6, 2.5f, new Color(.08f, .11f, .16f, .2f));
            mb.Poly(new[] { new Vector2(px - 5, py - 8), new Vector2(px + 5, py - 8), new Vector2(px + 4, py), new Vector2(px - 4, py) }, Art.Hex("#b8622a"));
            mb.Rect(px - 5.5f, py - 9, 11, 2, Art.Hex("#c97434"));
            string[] fl = { "#ee9583", "#ffd166", "#c3a6e8", "#ff8fa3" };
            mb.Circle(px - 3, py - 12, 4, Art.Hex("#5f9a58"), 8); mb.Circle(px + 3, py - 12, 4, Art.Hex("#4e8250"), 8); mb.Circle(px, py - 15, 4, Art.Hex("#69a064"), 8);
            for (int i = 0; i < 3; i++) mb.Circle(px - 3 + i * 3, py - 14 + (i % 2) * 2, 1.6f, Art.Hex(fl[(seed + i) % 4]), 6);
        }

        // ================================================================ restoran
        void DrawRest(MB mb, MB glow, Block b, int seed)
        {
            float x0 = b.x0, y0 = b.y0;
            float x = x0 + 18, y = y0 + 20, w = BS - 40, d = BS - 50, h = 32;
            string id = b.rest != null ? b.rest.id : "";
            var wall = id == "choyxona" ? Art.Hex("#efe0c4") : id == "shashlik" ? Art.Hex("#e9d2b8") : Art.Hex("#f4e9d6");
            var trim = Art.Shade(b.color, -.15f);

            // yerdagi terassa plitkasi
            mb.RRect(x - 6, y + d - 4, w + 12, BS - (y - y0) - d + 2, 4, Art.Hex("#d9c9ad"));
            for (float tx = x - 4; tx < x + w + 6; tx += 9) mb.Rect(tx, y + d - 2, .8f, BS - (y - y0) - d - 2, new Color(0, 0, 0, .06f));

            mb.Box(x, y, w, d, h, wall, Art.Shade(wall, .06f));
            // poydevor va yuqori karniz
            FRect(mb, x, y, d, 0, 0, w, 4, Art.Shade(wall, -.25f));
            FRect(mb, x, y, d, -1, h - 4, w + 1, h, trim);
            ERect(mb, x, y, w, 0, h - 4, d, h, Art.Shade(trim, -.2f));
            Ornament(mb, x, y, d, 3, w - 3, h - 11, Art.Hex("#f3e3b3"), Art.Hex("#2f9e92"), Art.Hex("#3d7fd0"));

            // katta vitrinalar (ichida stol siluetlari) va o'rtada eshik
            float doorU = w / 2 - 7;
            float[] wins = { 5, doorU + 18 };
            foreach (float wu in wins)
            {
                float ww = doorU - 9;
                FRect(mb, x, y, d, wu - 2, 4, wu + ww + 2, 22, trim);
                FRect(mb, x, y, d, wu, 6, wu + ww, 20, Art.Hex("#ffdca0"));
                glow.Quad(F(x, y, d, wu, 6), F(x, y, d, wu + ww, 6), F(x, y, d, wu + ww, 20), F(x, y, d, wu, 20), new Color(1f, .85f, .55f, .95f));
                for (int k = 0; k < 2; k++)
                {
                    float tu = wu + 4 + k * (ww / 2);
                    FRect(mb, x, y, d, tu, 6, tu + 6, 9, Art.Hex("#8a5a3a"));        // stol
                    FRect(mb, x, y, d, tu + 1.5f, 9, tu + 4.5f, 13, Art.Hex("#c98a4b", .7f)); // odam siluet
                }
                FRect(mb, x, y, d, wu + 1, 15, wu + ww * .4f, 19, new Color(1, 1, 1, .28f));
            }
            // eshik
            FRect(mb, x, y, d, doorU - 2, 0, doorU + 16, 24, trim);
            FRect(mb, x, y, d, doorU, 0, doorU + 14, 22, Art.Hex("#7a4a2c"));
            FRect(mb, x, y, d, doorU + 2, 10, doorU + 12, 20, Art.Hex("#ffd9a0"));
            var dk = F(x, y, d, doorU + 11, 9); mb.Circle(dk.x, dk.y, 1.2f, Art.Hex("#e9c46a"), 6);
            // soyabon (yo'l-yo'l, to'lqinli cheti)
            int seg = 8;
            for (int i = 0; i < seg; i++)
            {
                var a = F(x, y, d, i * w / seg, 25); var e = F(x, y, d, (i + 1) * w / seg, 25);
                var col = i % 2 == 1 ? Art.Hex("#fff8ec") : b.color;
                mb.Quad(a, e, new Vector2(e.x + 1, e.y + 10), new Vector2(a.x + 1, a.y + 10), col);
                mb.Arc((a.x + e.x) / 2 + 1, a.y + 10, (e.x - a.x) / 2, 3, 0, Mathf.PI, col, 6);
            }
            // soyabon chetidagi chiroqchalar
            for (int i = 0; i <= 10; i++)
            {
                var p = F(x, y, d, i * w / 10, 23);
                var lc = (i % 3) switch { 0 => Art.Hex("#ffd166"), 1 => Art.Hex("#ff9f6e"), _ => Art.Hex("#fff3c4") };
                mb.Circle(p.x + 1, p.y + 13 + Mathf.Sin(i * .9f) * 1.2f, 1.2f, lc, 6);
                glow.Circle(p.x + 1, p.y + 13 + Mathf.Sin(i * .9f) * 1.2f, 3.2f, Art.A(lc, .8f), 8);
            }

            // tom: parapet, konditsioner, o'simlik
            float rx = x + h * Art.OX, ry = y + h * Art.OY;
            var edge = Art.Shade(wall, -.12f);
            mb.Rect(rx, ry, w, 3, edge); mb.Rect(rx, ry + d - 3, w, 3, edge); mb.Rect(rx, ry, 3, d, edge); mb.Rect(rx + w - 3, ry, 3, d, edge);
            mb.Box(rx + w - 22, ry + 8, 14, 10, 6, Art.Hex("#c9ced2"), null, false);
            mb.Circle(rx + w - 15 + 6 * Art.OX, ry + 13 + 6 * Art.OY, 3, Art.Hex("#8e9aa3"), 8);

            if (id == "shashlik")
            {
                // mo'ri (tutun chiqadigan)
                mb.Box(rx + 10, ry + 10, 8, 8, 14, Art.Hex("#8a5a3a"), Art.Hex("#3a2a20"), false);
                // eshik oldida mangal
                float mx = x + w - 6, my = y0 + BS - 10;
                mb.Ellipse(mx + 3, my + 2, 14, 3, new Color(.08f, .11f, .16f, .25f));
                mb.Box(mx - 14, my - 6, 26, 6, 9, Art.Hex("#3a3a3a"), Art.Hex("#262626"), false);
                for (int i = 0; i < 5; i++) { mb.Rect(mx - 12 + i * 5 + 9 * Art.OX, my - 6 + 9 * Art.OY + 1, 3, 4, Art.Hex("#ff7a3d")); }
                glow.Circle(mx - 2 + 9 * Art.OX, my - 3 + 9 * Art.OY, 14, new Color(1f, .5f, .2f, .5f), 12);
                for (int i = 0; i < 4; i++) mb.Rect(mx - 13 + 9 * Art.OX, my - 5 + i * 1.5f + 9 * Art.OY, 26, .8f, Art.Hex("#c0c4c8"));
                for (int i = 0; i < 3; i++) mb.Circle(mx - 4 + i * 5 + 9 * Art.OX, my - 14 - i * 5 + 9 * Art.OY, 4 + i, new Color(.9f, .9f, .92f, .28f - i * .06f), 10);
            }
            else if (id == "choyxona")
            {
                // tapchan: gilam, yostiq, choynak
                float tx = x + w - 34, ty = y0 + BS - 16;
                mb.Box(tx, ty, 34, 12, 5, Art.Hex("#8a5a3a"), Art.Hex("#9c6a44"), true);
                float gx = tx + 5 * Art.OX, gy = ty + 5 * Art.OY;
                mb.Rect(gx + 2, gy + 1, 30, 10, Art.Hex("#b5403a"));
                for (float k = gx + 4; k < gx + 30; k += 5) mb.Poly(new[] { new Vector2(k, gy + 6), new Vector2(k + 2, gy + 3), new Vector2(k + 4, gy + 6), new Vector2(k + 2, gy + 9) }, Art.Hex("#e9c46a"));
                mb.Ellipse(gx + 17, gy + 5, 4, 2.5f, Art.Hex("#2c6aa8"));
                mb.Ellipse(gx + 17, gy + 4, 3, 1.5f, Color.white);
                mb.RRect(gx + 3, gy - 3, 8, 5, 2, Art.Hex("#e8a05c")); mb.RRect(gx + 22, gy - 3, 8, 5, 2, Art.Hex("#7b4a8c"));
                // tomdagi ayvon gumbazchasi
                mb.Arc(rx + 16, ry + d / 2, 9, 10, Mathf.PI, Mathf.PI * 2, Art.Hex("#3fb0bb"), 12);
                mb.Rect(rx + 15.2f, ry + d / 2 - 14, 1.6f, 4, Art.Hex("#e9c46a"));
            }
            else
            {
                // tashqi stollar soyabon bilan
                foreach (float ux in new[] { x - 2, x + w - 10 })
                {
                    float uy = y0 + BS - 8;
                    mb.Ellipse(ux + 6, uy + 2, 10, 3.5f, new Color(.08f, .11f, .16f, .2f));
                    mb.Ellipse(ux, uy - 4, 6, 3, Art.Hex("#f4f1ea"));
                    mb.Rect(ux - 7, uy - 5, 3, 4, Art.Hex("#8a5a3a")); mb.Rect(ux + 4, uy - 5, 3, 4, Art.Hex("#8a5a3a"));
                    mb.Rect(ux - .8f, uy - 20, 1.6f, 16, Art.Hex("#6b4a33"));
                    mb.Arc(ux, uy - 20, 12, 6, Mathf.PI, Mathf.PI * 2, b.color, 12);
                    mb.Arc(ux, uy - 20, 12, 2, 0, Mathf.PI, Art.Shade(b.color, -.2f), 12);
                    mb.Ellipse(ux - 4, uy - 23, 4, 1.5f, new Color(1, 1, 1, .35f));
                }
            }
            // eshik yonida gultuvaklar va menyu doskasi
            var d0 = F(x, y, d, doorU - 6, 0); var d1 = F(x, y, d, doorU + 20, 0);
            Planter(mb, d0.x, d0.y + 6, seed); Planter(mb, d1.x, d1.y + 6, seed + 1);
            var mbp = F(x, y, d, doorU + 26, 0);
            mb.Poly(new[] { new Vector2(mbp.x, mbp.y + 6), new Vector2(mbp.x + 3, mbp.y - 8), new Vector2(mbp.x + 11, mbp.y - 8), new Vector2(mbp.x + 14, mbp.y + 6) }, Art.Hex("#6b4a33"));
            mb.Rect(mbp.x + 3.5f, mbp.y - 6, 7, 9, Art.Hex("#2b2f2a"));
            for (int i = 0; i < 3; i++) mb.Rect(mbp.x + 4.5f, mbp.y - 5 + i * 2.6f, 5 - i, .8f, Art.Hex("#f4f1ea", .8f));

            // nomli peshtaxta (tomda)
            float sx = rx + 6, sy = ry + d / 2 - 12, sw = w - 12;
            mb.Rect(sx + 6, sy + 16, 2, 6, Art.Hex("#3a3a3a")); mb.Rect(sx + sw - 8, sy + 16, 2, 6, Art.Hex("#3a3a3a"));
            mb.Box(sx, sy, sw, 16, 14, b.color, Art.Shade(b.color, .12f), false);
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                var p = new Vector2(sx + sw * t, sy + 16);
                mb.Circle(p.x, p.y - 1, .9f, Art.Hex("#fff3c4"), 5);
                glow.Circle(p.x, p.y - 1, 2.6f, new Color(1f, .95f, .75f, .8f), 6);
            }
            b.labelPos = new Vector2(sx + sw / 2 + 14 * Art.OX, sy + 8 + 14 * Art.OY);
            signs.Add(new KeyValuePair<Vector2, Color>(b.labelPos, b.color));
            mb.Tree(x0 + 8, y0 + 12, 8, seed, D.trees);
        }

        // ================================================================ uy
        void DrawHouse(MB mb, MB glow, Block b, int seed)
        {
            float x0 = b.x0, y0 = b.y0;
            float h = b.h;
            int style = h <= 34 ? 1 : (h >= 66 && (b.c + b.r * 2) % 3 == 0) ? 2 : 0;
            float x = x0 + 22, y = y0 + 26, w = BS - 50, d = BS - 48;
            var wall = b.wall;

            // hovli: yo'lak, gulzor, skameyka
            mb.RRect(x + w / 2 - 9, y + d - 2, 18, BS - (y - y0) - d + 2, 3, Art.Hex("#d6c7ab"));
            float fbx = x + 4, fby = y + d + 6;
            mb.RRect(fbx, fby, 24, 9, 4, Art.Hex("#7a5a3a"));
            string[] fl = { "#ee9583", "#ffd166", "#c3a6e8", "#ff8fa3", "#ffffff" };
            for (int i = 0; i < 7; i++) mb.Circle(fbx + 3 + i * 3.2f, fby + 3 + (i % 2) * 3, 2.2f, i % 2 == 0 ? Art.Hex("#5f9a58") : Art.Hex(fl[(seed + i) % 5]), 6);
            float bx = x + w - 24, by = y + d + 8;
            mb.Rect(bx, by + 3, 18, 3, Art.Hex("#9a7452")); mb.Rect(bx, by, 18, 2, Art.Hex("#b08860"));
            mb.Rect(bx + 1, by + 6, 1.5f, 3, Art.Hex("#3a3a3a")); mb.Rect(bx + 15.5f, by + 6, 1.5f, 3, Art.Hex("#3a3a3a"));

            if (style == 1)
            {
                // ===== an'anaviy hovli uyi =====
                wall = Art.Hex(((seed % 3) == 0) ? "#ead2a8" : ((seed % 3) == 1) ? "#e6c9a0" : "#efd9b6");
                mb.Box(x, y, w, d, h, wall, Art.Hex("#c9b08a"));
                FRect(mb, x, y, d, 0, 0, w, 3, Art.Shade(wall, -.25f));
                // yog'och to'sinlar uchi
                for (float u = 3; u < w - 2; u += 8) FRect(mb, x, y, d, u, h - 4, u + 3, h - 1, Art.Hex("#7a5236"));
                FRect(mb, x, y, d, -1, h - 1, w + 1, h + 1, Art.Hex("#9c6a44"));
                // ko'k romli derazalar (panjarali)
                var blue = Art.Hex("#3d7fd0");
                foreach (float wu in new[] { 6f, w - 22 })
                {
                    bool lit = ((seed + (int)wu) % 3) != 0;
                    Window(mb, glow, x, y, d, wu, 8, 16, 12, blue, lit, seed);
                    FRect(mb, x, y, d, wu, 14, wu + 16, 14.8f, blue);
                }
                // o'ymakor yog'och eshik
                float du = w / 2 - 8;
                FRect(mb, x, y, d, du - 2, 0, du + 18, 21, Art.Hex("#9c6a44"));
                FRect(mb, x, y, d, du, 0, du + 16, 19, Art.Hex("#7a4a2c"));
                FRect(mb, x, y, d, du + 7.6f, 0, du + 8.4f, 19, Art.Hex("#5a341e"));
                for (int k = 0; k < 2; k++)
                    for (int r2 = 0; r2 < 3; r2++)
                    {
                        var c = F(x, y, d, du + 4 + k * 8, 4 + r2 * 5.5f);
                        mb.Poly(new[] { new Vector2(c.x, c.y - 2), new Vector2(c.x + 2, c.y), new Vector2(c.x, c.y + 2), new Vector2(c.x - 2, c.y) }, Art.Hex("#c98a4b"));
                    }
                Ornament(mb, x, y, d, 2, w - 2, h - 10, Art.Hex("#f6ead0"), Art.Hex("#2f9e92"), Art.Hex("#c0443a"));
                // yon devordagi deraza
                SideWindow(mb, x, y, w, d * .3f, 8, d * .35f, 11, blue, (seed % 2) == 0);
                // tom: tok so'risi (uzum)
                float rx = x + h * Art.OX, ry = y + h * Art.OY;
                for (float px = rx + 4; px < rx + w - 2; px += 9) mb.Rect(px, ry + 2, 1.5f, d - 4, Art.Hex("#8a6a48"));
                for (int i = 0; i < 18; i++)
                {
                    float vx = rx + 4 + Art.Rnd(seed + i) * (w - 8), vy = ry + 4 + Art.Rnd(seed + i + 40) * (d - 8);
                    mb.Circle(vx, vy, 4 + Art.Rnd(seed + i + 9) * 3, i % 3 == 0 ? Art.Hex("#4e8250") : i % 3 == 1 ? Art.Hex("#69a064") : Art.Hex("#86b878"), 8);
                }
                for (int i = 0; i < 4; i++)
                {
                    float gx = rx + 10 + Art.Rnd(seed + i * 7) * (w - 20), gy = ry + 8 + Art.Rnd(seed + i * 5) * (d - 16);
                    for (int k = 0; k < 4; k++) mb.Circle(gx + (k % 2) * 2.4f, gy + k * 1.8f, 1.6f, Art.Hex("#7b4a8c"), 6);
                }
                b.labelPos = new Vector2(rx + w / 2 + 8, ry + d / 2 + 6);
            }
            else if (style == 2)
            {
                // ===== zamonaviy shisha bino =====
                var glassA = Art.Hex("#4f7fa8"); var glassB = Art.Hex("#7fb3cf");
                mb.Box(x, y, w, d, h, glassA, Art.Hex("#c9d4dc"));
                // old devor gradiyenti va ustunlar
                for (int i = 0; i < 6; i++) FRect(mb, x, y, d, 0, i * h / 6, w, (i + 1) * h / 6, Color.Lerp(glassA, glassB, i / 6f));
                for (float u = 0; u <= w; u += w / 6) FRect(mb, x, y, d, u - .6f, 0, u + .6f, h, Art.Hex("#d6e2ea", .7f));
                for (float z = 10; z < h; z += 10) FRect(mb, x, y, d, 0, z - .6f, w, z + .6f, Art.Hex("#d6e2ea", .55f));
                // osmon aksi
                mb.Poly(new[] { F(x, y, d, w * .15f, 0), F(x, y, d, w * .35f, 0), F(x, y, d, w * .75f, h), F(x, y, d, w * .55f, h) }, new Color(1, 1, 1, .16f));
                // tasodifiy yoritilgan qavatlar
                for (float z = 2; z < h - 8; z += 10)
                    for (float u = 1; u < w - 2; u += w / 6)
                        if (Art.Rnd(seed + (int)(z * 7 + u)) < .35f)
                        {
                            FRect(mb, x, y, d, u + .8f, z, u + w / 6 - .8f, z + 7, Art.Hex("#ffe1a1", .85f));
                            glow.Quad(F(x, y, d, u + .8f, z), F(x, y, d, u + w / 6 - .8f, z), F(x, y, d, u + w / 6 - .8f, z + 7), F(x, y, d, u + .8f, z + 7), Art.Hex("#ffd48a"));
                        }
                // kirish (kanop)
                float du = w / 2 - 10;
                FRect(mb, x, y, d, du, 0, du + 20, 12, Art.Hex("#2b3d50"));
                mb.Box(F(x, y, d, du - 3, 13).x, F(x, y, d, du - 3, 13).y - 1, 26, 6, 2, Art.Hex("#e8eef4"), null, false);
                // tom: bog' va vertolyot maydonchasi
                float rx = x + h * Art.OX, ry = y + h * Art.OY;
                mb.Circle(rx + w * .62f, ry + d * .5f, 13, Art.Hex("#8e9aa3"), 20);
                mb.Circle(rx + w * .62f, ry + d * .5f, 11, Art.Hex("#a9b4bc"), 20);
                mb.Rect(rx + w * .62f - 4, ry + d * .5f - 5, 2, 10, Color.white); mb.Rect(rx + w * .62f + 2, ry + d * .5f - 5, 2, 10, Color.white); mb.Rect(rx + w * .62f - 4, ry + d * .5f - 1, 8, 2, Color.white);
                for (int i = 0; i < 6; i++) mb.Circle(rx + 8 + (i % 3) * 7, ry + 8 + (i / 3) * 7, 4, i % 2 == 0 ? Art.Hex("#5f9a58") : Art.Hex("#4e8250"), 8);
                glow.Circle(rx + w - 4, ry + 4, 3, new Color(1, .3f, .3f, 1), 8); mb.Circle(rx + w - 4, ry + 4, 1.4f, Art.Hex("#ff5d5d"), 6);
                b.labelPos = new Vector2(rx + w / 2 - 8, ry + d / 2 + 8);
            }
            else
            {
                // ===== ko'p qavatli turar joy (balkonli) =====
                mb.Box(x, y, w, d, h, wall);
                FRect(mb, x, y, d, 0, 0, w, 4, Art.Shade(wall, -.28f));
                int floors = Mathf.Max(2, Mathf.RoundToInt((h - 8) / 14f));
                float fh = (h - 10) / floors;
                var frame = Art.Shade(wall, -.38f);
                for (int f = 0; f < floors; f++)
                {
                    float z = 5 + f * fh;
                    // qavatlar orasidagi chiziq
                    if (f > 0) FRect(mb, x, y, d, 0, z - 1.5f, w, z - .8f, Art.Shade(wall, -.1f));
                    for (int k = 0; k < 4; k++)
                    {
                        float u = 6 + k * (w - 12) / 4f;
                        bool lit = ((b.c * 5 + b.r * 7 + f * 3 + k) % 5) < 2;
                        if (f == 0 && k == 1) continue; // eshik joyi
                        Window(mb, glow, x, y, d, u + 1, z + 2, 8, fh * .55f, frame, lit, seed + f + k);
                        // balkon (har ikkinchi ustunda, 1-qavatdan yuqorida)
                        if (f > 0 && (k % 2 == (b.c + b.r) % 2))
                        {
                            float bz = z + .5f;
                            FRect(mb, x, y, d, u - 1.5f, bz, u + 11.5f, bz + 1.5f, Art.Shade(wall, .15f));
                            var p0 = F(x, y, d, u - 1.5f, bz); var p1 = F(x, y, d, u + 11.5f, bz);
                            mb.Quad(new Vector2(p0.x, p0.y + 3), new Vector2(p1.x, p1.y + 3), p1, p0, Art.Shade(wall, -.15f));       // tag tomoni
                            mb.Quad(new Vector2(p0.x, p0.y + 3), new Vector2(p1.x, p1.y + 3), new Vector2(p1.x, p1.y - 2), new Vector2(p0.x, p0.y - 2), Art.Hex("#e9e1d2", .9f)); // to'siq
                            for (float r2 = p0.x + 2; r2 < p1.x - 1; r2 += 2.5f) mb.Rect(r2, p0.y - 2, .6f, 4, Art.Shade(wall, -.3f));
                            if ((seed + f + k) % 3 == 0) mb.Circle(p0.x + 3, p0.y - 3, 2.4f, Art.Hex("#69a064"), 6); // gul
                        }
                    }
                }
                // podyezd eshigi va soyabon
                float du = 6 + (w - 12) / 4f;
                FRect(mb, x, y, d, du, 0, du + 11, 14, Art.Hex("#6b5446"));
                FRect(mb, x, y, d, du + 1.5f, 8, du + 9.5f, 12.5f, Art.Hex("#ffd48a"));
                var cp = F(x, y, d, du - 3, 15);
                mb.Quad(cp, F(x, y, d, du + 14, 15), new Vector2(F(x, y, d, du + 14, 15).x, F(x, y, d, du + 14, 15).y + 3), new Vector2(cp.x, cp.y + 3), Art.Hex("#2f6f66"));
                var lamp = F(x, y, d, du + 5.5f, 16.5f);
                mb.Circle(lamp.x, lamp.y, 1.4f, Art.Hex("#fff1c9"), 6); glow.Circle(lamp.x, lamp.y, 5, new Color(1, .9f, .7f, .8f), 8);
                Ornament(mb, x, y, d, 2, w - 2, h - 8, Art.Shade(wall, .18f), Art.Hex("#2f9e92"), Art.Hex("#e9c46a"));
                // yon devor: derazalar va konditsionerlar
                for (int f = 0; f < floors; f++)
                    for (int k = 0; k < 2; k++)
                    {
                        float z = 5 + f * fh + 2, t = 10 + k * (d - 24) / 1.4f;
                        bool lit = ((b.c + f * 2 + k) % 4) == 0;
                        SideWindow(mb, x, y, w, t, z, 9, fh * .5f, frame, lit);
                        if ((f + k + b.r) % 3 == 0)
                        {
                            var ac = E(x, y, w, t + 11, z);
                            mb.Quad(ac, new Vector2(ac.x + 3, ac.y - 2), new Vector2(ac.x + 3, ac.y + 3), new Vector2(ac.x, ac.y + 5), Art.Hex("#e8eef4"));
                        }
                    }
                // tom: parapet, suv baki / antenna / quyosh panellari
                float rx = x + h * Art.OX, ry = y + h * Art.OY;
                var edge = Art.Shade(wall, -.15f);
                mb.Rect(rx + 2, ry + 2, w - 4, 2.5f, edge); mb.Rect(rx + 2, ry + d - 4.5f, w - 4, 2.5f, edge);
                mb.Rect(rx + 2, ry + 2, 2.5f, d - 4, edge); mb.Rect(rx + w - 4.5f, ry + 2, 2.5f, d - 4, edge);
                int kind = (b.c * 2 + b.r * 3) % 3;
                if (kind == 0) { mb.Box(rx + 8, ry + 8, 14, 10, 7, Art.Hex("#c9ced2"), null, false); mb.Box(rx + 28, ry + 8, 10, 8, 5, Art.Hex("#b9bec2"), null, false); }
                else if (kind == 1) { for (int i = 0; i < 3; i++) { mb.Rect(rx + 8 + i * 12, ry + 8, 10, 16, Art.Hex("#3f5f86")); mb.Rect(rx + 8 + i * 12, ry + 8, 10, 1, new Color(1, 1, 1, .35f)); mb.Rect(rx + 8 + i * 12, ry + 15.5f, 10, .8f, new Color(1, 1, 1, .25f)); } }
                else { mb.Rect(rx + 8, ry + 4, 16, 10, Art.Hex("#a9b4bc")); mb.Ellipse(rx + 16, ry + 14, 8, 4, Art.Hex("#8e9aa3")); mb.Ellipse(rx + 16, ry + 4, 8, 4, Art.Hex("#a9b4bc")); mb.Rect(rx + 34, ry + 2, 1, 14, Art.Hex("#6b737a")); mb.Rect(rx + 30, ry + 5, 9, .8f, Art.Hex("#6b737a")); }
                if (Eski) for (int i = 0; i < 5; i++) mb.Circle(rx + w - 36 + i * 6, ry + d - 10 + (i % 2) * 3, 5, Art.Hex("#5f8f4e"), 8);
                b.labelPos = new Vector2(rx + w / 2 + 8, ry + d / 2 + 6);
            }
            if (D.id == "markaz" && b.key == Data.HOME_R)
            {
                float rx = x + h * Art.OX, ry = y + h * Art.OY;
                for (int i = 0; i < 6; i++) mb.Circle(rx + w - 30 + i * 5, ry + d - 8 + (i % 2) * 3, 5, i % 2 == 1 ? Art.Hex("#5f8f4e") : Art.Hex("#4b7a40"), 8);
            }
            mb.Tree(x0 + 11, y0 + BS - 9, 9, seed, D.trees);
        }

        void BuildTall(Block b, MB glow)
        {
            var mb = new MB();
            float x0 = b.x0, y0 = b.y0;
            int seed = b.c * 31 + b.r * 17 + D.id.Length * 7;
            if (b.type == "park")
            {
                float[,] trees = { { 26, 28, 13 }, { 114, 30, 12 }, { 30, 112, 13 }, { 112, 112, 14 }, { 62, 108, 9 }, { 98, 54, 9 }, { 46, 64, 8 } };
                for (int i = 0; i < trees.GetLength(0); i++) mb.Tree(x0 + trees[i, 0], y0 + trees[i, 1], trees[i, 2], seed + i, D.trees);
                mb.Box(x0 + BS / 2 - 16, y0 + BS - 30, 32, 7, 6, Art.Hex("#9a7452"));
                mb.Box(x0 + 16, y0 + BS / 2 + 16, 7, 28, 6, Art.Hex("#9a7452"));
            }
            else if (b.type == "bazar")
            {
                float x = x0 + 16, y = y0 + 30, w = BS - 32, d = BS - 46, h = 30;
                mb.Box(x, y, w, d, h, Art.Hex("#e9d9b8"));
                for (int i = 0; i < 5; i++)
                {
                    var p = MB.FP(x, y, d, h, 8 + i * (w - 16) / 4.2f, 0);
                    mb.Rect(p.x, p.y - 12, 12, 12, Art.Hex("#7a5a3a"));
                    mb.Arc(p.x + 6, p.y - 12, 6, 6, Mathf.PI, Mathf.PI * 2, Art.Hex("#7a5a3a"), 8);
                }
                float cx = x + h * Art.OX + w / 2, cy = y + h * Art.OY + d / 2;
                mb.Rect(cx - 36, cy - 14, 72, 14, Art.Hex("#2f8f9a"));
                for (int i = 0; i < 9; i++) mb.Rect(cx - 34 + i * 8, cy - 10, 4, 6, Art.Hex("#e9c46a"));
                mb.Arc(cx, cy - 14, 38, 40, Mathf.PI, Mathf.PI * 2, Art.Hex("#2a9aa6"), 24);
                mb.Arc(cx - 6, cy - 16, 26, 32, Mathf.PI, Mathf.PI * 2, Art.Hex("#4fc1c9"), 20);
                mb.Arc(cx - 14, cy - 22, 9, 14, Mathf.PI, Mathf.PI * 2, Art.Hex("#8fe3e6"), 12);
                mb.Rect(cx - 1.5f, cy - 62, 3, 10, Art.Hex("#e9c46a"));
                mb.Circle(cx, cy - 64, 3, Art.Hex("#e9c46a"));
                mb.Arc(x + h * Art.OX + 12, y + h * Art.OY + 10, 9, 10, Mathf.PI, Mathf.PI * 2, Art.Hex("#3fb0bb"), 10);
                mb.Arc(x + h * Art.OX + w - 12, y + h * Art.OY + 10, 9, 10, Mathf.PI, Mathf.PI * 2, Art.Hex("#3fb0bb"), 10);
                b.labelPos = new Vector2(cx, cy + 21);
                signs.Add(new KeyValuePair<Vector2, Color>(new Vector2(cx, cy - 20), Art.Hex("#3fb0bb")));
            }
            else if (b.type == "rest") DrawRest(mb, glow, b, seed);
            else DrawHouse(mb, glow, b, seed);
            // fonar
            bool corner = (b.c + b.r) % 2 == 1;
            float lx = corner ? x0 + 7 : x0 + BS - 7, ly = corner ? y0 + BS - 7 : y0 + 7;
            mb.Ellipse(lx + 10, ly + 3, 10, 3, new Color(.08f, .11f, .16f, .25f));
            mb.Rect(lx - 1.5f, ly - 30, 3, 30, Art.Hex("#2c3138"));
            mb.Rect(lx - 1.5f, ly - 30, 8, 2.5f, Art.Hex("#2c3138"));
            mb.Circle(lx + 6, ly - 27, 2.6f, Art.Hex("#fff1c9"));
            glow.Circle(lx + 6, ly - 27, 6, new Color(1f, .96f, .82f, .9f), 10);
            lampGround.Add(new Vector2(lx + 6, ly));

            Art.MeshObj(root, "Block " + b.key, mb.Build(), 1000 + (int)(y0 + 120));
        }

        void BuildGaiBooths()
        {
            for (int i = 0; i < D.gai.GetLength(0); i++)
            {
                int c = D.gai[i, 0], r = D.gai[i, 1];
                float x = c * CELL + 70, y = r * CELL + 70;
                var mb = new MB();
                mb.Box(x, y, 26, 20, 26, Art.Hex("#e8eef4"), Art.Hex("#2f4f7f"));
                var f = MB.FP(x, y, 20, 26, 3, .55f);
                mb.Rect(f.x, f.y - 6, 20, 7, Art.Hex("#8cc4ff"));
                Art.MeshObj(root, "GAI booth", mb.Build(), 1000 + (int)(y + 20));
            }
        }

        void BuildLabels()
        {
            var go = new GameObject("Labels", typeof(RectTransform));
            go.transform.SetParent(root, false);
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.WorldSpace;
            cv.sortingOrder = 4000;
            var sc = go.AddComponent<CanvasScaler>();
            sc.dynamicPixelsPerUnit = UI.WorldDpu(Game.MH / Game.ZOOM);
            labelRoot = (RectTransform)go.transform;
            labelRoot.pivot = new Vector2(0, 1);
            labelRoot.sizeDelta = new Vector2(Game.MW, Game.MH);
            labelRoot.localScale = Vector3.one * Art.K;
            labelRoot.localPosition = Vector3.zero;

            foreach (var b in blocks)
            {
                if (b.type == "house")
                {
                    bool special = D.id == "markaz" && (b.key == Data.HOME_R || b.key == Data.HOME_S);
                    var shadow = UI.Box(labelRoot, new Color(0, 0, 0, .15f), "plateShadow");
                    shadow.pixelsPerUnitMultiplier = 3f;
                    UI.Place(shadow.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(b.labelPos.x + 1, -b.labelPos.y - 2), new Vector2(50, 22));
                    var plate = UI.Box(labelRoot, special ? Art.Hex("#fff3cf") : Color.white, "plate");
                    plate.pixelsPerUnitMultiplier = 3f;
                    var prt = plate.rectTransform;
                    UI.Place(prt, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(b.labelPos.x, -b.labelPos.y), new Vector2(50, 22));
                    var t = UI.Label(prt, "", 12, Art.Hex("#2a3a42"), TextAnchor.MiddleCenter, FontStyle.Bold);
                    UI.Fill(t.rectTransform);
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    b.label = t; b.plate = prt;
                    shadow.transform.SetParent(prt, true);
                    shadow.transform.SetAsFirstSibling();
                }
                else if (b.type == "rest")
                {
                    var t = UI.Label(labelRoot, "", 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UI.Place(t.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(b.labelPos.x, -b.labelPos.y), new Vector2(96, 18));
                    t.horizontalOverflow = HorizontalWrapMode.Overflow;
                    b.label = t;
                }
                else if (b.type == "bazar")
                {
                    var plate = UI.Box(labelRoot, Color.white, "bazar");
                    plate.pixelsPerUnitMultiplier = 3f;
                    UI.Place(plate.rectTransform, new Vector2(0, 1), new Vector2(.5f, .5f), new Vector2(b.labelPos.x, -b.labelPos.y), new Vector2(96, 20));
                    bazarLabel = UI.Label(plate.transform, "", 11, Art.Hex("#1d5f68"), TextAnchor.MiddleCenter, FontStyle.Bold);
                    UI.Fill(bazarLabel.rectTransform);
                    bazarLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                }
            }
        }

        public void RefreshLabels()
        {
            foreach (var b in blocks)
            {
                if (b.label == null) continue;
                if (b.type == "house")
                {
                    b.label.text = b.Label;
                    float w = Mathf.Max(48, b.label.preferredWidth + 14);
                    b.plate.sizeDelta = new Vector2(w, 22);
                    if (b.plate.childCount > 0) ((RectTransform)b.plate.GetChild(0)).sizeDelta = new Vector2(w, 22);
                }
                else if (b.type == "rest")
                {
                    string n = Loc.Tx(b.rest.name);
                    b.label.text = n;
                    b.label.fontSize = n.Length > 12 ? 10 : 12;
                }
            }
            if (bazarLabel != null) bazarLabel.text = Loc.Get("bazar");
        }
    }
}
