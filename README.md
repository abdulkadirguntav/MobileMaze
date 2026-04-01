# 🏃 Project: E.C.H.O (Test Subject Zero)

![Unity](https://img.shields.io/badge/Unity-2022.3%2B-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-Programming-blue?style=for-the-badge&logo=c-sharp)
![Platform](https://img.shields.io/badge/Platform-Android-green?style=for-the-badge&logo=android)
![Status](https://img.shields.io/badge/Status-In_Development-orange?style=for-the-badge)

> *"Bir simülasyonun içinde olduğunu anladığında, yapabileceğin tek şey kuralları yıkmaktır."*

**Project: E.C.H.O**, yapay zeka tarafından yönetilen renkli ve ölümcül bir VR simülasyonundan kaçmaya çalışan gelişmiş bir prototipi yönettiğimiz, 3 şeritli (3-lane) ve yüksek tempolu bir 3D Endless Runner (Sonsuz Koşu) oyunudur. Sadece sağa sola kaçmaktan ibaret değil; yerçekimine meydan okuyan akrobatik hareketlerle hayatta kalma mücadelesidir.

![Gameplay Promo]([Buraya_Oyunundan_Kisa_Bir_Oynanis_GIFi_Ekle_Mutlaka.gif])

## 🎮 Temel Mekanikler (Gameplay Features)

Klasik mobil koşu oyunlarına (Subway Surfers vb.) ek olarak, oyuncu reflekslerini ödüllendiren dinamik fizik mekanikleri entegre edilmiştir:

* **Pürüzsüz 3 Şerit Sistemi:** Ekranda kaydırma (Swipe) ile şeritler arası `Vector3.Lerp` kullanılarak kesintisiz ve akıcı geçiş.
* **Ağırlıksız Duvar Koşusu (Wall-Run):** Oyuncu uygun yüzeylerde yerçekimi kapatılarak duvara kilitlenir. Bu, geçilemez lazer ve engelleri aşmanın tek yoludur.
* **Fast-Fall (Hızlı Düşüş) & Kayma:** Havadayken aşağı kaydırma komutu verildiğinde karakter anında yere çakılır (Fast-Fall) ve kayma animasyonuna geçerek hitbox'ını küçültür.
* **Risk ve Ödül (Close Call):** Engellere çarpmasına milisaniyeler kala kaçınan oyuncular ekstra puanla ödüllendirilir.

## 🛠️ Teknik Altyapı ve Mimari (Under the Hood)

Bu proje, mobil platformlarda maksimum performans (60 FPS) hedeflenerek tasarlanmıştır:

* **Dinamik Level Jeneratörü (Chunk Spawner):** Sonsuz tünel hissini vermek için modüler yol parçaları rastgele ancak mantıklı bir sırayla oluşturulur.
* **Object Pooling:** Sürekli yeni obje üretmek (Instantiate/Destroy) yerine, arkada kalan yol parçaları (Chunk'lar) ve engeller havuza geri gönderilerek RAM dostu bir bellek yönetimi (Garbage Collection Optimizasyonu) sağlanır.
* **State-Driven Animasyonlar:** Karakterin rotasyonu ve fiziksel durumu (duvarda olma, havada olma) kod ile anlık olarak manipüle edilir. Mixamo animasyonları, Custom Rigidbody fizikleriyle harmanlanmıştır.

## 🎨 Sanat Tasarımı ve Atmosfer

Oyunun hikayesi "Yapay Zeka Test Simülasyonu" üzerine kuruludur. Bu nedenle kasvetli zindanlar yerine, renkli, düşük poligonlu (Low-Poly) ve canlı bir açık hava VR ortamı tercih edilmiştir. 
* *Görsel Varlıklar (Assets): Kenney & Quaternius (CC0 License)*

## 🚀 Kurulum (Installation)

Projeyi Unity'de açmak ve incelemek için:
1. Repoyu klonlayın: `git clone https://github.com/[KullaniciAdin]/Project-Echo.git`
2. Unity Hub üzerinden `Add Project` diyerek klasörü seçin.
3. `Scenes` klasörü altındaki `MainLevel` sahnesini açın.
4. Unity Simulator üzerinden Android veya iOS cihaz formatını seçerek Play tuşuna basın.

## 🗺️ Yol Haritası (Roadmap)

- [x] Temel Swipe ve 3-Lane sisteminin kodlanması
- [x] Zıplama, Kayma ve Fast-Fall mekanikleri
- [x] Wall-Run (Duvar koşusu) fizik manipülasyonu
- [x] Chunk Spawner (Sonsuz Level Üreticisi) entegrasyonu
- [ ] Low-Poly renkli simülasyon çevresinin tasarlanması (Yapım Aşamasında)
- [ ] Engel havuzu ve zorluk eğrisinin (Difficulty Curve) ayarlanması
- [ ] Toplanabilir eşyalar (Collectibles) ve UI (Kullanıcı Arayüzü) entegrasyonu
- [ ] Audio (BGM ve SFX) eklemeleri

## 👨‍💻 Geliştirici

**Abdülkadir Güntav**
4. Sınıf Bilgisayar Mühendisliği Öğrencisi | Sazakan Studio
* [LinkedIn Profilim]([LinkedIn_Linkini_Buraya_Koy])
* [Medium Makalelerim]([Medium_Linkini_Buraya_Koy])
