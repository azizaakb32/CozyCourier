using System;
using UnityEngine;

namespace CozyCourier
{
    /// <summary>Kunlik vazifa turi.</summary>
    public class TaskDef
    {
        public string id, ev;      // ev — o'yin hodisasi: deliver, tip, shift, clean, earn, stars3, ghost
        public int goal, reward;
        public string[] text;      // {0} — maqsad soni
        public bool money;         // maqsad pul bo'lsa, so'm qilib ko'rsatiladi
    }

    /// <summary>
    /// Kunlik sovg'a (7 kunlik ketma-ketlik) va kunlik vazifalar (har kuni 3 ta).
    /// O'yinchini har kuni qaytarish uchun.
    /// </summary>
    public static class Daily
    {
        static string[] T(string uz, string ru, string en) => new[] { uz, ru, en };

        public static readonly int[] Rewards = { 3000, 5000, 8000, 12000, 16000, 22000, 40000 };

        public static readonly TaskDef[] Pool =
        {
            new TaskDef{ id="d5",    ev="deliver", goal=5,     reward=6000,  text=T("{0} ta buyurtma yetkazing","Доставьте {0} заказов","Deliver {0} orders") },
            new TaskDef{ id="d12",   ev="deliver", goal=12,    reward=14000, text=T("{0} ta buyurtma yetkazing","Доставьте {0} заказов","Deliver {0} orders") },
            new TaskDef{ id="tip3",  ev="tip",     goal=3,     reward=8000,  text=T("{0} marta choychaqa oling","Получите чаевые {0} раза","Earn a tip {0} times") },
            new TaskDef{ id="sh2",   ev="shift",   goal=2,     reward=8000,  text=T("{0} ta smenani tugating","Завершите {0} смены","Finish {0} shifts") },
            new TaskDef{ id="clean", ev="clean",   goal=1,     reward=10000, text=T("Shtrafsiz smena o'ynang","Смена без штрафов","Play a shift with no fines") },
            new TaskDef{ id="earn",  ev="earn",    goal=40000, reward=8000,  money=true, text=T("{0} ishlang","Заработайте {0}","Earn {0}") },
            new TaskDef{ id="star3", ev="stars3",  goal=1,     reward=12000, text=T("Smenada 3 yulduz oling","Получите 3 звезды за смену","Get 3 stars in a shift") },
            new TaskDef{ id="ghost", ev="ghost",   goal=1,     reward=5000,  text=T("«Ko'rinmas» rejimni sinab ko'ring","Попробуйте «Невидимку»","Try Invisible mode") },
        };

        /// <summary>Vazifa bajarilganda (o'yin ichida xabar chiqarish uchun).</summary>
        public static event Action<TaskDef> OnTaskDone;

        static string Day(DateTime d) => d.ToString("yyyyMMdd");
        static string Today => Day(DateTime.Now);
        static string Yesterday => Day(DateTime.Now.AddDays(-1));

        // ------------------------------------------------ kunlik sovg'a
        static string Last { get => Save.GS("d_last", ""); set => Save.SS("d_last", value); }
        static int Streak { get => Save.GI("d_streak"); set => Save.SI("d_streak", value); }

        public static bool RewardAvailable => Last != Today;

        /// <summary>Bugun olinadigan (yoki olingan) kun: 0..6.</summary>
        public static int TodayIndex
        {
            get
            {
                if (Last == Today) return Mathf.Max(0, Streak - 1) % 7;
                if (Last == Yesterday) return Streak % 7;
                return 0; // bir kun o'tkazib yuborilsa, qaytadan
            }
        }

        /// <summary>7-kunning sovg'asi: mushukcha (hali bo'lmasa).</summary>
        public static bool Day7Cat => !Save.Owned("pet", "cat");

        public static int ClaimReward()
        {
            if (!RewardAvailable) return 0;
            int idx = TodayIndex;
            Streak = (Last == Yesterday ? Streak : 0) + 1;
            Last = Today;
            int amount = Rewards[idx];
            Save.Wallet += amount;
            if (idx == 6 && Day7Cat) { Save.Own("pet", "cat"); Save.Equip("pet", "cat"); }
            return amount;
        }

        // ------------------------------------------------ kunlik vazifalar
        static void Ensure()
        {
            if (Save.GS("t_date", "") == Today) return;
            Save.SS("t_date", Today);
            // sana bo'yicha har kuni boshqa 3 ta vazifa
            var rnd = new System.Random(int.Parse(Today));
            var picked = new System.Collections.Generic.List<int>();
            while (picked.Count < 3)
            {
                int k = rnd.Next(Pool.Length);
                bool dupEv = false;
                foreach (int p in picked) if (Pool[p].ev == Pool[k].ev) dupEv = true;
                if (!picked.Contains(k) && !dupEv) picked.Add(k);
            }
            for (int i = 0; i < 3; i++) { Save.SI("t_id" + i, picked[i]); Save.SI("t_p" + i, 0); Save.SI("t_c" + i, 0); }
        }

        public static TaskDef Task(int i) { Ensure(); return Pool[Mathf.Clamp(Save.GI("t_id" + i), 0, Pool.Length - 1)]; }
        public static int Progress(int i) { Ensure(); return Save.GI("t_p" + i); }
        public static bool Claimed(int i) { Ensure(); return Save.GI("t_c" + i) == 1; }
        public static bool Done(int i) => Progress(i) >= Task(i).goal;

        public static bool AnyClaimable
        {
            get { for (int i = 0; i < 3; i++) if (Done(i) && !Claimed(i)) return true; return false; }
        }

        public static bool Badge => RewardAvailable || AnyClaimable;

        public static string Text(TaskDef t) => Loc.Tx(t.text).Replace("{0}", t.money ? Loc.Money(t.goal) : t.goal.ToString());

        /// <summary>O'yin hodisasini qayd qiladi.</summary>
        public static void Track(string ev, int n = 1)
        {
            Ensure();
            for (int i = 0; i < 3; i++)
            {
                var t = Task(i);
                if (t.ev != ev || Claimed(i)) continue;
                int p = Progress(i);
                if (p >= t.goal) continue;
                int np = Mathf.Min(t.goal, p + n);
                Save.SI("t_p" + i, np);
                if (np >= t.goal) { try { OnTaskDone?.Invoke(t); } catch (Exception e) { Debug.LogException(e); } }
            }
        }

        public static int ClaimTask(int i)
        {
            if (!Done(i) || Claimed(i)) return 0;
            Save.SI("t_c" + i, 1);
            int r = Task(i).reward;
            Save.Wallet += r;
            return r;
        }
    }
}
