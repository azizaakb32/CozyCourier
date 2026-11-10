using System;
using UnityEngine;
#if COZY_ADMOB
using GoogleMobileAds.Api;
#endif

namespace CozyCourier
{
    /// <summary>
    /// Reklama (AdMob). Plagin o'rnatilmaguncha "COZY_ADMOB" belgisi qo'yilmaydi va bu yerda soxta
    /// (stub) versiya ishlaydi: Editor'da reklama "ko'rildi" deb hisoblanadi, telefonda esa tugma ko'rinmaydi.
    /// O'rnatish: README.md → "Reklama (AdMob)".
    /// </summary>
    public static class Ads
    {
        // Google'ning rasmiy TEST ID'lari. Play Market'ga chiqarishdan oldin o'zingiznikiga almashtiring.
        const string REWARDED_ID = "ca-app-pub-3940256099942544/5224354917";
        const string INTERSTITIAL_ID = "ca-app-pub-3940256099942544/1033173712";
        const int INTERSTITIAL_EVERY = 2; // har 2 smenada bir marta

        static int shifts;

#if COZY_ADMOB
        static RewardedAd rewarded;
        static InterstitialAd interstitial;
        static bool ready;

        public static void Init()
        {
            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.Initialize(_ => { ready = true; LoadRewarded(); LoadInterstitial(); });
        }

        static void LoadRewarded()
        {
            if (rewarded != null) { rewarded.Destroy(); rewarded = null; }
            RewardedAd.Load(REWARDED_ID, new AdRequest(), (ad, err) =>
            {
                if (err != null || ad == null) return;
                rewarded = ad;
            });
        }

        static void LoadInterstitial()
        {
            if (interstitial != null) { interstitial.Destroy(); interstitial = null; }
            InterstitialAd.Load(INTERSTITIAL_ID, new AdRequest(), (ad, err) =>
            {
                if (err != null || ad == null) return;
                interstitial = ad;
            });
        }

        public static bool RewardReady => ready && rewarded != null && rewarded.CanShowAd();

        public static void ShowRewarded(Action<bool> done)
        {
            if (!RewardReady) { done?.Invoke(false); LoadRewarded(); return; }
            bool earned = false, finished = false;
            var ad = rewarded;
            void End() { if (finished) return; finished = true; LoadRewarded(); done?.Invoke(earned); }
            ad.OnAdFullScreenContentClosed += End;
            ad.OnAdFullScreenContentFailed += _ => End();
            ad.Show(_ => earned = true);
        }

        public static void MaybeInterstitial(Action done)
        {
            shifts++;
            if (shifts % INTERSTITIAL_EVERY != 0 || interstitial == null || !interstitial.CanShowAd()) { done?.Invoke(); return; }
            var ad = interstitial;
            bool finished = false;
            void End() { if (finished) return; finished = true; LoadInterstitial(); done?.Invoke(); }
            ad.OnAdFullScreenContentClosed += End;
            ad.OnAdFullScreenContentFailed += _ => End();
            ad.Show();
        }
#else
        public static void Init() { }

        /// <summary>Editor'da sinash uchun "reklama" doim tayyor; telefonda plaginsiz — yo'q.</summary>
        public static bool RewardReady => Application.isEditor;

        public static void ShowRewarded(Action<bool> done)
        {
            if (Application.isEditor) Debug.Log("[Ads] Editor: mukofotli reklama ko'rildi deb hisoblandi.");
            done?.Invoke(Application.isEditor);
        }

        public static void MaybeInterstitial(Action done)
        {
            shifts++;
            if (Application.isEditor && shifts % INTERSTITIAL_EVERY == 0) Debug.Log("[Ads] Editor: bu yerda oraliq reklama chiqardi.");
            done?.Invoke();
        }
#endif
    }
}
