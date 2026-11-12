using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyCourier
{
    /// <summary>
    /// Koddan yaratiladigan grafika: spraytlar, material, ranglar va mesh yig'uvchi.
    /// Xarita koordinatalari "piksel"da (x o'ngga, y pastga), dunyoga K bilan o'tkaziladi.
    /// </summary>
    public static class Art
    {
        public const float K = 0.01f;               // 1 xarita pikseli = 0.01 Unity birligi
        public const float OX = 0.32f, OY = -0.72f; // 2.5D qiyalik

        static Material mat;
        static Sprite square, circle, soft, tri, round, star, vignette;

        public static Material Mat
        {
            get
            {
                if (mat == null)
                {
                    var sh = Shader.Find("Sprites/Default");
                    if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    mat = new Material(sh);
                }
                return mat;
            }
        }

        static Material meshMat;
        /// <summary>
        /// Meshlar (binolar, yo'llar, sahnalar) va chiziqlar uchun material.
        /// Sprites/Default meshlarda ba'zan rangni noto'g'ri olib, binolarni "yo'qotib" qo'yardi — shuning uchun alohida shader.
        /// </summary>
        public static Material MeshMat
        {
            get
            {
                if (meshMat == null)
                {
                    var sh = Resources.Load<Shader>("CozyVertexColor");
                    if (sh == null) sh = Shader.Find("CozyCourier/VertexColor");
                    meshMat = sh != null ? new Material(sh) : new Material(Mat);
                    meshMat.name = "CozyMesh";
                }
                return meshMat;
            }
        }

        /// <summary>Rangi kod orqali o'zgartiriladigan alohida material nusxasi (masalan, kechki deraza nurlari uchun).</summary>
        public static Material NewMat() => new Material(MeshMat);

        /// <summary>Rasm sifati: rang fazosi, silliq qirralar (MSAA), to'liq o'lchamda chizish.</summary>
        public static void SetupQuality(Camera cam)
        {
            Shader.SetGlobalFloat("_CozyLinear", QualitySettings.activeColorSpace == ColorSpace.Linear ? 1f : 0f);
            QualitySettings.antiAliasing = 4;
            if (cam != null) cam.allowMSAA = true;
            var rp = GraphicsSettings.currentRenderPipeline;
            if (rp == null) return;
            try
            {
                var t = rp.GetType();
                var p = t.GetProperty("msaaSampleCount");
                if (p != null && p.CanWrite) p.SetValue(rp, 4);
                p = t.GetProperty("renderScale");
                if (p != null && p.CanWrite) p.SetValue(rp, 1f);
            }
            catch (System.Exception e) { Debug.Log("[Cozy] Sifat sozlamasi o'zgarmadi: " + e.Message); }
        }

        public static Vector3 W(float x, float y, float z = 0f) => new Vector3(x * K, -y * K, z);

        public static Sprite Square => square != null ? square : (square = Make(4, (x, y) => 1f, 4f, Vector4.zero));
        public static Sprite Circle => circle != null ? circle : (circle = Make(256, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
            return Mathf.Clamp01(127.5f - d);
        }, 256f, Vector4.zero));
        public static Sprite Soft => soft != null ? soft : (soft = Make(128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64)) / 64f;
            float a = Mathf.Clamp01(1f - d);
            return a * a;
        }, 128f, Vector4.zero));
        public static Sprite Tri => tri != null ? tri : (tri = Make(128, (x, y) =>
        {
            // silliq qirrali uchburchak
            float u = x / 128f, v = Mathf.Abs(y - 64f) / 64f;
            return Mathf.Clamp01(((1f - u) - v) * 64f + .5f);
        }, 128f, Vector4.zero));
        public static Sprite Round => round != null ? round : (round = Make(128, (x, y) =>
        {
            const float r = 32f;
            float cx = Mathf.Clamp(x, r, 128 - r), cy = Mathf.Clamp(y, r, 128 - r);
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(r + 0.5f - d);
        }, 200f, new Vector4(32, 32, 32, 32)));
        public static Sprite Star => star != null ? star : (star = Make(128, (x, y) =>
        {
            // 4x4 namuna bilan silliq qirra
            int n = 0;
            for (int i = 0; i < 4; i++) for (int j = 0; j < 4; j++) if (InStar((x - .5f + (i + .5f) / 4f - 64) / 2f, (64 - (y - .5f + (j + .5f) / 4f)) / 2f)) n++;
            return n / 16f;
        }, 128f, Vector4.zero));
        public static Sprite Vignette => vignette != null ? vignette : (vignette = Make(128, (x, y) =>
        {
            float dx = (x - 64) / 64f, dy = (y - 64) / 64f;
            float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.45f, 1f, d));
        }, 128f, Vector4.zero));

        static bool InStar(float x, float y)
        {
            // 5 burchakli yulduz (radiuslar 30 va 13)
            var pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2 + i * Mathf.PI / 5, rr = i % 2 == 0 ? 30f : 13f;
                pts[i] = new Vector2(Mathf.Cos(a) * rr, Mathf.Sin(a) * rr);
            }
            bool inside = false;
            for (int i = 0, j = 9; i < 10; j = i++)
            {
                if (((pts[i].y > y) != (pts[j].y > y)) && (x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x)) inside = !inside;
            }
            return inside;
        }

        static Sprite Make(int size, System.Func<float, float, float> alpha, float ppu, Vector4 border)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, size >= 128);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = size >= 128 ? FilterMode.Trilinear : FilterMode.Bilinear;
            tex.anisoLevel = 1;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float a = Mathf.Clamp01(alpha(x + 0.5f, y + 0.5f));
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(size >= 128, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
        }

        public static Color Hex(string hex, float a = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            c.a = a;
            return c;
        }

        public static Color Shade(Color c, float f)
        {
            var t = f < 0 ? Color.black : Color.white;
            var r = Color.Lerp(c, t, Mathf.Abs(f));
            r.a = c.a;
            return r;
        }

        public static Color A(Color c, float a) { c.a = a; return c; }

        public static float Rnd(float i) { float x = Mathf.Sin(i * 127.1f + 311.7f) * 43758.5453f; return x - Mathf.Floor(x); }

        /// <summary>Xarita koordinatasidagi markaz va o'lcham bilan sprayt yaratadi.</summary>
        public static Quad Spr(Transform parent, Sprite s, float x, float y, float w, float h, Color col, int order, string name = "spr")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = W(x, y);
            go.transform.localScale = new Vector3(w * K, h * K, 1f);
            return Quad.Make(go, s, col, order);
        }

        static Material quadMat;
        public static Material QuadMat
        {
            get
            {
                if (quadMat == null)
                {
                    var sh = Resources.Load<Shader>("CozySprite");
                    if (sh == null) sh = Shader.Find("CozyCourier/Sprite");
                    quadMat = sh != null ? new Material(sh) : new Material(Mat);
                    quadMat.name = "CozySprite";
                }
                return quadMat;
            }
        }

        public static MeshRenderer MeshObj(Transform parent, string name, Mesh mesh, int order, Material m = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = m != null ? m : MeshMat;
            mr.sortingOrder = order;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.allowOcclusionWhenDynamic = false;
            return mr;
        }
    }

    /// <summary>Rangli tekis shakllardan bitta mesh yig'adi (xarita koordinatalarida).</summary>
    public class MB
    {
        readonly List<Vector3> v = new List<Vector3>();
        readonly List<Color> c = new List<Color>();
        readonly List<int> t = new List<int>();

        // ixtiyoriy siljitish va masshtab (personajlar uchun)
        public float ox, oy, sc = 1f, alpha = 1f;

        Vector3 M(Vector2 p) => new Vector3((ox + p.x * sc) * Art.K, -(oy + p.y * sc) * Art.K, 0f);
        Color Ca(Color col) { col.a *= alpha; return col; }

        public void Clear() { v.Clear(); c.Clear(); t.Clear(); ox = oy = 0; sc = 1; alpha = 1; }

        public void Quad(Vector2 a, Vector2 b, Vector2 d, Vector2 e, Color col)
        {
            int i = v.Count;
            v.Add(M(a)); v.Add(M(b)); v.Add(M(d)); v.Add(M(e));
            var cc = Ca(col);
            for (int k = 0; k < 4; k++) c.Add(cc);
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        public void Rect(float x, float y, float w, float h, Color col)
        {
            Quad(new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h), col);
        }

        /// <summary>Burchaklari yumaloq to'rtburchak (taxminiy).</summary>
        public void RRect(float x, float y, float w, float h, float r, Color col)
        {
            r = Mathf.Min(r, w / 2, h / 2);
            Rect(x + r, y, w - 2 * r, h, col);
            Rect(x, y + r, r, h - 2 * r, col);
            Rect(x + w - r, y + r, r, h - 2 * r, col);
            Ellipse(x + r, y + r, r, r, col, 8);
            Ellipse(x + w - r, y + r, r, r, col, 8);
            Ellipse(x + r, y + h - r, r, r, col, 8);
            Ellipse(x + w - r, y + h - r, r, r, col, 8);
        }

        public void Poly(Vector2[] pts, Color col)
        {
            int i0 = v.Count;
            var cc = Ca(col);
            foreach (var p in pts) { v.Add(M(p)); c.Add(cc); }
            for (int k = 1; k < pts.Length - 1; k++) { t.Add(i0); t.Add(i0 + k); t.Add(i0 + k + 1); }
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color col, int seg = 20) => Arc(cx, cy, rx, ry, 0, Mathf.PI * 2, col, seg);

        /// <summary>Ellips bo'lagi (a0 dan a1 gacha, radian; y pastga).</summary>
        public void Arc(float cx, float cy, float rx, float ry, float a0, float a1, Color col, int seg = 20)
        {
            int i0 = v.Count;
            var cc = Ca(col);
            v.Add(M(new Vector2(cx, cy))); c.Add(cc);
            for (int k = 0; k <= seg; k++)
            {
                float a = Mathf.Lerp(a0, a1, k / (float)seg);
                v.Add(M(new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry))); c.Add(cc);
            }
            for (int k = 1; k <= seg; k++) { t.Add(i0); t.Add(i0 + k); t.Add(i0 + k + 1); }
        }

        public void Circle(float cx, float cy, float r, Color col, int seg = 16) => Ellipse(cx, cy, r, r, col, seg);

        /// <summary>Yuqoridan pastga rang o'tishi bo'lgan to'rtburchak.</summary>
        public void GradRect(float x, float y, float w, float h, Color top, Color bottom)
        {
            int i = v.Count;
            v.Add(M(new Vector2(x, y))); v.Add(M(new Vector2(x + w, y))); v.Add(M(new Vector2(x + w, y + h))); v.Add(M(new Vector2(x, y + h)));
            c.Add(Ca(top)); c.Add(Ca(top)); c.Add(Ca(bottom)); c.Add(Ca(bottom));
            t.Add(i); t.Add(i + 1); t.Add(i + 2);
            t.Add(i); t.Add(i + 2); t.Add(i + 3);
        }

        /// <summary>Ko'p bosqichli vertikal gradient: (ulush, rang) juftliklari.</summary>
        public void GradStops(float x, float y, float w, float h, params (float, Color)[] stops)
        {
            for (int k = 0; k < stops.Length - 1; k++)
            {
                float y0 = y + h * stops[k].Item1, y1 = y + h * stops[k + 1].Item1;
                GradRect(x, y0, w, y1 - y0, stops[k].Item2, stops[k + 1].Item2);
            }
        }

        /// <summary>Markazdan chetga so'nadigan yumshoq nur.</summary>
        public void Glow(float cx, float cy, float r, Color col, int seg = 20)
        {
            int i0 = v.Count;
            v.Add(M(new Vector2(cx, cy))); c.Add(Ca(col));
            var edge = col; edge.a = 0;
            for (int k = 0; k <= seg; k++)
            {
                float a = k / (float)seg * Mathf.PI * 2f;
                v.Add(M(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r))); c.Add(Ca(edge));
            }
            for (int k = 1; k <= seg; k++) { t.Add(i0); t.Add(i0 + k); t.Add(i0 + k + 1); }
        }

        /// <summary>Qalinlikka ega chiziq.</summary>
        public void Line(float x1, float y1, float x2, float y2, float w, Color col)
        {
            var d = new Vector2(x2 - x1, y2 - y1);
            if (d.sqrMagnitude < 1e-6f) return;
            var n = new Vector2(-d.y, d.x).normalized * (w / 2);
            Quad(new Vector2(x1, y1) + n, new Vector2(x2, y2) + n, new Vector2(x2, y2) - n, new Vector2(x1, y1) - n, col);
        }

        /// <summary>2.5D quti: asos (x,y,w,d), balandlik h.</summary>
        public void Box(float x, float y, float w, float d, float h, Color col, Color? top = null, bool shadow = true)
        {
            float ox2 = h * Art.OX, oy2 = h * Art.OY;
            if (shadow)
                Poly(new[] { new Vector2(x, y + d), new Vector2(x + w, y + d), new Vector2(x + w + h * .6f, y + d + h * .2f), new Vector2(x + w + h * .6f, y + h * .2f), new Vector2(x + w, y) },
                    new Color(.08f, .11f, .16f, .2f));
            Quad(new Vector2(x + w, y), new Vector2(x + w, y + d), new Vector2(x + w + ox2, y + d + oy2), new Vector2(x + w + ox2, y + oy2), Art.Shade(col, -.3f));
            Quad(new Vector2(x, y + d), new Vector2(x + w, y + d), new Vector2(x + w + ox2, y + d + oy2), new Vector2(x + ox2, y + d + oy2), Art.Shade(col, -.12f));
            Rect(x + ox2, y + oy2, w, d, top ?? Art.Shade(col, .16f));
        }

        /// <summary>Old devordagi nuqta: u — eni bo'ylab, v — balandlik ulushi (0..1).</summary>
        public static Vector2 FP(float x, float y, float d, float h, float u, float vv) => new Vector2(x + u + h * vv * Art.OX, y + d + h * vv * Art.OY);

        public void Tree(float x, float y, float r, int seed, float greens = .45f)
        {
            float k = Art.Rnd(seed * 3.17f + 1.3f);
            Color a, b, cc;
            if (Weather.Season == Season.Winter)
            {
                // qish: yalang'och shoxlar va qor
                Ellipse(x + r * .7f, y + r * .2f, r * 1.1f, r * .45f, new Color(.08f, .11f, .16f, .18f));
                Rect(x - 2, y - r * 1.3f, 4, r * 1.3f, Art.Hex("#6f513a"));
                Line(x, y - r * .9f, x - r * .7f, y - r * 1.7f, 2, Art.Hex("#6f513a"));
                Line(x, y - r * 1.1f, x + r * .7f, y - r * 1.8f, 2, Art.Hex("#6f513a"));
                Line(x, y - r * 1.3f, x, y - r * 2.1f, 2, Art.Hex("#6f513a"));
                Circle(x - r * .5f, y - r * 1.7f, r * .5f, new Color(1, 1, 1, .92f));
                Circle(x + r * .5f, y - r * 1.8f, r * .45f, new Color(.93f, .96f, 1, .92f));
                Circle(x, y - r * 2.1f, r * .5f, new Color(1, 1, 1, .95f));
                return;
            }
            if (Weather.Season == Season.Spring && k > .55f)
            {
                // bahor: gullagan daraxtlar
                Ellipse(x + r * .7f, y + r * .2f, r * 1.1f, r * .45f, new Color(.08f, .11f, .16f, .22f));
                Rect(x - 2, y - r, 4, r, Art.Hex("#6f513a"));
                Circle(x, y - r * 1.25f, r, Art.Hex("#f7a8c4"));
                Circle(x - r * .28f, y - r * 1.45f, r * .7f, Art.Hex("#ffc0d6"));
                Circle(x - r * .45f, y - r * 1.65f, r * .32f, Art.Hex("#ffe3ec"));
                return;
            }
            if (k < 1 - greens) { a = Art.Hex("#c9772f"); b = Art.Hex("#e0a03a"); cc = Art.Hex("#f2c25a"); }
            else if (k < 1 - greens * .4f) { a = Art.Hex("#4e8250"); b = Art.Hex("#69a064"); cc = Art.Hex("#86b878"); }
            else { a = Art.Hex("#b5543b"); b = Art.Hex("#d9734a"); cc = Art.Hex("#ee9a62"); }
            Ellipse(x + r * .7f, y + r * .2f, r * 1.1f, r * .45f, new Color(.08f, .11f, .16f, .22f));
            Rect(x - 2, y - r, 4, r, Art.Hex("#6f513a"));
            Circle(x, y - r * 1.25f, r, a);
            Circle(x - r * .28f, y - r * 1.45f, r * .7f, b);
            Circle(x - r * .45f, y - r * 1.65f, r * .32f, cc);
        }

        public Mesh Build()
        {
            var m = new Mesh();
            Fill(m);
            return m;
        }

        /// <summary>Mavjud meshni qayta to'ldiradi (har kadrda yangilanadigan shakllar uchun).</summary>
        public void Fill(Mesh m)
        {
            m.Clear();
            m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetColors(c);
            m.SetTriangles(t, 0);
            // Kamera "ko'rinmayapti" deb chizmay qo'ymasligi uchun chegara katta va qalin qilinadi.
            m.bounds = new Bounds(new Vector3(20f, -10f, 0f), new Vector3(400f, 400f, 50f));
        }
    }

    /// <summary>
    /// SpriteRenderer o'rniga ishlatiladigan oddiy to'rtburchak rasm.
    /// O'zimizning shader bilan chiziladi, shuning uchun nurlar va doiralar doim yumaloq va bir xil ko'rinadi.
    /// </summary>
    public class Quad : MonoBehaviour
    {
        static Mesh unit;
        static MaterialPropertyBlock mpb;
        MeshRenderer mr;
        Sprite spr;
        Color col = Color.white;

        static Mesh Unit
        {
            get
            {
                if (unit != null) return unit;
                unit = new Mesh { name = "CozyQuad" };
                unit.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(.5f, .5f), new Vector3(-.5f, .5f) };
                unit.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
                unit.triangles = new[] { 0, 2, 1, 0, 3, 2 };
                unit.bounds = new Bounds(Vector3.zero, new Vector3(2, 2, 2));
                return unit;
            }
        }

        public static Quad Make(GameObject go, Sprite s, Color c, int order)
        {
            go.AddComponent<MeshFilter>().sharedMesh = Unit;
            var q = go.AddComponent<Quad>();
            q.mr = go.AddComponent<MeshRenderer>();
            q.mr.sharedMaterial = Art.QuadMat;
            q.mr.sortingOrder = order;
            q.mr.shadowCastingMode = ShadowCastingMode.Off;
            q.mr.receiveShadows = false;
            q.mr.allowOcclusionWhenDynamic = false;
            q.spr = s; q.col = c;
            q.Apply();
            return q;
        }

        void Apply()
        {
            if (mpb == null) mpb = new MaterialPropertyBlock();
            mpb.Clear();
            if (spr != null) mpb.SetTexture("_MainTex", spr.texture);
            mpb.SetColor("_Color", col);
            mr.SetPropertyBlock(mpb);
        }

        public Color color { get => col; set { if (col == value) return; col = value; Apply(); } }
        public Sprite sprite { get => spr; set { spr = value; Apply(); } }
        public int sortingOrder { get => mr.sortingOrder; set => mr.sortingOrder = value; }
        public new bool enabled { get => mr.enabled; set => mr.enabled = value; }
    }
}
