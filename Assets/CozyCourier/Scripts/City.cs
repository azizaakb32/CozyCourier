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
            else if (b.type == "rest")
            {
                float x = x0 + 18, y = y0 + 22, w = BS - 40, d = BS - 40, h = 30;
                mb.Box(x, y, w, d, h, Art.Hex("#f1e5cf"), Art.Hex("#f7efe0"));
                mb.Rect(x + h * Art.OX + 4, y + h * Art.OY + 4, w - 8, d - 8, new Color(0, 0, 0, .06f));
                for (int i = 0; i < 7; i++)
                {
                    var a = MB.FP(x, y, d, h, i * w / 7, .6f);
                    var e = MB.FP(x, y, d, h, (i + 1) * w / 7, .6f);
                    mb.Quad(a, e, new Vector2(e.x - 1, e.y + 11), new Vector2(a.x + 1, a.y + 11), i % 2 == 1 ? Art.Hex("#fff8ec") : b.color);
                    mb.Arc((a.x + e.x) / 2, a.y + 11, (e.x - a.x) / 2, 3, 0, Mathf.PI, i % 2 == 1 ? Art.Hex("#fff8ec") : b.color, 6);
                }
                for (int i = 0; i < 3; i++)
                {
                    var p = MB.FP(x, y, d, h, 10 + i * (w - 20) / 3, .05f);
                    mb.Rect(p.x, p.y - 11, (w - 20) / 3 - 6, 10, Art.Hex("#ffe1a1"));
                    glow.Rect(p.x, p.y - 11, (w - 20) / 3 - 6, 10, new Color(1f, .82f, .55f, .9f));
                }
                float ux = x0 + BS - 24, uy = y0 + BS - 18;
                mb.Ellipse(ux + 5, uy + 3, 12, 5, new Color(.08f, .11f, .16f, .2f));
                mb.Rect(ux - 1, uy - 18, 2, 18, Art.Hex("#6b4a33"));
                mb.Ellipse(ux, uy - 18, 13, 6, b.color);
                mb.Ellipse(ux - 3, uy - 19, 5, 2, new Color(1, 1, 1, .35f));
                float sx = x + h * Art.OX + 6, sy = y + h * Art.OY + d / 2 - 14, sw = w - 12;
                mb.Box(sx, sy, sw, 16, 14, b.color, b.color, false);
                b.labelPos = new Vector2(sx + sw / 2 + 14 * Art.OX, sy + 8 + 14 * Art.OY);
                signs.Add(new KeyValuePair<Vector2, Color>(b.labelPos, b.color));
                mb.Tree(x0 + 10, y0 + BS - 8, 9, seed, D.trees);
            }
            else
            {
                float x = x0 + 22, y = y0 + 26, w = BS - 50, d = BS - 48, h = b.h;
                mb.Box(x, y, w, d, h, b.wall);
                int floors = Mathf.Max(2, Mathf.RoundToInt(h / 15f));
                for (int f = 0; f < floors; f++)
                    for (int k = 0; k < 4; k++)
                    {
                        var p = MB.FP(x, y, d, h, 8 + k * (w - 16) / 3 - 4, (f + .5f) / (floors + .3f));
                        bool lit = ((b.c * 5 + b.r * 7 + f * 3 + k) % 5) < 2;
                        mb.Rect(p.x - 1, p.y - 5, 10, 9, Art.Shade(b.wall, -.35f));
                        mb.Rect(p.x, p.y - 4, 8, 7, lit ? Art.Hex("#ffe1a1") : Art.Hex("#5f7488"));
                        mb.Rect(p.x, p.y - 4, 3, 2, new Color(1, 1, 1, .3f));
                        if (lit)
                        {
                            glow.Rect(p.x, p.y - 4, 8, 7, Art.Hex("#ffd48a"));
                            glow.Circle(p.x + 4, p.y, 11, new Color(1f, .78f, .47f, .18f), 10);
                        }
                        if (k == 1 && f > 0 && floors > 3) mb.Rect(p.x - 3, p.y + 3, 14, 2, Art.Shade(b.wall, -.25f));
                    }
                for (int f = 0; f < floors; f++)
                    for (int k = 0; k < 3; k++)
                    {
                        float vv = (f + .5f) / (floors + .3f), u = 10 + k * (d - 20) / 2.4f;
                        float px = x + w + h * vv * Art.OX + 2, py = y + u + h * vv * Art.OY;
                        mb.Poly(new[] { new Vector2(px, py), new Vector2(px + 3, py - 6), new Vector2(px + 3, py + 1), new Vector2(px, py + 7) }, new Color(.24f, .31f, .39f, .45f));
                    }
                var dp = MB.FP(x, y, d, h, w / 2 - 6, 0);
                mb.Rect(dp.x, dp.y - 12, 12, 12, Art.Hex("#6b5446"));
                mb.Rect(dp.x + 5, dp.y - 16, 2, 2, Art.Hex("#ffd48a"));
                float rx = x + h * Art.OX, ry = y + h * Art.OY;
                var edge = Art.Shade(b.wall, -.15f);
                mb.Rect(rx + 3, ry + 3, w - 6, 2, edge); mb.Rect(rx + 3, ry + d - 5, w - 6, 2, edge);
                mb.Rect(rx + 3, ry + 3, 2, d - 6, edge); mb.Rect(rx + w - 5, ry + 3, 2, d - 6, edge);
                int kind = (b.c * 2 + b.r * 3) % 3;
                if (kind == 0) { mb.Box(rx + 8, ry + 8, 14, 10, 7, Art.Hex("#c9ced2"), null, false); mb.Box(rx + 28, ry + 8, 10, 8, 5, Art.Hex("#b9bec2"), null, false); }
                else if (kind == 1) { for (int i = 0; i < 3; i++) { mb.Rect(rx + 8 + i * 12, ry + 8, 10, 16, Art.Hex("#4a6b8f")); mb.Rect(rx + 8 + i * 12, ry + 15.5f, 10, 1, new Color(1, 1, 1, .3f)); } }
                else { mb.Rect(rx + 8, ry + 4, 16, 10, Art.Hex("#a9b4bc")); mb.Ellipse(rx + 16, ry + 14, 8, 4, Art.Hex("#8e9aa3")); mb.Ellipse(rx + 16, ry + 4, 8, 4, Art.Hex("#a9b4bc")); }
                if (Eski) for (int i = 0; i < 5; i++) mb.Circle(rx + w - 36 + i * 6, ry + d - 10 + (i % 2) * 3, 5, Art.Hex("#5f8f4e"), 8);
                if (D.id == "markaz" && b.key == Data.HOME_R)
                    for (int i = 0; i < 6; i++) mb.Circle(rx + w - 30 + i * 5, ry + d - 8 + (i % 2) * 3, 5, i % 2 == 1 ? Art.Hex("#5f8f4e") : Art.Hex("#4b7a40"), 8);
                b.labelPos = new Vector2(rx + w / 2 + 8, ry + d / 2 + 6);
                mb.Tree(x0 + 11, y0 + BS - 9, 9, seed, D.trees);
            }
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
