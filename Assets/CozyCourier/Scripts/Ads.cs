using System;
using UnityEngine;
#if COZY_ADMOB
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api;
#endif

namespace CozyCourier
{
    /// <summary>
    /// Reklama (Google AdMob).
    /// • Oraliq (interstitial) reklama — har 2 smenadan keyin. "Reklamasiz" sotib olinsa chiqmaydi.
    /// • Mukofotli (rewarded) reklama — o'yinchi o'zi tanlaydi: daromadni ×2 qilish yoki garajda bonus pul.
    ///   "Reklamasiz" sotib olinganda bu bonuslar reklamasiz darhol beriladi.
    /// • Yevropa foydalanuvchilari uchun Google'ning rozilik oynasi (UMP) avtomatik chiqadi.
    /// Plagin o'rnatilmaguncha "COZY_ADMOB" belgisi qo'yilmaydi va soxta (stub) versiya ishlaydi.
    /// </summary>
    public static class Ads
    {
        // Google'ning rasmiy TEST ID'lari. Play Market'ga chiqarishdan oldin o'zingiznikiga almashtiring!
#if UNITY_IOS
        const string REWARDED_ID = "ca-app-pub-3940256099942544/1712485313";
        const string INTERSTITIAL_ID = "ca-app-pub-3940256099942544/4411468910";
#else
        const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
        const string INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
#endif
        const int INTERSTITIAL_EVERY = 2;      // har nechta smenada bir marta
        const float FIRST_SHIFT_FREE = 1;      // birinchi smenadan keyin reklama ko'rsatilmaydi
        public const int AD_COINS = 20000;     // garajdagi mukofotli reklama puli
        public const float AD_COINS_COOLDOWN = 180f;

        static int shifts;
        static float lastCoinsAd = -999f;

        public static bool NoAds => Save.NoAds;

        /// <summary>Garajdagi "reklama ko'rib pul" tugmasi qachon yana ishlaydi (soniya).</summary>
        public static int CoinsWait => Mathf.Max(0, Mathf.CeilToInt(AD_COINS_COOLDOWN - (Time.realtimeSinceStartup - lastCoinsAd)));

        public static void ShowRewardedCoins(Action<bool> done)
        {
            if (CoinsWait > 0) { done?.Invoke(false); return; }
            ShowRewarded(ok =>
            {
                if (ok) { Save.Wallet += AD_COINS; lastCoinsAd = Time.realtimeSinceStartup; }
                done?.Invoke(ok);
            });
        }

#if COZY_ADMOB
        static RewardedAd rewarded;
        static InterstitialAd interstitial;
        static bool ready, started;

        public static void Init()
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            // 1) rozilik (GDPR) — kerak bo'lsa oyna chiqadi
            var req = new ConsentRequestParameters();
            ConsentInformation.Update(req, err =>
            {
                if (err != null) Debug.LogWarning("[Ads] Rozilik ma'lumoti olinmadi: " + err.Message);
                ConsentForm.LoadAndShowConsentFormIfRequired(formErr =>
                {
                    if (formErr != null) Debug.LogWarning("[Ads] Rozilik oynasi: " + formErr.Message);
                    if (ConsentInformation.CanRequestAds()) Start();
                });
            });
            // oldingi sessiyada rozilik olingan bo'lsa, kutmasdan boshlaymiz
            if (ConsentInformation.CanRequestAds()) Start();
        }

        static void Start()
        {
            if (started) return;
            started = true;
            MobileAds.Initialize(_ => { ready = true; LoadRewarded(); if (!NoAds) LoadInterstitial(); });
        }

        /// <summary>Sozlamalardagi "Maxfiylik" tugmasi kerakmi (Yevropada majburiy).</summary>
        public static bool PrivacyRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public static void ShowPrivacy()
        {
            ConsentForm.ShowPrivacyOptionsForm(err => { if (err != null) Debug.LogWarning("[Ads] " + err.Message); });
        }

        static void LoadRewarded()
        {
            if (rewarded != null) { rewarded.Destroy(); rewarded = null; }
            RewardedAd.Load(REWARDED_ID, new AdRequest(), (ad, err) =>
            {
                if (err != null || ad == null) { Debug.Log("[Ads] Mukofotli reklama yuklanmadi: " + err); return; }
                rewarded = ad;
            });
        }

        static void LoadInterstitial()
        {
            if (interstitial != null) { interstitial.Destroy(); interstitial = null; }
            InterstitialAd.Load(INTERSTITIAL_ID, new AdRequest(), (ad, err) =>
            {
                if (err != null || ad == null) { Debug.Log("[Ads] Oraliq reklama yuklanmadi: " + err); return; }
                interstitial = ad;
            });
        }

        public static bool RewardReady => NoAds || (ready && rewarded != null && rewarded.CanShowAd());

        public static void ShowRewarded(Action<bool> done)
        {
            if (NoAds) { done?.Invoke(true); return; } // sotib olganlarga — reklamasiz
            if (!(ready && rewarded != null && rewarded.CanShowAd())) { done?.Invoke(false); if (ready) LoadRewarded(); return; }
            bool earned = false, finished = false;
            var ad = rewarded;
            void End() { if (finished) return; finished = true; LoadRewarded(); Sound(true); done?.Invoke(earned); }
            ad.OnAdFullScreenContentClosed += End;
            ad.OnAdFullScreenContentFailed += _ => End();
            Sound(false);
            ad.Show(_ => earned = true);
        }

        public static void MaybeInterstitial(Action done)
        {
            shifts++;
            if (NoAds || shifts <= FIRST_SHIFT_FREE || shifts % INTERSTITIAL_EVERY != 0 || interstitial == null || !interstitial.CanShowAd())
            {
                if (!NoAds && ready && interstitial == null) LoadInterstitial();
                done?.Invoke();
                return;
            }
            var ad = interstitial;
            bool finished = false;
            void End() { if (finished) return; finished = true; LoadInterstitial(); Sound(true); done?.Invoke(); }
            ad.OnAdFullScreenContentClosed += End;
            ad.OnAdFullScreenContentFailed += _ => End();
            Sound(false);
            ad.Show();
        }

        // reklama paytida o'yin ovozini o'chiramiz
        static void Sound(bool on) { AudioListener.pause = !on; }
#else
        public static void Init() { }
        public static bool PrivacyRequired => false;
        public static void ShowPrivacy() { }

        /// <summary>Editor'da sinash uchun "reklama" doim tayyor; telefonda plaginsiz — yo'q.</summary>
        public static bool RewardReady => NoAds || Application.isEditor;

        public static void ShowRewarded(Action<bool> done)
        {
            if (NoAds) { done?.Invoke(true); return; }
            if (Application.isEditor) Debug.Log("[Ads] Editor: mukofotli reklama ko'rildi deb hisoblandi.");
            done?.Invoke(Application.isEditor);
        }

        public static void MaybeInterstitial(Action done)
        {
            shifts++;
            if (!NoAds && Application.isEditor && shifts > FIRST_SHIFT_FREE && shifts % INTERSTITIAL_EVERY == 0)
                Debug.Log("[Ads] Editor: bu yerda oraliq reklama chiqardi.");
            done?.Invoke();
        }
#endif
    }
}
