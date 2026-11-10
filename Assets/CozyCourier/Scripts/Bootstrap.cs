using UnityEngine;

namespace CozyCourier
{
    /// <summary>
    /// O'yinni avtomatik ishga tushiradi: istalgan bo'sh sahnada Play bosilganda
    /// "CozyCourier" obyekti yaratiladi va butun o'yin koddan quriladi.
    /// </summary>
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Init()
        {
            if (Object.FindAnyObjectByType<Game>() != null) return;
            var go = new GameObject("CozyCourier");
            go.AddComponent<Game>();
        }
    }
}
