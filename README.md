# SimpleARPlacement

Aplikasi Augmented Reality (AR) sederhana berbasis **Unity** + **AR Foundation** + **ARCore**
untuk perangkat Android. Aplikasi mendeteksi permukaan (lantai/meja), menampilkan
visualisasi grid permukaan, dan memungkinkan pengguna **menempatkan objek 3D (kursi)**
dengan mengetuk layar, lalu memindahkan, merotasi, mengubah skala, atau mereset objek.

Dokumen ini berbahasa Indonesia dan menjelaskan seluruh langkah dari membuka proyek
sampai menghasilkan APK.

---

## 1. Prasyarat Instalasi

| Kebutuhan | Keterangan |
|---|---|
| OS pengembangan | Windows 10/11 64-bit (proyek ini dikembangkan dan divalidasi di Windows) |
| Unity Editor | **6000.3.24f1** (LTS) dengan **Android Build Support** |
| Modul Android | Android Build Support, OpenJDK 17 (bawaan), Android SDK, Android NDK (bawaan Unity) |
| Editor IDE | Visual Studio 2022 / Rider (opsional, untuk debugging) |
| Perangkat uji | Smartphone Android dengan dukungan **ARCore**, Android 8.0 (API 26) atau lebih baru |
| Aksesori | Kabel USB, **USB Debugging** aktif di perangkat uji |
| Koneksi internet | Dibutuhkan saat impor proyek pertama (Package Manager) dan build pertama (Gradle) |

> Catatan: instalasi Android Build Support + OpenJDK + SDK + NDK pada proyek ini
> sudah dilakukan melalui Unity Hub. Jika Anda memindahkan proyek ke mesin lain,
> ulangi instalasi modul Android melalui Unity Hub.

---

## 2. Versi Unity dan Package

* **Unity Editor:** 6000.3.24f1 (`ProjectSettings/ProjectVersion.txt`)
* **Render Pipeline:** Universal Render Pipeline (URP) 17.3.0

Package utama (`Packages/manifest.json`):

| Package | Versi | Fungsi |
|---|---|---|
| `com.unity.xr.arfoundation` | 6.5.0 | API AR (plane, raycast, anchor, session) |
| `com.unity.xr.arcore` | 6.5.0 | Provider AR untuk Android |
| `com.unity.xr.arkit` | 6.5.0 | Provider AR untuk iOS (belum dikonfigurasi build iOS) |
| `com.unity.xr.management` | 4.6.1 | XR Plug-in Management |
| `com.unity.inputsystem` | 1.20.0 | Input System (tap sentuh/mouse) |
| `com.unity.xr.interaction.toolkit` | 3.5.1 | Menyediakan prefab **XR Origin (AR Rig)** |
| `com.unity.render-pipelines.universal` | 17.3.0 | Shader/material URP |

`activeInputHandler: 1` → proyek memakai **Input System Package** (bukan input legacy).
Seluruh skrip penempatan memakai `Touchscreen`/`Mouse` dari Input System.

---

## 3. Cara Membuka Proyek

1. Buka **Unity Hub** → tab *Projects* → *Add* → pilih folder proyek
   (contoh: `D:\Unity\RVA 1`).
2. Buka proyek dengan editor **6000.3.24f1**.
3. Tunggu proses impor selesai (Package Manager + shader).
4. Buka scene **`Assets/Scenes/SimpleARPlacement.unity`** (sudah menjadi scene aktif
   di Build Settings, urutan pertama).

> Scene lama `Assets/Scenes/SampleScene.unity` (template AR bawaan) **tetap ada**
> dan tidak dihapus, hanya dinonaktifkan dari Build Settings.

### (Opsional) Bangun ulang scene dari tool

Scene, prefab, dan material dapat dibuat ulang kapan saja melalui menu:

```
Tools > SimpleARPlacement > 1. Buat Scene dan Prefab
Tools > SimpleARPlacement > 2. Validasi Konfigurasi
Tools > SimpleARPlacement > 3. Build APK Android
```

Atau melalui command line (batchmode):

```powershell
& "D:\Unity\6000.3.24f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "D:\Unity\RVA 1" `
  -executeMethod SimpleARPlacement.EditorTools.SimpleARPlacementTool.CreateSceneAndPrefabsBatch `
  -logFile "Logs\setup_scene.log"
```

---

## 4. Struktur File yang Dibuat

```
Assets/
├── Scenes/
│   └── SimpleARPlacement.unity          ← scene utama (dibuat tool)
├── SimpleARPlacement/
│   ├── Scripts/
│   │   ├── ARPlacementManager.cs        ← input tap, raycast, tempat/pindah/rotasi/skala/reset
│   │   ├── ARPlaneController.cs         ← deteksi & visualisasi plane, izin kamera, status AR
│   │   └── ARUIController.cs            ← teks status/instruksi, toast, tombol UI
│   ├── Editor/
│   │   └── SimpleARPlacementTool.cs     ← tool pembuat scene/prefab + build APK
│   ├── Prefabs/
│   │   ├── SimpleARPlaneVisualizer.prefab  ← visualisasi permukaan (grid)
│   │   └── SimpleChair.prefab              ← objek 3D kursi (dari primitive)
│   ├── Materials/
│   │   ├── SimpleARPlaneMaterial.mat       ← material grid transparan (URP Unlit)
│   │   ├── SimpleARIndicator.mat           ← material indikator penempatan
│   │   ├── SimpleChairSeat.mat             ← material kayu jok sandaran
│   │   └── SimpleChairLeg.mat              ← material kayu kaki kursi
│   └── Textures/
│       └── PlaneGridTexture.asset          ← tekstur grid (dibuat prosedural)
Docs/SimpleARPlacement/
│   ├── Panduan_Pengujian.md              ← tabel 5 skenario pengujian
│   ├── Checklist_Screenshot_dan_Video.md ← checklist bukti visual
│   └── Laporan_Akhir.md                  ← laporan hasil kerja
README.md                                 ← dokumen ini
```

### Komponen pada scene `SimpleARPlacement`

| Objek | Komponen utama | Tugas |
|---|---|---|
| `AR Session` | `ARSession`, `ARInputManager` | Menjalankan sesi AR, mencocokkan frame rate |
| `XR Origin (AR Rig)` | `XROrigin`, `ARCameraManager`, `ARCameraBackground`, `TrackedPoseDriver`, `ARPlaneManager`, `ARRaycastManager`, `ARAnchorManager`, `ARPlaneController`, `ARPlacementManager` | Kamera AR + manajer permukaan + logika penempatan |
| `Placement Indicator` | Ring/pin primitif | Menunjukkan titik hasil raycast sebelum objek ditempatkan |
| `SimpleAR Placement UI` | `Canvas`, `ARUIController` | Status, instruksi, toast, tombol |
| `EventSystem` | `EventSystem`, `InputSystemUIInputModule` | Input UI sesuai Input System |
| `Directional Light` | `Light` | Pencahayaan objek 3D |

---

## 5. Cara Konfigurasi Android

Konfigurasi berikut **sudah diterapkan** pada proyek (dicek oleh menu *Validasi Konfigurasi*):

| Pengaturan | Nilai | Lokasi |
|---|---|---|
| Build Target | Android | File > Build Settings |
| XR Plug-in Management | **ARCore** aktif (Android) | Project Settings > XR Plug-in Management |
| ARCore Settings | Requirement = Required | Project Settings > XR Plug-in Management > ARCore |
| Scripting Backend | IL2CPP | Project Settings > Player |
| Target Architectures | ARM64 | Project Settings > Player |
| Minimum API Level | 26 (syarat ARCore di Unity 6000.3 = 25) | Project Settings > Player |
| Graphics API | OpenGLES3 (manual, tanpa Vulkan) | Project Settings > Player |
| Package name | `com.simplearplacement.app` | Project Settings > Player |
| Orientation | Auto Rotation | Project Settings > Player |
| Tool Android (JDK/SDK/NDK) | Bawaan Unity di `Editor\Data\PlaybackEngines\AndroidPlayer\` | Diatur otomatis oleh tool saat build |

Jika proyek dipindah ke mesin lain, pastikan **Android Build Support** terpasang
(Unity Hub → Installs → Add Modules → Android Build Support).

---

## 6. Cara Menjalankan Aplikasi

### A. Di Editor (tanpa perangkat)

AR sesungguhnya tidak berjalan di editor, tetapi UI dan alur logika dapat dicek:

1. Buka scene `SimpleARPlacement.unity`.
2. Tekan **Play**. Pesan status akan menunjukkan kondisi AR (mis. `AR: None`).
3. Gunakan simulasi XR jika diperlukan: *Project Settings > XR Plug-in Management >
   Standalone > AR Foundation Simulation* (sudah tersedia di proyek ini).

### B. Di perangkat Android

1. Aktifkan **USB Debugging** (Settings > Developer Options), sambungkan kabel USB.
2. Pilih perangkat di **File > Build Settings > Android > Run Device**.
3. Tekan **Play** pada toolbar Unity (Build And Run), atau pasang APK hasil build.
4. Beri **izin kamera** saat diminta.
5. Arahkan kamera ke lantai/meja bergaris/bertekstur, gerakkan perlahan.
6. Setelah grid muncul → **ketuk permukaan** untuk menempatkan kursi.

---

## 7. Cara Membangun APK

### Lewat menu editor

1. `Tools > SimpleARPlacement > 3. Build APK Android`
2. Tunggu sampai selesai. Hasil: `Builds/SimpleARPlacement.apk` (folder proyek).

### Lewat command line (reproducible)

```powershell
& "D:\Unity\6000.3.24f1\Editor\Unity.exe" -batchmode -nographics `
  -projectPath "D:\Unity\RVA 1" -buildTarget Android `
  -executeMethod SimpleARPlacement.EditorTools.SimpleARPlacementTool.BuildApkBatch `
  -logFile "Logs\build_apk.log"
```

Build pertama memerlukan waktu lebih lama (impor Android + IL2CPP + Gradle).
Pastikan koneksi internet aktif agar Gradle dapat mengunduh dependensinya.

### Instal ke perangkat

```powershell
adb install -r "Builds\SimpleARPlacement.apk"
```

> APK ditandatangani dengan **debug keystore** bawaan Unity sehingga hanya cocok
> untuk pengujian internal/instalasi manual (belum untuk Play Store).

---

## 8. Kontrol Aplikasi (UI)

| Elemen | Fungsi |
|---|---|
| Baris atas (biru) | Status AR: `AR: <state> | Izin | Plane: <jumlah>` |
| Instruksi (putih) | Pesan langkah berikutnya untuk pengguna |
| Pesan kuning (toast) | Umpan balik singkat: objek ditempatkan/dipindah, raycast gagal, dll. |
| `◀` `▶` | Rotasi objek 30° per tekan |
| `−` `+` | Perkecil / perbesar objek (faktor 1.2, batas 0.4×–3×) |
| `Reset` | Hapus objek yang ditempatkan |
| `Grid` | Tampilkan/sembunyikan visualisasi permukaan |

Perilaku penempatan:

* **Tap pertama** pada permukaan → objek muncul tepat di hasil raycast (pivot di dasar objek).
* **Tap berikutnya** pada permukaan lain → objek **dipindahkan** (tidak membuat objek ganda).
* Objek diikat ke **ARAnchor** yang menempel pada plane, sehingga mengikuti perbaikan tracking.
* Tap di atas UI atau area tanpa permukaan → tidak menempatkan objek dan menampilkan toast.

---

## 9. Cara Melakukan Pengujian

Ikuti **5 skenario** pada
[`Docs/SimpleARPlacement/Panduan_Pengujian.md`](Docs/SimpleARPlacement/Panduan_Pengujian.md):
permukaan bertekstur, permukaan polos, cahaya terang, cahaya redup, gerakan kamera cepat.
Isi kolom hasil setelah diuji pada perangkat fisik.

Checklist bukti visual (screenshot & video 1–2 menit):
[`Docs/SimpleARPlacement/Checklist_Screenshot_dan_Video.md`](Docs/SimpleARPlacement/Checklist_Screenshot_dan_Video.md)

---

## 10. Batasan Implementasi yang Diketahui

1. **Belum diuji pada perangkat fisik** dalam sesi pengembangan ini (lihat
   `Docs/SimpleARPlacement/Laporan_Akhir.md`). Kompilasi, pembuatan scene/prefab,
   validasi konfigurasi, dan build APK dilakukan lewat Unity batchmode.
2. Objek 3D berupa **kursi sederhana dari primitive Unity** (bukan model eksternal),
   sesuai ketentuan agar proyek berjalan tanpa aset tambahan.
3. **Belum ada bayangan pantulan (AR shadow receiver)** — objek memakai pencahayaan
   directional biasa sehingga bayangan di permukaan nyata tidak dirender.
   Dapat ditambahkan dengan *AR Shadow Receiver* bila diperlukan.
4. **Tidak ada mode multi-objek**: hanya satu objek aktif (ketentuan anti-duplikasi).
5. Build iOS belum dikonfigurasi (hanya Android yang disiapkan), meskipun ARKit terpasang.
6. APK ditandatangani debug keystore — untuk distribusi perlu keystore rilis.
7. Fitur **drag dengan jari** belum diimplementasikan; pemindahan dilakukan dengan
   mengetuk titik permukaan baru (sesuai skenario nilai tambah yang dipilih).
8. Jika perangkat tidak mendukung ARCore, APK dengan *requirement = Required* tidak
   dapat dipasang; jika dipasang pada kondisi lain, aplikasi menampilkan
   “Perangkat ini tidak mendukung ARCore”.

---

## 11. Penanganan Kesalahan yang Tersedia

| Kondisi | Perilaku aplikasi |
|---|---|
| Perangkat tidak mendukung ARCore | Pesan “Perangkat ini tidak mendukung ARCore…” |
| Izin kamera ditolak | Pesan instruksi membuka Pengaturan aplikasi |
| Izin kamera sedang diminta | Pesan “Menunggu izin kamera…” |
| Belum ada permukaan | “Arahkan kamera ke lantai atau meja, lalu gerakkan perlahan.” |
| Tracking terganggu / alasan | “Tracking terganggu (&lt;alasan&gt;). Arahkan kamera ke permukaan bertekstur…” |
| Tap tanpa hasil raycast | Toast “Tidak ada permukaan pada titik tersebut.” |
| Prefab objek tidak ada | Log error + kubus cadangan dibuat runtime |
| ARPlaneManager / ARRaycastManager tidak ada | Log error pada Awake |

---

## 12. Referensi

* Unity AR Foundation Manual: *Plane detection*, *Raycasts*, *Anchors*
* ARCore Unity requirements (min API level, arsitektur 64-bit)
* Skrip sumber ada di `Assets/SimpleARPlacement/Scripts/`
