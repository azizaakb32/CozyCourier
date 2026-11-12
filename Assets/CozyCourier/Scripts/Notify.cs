using System;
using UnityEngine;
#if COZY_NOTIF && UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace CozyCourier
{
    /// <summary>
    /// Telefon bildirishnomalari (Unity "Mobile Notifications" paketi). O'yinchi chiqib ketganda rejalashtiriladi:
    /// ertaga — kunlik sovg'a, 3 kundan keyin — Rustam bobo, 7 kundan keyin — Malika opa.
    /// Paket o'rnatilmaguncha (COZY_NOTIF belgisi yo'q) hech narsa qilmaydi.
    /// </summary>
    public static class Notify
    {
        static string[] T(string uz, string ru, string en) => new[] { uz, ru, en };
        static readonly string[][] Daily1 = { T("Kunlik sovg'angiz tayyor 🎁", "Ваш ежедневный подарок готов 🎁", "Your daily gift is ready 🎁"), T("Bugungi vazifalar ham kutmoqda. Bir smena o'ynaymizmi?", "Задания дня тоже ждут. Сыграем смену?", "Today's tasks are waiting too. One shift?") };
        static readonly string[][] Day3 = { T("Rustam bobo sizni so'rayapti 🍵", "Рустам-бобо спрашивает о вас 🍵", "Grandpa Rustam is asking about you 🍵"), T("Choy damlab qo'yibdi. Kunlik sovg'a ham 1-kundan boshlanmasin!", "Он заварил чай. И подарки не начнутся с 1-го дня!", "He's made tea. Don't let your gift streak reset!") };
        static readonly string[][] Day7 = { T("Toshkent ko'chalari sizni sog'indi 🛵", "Улицы Ташкента соскучились 🛵", "Tashkent's streets miss you 🛵"), T("Malika opa yangi osh damladi. «Tez Yetkaz» kuryersiz qoldi!", "Малика-опа приготовила плов. «Tez Yetkaz» без курьера!", "Malika made fresh plov. Tez Yetkaz needs its courier!") };

#if COZY_NOTIF && UNITY_ANDROID
        const string CH = "cozy_main";
        static bool inited;

        public static void Init()
        {
            try
            {
                AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
                {
                    Id = CH, Name = "Cozy Courier", Importance = Importance.Default, Description = "Daily gift reminders",
                });
                inited = true;
                AndroidNotificationCenter.CancelAllNotifications();
            }
            catch (Exception e) { Debug.LogWarning("[Notify] " + e.Message); }
        }

        /// <summary>Android 13+ da ruxsat so'raydi (o'yinchi birinchi smenani tugatgandan keyin chaqiriladi).</summary>
        public static void AskPermission()
        {
            const string P = "android.permission.POST_NOTIFICATIONS";
            try { if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(P)) UnityEngine.Android.Permission.RequestUserPermission(P); }
            catch (Exception e) { Debug.LogWarning("[Notify] " + e.Message); }
        }

        public static void ScheduleAll()
        {
            if (!inited) return;
            try
            {
                AndroidNotificationCenter.CancelAllScheduledNotifications();
                var now = DateTime.Now;
                // ertaga kechki 19:00 (sovg'a bugun olinmagan bo'lsa — 4 soatdan keyin)
                var first = CozyCourier.Daily.RewardAvailable ? now.AddHours(4) : now.Date.AddDays(1).AddHours(19);
                Send(Daily1, first);
                Send(Day3, now.Date.AddDays(3).AddHours(18));
                Send(Day7, now.Date.AddDays(7).AddHours(18));
            }
            catch (Exception e) { Debug.LogWarning("[Notify] " + e.Message); }
        }

        public static void CancelAll()
        {
            if (!inited) return;
            try { AndroidNotificationCenter.CancelAllNotifications(); } catch { }
        }

        static void Send(string[][] msg, DateTime when)
        {
            var n = new AndroidNotification { Title = Loc.Tx(msg[0]), Text = Loc.Tx(msg[1]), FireTime = when };
            AndroidNotificationCenter.SendNotification(n, CH);
        }
#else
        public static void Init() { }
        public static void AskPermission() { }
        public static void ScheduleAll() { }
        public static void CancelAll() { }
#endif
    }
}
