using System;
using UnityEngine;

namespace CozyCourier
{
    public enum Season { Spring, Summer, Autumn, Winter }

    /// <summary>
    /// Fasl (telefon sanasiga qarab) va ob-havo (har smenada tasodifiy).
    /// Yomg'ir va qorda buyurtmalar uchun +20% to'lanadi — yomon ob-havoda kuryer qadrliroq.
    /// </summary>
    public static class Weather
    {
        /// <summary>Sinash uchun: -1 = haqiqiy sana, 0..3 = majburiy fasl.</summary>
        public static int ForceSeason = -1;

        public static Season Season
        {
            get
            {
                if (ForceSeason >= 0) return (Season)ForceSeason;
                int m = DateTime.Now.Month;
                return m >= 3 && m <= 5 ? Season.Spring : m >= 6 && m <= 8 ? Season.Summer : m >= 9 && m <= 11 ? Season.Autumn : Season.Winter;
            }
        }

        /// <summary>Navro'z haftalari (14–31 mart).</summary>
        public static bool Navruz { get { var d = DateTime.Now; return ForceSeason < 0 && d.Month == 3 && d.Day >= 14; } }
        /// <summary>Yangi yil kunlari (20 dekabr – 10 yanvar).</summary>
        public static bool NewYear { get { var d = DateTime.Now; return ForceSeason < 0 && ((d.Month == 12 && d.Day >= 20) || (d.Month == 1 && d.Day <= 10)); } }

        /// <summary>Hozirgi ob-havo: clear, rain, snow, fog.</summary>
        public static string Now = "clear";

        public static void Roll()
        {
            float r = UnityEngine.Random.value;
            switch (Season)
            {
                case Season.Winter: Now = r < .5f ? "snow" : r < .62f ? "fog" : "clear"; break;
                case Season.Autumn: Now = r < .3f ? "rain" : r < .42f ? "fog" : "clear"; break;
                case Season.Spring: Now = r < .25f ? "rain" : "clear"; break;
                default: Now = r < .08f ? "rain" : "clear"; break;
            }
        }

        public static void Clear() { Now = "clear"; }

        public static bool Wet => Now == "rain" || Now == "snow";
        public static float PayBonus => (Wet ? 1.2f : 1f) * (Navruz || NewYear ? 1.1f : 1f);

        /// <summary>Daraxtlarning yashillik ulushi: bahor/yozda hammasi yashil, kuzda sarg'aygan, qishda yalang'och-qorli.</summary>
        public static float Greens(float autumnValue) =>
            Season == Season.Spring || Season == Season.Summer ? 1f : Season == Season.Autumn ? autumnValue : .9f;

        public static string LocKey => Now == "rain" ? "wRain" : Now == "snow" ? "wSnow" : Now == "fog" ? "wFog" : null;
    }
}
