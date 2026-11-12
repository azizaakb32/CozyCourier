using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CozyCourier.EditorTools
{
    /// <summary>
    /// Loyiha sozlamalarini avtomatik qo'llaydi: faqat yotiq ekran, nom, paket nomi,
    /// Unity logotipisiz ochilish, ikonkalar. Menyu: "Cozy Courier → Sozlamalarni qo'llash".
    /// Unity ochilganda bir marta o'zi ishlaydi.
    /// </summary>
    [InitializeOnLoad]
    static class CozySetup
    {
        const string KEY = "CozyCourier.Setup.v1";
        const string ICON = "Assets/CozyCourier/Icon/";

        static CozySetup()
        {
            EditorApplication.delayCall += SyncDefines;
            EditorApplication.delayCall += () =>
            {
                // har bir loyiha va platforma (masalan, Android'ga o'tganda) uchun bir marta
                string k = KEY + "." + Application.dataPath + "." + EditorUserBuildSettings.activeBuildTarget;
                if (EditorPrefs.GetBool(k, false)) return;
                Apply(false);
                EditorPrefs.SetBool(k, true);
            };
        }

        [MenuItem("Cozy Courier/Sozlamalarni qo'llash (landscape, ikonka, Android)")]
        static void Menu() { Apply(true); SyncDefines(); }

        /// <summary>
        /// AdMob va IAP paketlari o'rnatilganini tekshiradi va COZY_ADMOB / COZY_IAP belgilarini
        /// avtomatik qo'yadi yoki olib tashlaydi. Shunda paketni o'rnatishning o'zi kifoya.
        /// </summary>
        static void SyncDefines()
        {
            bool admob = HasType("GoogleMobileAds.Api.MobileAds") && HasType("GoogleMobileAds.Ump.Api.ConsentInformation");
            bool iap = HasType("UnityEngine.Purchasing.UnityIAPServices") && HasType("Unity.Services.Core.UnityServices");
            bool pgs = HasType("GooglePlayGames.PlayGamesPlatform");
            bool notif = HasType("Unity.Notifications.Android.AndroidNotificationCenter");
            foreach (var t in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
            {
                string cur = PlayerSettings.GetScriptingDefineSymbols(t);
                var list = new System.Collections.Generic.List<string>();
                foreach (var d in cur.Split(';')) if (d.Trim().Length > 0) list.Add(d.Trim());
                bool changed = Toggle(list, "COZY_ADMOB", admob) | Toggle(list, "COZY_IAP", iap) | Toggle(list, "COZY_PGS", pgs) | Toggle(list, "COZY_NOTIF", notif);
                if (changed)
                {
                    PlayerSettings.SetScriptingDefineSymbols(t, string.Join(";", list));
                    Debug.Log("[Cozy] " + t.TargetName + " belgilari: " + string.Join(";", list));
                }
            }
        }

        static bool Toggle(System.Collections.Generic.List<string> list, string sym, bool on)
        {
            if (on && !list.Contains(sym)) { list.Add(sym); return true; }
            if (!on && list.Contains(sym)) { list.Remove(sym); return true; }
            return false;
        }

        static bool HasType(string fullName)
        {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try { if (asm.GetType(fullName, false) != null) return true; } catch { }
            }
            return false;
        }

        static void Apply(bool verbose)
        {
            // 1) faqat yotiq ekran
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // 2) nom va paket
            PlayerSettings.productName = "Cozy Courier";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "uz.cozycourier.game");

            // 3) ochilish: Unity logotipi o'rniga o'yinning o'z ekrani (Hud.ShowSplash)
            try
            {
                PlayerSettings.SplashScreen.showUnityLogo = false;
                PlayerSettings.SplashScreen.show = false;
            }
            catch (System.Exception e) { Debug.LogWarning("[Cozy] Splash sozlanmadi: " + e.Message); }
            PlayerSettings.SplashScreen.backgroundColor = new Color32(0x3e, 0x34, 0x66, 255);

            // 4) ikonkalar
            var full = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON + "icon_1024.png");
            var fg = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON + "adaptive_foreground.png");
            var bg = AssetDatabase.LoadAssetAtPath<Texture2D>(ICON + "adaptive_background.png");
            if (full != null) PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { full }, IconKind.Any);
#if UNITY_ANDROID
            try
            {
                if (fg != null && bg != null)
                {
                    var adaptive = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
                    foreach (var ic in adaptive) ic.SetTextures(bg, fg);
                    PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptive);
                }
                if (full != null)
                {
                    foreach (var kind in new[] { UnityEditor.Android.AndroidPlatformIconKind.Round, UnityEditor.Android.AndroidPlatformIconKind.Legacy })
                    {
                        var arr = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
                        foreach (var ic in arr) ic.SetTexture(full);
                        PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, arr);
                    }
                }
            }
            catch (System.Exception e) { Debug.LogWarning("[Cozy] Android ikonkalari sozlanmadi: " + e.Message); }
#endif
            // 5) Google Play talablari: IL2CPP, 64-bit, AAB, eng yangi API darajasi
            try
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
                PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto; // o'rnatilgan eng yangi (36+)
                if ((int)PlayerSettings.Android.minSdkVersion < 24) PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)24;
                EditorUserBuildSettings.buildAppBundle = true;
                if (PlayerSettings.bundleVersion == "0.1" || PlayerSettings.bundleVersion == "0.1.0") PlayerSettings.bundleVersion = "1.0.0";
                if (PlayerSettings.Android.bundleVersionCode < 1) PlayerSettings.Android.bundleVersionCode = 1;
            }
            catch (System.Exception e) { Debug.LogWarning("[Cozy] Android sozlamalari: " + e.Message); }

            AssetDatabase.SaveAssets();
            Debug.Log("[Cozy] Sozlamalar qo'llandi: landscape, nom, ikonka, Unity logotipisiz ochilish.");
            if (verbose) EditorUtility.DisplayDialog("Cozy Courier", "Sozlamalar qo'llandi:\n• faqat yotiq ekran\n• nom va paket nomi\n• ikonka\n• Unity logotipisiz ochilish", "OK");
        }
    }
}
