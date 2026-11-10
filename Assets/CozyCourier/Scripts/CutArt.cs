using UnityEngine;

namespace CozyCourier
{
    /// <summary>
    /// Sahna fonlari va personajlar (veb versiyadagi chizmalar ko'chirmasi).
    /// Koordinatalar 1260×860 "sahna" pikselida, y pastga.
    /// </summary>
    public static class CutArt
    {
        const float W = 1260, H = 860;
        static Color C(string h, float a = 1) => Art.Hex(h, a);
        static float Rnd(int i) => Art.Rnd(i);

        // ---------------------------------------------------------------- statik fonlar
        public static void Background(MB m, Shot sh)
        {
            switch (sh.bg)
            {
                case "office": Office(m); break;
                case "street": Street(m); break;
                case "kitchen": Kitchen(m); break;
                case "door": Door(m, sh); break;
                case "yard": Yard(m); break;
            }
        }

        // ---------------------------------------------------------------- har kadr o'zgaradigan qismlar
        public static void Anim(MB m, Shot sh, float t)
        {
            switch (sh.bg)
            {
                case "office":
                    {
                        float a1 = t / 8f, a2 = t / .7f;
                        m.Line(540, 180, 540 + Mathf.Sin(a1) * 22, 180 - Mathf.Cos(a1) * 22, 4, C("#2a1606"));
                        m.Line(540, 180, 540 + Mathf.Sin(a2) * 34, 180 - Mathf.Cos(a2) * 34, 2, C("#2a1606"));
                        Steam(m, 260, 528, t); Steam(m, 150, 484, t + .5f);
                        if (!Save.RenoDone("chair"))
                            for (int i = 0; i < 9; i++)
                            {
                                float a = -1.2f + i * .3f + Mathf.Sin(t / 1.5f + i) * .03f;
                                Leaf(m, 1175, 690, a, 10, 50, i % 2 == 1 ? C("#4e8250") : C("#69a064"));
                            }
                        break;
                    }
                case "street":
                    {
                        for (int i = 0; i < 5; i++) m.Ellipse(Mathf.Repeat(i * 260 + t * 16, W + 300) - 150, 90 + i * 30, 120, 14, C("#ffdcc8", .25f));
                        bool newSign = Save.RenoDone("sign");
                        if (newSign) for (int i = 0; i < 14; i++) { bool on = Mathf.Sin(t * 5 + i) > 0; var cc = on ? C("#fff6c8") : C("#e9c46a"); m.Circle(480 + i * 23, 472, 3, cc, 8); m.Circle(480 + i * 23, 520, 3, cc, 8); }
                        int ph = Mathf.FloorToInt(t / 2.5f) % 3;
                        string[] lc = { "#ff5d5d", "#f2c14e", "#5fd08a" };
                        for (int i = 0; i < 3; i++) m.Circle(1024, 440 + i * 28, 10, ph == i ? C(lc[i]) : new Color(1, 1, 1, .1f), 12);
                        break;
                    }
                case "kitchen":
                    for (int k = 0; k < 3; k++) Steam(m, 1030 + k * 40, 500, t + k * .4f);
                    break;
                case "door":
                    for (int k = 0; k < 2; k++)
                    {
                        float px = k == 0 ? 460 : 880;
                        string[] fc = { "#ee9583", "#ffd166", "#c3a6e8" };
                        for (int i = 0; i < 3; i++) m.Circle(px - 16 + i * 16, 666 + Mathf.Sin(t / .8f + i) * 2, 6, C(fc[i]), 10);
                    }
                    break;
                case "yard":
                    {
                        for (int i = 0; i < 90; i++) m.Rect(Rnd(i) * W, Rnd(i + 50) * 360, 2, 2, new Color(1, 1, 1, .3f + .7f * Mathf.Abs(Mathf.Sin(t / .7f + i * 1.7f))));
                        string[] cols = { "#ffd166", "#ee9583", "#7ec8e3", "#8fd6a8" };
                        for (int i = 0; i <= 24; i++)
                        {
                            float x = i * W / 24, y = 200 + Mathf.Sin(i / 24f * Mathf.PI) * 70, on = .6f + .4f * Mathf.Sin(t / .5f + i);
                            m.Glow(x, y, 30, C(cols[i % 4], on * .6f), 12);
                            m.Circle(x, y + 3, 5, C(cols[i % 4]), 8);
                        }
                        break;
                    }
            }
        }

        public static void Foreground(MB m, Shot sh, float t)
        {
            if (sh.bg != "yard") return;
            m.Rect(180, 640, 900, 60, C("#4f3524"));
            m.Rect(160, 600, 940, 46, C("#7a5236"));
            m.Rect(200, 588, 860, 30, C("#b5403a"));
            for (float x = 210; x < 1060; x += 24) m.Poly(new[] { new Vector2(x, 606), new Vector2(x + 5, 600), new Vector2(x + 10, 606), new Vector2(x + 5, 612) }, C("#e9c46a"));
            m.Ellipse(630, 592, 150, 30, C("#2c6aa8"), 30);
            m.Ellipse(630, 590, 134, 25, Color.white, 30);
            m.Arc(630, 584, 110, 32, Mathf.PI, Mathf.PI * 2, C("#e8a33a"), 24);
            for (int i = 0; i < 26; i++) m.Rect(540 + Rnd(i) * 180, 558 + Rnd(i + 9) * 22, 9, 3, C("#d9694f"));
            for (int i = 0; i < 40; i++) m.Rect(540 + Rnd(i + 90) * 180, 560 + Rnd(i + 30) * 22, 2, 2, C("#fff5dc"));
            m.Ellipse(630, 556, 26, 10, C("#8a4a2a"));
            Steam(m, 630, 540, t);
            foreach (float bx in new float[] { 330, 920 }) { m.Ellipse(bx, 588, 46, 16, C("#c98a4b")); m.Ellipse(bx, 586, 26, 8, C("#e7b878")); }
            foreach (float px in new float[] { 440, 820 }) { m.Poly(new[] { new Vector2(px - 16, 572), new Vector2(px + 16, 572), new Vector2(px + 10, 590), new Vector2(px - 10, 590) }, C("#2c6aa8")); m.Rect(px - 13, 575, 26, 2, Color.white); }
        }

        static void Steam(MB m, float x, float y, float t)
        {
            for (int k = 0; k < 2; k++)
                for (int i = 0; i < 20; i++)
                {
                    float y1 = y - i * 3, y2 = y - (i + 1) * 3;
                    float x1 = x + k * 12 - 6 + Mathf.Sin(i / 3f + t / .3f + k) * 5, x2 = x + k * 12 - 6 + Mathf.Sin((i + 1) / 3f + t / .3f + k) * 5;
                    m.Line(x1, y1, x2, y2, 3, new Color(1, 1, 1, .35f * (1 - i / 20f)));
                }
        }

        static void Leaf(MB m, float x, float y, float ang, float rx, float ry, Color col)
        {
            // ang burchak ostida yuqoriga qaragan barg
            var pts = new Vector2[12];
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2;
                float lx = Mathf.Cos(a) * rx, ly = Mathf.Sin(a) * ry - ry;
                pts[i] = new Vector2(x + lx * Mathf.Cos(ang) - ly * Mathf.Sin(ang), y + lx * Mathf.Sin(ang) + ly * Mathf.Cos(ang));
            }
            m.Poly(pts, col);
        }

        // ---------------------------------------------------------------- ofis
        static void Office(MB m)
        {
            bool off = Save.RenoDone("office"), map = Save.RenoDone("map"), chair = Save.RenoDone("chair"), pot = Save.RenoDone("teapot");
            m.Rect(-600, -100, W + 1200, H + 200, C("#0c151b"));
            m.GradRect(-600, -40, W + 1200, 640, off ? C("#cfe3dc") : C("#e4cca6"), off ? C("#b7d2c8") : C("#d2b48a"));
            if (off) for (float x = -580; x < W + 600; x += 60) m.Rect(x, -40, 2, 640, new Color(1, 1, 1, .25f));
            else for (int i = 0; i < 30; i++) m.Circle(Rnd(i) * W, Rnd(i + 3) * 560, 30 + Rnd(i + 6) * 40, C("#7a5236", .08f), 14);
            m.Rect(-600, 600, W + 1200, 150, off ? C("#2f6f66") : C("#93613f"));
            m.Rect(-600, 596, W + 1200, 8, off ? C("#3f8a7f") : C("#a97550"));
            for (float x = -540; x < W + 600; x += 90) m.Rect(x, 610, 3, 140, off ? C("#285e57") : C("#7f5235"));
            m.GradRect(-600, 750, W + 1200, H - 700, off ? C("#a0703f") : C("#6a4830"), off ? C("#7f5530") : C("#4d3322"));
            for (float y = 770; y < H + 40; y += 26) m.Rect(-600, y, W + 1200, 1, new Color(0, 0, 0, .15f));
            if (off)
            {
                m.Rect(380, 790, 520, 60, C("#b5403a"));
                for (float x = 390; x < 890; x += 26) m.Poly(new[] { new Vector2(x, 820), new Vector2(x + 8, 806), new Vector2(x + 16, 820), new Vector2(x + 8, 834) }, C("#e9c46a"));
            }
            // deraza
            m.Rect(810, 100, 370, 300, C("#6f4a30"));
            m.GradStops(825, 115, 340, 270, (0f, C("#1f3157")), (.7f, C("#5a4a7a")), (1f, C("#c4746a")));
            m.Circle(1090, 170, 22, C("#f6ecc8"));
            for (int i = 0; i < 8; i++)
            {
                float bh = 60 + Rnd(i) * 120, bx = 825 + i * 44;
                m.Rect(bx, 385 - bh, 40, bh, C("#1a2742"));
                for (int k = 0; k < 6; k++) if (Rnd(i * 9 + k) < .45f) m.Rect(bx + 8 + (k % 2) * 16, 385 - bh + 12 + (k / 2) * 22, 8, 10, C("#ffd28a"));
            }
            m.Rect(990, 115, 10, 270, C("#6f4a30")); m.Rect(825, 245, 340, 10, C("#6f4a30"));
            m.Poly(new[] { new Vector2(840, 380), new Vector2(900, 120), new Vector2(940, 120), new Vector2(880, 380) }, new Color(1, 1, 1, .07f));
            var cur = off ? C("#e9c46a") : C("#c0506a");
            m.Rect(790, 90, 26, 320, cur); m.Rect(1174, 90, 26, 320, cur);
            // plakat
            m.Rect(120, 116, 250, 180, new Color(0, 0, 0, .18f));
            m.Rect(110, 110, 250, 180, C("#f4a259"));
            if (map)
            {
                m.Rect(136, 336, 228, 128, C("#6f4a30")); m.Rect(144, 344, 212, 112, C("#efe6cf"));
                for (int i = 0; i < 20; i++) { float x1 = 144 + i * 10.6f; m.Line(x1, 390 + Mathf.Sin(i * .5f) * 14, x1 + 10.6f, 390 + Mathf.Sin((i + 1) * .5f) * 14, 4, C("#7ec8e3")); }
                int L = Save.Level;
                m.Circle(230, 410, 7, C("#f4a259"));
                if (L >= 3) m.Circle(180, 430, 7, C("#2f9e92"));
                if (L >= 5) m.Circle(300, 370, 7, C("#9a6cc4"));
            }
            // soat
            m.Circle(540, 180, 47, C("#6f4a30"), 28);
            m.Circle(540, 180, 44, C("#fff8ea"), 28);
            // chiroq
            m.Line(660, -60, 660, 80, 3, C("#3a2a1e"));
            m.Glow(660, 110, 380, C("#ffd68c", .45f), 28);
            m.Poly(new[] { new Vector2(630, 80), new Vector2(690, 80), new Vector2(712, 116), new Vector2(608, 116) }, C("#2f6f66"));
            m.Ellipse(660, 116, 40, 5, C("#fff3cf"));
            if (chair)
            {
                m.Ellipse(1110, 770, 110, 14, new Color(0, 0, 0, .25f));
                m.RRect(1020, 540, 180, 150, 30, C("#8a3a3a"));
                m.RRect(1010, 660, 200, 70, 20, C("#a54848"));
                m.RRect(990, 610, 40, 120, 16, C("#7a3030")); m.RRect(1190, 610, 40, 120, 16, C("#7a3030"));
                m.Rect(1020, 730, 12, 40, C("#5c3d27")); m.Rect(1188, 730, 12, 40, C("#5c3d27"));
            }
            // stol va choynak
            m.Rect(40, 560, 330, 26, C("#6f4a30")); m.Rect(40, 556, 330, 8, C("#8a5d3d"));
            m.Rect(60, 586, 16, 170, C("#5c3d27")); m.Rect(334, 586, 16, 170, C("#5c3d27"));
            var tp = pot ? C("#f4f1ea") : C("#2c6aa8"); var tpd = pot ? C("#d6a62a") : Color.white;
            if (pot) m.Ellipse(150, 526, 43, 35, C("#d6a62a"));
            m.Ellipse(150, 526, 40, 32, tp);
            for (int i = 0; i < 5; i++) m.Circle(122 + i * 14, 524 + (i % 2) * 8, 4, tpd, 8);
            m.Ellipse(136, 512, 10, 6, new Color(1, 1, 1, .25f));
            m.Rect(140, 488, 20, 8, pot ? C("#d6a62a") : tp);
            m.Poly(new[] { new Vector2(188, 521), new Vector2(226, 496), new Vector2(228, 502), new Vector2(192, 531) }, pot ? C("#d6a62a") : tp);
            foreach (float px in new float[] { 260, 310 }) { m.Poly(new[] { new Vector2(px - 20, 536), new Vector2(px + 20, 536), new Vector2(px + 12, 556), new Vector2(px - 12, 556) }, tp); m.Rect(px - 16, 540, 32, 3, tpd); }
            if (!chair) m.Poly(new[] { new Vector2(1140, 690), new Vector2(1210, 690), new Vector2(1200, 750), new Vector2(1150, 750) }, C("#b8622a"));
        }

        // ---------------------------------------------------------------- ko'cha
        static void Street(MB m)
        {
            m.Rect(-600, -100, W + 1200, H + 200, C("#454e57"));
            m.GradStops(-600, -40, W + 1200, 680, (0f, C("#3e3466")), (.55f, C("#b86a6a")), (1f, C("#f4ac5f")));
            m.Glow(980, 470, 260, C("#ffe6aa", .9f), 28);
            m.Circle(980, 470, 74, C("#ffdca0", .8f), 28);
            for (int i = -4; i < 19; i++) { float bh = 140 + Rnd(i + 3) * 220, bx = i * 95 - 40; m.Rect(bx, 640 - bh, 100, bh, C("#7a6286")); }
            for (int i = -3; i < 13; i++)
            {
                float bh = 200 + Rnd(i + 40) * 200, bx = i * 150 - 50;
                m.Rect(bx, 640 - bh, 130, bh, C("#43375a")); m.Rect(bx, 640 - bh, 130, 6, C("#4d4066"));
                for (int r = 0; r < Mathf.FloorToInt(bh / 46); r++) for (int k = 0; k < 3; k++) if (Rnd(i * 31 + r * 7 + k) < .5f) m.Rect(bx + 18 + k * 36, 640 - bh + 20 + r * 46, 16, 20, C("#ffd28a", .85f));
            }
            bool newSign = Save.RenoDone("sign");
            if (newSign) { m.RRect(470, 466, 320, 62, 12, C("#f4a259")); m.Rect(470, 522, 320, 6, C("#d4772f")); }
            else { m.Rect(470, 470, 320, 58, C("#2f6f66")); m.Rect(470, 522, 320, 6, C("#24574f")); }
            m.Rect(-600, 640, W + 1200, 70, C("#8e8278")); m.Rect(-600, 640, W + 1200, 5, C("#a49789"));
            m.Rect(-600, 710, W + 1200, H - 660, C("#454e57"));
            for (float x = -560; x < W + 600; x += 80) m.Rect(x, 780, 44, 5, C("#f0dc96", .6f));
            foreach (float lx in new float[] { 130, 1130 })
            {
                m.Rect(lx - 4, 400, 8, 240, C("#2b2d33")); m.Rect(lx - 4, 400, 40, 6, C("#2b2d33"));
                m.Glow(lx + 34, 412, 140, C("#ffdc96", .7f), 24);
                m.Circle(lx + 34, 414, 7, C("#ffe2a6"));
            }
            m.Rect(1020, 470, 8, 170, C("#2b2d33")); m.RRect(1004, 420, 40, 96, 10, C("#1d2126"));
            // skuter
            var sc = Data.Look("scooter", Save.Equipped("scooter")); var bg = Data.Look("bag", Save.Equipped("bag"));
            float ox = 300, oy = 700;
            m.Ellipse(ox, oy + 6, 80, 10, new Color(0, 0, 0, .25f));
            m.Circle(ox - 50, oy - 10, 18, C("#1e1e1e")); m.Circle(ox + 50, oy - 10, 18, C("#1e1e1e"));
            m.RRect(ox - 60, oy - 50, 100, 30, 14, C(sc.c1)); m.RRect(ox - 56, oy - 50, 92, 12, 6, C(sc.c0)); m.Rect(ox + 30, oy - 90, 10, 60, C(sc.c1));
            m.RRect(ox - 70, oy - 100, 52, 52, 6, C(bg.c1)); m.RRect(ox - 70, oy - 100, 52, 20, 6, C(bg.c0));
        }

        // ---------------------------------------------------------------- oshxona
        static void Kitchen(MB m)
        {
            m.Rect(-600, -100, W + 1200, H + 200, C("#7f5f46"));
            m.Rect(-600, -40, W + 1200, 640, C("#d4e5e8"));
            for (float x = -600; x < W + 600; x += 50) m.Rect(x, -40, 2, 640, C("#bad0d4"));
            for (float y = 0; y < 600; y += 50) m.Rect(-600, y, W + 1200, 2, C("#bad0d4"));
            for (int i = 0; i < 12; i++) { float x = Mathf.Floor(Rnd(i) * 25) * 50, y = Mathf.Floor(Rnd(i + 9) * 11) * 50; m.Rect(x + 2, y + 2, 46, 46, C("#4f97c9", .25f)); }
            m.Rect(80, 100, 280, 230, C("#7f5f46")); m.Rect(92, 112, 256, 206, C("#cfeaf6"));
            m.Poly(new[] { new Vector2(110, 300), new Vector2(170, 130), new Vector2(200, 130), new Vector2(140, 300) }, new Color(1, 1, 1, .4f));
            m.Poly(new[] { new Vector2(92, 112), new Vector2(348, 112), new Vector2(700, 600), new Vector2(200, 600) }, C("#fffadc", .12f));
            m.Rect(500, 190, 420, 14, C("#7f5f46"));
            string[] jars = { "#e9c46a", "#d9694f", "#8fd6a8", "#c3a6e8", "#f4a259" };
            for (int i = 0; i < 5; i++) { m.RRect(520 + i * 78, 130, 48, 60, 8, C(jars[i])); m.Rect(528 + i * 78, 138, 8, 40, new Color(1, 1, 1, .35f)); m.Rect(526 + i * 78, 122, 36, 10, Color.white); }
            m.Rect(-600, 580, W + 1200, 24, C("#c8b49a")); m.Rect(-600, 604, W + 1200, H - 560, C("#7f5f46"));
            for (float x = 40; x < W; x += 220) m.Rect(x, 640, 180, 160, C("#6d5140"));
            foreach (var p in new[] { new Vector2(300, 566), new Vector2(360, 570) }) { m.Ellipse(p.x, p.y, 34, 12, C("#c98a4b")); m.Ellipse(p.x, p.y - 1, 18, 6, C("#e7b878")); }
            m.Rect(960, 560, 220, 30, C("#3a3a3a"));
            m.Glow(1070, 600, 80, C("#ff8c32", .7f), 20);
            m.Arc(1070, 540, 110, 34, 0, Mathf.PI, C("#1f1f1f"), 20);
            m.Ellipse(1070, 530, 110, 26, C("#1f1f1f"), 28);
            m.Ellipse(1070, 528, 92, 18, C("#e8a33a"), 28);
            for (int i = 0; i < 14; i++) m.Rect(1000 + Rnd(i) * 140, 520 + Rnd(i + 5) * 14, 8, 3, C("#d9694f"));
        }

        // ---------------------------------------------------------------- uy eshigi
        static void Door(MB m, Shot sh)
        {
            m.Rect(-600, -100, W + 1200, H + 200, C("#c7b796"));
            m.Rect(-600, -40, W + 1200, 800, C("#e4c6a4"));
            for (float y = 30; y < 760; y += 40)
            {
                m.Rect(-600, y, W + 1200, 2, C("#966e50", .18f));
                for (float x = (Mathf.RoundToInt((y - 30) / 40) % 2) * 60 - 600; x < W + 600; x += 120) m.Rect(x, y, 2, 40, C("#966e50", .18f));
            }
            m.GradRect(-600, -40, W + 1200, 340, C("#3c281e", .25f), C("#3c281e", 0));
            m.Rect(-600, 760, W + 1200, 6, C("#b5a584"));
            m.Rect(120, 240, 240, 240, C("#7f5236")); m.Rect(110, 478, 260, 14, C("#93613f"));
            if (sh.closed)
            {
                m.Rect(132, 252, 216, 216, C("#6f8fa6"));
                for (float y = 262; y < 468; y += 16) m.Rect(132, y, 216, 1, C("#5a7488"));
            }
            else
            {
                m.Rect(132, 252, 216, 216, C("#ffe1a1")); m.Rect(236, 252, 8, 216, C("#7f5236")); m.Rect(132, 356, 216, 8, C("#7f5236"));
                m.Rect(132, 252, 40, 216, C("#c0506a")); m.Rect(308, 252, 40, 216, C("#c0506a"));
                m.Glow(240, 360, 220, C("#ffd28c", .3f), 24);
            }
            m.Rect(536, 240, 248, 520, C("#5d4030")); m.Rect(526, 232, 268, 16, C("#7f5236"));
            if (!sh.closed)
            {
                m.GradRect(560, 270, 200, 490, C("#ffd9a0"), C("#f0b56c"));
                m.Rect(560, 640, 200, 120, C("#e6a35a")); m.Rect(590, 680, 140, 60, C("#c0506a"));
                m.Poly(new[] { new Vector2(560, 270), new Vector2(612, 292), new Vector2(612, 738), new Vector2(560, 760) }, C("#a5734c"));
                m.Glow(660, 700, 320, C("#ffd28c", .5f), 24);
            }
            else
            {
                m.Rect(560, 270, 200, 490, C("#a5734c")); m.Rect(580, 300, 160, 180, C("#8a5a3a")); m.Rect(580, 520, 160, 200, C("#8a5a3a"));
                m.Circle(735, 520, 7, C("#e9c46a"));
            }
            m.RRect(822, 304, 140, 54, 8, C("#24574f")); m.RRect(820, 300, 140, 54, 8, C("#2f6f66"));
            m.Rect(650, 196, 20, 30, C("#2b2d33"));
            m.Glow(660, 236, 120, C("#ffe2a6", .8f), 20);
            m.Circle(660, 234, 10, C("#ffe2a6"));
            foreach (float px in new float[] { 460, 880 })
            {
                m.Poly(new[] { new Vector2(px - 34, 700), new Vector2(px + 34, 700), new Vector2(px + 24, 760), new Vector2(px - 24, 760) }, C("#b8622a"));
                m.Rect(px - 36, 696, 72, 8, C("#c97434"));
                for (int i = 0; i < 7; i++) m.Circle(px - 26 + i * 9, 680 - ((i * 7) % 3) * 10, 14, i % 2 == 1 ? C("#4e8250") : C("#69a064"), 12);
            }
        }

        // ---------------------------------------------------------------- hovli (yakun)
        static void Yard(MB m)
        {
            m.Rect(-600, -100, W + 1200, H + 200, C("#5f4e3c"));
            m.GradRect(-600, -40, W + 1200, 560, C("#121c36"), C("#34406a"));
            m.Glow(1110, 110, 120, C("#f6ecc8", .35f), 24);
            m.Circle(1110, 110, 40, C("#f6ecc8"), 24); m.Circle(1126, 100, 36, C("#16203d"), 24);
            m.Rect(-600, 400, W + 1200, 240, C("#b18d6b"));
            for (float x = -560; x < W + 600; x += 160) m.Rect(x, 400, 6, 240, C("#a07e5e"));
            foreach (float wx in new float[] { 140, 1000 }) { m.Rect(wx - 8, 452, 136, 116, C("#7f5236")); m.Rect(wx, 460, 120, 100, C("#ffe1a1")); m.Glow(wx + 60, 510, 140, C("#ffd28c", .35f), 20); }
            m.Rect(-600, 640, W + 1200, H - 600, C("#5f4e3c"));
            for (int i = 0; i < 30; i++) m.Rect(Rnd(i) * W, 650 + Rnd(i + 4) * 200, 30, 3, C("#4f3f30"));
            m.Rect(-600, 130, W + 1200, 12, C("#5a3d28")); m.Rect(54, 130, 12, 520, C("#5a3d28")); m.Rect(1194, 130, 12, 520, C("#5a3d28"));
            string[] lv = { "#3f7a45", "#56934f", "#6aa85e" };
            for (int i = 0; i < 80; i++) m.Circle(Rnd(i + 7) * W, 108 + Rnd(i + 70) * 64, 18 + Rnd(i + 30) * 14, C(lv[i % 3]), 12);
            for (int i = 0; i < 10; i++) { float gx = 80 + i * 120; for (int k = 0; k < 6; k++) m.Circle(gx + (k % 2) * 8, 170 + k * 7, 6, C("#7b4a8c"), 8); }
            for (int i = 0; i < 48; i++) { float x1 = i * W / 48, x2 = (i + 1) * W / 48; m.Line(x1, 200 + Mathf.Sin(i / 48f * Mathf.PI) * 70, x2, 200 + Mathf.Sin((i + 1) / 48f * Mathf.PI) * 70, 1.5f, C("#2b2b2b")); }
        }

        // ---------------------------------------------------------------- personajlar
        /// <summary>Personajni chizadi. x — markaz, feet — oyoq ostidagi chiziq, baseScale — umumiy masshtab.</summary>
        public static void Char(MB m, string id, float x, float feet, bool talk, bool dim, float t, float baseScale = 2.4f)
        {
            if (!Data.Chars.TryGetValue(id, out var d) || d.coat == null) return;
            float s = baseScale * d.s, w = d.wide;
            float breathe = Mathf.Sin(t / .65f + x) * .8f, bob = talk ? Mathf.Abs(Mathf.Sin(t / .12f)) * 2 : 0;
            m.ox = x; m.oy = feet; m.sc = s; m.alpha = dim ? .72f : 1f;
            Color coatC = C(d.coat);
            if (id == "jasur") coatC = C(Data.Look("scooter", Save.Equipped("scooter")).c1);
            var skin = C(d.skin);
            var OUT = new Color(.12f, .08f, .08f, .35f);

            m.Ellipse(0, 0, 32 * w, 7, new Color(.06f, .08f, .12f, .28f));
            float b = -bob;
            // oyoqlar
            if (d.dress)
            {
                m.Poly(new[] { new Vector2(-22, -110 + b), new Vector2(22, -110 + b), new Vector2(31, -14 + b), new Vector2(-31, -14 + b) }, Art.Shade(coatC, -.05f));
                m.RRect(-17, -14 + b, 13, 12, 4, C("#3a2a24")); m.RRect(4, -14 + b, 13, 12, 4, C("#3a2a24"));
            }
            else
            {
                var pc = C(d.pants);
                m.RRect(-14 * w, -64 + b, 12 * w, 58, 4, pc); m.RRect(2 * w, -64 + b, 12 * w, 58, 4, pc);
                m.Rect(2 * w, -64 + b, 4, 58, new Color(0, 0, 0, .15f));
                m.RRect(-17 * w, -9 + b, 16 * w, 9, 4, C("#2a2422")); m.RRect(1 * w, -9 + b, 16 * w, 9, 4, C("#2a2422"));
            }
            float by = -132 + breathe + b;
            // tana
            m.RRect(-24 * w - 1, by - 1, 48 * w + 2, (d.dress ? 42 : 80) + 2, 13, OUT);
            m.RRect(-24 * w, by, 48 * w, d.dress ? 42 : 80, 13, Art.Shade(coatC, .06f));
            m.Rect(0, by + 6, 24 * w - 4, (d.dress ? 42 : 80) - 12, Art.A(Art.Shade(coatC, -.12f), .6f));
            if (d.vest)
            {
                m.RRect(-20 * w, by + 4, 40 * w, 60, 10, C("#c8e04a"));
                m.Rect(-20 * w, by + 24, 40 * w, 4, C("#e8eef4")); m.Rect(-20 * w, by + 38, 40 * w, 4, C("#e8eef4"));
            }
            if (d.stripe != null) { for (int i = -3; i <= 3; i++) m.Rect(i * 8 * w - 1.5f, by + 4, 3, 74, C(d.stripe)); m.Rect(-24 * w, by + 48, 48 * w, 5, C("#e9c46a")); }
            if (d.apron) { m.RRect(-15, -114 + b, 30, 92, 7, id == "bahodir" ? C("#d9cbb3") : C("#fbf6ee")); if (id == "malika") m.Rect(-4, -96 + b, 8, 8, coatC); }
            if (d.badge) { m.Rect(8, by + 14, 12, 12, Color.white); m.Rect(12.5f, by + 16, 3, 8, C("#d9694f")); m.Rect(10, by + 18.5f, 8, 3, C("#d9694f")); }
            if (id == "jasur") { m.Rect(-24, by + 34, 48, 6, C("#f4a259")); m.Rect(-2, by + 4, 4, 30, C("#e3f4f1")); }
            if (id == "sardor") m.Circle(0, by + 24, 7, C("#ffd166"), 10);
            m.Poly(new[] { new Vector2(-10, by), new Vector2(0, by + 14), new Vector2(10, by), new Vector2(6, by - 2), new Vector2(0, by + 8), new Vector2(-6, by - 2) }, Art.Shade(coatC, -.25f));
            // qo'llar
            float sw = talk ? Mathf.Sin(t / .18f) * 3 : 0;
            var arm = Art.Shade(coatC, -.12f);
            m.RRect(-33 * w, by + 6, 11, 60 + sw, 6, arm); m.RRect(22 * w, by + 6, 11, 60 - sw, 6, arm);
            m.Circle(-27.5f * w, by + 68 + sw, 6, skin, 10); m.Circle(27.5f * w, by + 68 - sw, 6, skin, 10);
            if (id == "anvar") for (int i = 0; i < 5; i++) m.Rect(27.5f * w - 2, by + 60 - sw - (i + 1) * 7, 4, 7, i % 2 == 1 ? C("#111111") : Color.white);
            if (d.cane) { m.Line(30 * w, -68 + b, 38, -2 + b, 4, C("#6b4a33")); m.Arc(24 * w + 4, -68 + b, 6, 6, Mathf.PI, Mathf.PI * 2, C("#6b4a33"), 8); }
            // bosh
            float hy = -155 + breathe + b;
            m.Rect(-6, hy + 14, 12, 12, Art.Shade(skin, -.1f));
            if (d.hat == "scarf")
            {
                var sc2 = Art.Shade(C(d.scarf), -.08f);
                m.Arc(0, hy, 27, 27, Mathf.PI * .8f, Mathf.PI * 2.2f, sc2, 20);
                m.Poly(new[] { new Vector2(-20, hy + 10), new Vector2(20, hy + 10), new Vector2(20, hy + 30), new Vector2(-20, hy + 30) }, sc2);
            }
            if (d.bun) m.Circle(0, hy - 24, 10, C(d.hair), 12);
            m.Ellipse(-21, hy + 1, 4, 6, Art.Shade(skin, -.06f), 10); m.Ellipse(21, hy + 1, 4, 6, Art.Shade(skin, -.06f), 10);
            m.Circle(0, hy, 22, OUT, 24);
            m.Circle(0, hy, 21, skin, 24);
            m.Circle(-5, hy - 6, 13, Art.A(Art.Shade(skin, .1f), .6f), 16);
            if (d.hair != null) m.Arc(0, hy - 3, 21.5f, 21.5f, Mathf.PI * 1.02f, Mathf.PI * 1.98f, C(d.hair), 16);
            if (d.beard != null)
            {
                var bc = C(d.beard);
                m.Arc(0, hy + 14, d.glasses ? 17 : 15, d.glasses ? 16 : 11, 0, Mathf.PI, bc, 14);
                m.Rect(-17, hy + 4, 6, 12, bc); m.Rect(11, hy + 4, 6, 12, bc);
                m.Ellipse(-5, hy + 7, 7, 3, bc, 10); m.Ellipse(5, hy + 7, 7, 3, bc, 10);
            }
            if (id == "anvar") m.Ellipse(0, hy + 7, 9, 2.5f, C("#3a2a20"), 10);
            bool blink = Mathf.Repeat(t + x * .013f, 3.6f) < .13f;
            var browC = C(d.beard ?? d.hair ?? "#3a2a20");
            float brow = talk ? Mathf.Sin(t / .3f) : 0;
            m.Line(-11, hy - 9 - brow, -4, hy - 10 - brow, 2, browC); m.Line(4, hy - 10 - brow, 11, hy - 9 - brow, 2, browC);
            var eye = C("#2a1c18");
            if (blink) { m.Rect(-10, hy - 3, 6, 2, eye); m.Rect(4, hy - 3, 6, 2, eye); }
            else
            {
                m.Ellipse(-7, hy - 3, 2.6f, 3.2f, eye, 10); m.Ellipse(7, hy - 3, 2.6f, 3.2f, eye, 10);
                m.Circle(-6.2f, hy - 4.2f, .9f, Color.white, 6); m.Circle(7.8f, hy - 4.2f, .9f, Color.white, 6);
            }
            if (d.glasses)
            {
                var gc = C("#4a3a2a");
                for (int k = 0; k < 2; k++)
                {
                    float gx = k == 0 ? -7 : 7;
                    for (int i = 0; i < 16; i++) { float a1 = i / 16f * Mathf.PI * 2, a2 = (i + 1) / 16f * Mathf.PI * 2; m.Line(gx + Mathf.Cos(a1) * 6, hy - 3 + Mathf.Sin(a1) * 6, gx + Mathf.Cos(a2) * 6, hy - 3 + Mathf.Sin(a2) * 6, 1.6f, gc); }
                }
            }
            m.Ellipse(0, hy + 3, 2.2f, 3, Art.Shade(skin, -.15f), 8);
            m.Circle(-12, hy + 6, 4, C("#e6786e", .3f), 8); m.Circle(12, hy + 6, 4, C("#e6786e", .3f), 8);
            var mouth = C("#6a2e2a");
            if (talk) m.Ellipse(0, hy + 10, 4.5f, 1.5f + Mathf.Abs(Mathf.Sin(t / .085f)) * 3, mouth, 10);
            else m.Arc(0, hy + 6, 5, 5, .2f * Mathf.PI, .8f * Mathf.PI, mouth, 6);
            // bosh kiyim
            if (d.hat == "cap")
            {
                var hc = Data.Look("helmet", Save.Equipped("helmet"));
                m.Arc(0, hy - 6, 21.5f, 21.5f, Mathf.PI, Mathf.PI * 2, C(hc.c0), 16);
                m.Arc(0, hy - 6, 21.5f, 10, Mathf.PI, Mathf.PI * 2, C(hc.c1), 16);
                m.Ellipse(16, hy - 6, 16, 4, Art.Shade(C(hc.c1), -.1f), 12);
            }
            if (d.hat == "doppi")
            {
                m.RRect(-17, hy - 31, 34, 17, 4, C("#1d1d1d"));
                for (int k = -1; k <= 1; k++) m.Arc(k * 10, hy - 23, 3.5f, 3.5f, Mathf.PI * .2f, Mathf.PI * 1.6f, C("#f3efe6"), 6);
            }
            if (d.hat == "scarf")
            {
                m.Arc(0, hy - 4, 23.5f, 23.5f, Mathf.PI, Mathf.PI * 2, C(d.scarf), 16);
                for (int i = -2; i <= 2; i++) m.Circle(i * 8, hy - 16 + Mathf.Abs(i) * 3, 2, Art.Shade(C(d.scarf), -.22f), 6);
            }
            if (d.hat == "police")
            {
                m.RRect(-23, hy - 34, 46, 16, 6, C("#2f4f7f"));
                m.Rect(-20, hy - 20, 40, 5, C("#24324a"));
                m.Arc(4, hy - 15, 22, 4, 0, Mathf.PI, C("#1d2433"), 10);
                m.Circle(0, hy - 27, 4, C("#e9c46a"), 8);
            }
            m.ox = 0; m.oy = 0; m.sc = 1; m.alpha = 1;
        }

        /// <summary>Personaj boshining sahnadagi joyi (portret kamerasi uchun).</summary>
        public static Vector2 HeadPos(string id, float x, float feet, float baseScale = 2.4f)
        {
            if (!Data.Chars.TryGetValue(id, out var d)) return new Vector2(x, feet - 300);
            return new Vector2(x, feet - 155 * baseScale * d.s);
        }
    }
}
