using UnityEngine;

namespace CozyCourier
{
    /// <summary>O'yin natijalarini qurilmada saqlaydi (PlayerPrefs).</summary>
    public static class Save
    {
        internal static int GI(string k, int d = 0) => PlayerPrefs.GetInt("cc_" + k, d);
        internal static void SI(string k, int v) { Reg('i', k); PlayerPrefs.SetInt("cc_" + k, v); PlayerPrefs.Save(); Dirty = true; }
        internal static string GS(string k, string d) => PlayerPrefs.GetString("cc_" + k, d);
        internal static void SS(string k, string v) { Reg('s', k); PlayerPrefs.SetString("cc_" + k, v); PlayerPrefs.Save(); Dirty = true; }

        /// <summary>Oxirgi bulutga yuklashdan keyin o'zgarish bo'ldimi.</summary>
        public static bool Dirty;

        // ---------------- kalitlar ro'yxati (bulutga saqlash uchun: PlayerPrefs kalitlarni sanab bera olmaydi)
        static System.Collections.Generic.HashSet<string> keys;
        static void Reg(char type, string k)
        {
            if (keys == null) LoadKeys();
            string e = type + ":" + k;
            if (keys.Contains(e)) return;
            keys.Add(e);
            PlayerPrefs.SetString("cc__keys", string.Join("|", keys));
        }
        static void LoadKeys()
        {
            keys = new System.Collections.Generic.HashSet<string>();
            string raw = PlayerPrefs.GetString("cc__keys", "");
            if (raw.Length > 0) { foreach (var e in raw.Split('|')) if (e.Length > 2) keys.Add(e); return; }
            // eski saqlanishlar uchun: ma'lum kalitlarni tekshirib ro'yxatga qo'shamiz
            var ints = new System.Collections.Generic.List<string> { "wallet", "xp", "best", "lang", "music", "sfx", "chapter", "gaimet", "iap_noads", "iap_vip", "iap_rocket", "tut", "d_streak", "playtime", "total_done", "total_earned" };
            foreach (var u in Data.Upgrades) ints.Add("up_" + u.id);
            ints.Add("up_speed");
            foreach (var cat in Data.LookCats) foreach (var it in Data.Looks[cat]) ints.Add("own_" + cat + "_" + it.id);
            foreach (var r in Data.Renos) ints.Add("reno_" + r.id);
            foreach (var v in Data.Vehicles) ints.Add("veh_" + v.id);
            foreach (var d in Data.DistrictOrder) ints.Add("seen_" + d);
            foreach (var k in ints) if (PlayerPrefs.HasKey("cc_" + k)) keys.Add("i:" + k);
            var strs = new System.Collections.Generic.List<string> { "district", "veh", "d_last" };
            foreach (var cat in Data.LookCats) strs.Add("eq_" + cat);
            foreach (var k in strs) if (PlayerPrefs.HasKey("cc_" + k)) keys.Add("s:" + k);
            PlayerPrefs.SetString("cc__keys", string.Join("|", keys));
        }

        /// <summary>Hamma natijani matnga aylantiradi (bulut uchun).</summary>
        public static string Export()
        {
            if (keys == null) LoadKeys();
            var sb = new System.Text.StringBuilder("cozy1\n");
            foreach (var e in keys)
            {
                string k = e.Substring(2);
                if (k == "lang" || k == "music" || k == "sfx") continue; // qurilma sozlamalari
                if (e[0] == 'i') sb.Append("i\t").Append(k).Append('\t').Append(GI(k)).Append('\n');
                else sb.Append("s\t").Append(k).Append('\t').Append(GS(k, "").Replace("\t", " ").Replace("\n", " ")).Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>Bulutdagi natijani qurilmaga yozadi.</summary>
        public static bool Import(string data)
        {
            if (string.IsNullOrEmpty(data) || !data.StartsWith("cozy1")) return false;
            foreach (var line in data.Split('\n'))
            {
                var p = line.Split('\t');
                if (p.Length != 3) continue;
                if (p[0] == "i" && int.TryParse(p[2], out int v)) SI(p[1], v);
                else if (p[0] == "s") SS(p[1], p[2]);
            }
            Dirty = false;
            return true;
        }

        /// <summary>Bulutdagi natijaning XP'sini (taqqoslash uchun) o'qiydi.</summary>
        public static int PeekXp(string data)
        {
            if (string.IsNullOrEmpty(data)) return -1;
            foreach (var line in data.Split('\n')) { var p = line.Split('\t'); if (p.Length == 3 && p[1] == "xp" && int.TryParse(p[2], out int v)) return v; }
            return -1;
        }

        public static int PlayTime { get => GI("playtime"); set => SI("playtime", value); }
        public static int TotalDone { get => GI("total_done"); set => SI("total_done", value); }
        public static int TotalEarned { get => GI("total_earned"); set => SI("total_earned", value); }

        public static int Wallet { get => GI("wallet"); set => SI("wallet", Mathf.Max(0, value)); }
        public static int XP { get => GI("xp"); set => SI("xp", value); }
        public static int Best { get => GI("best"); set => SI("best", value); }
        public static int Lang { get => GI("lang"); set => SI("lang", value); }
        public static bool Music { get => GI("music") == 1; set => SI("music", value ? 1 : 0); }
        public static bool Sfx { get => GI("sfx", 1) == 1; set => SI("sfx", value ? 1 : 0); }
        public static int Chapter { get => Mathf.Clamp(GI("chapter"), 0, 5); set => SI("chapter", value); }
        public static string District { get => GS("district", "markaz"); set => SS("district", value); }
        public static bool GaiMet { get => GI("gaimet") == 1; set => SI("gaimet", value ? 1 : 0); }

        /// <summary>Pullik xaridlar (o'chirib yuborilmaydi).</summary>
        public static bool NoAds { get => GI("iap_noads") == 1; set => SI("iap_noads", value ? 1 : 0); }
        public static bool Vip { get => GI("iap_vip") == 1; set => SI("iap_vip", value ? 1 : 0); }

        public static bool Rocket { get => GI("iap_rocket") == 1; set => SI("iap_rocket", value ? 1 : 0); }

        /// <summary>Tanlangan transport. Eski o'yinchilar (skuter bilan boshlaganlar) skuterda qoladi.</summary>
        public static string Vehicle
        {
            get
            {
                string v = GS("veh", "");
                if (v.Length > 0) return v;
                if (XP > 0 || Up("speed") > 0)
                {
                    OwnVeh("bike"); OwnVeh("scooter");
                    if (Up("speed") >= 2) OwnVeh("car");
                    SS("veh", "scooter"); return "scooter";
                }
                return "foot";
            }
            set => SS("veh", value);
        }
        public static bool OwnedVeh(string id) => id == "foot" || (id == "rocket" ? Rocket : GI("veh_" + id) == 1);
        public static void OwnVeh(string id) => SI("veh_" + id, 1);

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
        public const int MAX_LEVEL = 100;
        public static int LevelOf(int xp) { int l = 1; while (l < MAX_LEVEL && xp >= Need(l + 1)) l++; return l; }
        /// <summary>Yangi darajaga chiqqanda beriladigan pul (har 5-darajada uch baravar).</summary>
        public static int LevelReward(int level) => (2000 + level * 500) * (level % 5 == 0 ? 3 : 1);
        public static bool TutDone { get => GI("tut") == 1; set => SI("tut", value ? 1 : 0); }
        public static int Level => LevelOf(XP);

        /// <summary>Hamma natijani o'chiradi, lekin til va ovoz sozlamalarini qoldiradi.</summary>
        public static void ResetAll()
        {
            int lang = Lang; bool music = Music, sfx = Sfx, noAds = NoAds, vip = Vip, rocket = Rocket;
            PlayerPrefs.DeleteAll();
            keys = null;
            Lang = lang; Music = music; Sfx = sfx; NoAds = noAds; Vip = vip; Rocket = rocket;
            Vehicle = "foot";
            TutDone = true;
            if (vip) Own("scooter", "gold");
        }
    }
}
