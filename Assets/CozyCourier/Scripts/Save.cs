using UnityEngine;

namespace CozyCourier
{
    /// <summary>O'yin natijalarini qurilmada saqlaydi (PlayerPrefs).</summary>
    public static class Save
    {
        static int GI(string k, int d = 0) => PlayerPrefs.GetInt("cc_" + k, d);
        static void SI(string k, int v) { PlayerPrefs.SetInt("cc_" + k, v); PlayerPrefs.Save(); }
        static string GS(string k, string d) => PlayerPrefs.GetString("cc_" + k, d);
        static void SS(string k, string v) { PlayerPrefs.SetString("cc_" + k, v); PlayerPrefs.Save(); }

        public static int Wallet { get => GI("wallet"); set => SI("wallet", Mathf.Max(0, value)); }
        public static int XP { get => GI("xp"); set => SI("xp", value); }
        public static int Best { get => GI("best"); set => SI("best", value); }
        public static int Lang { get => GI("lang"); set => SI("lang", value); }
        public static bool Music { get => GI("music") == 1; set => SI("music", value ? 1 : 0); }
        public static bool Sfx { get => GI("sfx", 1) == 1; set => SI("sfx", value ? 1 : 0); }
        public static int Chapter { get => Mathf.Clamp(GI("chapter"), 0, 5); set => SI("chapter", value); }
        public static string District { get => GS("district", "markaz"); set => SS("district", value); }
        public static bool GaiMet { get => GI("gaimet") == 1; set => SI("gaimet", value ? 1 : 0); }

        public static int Up(string id) => GI("up_" + id);
        public static void SetUp(string id, int v) => SI("up_" + id, v);
        public static bool Owned(string cat, string id) => Data.Looks[cat][0].id == id || GI("own_" + cat + "_" + id) == 1;
        public static void Own(string cat, string id) => SI("own_" + cat + "_" + id, 1);
        public static string Equipped(string cat) => GS("eq_" + cat, Data.Looks[cat][0].id);
        public static void Equip(string cat, string id) => SS("eq_" + cat, id);
        public static bool RenoDone(string id) => GI("reno_" + id) == 1;
        public static void SetReno(string id) => SI("reno_" + id, 1);
        public static bool Seen(string d) => GI("seen_" + d) == 1;
        public static void SetSeen(string d) => SI("seen_" + d, 1);

        public static int Need(int level) => 40 * level * (level - 1);
        public static int LevelOf(int xp) { int l = 1; while (l < 30 && xp >= Need(l + 1)) l++; return l; }
        public static int Level => LevelOf(XP);

        /// <summary>Hamma natijani o'chiradi, lekin til va ovoz sozlamalarini qoldiradi.</summary>
        public static void ResetAll()
        {
            int lang = Lang; bool music = Music, sfx = Sfx;
            PlayerPrefs.DeleteAll();
            Lang = lang; Music = music; Sfx = sfx;
        }
    }
}
