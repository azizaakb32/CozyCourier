using System;
using UnityEngine;
#if COZY_PGS && UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;
#endif

namespace CozyCourier
{
    /// <summary>
    /// Google Play Games: bulutda saqlash, reyting jadvali, yutuqlar.
    /// "Play Games Plugin for Unity" o'rnatilmaguncha (COZY_PGS belgisi yo'q) hech narsa qilmaydi.
    ///
    /// ID'lar: Play Console → Play Games Services → Setup and management → Leaderboards / Achievements
    /// da yaratib, shu yerga yozing.
    /// </summary>
    public static class Cloud
    {
        // ====== Play Console'dan olingan ID'lar (o'zingiznikiga almashtiring) ======
        public const string LEADERBOARD = "REPLACE_LEADERBOARD_ID";      // "Jami daromad"
        public const string ACH_FIRST   = "REPLACE_ACH_FIRST_DELIVERY";  // Birinchi yetkazish
        public const string ACH_100     = "REPLACE_ACH_100_DELIVERIES";  // 100 ta yetkazish
        public const string ACH_1000    = "REPLACE_ACH_1000_DELIVERIES"; // 1000 ta yetkazish
        public const string ACH_CLEAN   = "REPLACE_ACH_CLEAN_SHIFT";     // Shtrafsiz smena
        public const string ACH_STARS   = "REPLACE_ACH_THREE_STARS";     // 3 yulduzli smena
        public const string ACH_LVL10   = "REPLACE_ACH_LEVEL_10";
        public const string ACH_LVL50   = "REPLACE_ACH_LEVEL_50";
        public const string ACH_STORY   = "REPLACE_ACH_STORY_DONE";      // Hikoyani tugatish
        // ============================================================================

        const string SAVE_NAME = "cozy_save";

        public static bool SignedIn { get; private set; }
        /// <summary>Bulutdan yangiroq natija yuklanganda (o'yin ekranni yangilashi kerak).</summary>
        public static event Action OnImported;

        static bool Valid(string id) => !string.IsNullOrEmpty(id) && !id.StartsWith("REPLACE");

#if COZY_PGS && UNITY_ANDROID
        static bool busy;
        public static bool Available => true;

        public static void Init()
        {
            try
            {
                PlayGamesPlatform.Activate();
                PlayGamesPlatform.Instance.Authenticate(OnAuth);
            }
            catch (Exception e) { Debug.LogWarning("[Cloud] " + e.Message); }
        }

        public static void SignIn()
        {
            try { PlayGamesPlatform.Instance.ManuallyAuthenticate(OnAuth); }
            catch (Exception e) { Debug.LogWarning("[Cloud] " + e.Message); }
        }

        static void OnAuth(SignInStatus status)
        {
            SignedIn = status == SignInStatus.Success;
            Debug.Log("[Cloud] Kirish: " + status);
            if (SignedIn) Game.OnMain(Load);
        }

        static void Load()
        {
            if (busy) return; busy = true;
            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(SAVE_NAME, DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime, (st, meta) =>
                {
                    if (st != SavedGameRequestStatus.Success) { busy = false; Debug.LogWarning("[Cloud] Ochilmadi: " + st); return; }
                    PlayGamesPlatform.Instance.SavedGame.ReadBinaryData(meta, (st2, bytes) =>
                    {
                        busy = false;
                        if (st2 != SavedGameRequestStatus.Success || bytes == null || bytes.Length == 0) { Game.OnMain(() => Upload(true)); return; }
                        string data = System.Text.Encoding.UTF8.GetString(bytes);
                        Game.OnMain(() =>
                        {
                            // qaysi biri "oldinda" bo'lsa, o'sha qoladi
                            if (Save.PeekXp(data) > Save.XP)
                            {
                                if (Save.Import(data)) { Debug.Log("[Cloud] Bulutdagi natija yuklandi."); OnImported?.Invoke(); }
                            }
                            else Upload(true);
                        });
                    });
                });
        }

        /// <summary>Natijani bulutga yozadi (o'zgarish bo'lgan bo'lsa).</summary>
        public static void Upload(bool force = false)
        {
            if (!SignedIn || busy || (!force && !Save.Dirty)) return;
            busy = true;
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(Save.Export());
            int playtime = Save.PlayTime, level = Save.Level;
            PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution(SAVE_NAME, DataSource.ReadCacheOrNetwork,
                ConflictResolutionStrategy.UseLongestPlaytime, (st, meta) =>
                {
                    if (st != SavedGameRequestStatus.Success) { busy = false; return; }
                    var upd = new SavedGameMetadataUpdate.Builder()
                        .WithUpdatedDescription("Level " + level)
                        .WithUpdatedPlayedTime(TimeSpan.FromSeconds(Mathf.Max(1, playtime)))
                        .Build();
                    PlayGamesPlatform.Instance.SavedGame.CommitUpdate(meta, upd, bytes, (st2, m2) =>
                    {
                        busy = false;
                        if (st2 == SavedGameRequestStatus.Success) Save.Dirty = false;
                    });
                });
        }

        public static void ReportScore(long score)
        {
            if (SignedIn && Valid(LEADERBOARD)) PlayGamesPlatform.Instance.ReportScore(score, LEADERBOARD, _ => { });
        }

        public static void Unlock(string id)
        {
            if (SignedIn && Valid(id)) PlayGamesPlatform.Instance.UnlockAchievement(id, _ => { });
        }

        public static void ShowLeaderboard()
        {
            if (!SignedIn) { SignIn(); return; }
            if (Valid(LEADERBOARD)) PlayGamesPlatform.Instance.ShowLeaderboardUI(LEADERBOARD);
        }

        public static void ShowAchievements()
        {
            if (!SignedIn) { SignIn(); return; }
            PlayGamesPlatform.Instance.ShowAchievementsUI();
        }
#else
        public static bool Available => false;
        public static void Init() { }
        public static void SignIn() { }
        public static void Upload(bool force = false) { }
        public static void ReportScore(long score) { }
        public static void Unlock(string id) { }
        public static void ShowLeaderboard() { }
        public static void ShowAchievements() { }
        static void Unused() { OnImported?.Invoke(); SignedIn = false; }
#endif

        /// <summary>Smena tugaganda: yutuqlarni tekshiradi, reytingni yuboradi, bulutga saqlaydi.</summary>
        public static void AfterShift(int done, int fines, int stars)
        {
            if (Save.TotalDone >= 1) Unlock(ACH_FIRST);
            if (Save.TotalDone >= 100) Unlock(ACH_100);
            if (Save.TotalDone >= 1000) Unlock(ACH_1000);
            if (fines == 0 && done > 0) Unlock(ACH_CLEAN);
            if (stars >= 3) Unlock(ACH_STARS);
            if (Save.Level >= 10) Unlock(ACH_LVL10);
            if (Save.Level >= 50) Unlock(ACH_LVL50);
            if (Save.Chapter >= 5) Unlock(ACH_STORY);
            ReportScore(Save.TotalEarned);
            Upload();
        }
    }
}
