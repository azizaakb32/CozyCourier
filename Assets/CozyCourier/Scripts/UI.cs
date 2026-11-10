using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CozyCourier
{
    /// <summary>Interfeys elementlarini koddan yaratish uchun yordamchilar.</summary>
    public static class UI
    {
        static Font font;
        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    // Tizimdagi chiroyli shriftni olamiz (Windows: Segoe UI, Android: Roboto, Mac: Helvetica Neue).
                    try
                    {
                        var names = new[] { "Segoe UI", "Roboto", "Noto Sans", "Helvetica Neue", "SF Pro Text", "Ubuntu", "Arial", "Segoe UI Symbol" };
                        var have = new System.Collections.Generic.HashSet<string>(Font.GetOSInstalledFontNames());
                        var pick = new System.Collections.Generic.List<string>();
                        foreach (var n in names) if (have.Contains(n)) pick.Add(n);
                        if (pick.Count > 0) font = Font.CreateDynamicFontFromOSFont(pick.ToArray(), 32);
                    }
                    catch (System.Exception) { font = null; }
                    if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                return font;
            }
        }

        public static readonly Color Ink = Art.Hex("#101b22");
        public static readonly Color Panel = Art.Hex("#17262e", .92f);
        public static readonly Color Panel2 = Art.Hex("#1d3039");
        public static readonly Color Fg = Art.Hex("#f1efe8");
        public static readonly Color Muted = Art.Hex("#94a9ac");
        public static readonly Color Accent = Art.Hex("#f4a259");
        public static readonly Color Gold = Art.Hex("#ffd166");
        public static readonly Color Teal = Art.Hex("#46d0bf");
        public static readonly Color Ok = Art.Hex("#8fd6a8");
        public static readonly Color Warn = Art.Hex("#ebc66e");
        public static readonly Color Soft = Art.Hex("#ee9583");

        public static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Box(Transform parent, Color c, string name = "box")
        {
            var rt = Node(name, parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = Art.Round;
            im.type = Image.Type.Sliced;
            im.color = c;
            im.raycastTarget = false;
            return im;
        }

        public static Image Pic(Transform parent, Sprite s, Color c, string name = "pic")
        {
            var rt = Node(name, parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = s;
            im.color = c;
            im.raycastTarget = false;
            return im;
        }

        public static UnityEngine.UI.Text Label(Transform parent, string s, int size, Color c, TextAnchor a = TextAnchor.MiddleLeft, FontStyle st = FontStyle.Normal)
        {
            var rt = Node("text", parent);
            var t = rt.gameObject.AddComponent<UnityEngine.UI.Text>();
            t.font = Font;
            t.text = s;
            t.fontSize = size;
            t.color = c;
            t.alignment = a;
            t.fontStyle = st;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Button Btn(Transform parent, string label, Color bg, Color fg, UnityAction onClick, int size = 26)
        {
            var im = Box(parent, bg, "button");
            im.raycastTarget = true;
            var b = im.gameObject.AddComponent<Button>();
            b.targetGraphic = im;
            var cb = b.colors; cb.highlightedColor = new Color(1, 1, 1, .92f); cb.pressedColor = new Color(.85f, .85f, .85f, 1); b.colors = cb;
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Label(im.transform, label, size, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fill(t.rectTransform, 6);
            return b;
        }

        public static void Fill(RectTransform rt, float pad = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
            rt.anchoredPosition = pos; rt.sizeDelta = size;
        }

        public static LayoutElement LE(Component c, float h = -1, float w = -1, float flexW = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (w >= 0) le.preferredWidth = w;
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        public static VerticalLayoutGroup VL(Component c, int pad, float spacing)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.spacing = spacing;
            g.childControlHeight = true; g.childControlWidth = true;
            g.childForceExpandHeight = false; g.childForceExpandWidth = true;
            return g;
        }

        public static HorizontalLayoutGroup HL(Component c, int pad, float spacing)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.padding = new RectOffset(pad, pad, pad, pad);
            g.spacing = spacing;
            g.childControlHeight = true; g.childControlWidth = true;
            g.childForceExpandHeight = true; g.childForceExpandWidth = true;
            return g;
        }

        /// <summary>
        /// Dunyo kanvasidagi yozuvlar uchun piksel zichligi: ekranda qancha piksel bo'lsa, shuncha (biroz zaxira bilan).
        /// Juda katta qiymat yozuvni "g'alati" (qirrali, titroq) qiladi, kichigi esa xira.
        /// </summary>
        public static float WorldDpu(float visibleUnitsTall) => Mathf.Clamp(Screen.height / Mathf.Max(1f, visibleUnitsTall) * 1.3f, 1f, 6f);

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
    }

    /// <summary>Telefon ekranidagi bosib turiladigan boshqaruv tugmasi.</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public int dir; // 0 yuqori, 1 past, 2 chap, 3 o'ng
        Image img;
        void Awake() { img = GetComponent<Image>(); }
        void Set(bool v)
        {
            switch (dir)
            {
                case 0: Game.padU = v; break;
                case 1: Game.padD = v; break;
                case 2: Game.padL = v; break;
                default: Game.padR = v; break;
            }
            if (img != null) img.color = v ? UI.Accent : UI.Panel2;
        }
        public void OnPointerDown(PointerEventData e) => Set(true);
        public void OnPointerUp(PointerEventData e) => Set(false);
        public void OnPointerExit(PointerEventData e) => Set(false);
        void OnDisable() => Set(false);
    }

    /// <summary>
    /// Aylanadigan (suzuvchi) joystik. Barmoq tekkan joyda paydo bo'ladi, dastasi aylana ichida yuradi.
    /// Natija Game.joy ga yoziladi (-1..1).
    /// </summary>
    public class Joystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform bas, knob, zone;
        Vector2 home;
        float radius;
        bool held;
        Image knobImg;

        public void Init(RectTransform b, RectTransform k, float r)
        {
            bas = b; knob = k; radius = r; zone = (RectTransform)transform;
            home = b.anchoredPosition;
            knobImg = k.GetComponent<Image>();
        }

        Vector2 Local(PointerEventData e)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(zone, e.position, e.pressEventCamera, out var lp);
            return lp - zone.rect.min; // pastki chap burchakdan
        }

        public void OnPointerDown(PointerEventData e)
        {
            held = true;
            var p = Local(e);
            float m = radius + 30;
            bas.anchoredPosition = new Vector2(Mathf.Max(m, p.x), Mathf.Max(m, p.y));
            Move(e);
        }

        public void OnDrag(PointerEventData e) { if (held) Move(e); }

        void Move(PointerEventData e)
        {
            var d = Local(e) - bas.anchoredPosition;
            // barmoq juda uzoqlashsa, joystik ham orqasidan siljiydi
            if (d.magnitude > radius * 1.6f) { bas.anchoredPosition += d - d.normalized * radius * 1.6f; d = d.normalized * radius * 1.6f; }
            var k = Vector2.ClampMagnitude(d, radius);
            knob.anchoredPosition = k;
            var v = k / radius;
            Game.joy = v.magnitude < .2f ? Vector2.zero : new Vector2(v.x, -v.y); // xaritada y pastga
            if (knobImg != null) knobImg.color = Color.Lerp(UI.Accent, UI.Gold, v.magnitude);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        void OnDisable() => Release();

        void Release()
        {
            held = false;
            Game.joy = Vector2.zero;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (bas != null) bas.anchoredPosition = home;
            if (knobImg != null) knobImg.color = UI.Accent;
        }

        void Update()
        {
            // qo'yib yuborilganda dasta silliq markazga qaytadi
            if (!held && knob != null) knob.anchoredPosition = Vector2.Lerp(knob.anchoredPosition, Vector2.zero, Time.unscaledDeltaTime * 14f);
        }
    }
}
