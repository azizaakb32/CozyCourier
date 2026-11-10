using UnityEngine;

namespace CozyCourier
{
    /// <summary>
    /// O'yinni avtomatik ishga tushiradi: istalgan bo'sh sahnada Play bosilganda
    /// "CozyCourier" obyekti yaratiladi va butun o'yin koddan quriladi.
    /// </summary>
    public static class Bootstrap
    {
        /// <summary>Sahna yuklanishidan oldin: faqat yotiq (landscape) ekran, 60 kadr.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Early()
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            if (Application.isMobilePlatform) Screen.orientation = ScreenOrientation.AutoRotation;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindAnyObjectByType<Game>() != null) return;
            var go = new GameObject("CozyCourier");
            go.AddComponent<Game>();
        }
    }
}
