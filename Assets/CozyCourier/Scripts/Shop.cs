using System;
using System.Collections.Generic;
using UnityEngine;
#if COZY_IAP
using Unity.Services.Core;
using UnityEngine.Purchasing;
#endif

namespace CozyCourier
{
    /// <summary>Do'kondagi mahsulot (Google Play Console'da AYNAN shu ID bilan yaratiladi).</summary>
    public class ShopItem
    {
        public string id;
        public bool consumable;
        public string nameKey, descKey;
        public int coins;
        public string fallbackPrice; // do'kon ulanmagan paytda ko'rsatiladigan taxminiy narx
        public string color;
    }

    /// <summary>
    /// Ichki xaridlar (Unity In-App Purchasing 5). Paket o'rnatilmaguncha "COZY_IAP" belgisi qo'yilmaydi va
    /// soxta versiya ishlaydi: Editor'da xarid darhol "muvaffaqiyatli" bo'ladi (sinash uchun), telefonda esa
    /// "Do'kon hozir mavjud emas" chiqadi.
    /// </summary>
    public static class Shop
    {
        public const string NO_ADS = "cozy_no_ads", VIP = "cozy_golden_pack",
            COIN_S = "cozy_coins_100k", COIN_M = "cozy_coins_300k", COIN_L = "cozy_coins_1m";

        public static readonly ShopItem[] Items =
        {
            new ShopItem { id = VIP, nameKey = "pVip", descKey = "pVipD", coins = 500000, fallbackPrice = "$4.99", color = "#e9c46a" },
            new ShopItem { id = NO_ADS, nameKey = "pNoAds", descKey = "pNoAdsD", fallbackPrice = "$2.99", color = "#46d0bf" },
            new ShopItem { id = COIN_S, consumable = true, nameKey = "pCoinS", descKey = "pCoinD", coins = 100000, fallbackPrice = "$0.99", color = "#f4a259" },
            new ShopItem { id = COIN_M, consumable = true, nameKey = "pCoinM", descKey = "pCoinD", coins = 300000, fallbackPrice = "$1.99", color = "#ee9583" },
            new ShopItem { id = COIN_L, consumable = true, nameKey = "pCoinL", descKey = "pCoinD", coins = 1000000, fallbackPrice = "$4.99", color = "#c3a6e8" },
        };

        /// <summary>Xarid tugaganda (muvaffaqiyatli yoki yo'q) — interfeysni yangilash uchun.</summary>
        public static event Action<string, bool> OnResult;

        public static bool Owned(string id) => id == NO_ADS ? Save.NoAds : id == VIP ? Save.Vip : false;

        /// <summary>Mahsulotni o'yinchiga beradi (do'kon tasdiqlagandan keyin).</summary>
        static void Grant(string id)
        {
            ShopItem item = null;
            foreach (var it in Items) if (it.id == id) item = it;
            if (item == null) { Debug.LogWarning("[Shop] Noma'lum mahsulot: " + id); return; }
            if (id == NO_ADS) Save.NoAds = true;
            else if (id == VIP)
            {
                if (Save.Vip) return; // ikki marta berilmasin (tiklashda)
                Save.Vip = true; Save.NoAds = true;
                Save.Own("scooter", "gold");
                Save.Wallet += item.coins;
            }
            else Save.Wallet += item.coins;
            Debug.Log("[Shop] Berildi: " + id);
        }

        static void Report(string id, bool ok)
        {
            try { OnResult?.Invoke(id, ok); } catch (Exception e) { Debug.LogException(e); }
        }

#if COZY_IAP
        static StoreController store;
        static bool connected, fetched;
        static readonly Dictionary<string, Product> products = new Dictionary<string, Product>();

        public static bool Ready => connected && fetched;

        public static async void Init()
        {
            try
            {
                try { await UnityServices.InitializeAsync(); }
                catch (Exception e) { Debug.LogWarning("[Shop] Unity Services ishga tushmadi (xaridlar baribir ishlaydi): " + e.Message); }

                store = UnityIAPServices.StoreController();
                store.OnStoreDisconnected += d => { connected = false; Debug.LogWarning("[Shop] Do'kon uzildi: " + d.message); };
                store.OnProductsFetched += list =>
                {
                    foreach (var p in list) products[p.definition.id] = p;
                    fetched = true;
                    store.FetchPurchases(); // oldin sotib olinganlarni tiklash
                };
                store.OnProductsFetchFailed += f => Debug.LogWarning("[Shop] Mahsulotlar olinmadi: " + f.FailureReason);
                store.OnPurchasePending += order =>
                {
                    foreach (var item in order.CartOrdered.Items()) Grant(item.Product.definition.id);
                    store.ConfirmPurchase(order);
                    foreach (var item in order.CartOrdered.Items()) Report(item.Product.definition.id, true);
                };
                store.OnPurchaseFailed += failed =>
                {
                    string id = "";
                    foreach (var item in failed.CartOrdered.Items()) id = item.Product.definition.id;
                    Debug.LogWarning("[Shop] Xarid bo'lmadi: " + failed.FailureReason + " " + failed.Details);
                    Report(id, false);
                };
                store.OnPurchasesFetched += orders =>
                {
                    // qayta o'rnatilganda: bir martalik (non-consumable) xaridlarni tiklaymiz
                    foreach (var o in orders.ConfirmedOrders)
                        foreach (var item in o.CartOrdered.Items())
                        {
                            string id = item.Product.definition.id;
                            if (id == NO_ADS || id == VIP) Grant(id);
                        }
                    Report("", true);
                };

                await store.Connect();
                connected = true;

                var defs = new List<ProductDefinition>();
                foreach (var it in Items) defs.Add(new ProductDefinition(it.id, it.consumable ? ProductType.Consumable : ProductType.NonConsumable));
                store.FetchProducts(defs);
            }
            catch (Exception e) { Debug.LogWarning("[Shop] Do'kon ishga tushmadi: " + e.Message); }
        }

        public static string Price(ShopItem it)
        {
            if (products.TryGetValue(it.id, out var p) && p != null && p.metadata != null && !string.IsNullOrEmpty(p.metadata.localizedPriceString))
                return p.metadata.localizedPriceString;
            return it.fallbackPrice;
        }

        public static void Buy(string id)
        {
            if (!Ready || !products.TryGetValue(id, out var p) || p == null) { Report(id, false); return; }
            store.PurchaseProduct(p);
        }

        public static void Restore()
        {
            if (store != null && connected) store.FetchPurchases();
            else Report("", false);
        }
#else
        public static bool Ready => Application.isEditor;
        public static void Init() { }
        public static string Price(ShopItem it) => it.fallbackPrice;

        public static void Buy(string id)
        {
            if (Application.isEditor)
            {
                Debug.Log("[Shop] Editor: xarid sinov tariqasida berildi: " + id);
                Grant(id);
                Report(id, true);
            }
            else Report(id, false);
        }

        public static void Restore() => Report("", Application.isEditor);
#endif
    }
}
