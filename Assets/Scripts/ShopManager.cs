using UnityEngine;

public class ShopManager : MonoBehaviour
{
    // --- OYUNCU VERİSİ (KAYITLI) ---
    [Header("Oyuncu Verileri (Sadece bilgi amaçlı)")]
    public int totalCoins = 0;

    // ELDİVEN DURUMLARI (0: Kilitli, 1: Açık)
    public int hasStandardGlove = 1; // Varsayılan açık
    public int hasCarbonGlove = 0;
    public int hasGoldGlove = 0;

    // AKTİF ELDİVEN (0: Standart, 1: Karbon, 2: Altın)
    public int equippedGloveIndex = 0;

    // GÜÇLENDİRME SEVİYELERİ
    public int timeUpgradeLevel = 0; // 0: Normal, 1: Gelişmiş
    public int boostUpgradeLevel = 0; // 0: 120sn, 1: 110sn

    // --- MARKET FİYATLARI ---
    [Header("Fiyatlandırma")]
    public int priceCarbonGlove = 2000;
    public int priceGoldGlove = 10000;
    public int priceTimeUpgrade = 5000;
    public int priceBoostUpgrade = 8000;

    private void Awake()
    {
        LoadData();
    }

    // ==========================================
    // 1. KOZMETİK: ELDİVEN SATIN ALMA VE KUŞANMA
    // ==========================================

    public void EquipStandardGlove()
    {
        equippedGloveIndex = 0;
        SaveData();
        ApplyEquippedGlove();
        Debug.Log("Standart Eldiven Kuşanıldı.");
    }

    public void BuyCarbonGlove()
    {
        if (hasCarbonGlove == 1)
        {
            Debug.Log("Zaten Karbon Eldivene Sahipsin!");
            return;
        }

        if (totalCoins >= priceCarbonGlove)
        {
            totalCoins -= priceCarbonGlove;
            hasCarbonGlove = 1;
            EquipCarbonGlove(); // Aldıktan sonra otomatik kuşan
            Debug.Log("Karbon Eldiven Başarıyla Satın Alındı!");
        }
        else
        {
            Debug.LogWarning("Yetersiz Bakiye! Gereken: " + priceCarbonGlove + " | Senin: " + totalCoins);
        }
    }

    public void EquipCarbonGlove()
    {
        if (hasCarbonGlove == 1)
        {
            equippedGloveIndex = 1;
            SaveData();
            ApplyEquippedGlove();
            Debug.Log("Karbon Eldiven Kuşanıldı.");
        }
        else
        {
            Debug.Log("Önce Karbon Eldiveni satın almalısın!");
        }
    }

    public void BuyGoldGlove()
    {
        if (hasGoldGlove == 1)
        {
            Debug.Log("Zaten Altın Eldivene Sahipsin!");
            return;
        }

        if (totalCoins >= priceGoldGlove)
        {
            totalCoins -= priceGoldGlove;
            hasGoldGlove = 1;
            EquipGoldGlove();
            Debug.Log("Altın Eldiven Başarıyla Satın Alındı!");
        }
        else
        {
            Debug.LogWarning("Yetersiz Bakiye! Gereken: " + priceGoldGlove + " | Senin: " + totalCoins);
        }
    }

    public void EquipGoldGlove()
    {
        if (hasGoldGlove == 1)
        {
            equippedGloveIndex = 2;
            SaveData();
            ApplyEquippedGlove();
            Debug.Log("Altın Eldiven Kuşanıldı.");
        }
        else
        {
            Debug.Log("Önce Altın Eldiveni satın almalısın!");
        }
    }

    // Eldivenin oyundaki gerçek materyaline/modeline yansıması
    private void ApplyEquippedGlove()
    {
        // TODO: FPS Kamera'daki kolları tutan script'e veya objeye erişip materyali değiştireceğiz.
        // Örn: HandController.Instance.ChangeMaterial(equippedGloveIndex);
    }

    // ==========================================
    // 2. GÜÇLENDİRME: MEKANİK GELİŞTİRMELERİ
    // ==========================================

    public void BuyTimeUpgrade()
    {
        if (timeUpgradeLevel == 1)
        {
            Debug.Log("Zaman Bükücü zaten maksimum seviyede!");
            return;
        }

        if (totalCoins >= priceTimeUpgrade)
        {
            totalCoins -= priceTimeUpgrade;
            timeUpgradeLevel = 1;
            SaveData();
            ApplyUpgradesToPlayer();
            Debug.Log("Zaman Bükücü Lvl 2 Satın Alındı! Artık süre daha yavaş akacak.");
        }
        else
        {
            Debug.LogWarning("Yetersiz Bakiye! Gereken: " + priceTimeUpgrade + " | Senin: " + totalCoins);
        }
    }

    public void BuyBoostUpgrade()
    {
        if (boostUpgradeLevel == 1)
        {
            Debug.Log("Öfke Kontrolü zaten maksimum seviyede!");
            return;
        }

        if (totalCoins >= priceBoostUpgrade)
        {
            totalCoins -= priceBoostUpgrade;
            boostUpgradeLevel = 1;
            SaveData();
            ApplyUpgradesToPlayer();
            Debug.Log("Öfke Kontrolü Satın Alındı! Boost süresi 110 saniyeye düştü.");
        }
        else
        {
            Debug.LogWarning("Yetersiz Bakiye! Gereken: " + priceBoostUpgrade + " | Senin: " + totalCoins);
        }
    }

    // Geliştirmelerin PlayerController'a uygulanması
    private void ApplyUpgradesToPlayer()
    {
        // Eğer oyun içindeysek ve Player sahnede varsa ona yeni ayarları ilet
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            if (boostUpgradeLevel == 1)
            {
                player.autoBoostCooldown = 110f; // 120'den 110'a düşürdük
            }
            
            if (timeUpgradeLevel == 1)
            {
                player.slowMotionDuration = 8f; // Normal sürenin örneğin 2 katı
            }
        }
    }

    // ==========================================
    // ALTIN KAZANMA VE KAYIT (SAVE/LOAD) SİSTEMİ
    // ==========================================

    // Oyun sonunda kazanılan altını buraya ekleyeceğiz (Örn: GameManager'dan çağırılabilir)
    public void AddCoins(int amount)
    {
        totalCoins += amount;
        SaveData();
        Debug.Log(amount + " Coin Eklendi. Toplam Coin: " + totalCoins);
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt("TotalCoins", totalCoins);
        
        PlayerPrefs.SetInt("HasStandardGlove", hasStandardGlove);
        PlayerPrefs.SetInt("HasCarbonGlove", hasCarbonGlove);
        PlayerPrefs.SetInt("HasGoldGlove", hasGoldGlove);
        PlayerPrefs.SetInt("EquippedGloveIndex", equippedGloveIndex);
        
        PlayerPrefs.SetInt("TimeUpgradeLevel", timeUpgradeLevel);
        PlayerPrefs.SetInt("BoostUpgradeLevel", boostUpgradeLevel);

        PlayerPrefs.Save();
    }

    private void LoadData()
    {
        totalCoins = PlayerPrefs.GetInt("TotalCoins", 0); // Varsayılan 0

        hasStandardGlove = PlayerPrefs.GetInt("HasStandardGlove", 1); // Standart hep var
        hasCarbonGlove = PlayerPrefs.GetInt("HasCarbonGlove", 0);
        hasGoldGlove = PlayerPrefs.GetInt("HasGoldGlove", 0);
        equippedGloveIndex = PlayerPrefs.GetInt("EquippedGloveIndex", 0);

        timeUpgradeLevel = PlayerPrefs.GetInt("TimeUpgradeLevel", 0);
        boostUpgradeLevel = PlayerPrefs.GetInt("BoostUpgradeLevel", 0);

        // Yüklendikten sonra statüleri oyuna yansıt
        ApplyEquippedGlove();
        ApplyUpgradesToPlayer();
    }
}
