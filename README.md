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
│   ├── SimpleARPlacement.unity            ← scene utama = SALINAN template Mobile AR
│   │                                           (dibuat tool dari SampleScene.unity)
│   └── SampleScene.unity                  ← scene template asli (tidak diubah, nonaktif di build)
├── MobileARTemplateAssets/                ← aset bawaan template (UI, prompt, prefab, script)
├── Samples/XR Interaction Toolkit/3.5.1/  ← rig AR + interactor bawaan template
├── SimpleARPlacement/
│   ├── Scripts/
│   │   ├── ARPlacementManager.cs          ← versi awal: penempatan sendiri (tidak dipakai scene final)
│   │   ├── ARPlaneController.cs           ← versi awal: status/izin (tidak dipakai scene final)
│   │   └── ARUIController.cs              ← versi awal: UI sendiri (tidak dipakai scene final)
│   ├── Editor/
│   │   └── SimpleARPlacementTool.cs       ← tool: salin scene template, validasi, build APK
│   ├── Prefabs/ , Materials/ , Textures/  ← aset versi awal (dipertahankan, tidak dipakai scene final)
├── XR/ , XRI/ , Settings/                 ← konfigurasi XR/URP bawaan template
Docs/SimpleARPlacement/
│   ├── Panduan_Pengujian.md               ← tabel 5 skenario pengujian
│   ├── Checklist_Screenshot_dan_Video.md  ← checklist bukti visual
│   ├── Screenshots/                       ← screenshot bukti dari perangkat
│   └── Laporan_Akhir.md                   ← laporan hasil kerja
README.md                                  ← dokumen ini
```

### Komponen pada scene `SimpleARPlacement` (milik template Mobile AR)

| Objek | Komponen utama | Tugas |
|---|---|---|
| `AR Session` | `ARSession`, `ARInputManager` | Menjalankan sesi AR |
| `XR Origin (AR Rig)` | `XROrigin`, `ARCameraManager`, `ARCameraBackground`, `TrackedPoseDriver`, `ARPlaneManager`, `ARRaycastManager`, `ARFeatheredPlaneMeshVisualizer` | Kamera AR + deteksi/visualisasi permukaan |
| `Object Spawner` | `ObjectSpawner`, `ARInteractorSpawnTrigger`, `XRRayInteractor`, `XRInteractionGroup` | Menempatkan objek yang dipilih pada permukaan |
| `UI` | `Canvas`, `GraphicRaycaster`, `XRUIInputModule`, `EventSystem`, `ARTemplateMenuManager`, `GoalManager`, `ARDebugMenu` | Coaching prompt, object menu, tombol, onboarding |
| `EventSystem` | `EventSystem` + `XRUIInputModule` | Input UI (module khusus XRI) |
| `Directional Light` | `Light` | Pencahayaan objek |

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

## 8. Kontrol Aplikasi (UI bawaan template)

| Elemen | Fungsi |
|---|---|
| Coaching prompt (tengah layar) | Panduan langkah: *Scan Surfaces* → *Tap to Place* → *Move/Rotate/Scale Object* |
| Object menu (baris bawah) | Pilih objek yang akan ditempatkan (cube, pyramid, torus, wedge, dll.) |
| `Cancel` | Menutup object menu |
| Tombol `⋯` (kanan atas) | Membuka menu opsi (object menu, debug, slider, hapus objek) |
| `Options Modal` | Create/Delete, Remove Objects, Debug Plane Toggle, Debug Menu Toggle, Hints |
| `Debug Plane Toggle` | Menampilkan/menyembunyikan visualisasi permukaan (plane) |
| `Greeting Prompt` / `Hints` | Onboarding & petunjuk gestur |

Interaksi objek (disediakan template):

* **Ketuk permukaan** → objek terpilih ditempatkan pada titik raycast.
* **Seret (drag) satu jari** → memindahkan objek.
* **Cubit/putar dua jari** → mengubah skala dan rotasi objek (gestur XRI
  `TouchscreenGestureInputController`).
* **Delete / Remove Objects** → menghapus objek (reset).

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

1. **UI diganti dengan UI bawaan template Mobile AR** (permintaan saat pengembangan,
   karena sudah lengkap): coaching prompt, object menu, options, debug plane toggle,
   onboarding goal. Skrip buatan sendiri (`ARPlacementManager`, `ARPlaneController`,
   `ARUIController`) tetap ada di repo tetapi **tidak dipakai oleh scene final**.
2. Objek 3D memakai **objek bawaan template** (cube, pyramid, torus, wedge, arch, dll.
   dari `MobileARTemplateAssets/Prefabs`), bukan kursi buatan sendiri.
3. **Multi-objek diizinkan** (berbeda dengan ketentuan awal anti-duplikasi): template
   menyediakan Create/Delete/Remove Objects.
4. Rotasi & skala dilakukan dengan **gestur dua jari** (bukan tombol +/−); pemindahan
   dengan drag satu jari.
5. Teks UI template berbahasa **Inggris** (bawaan Unity), tidak diterjemahkan.
6. **Belum ada bayangan pantulan (AR shadow receiver)** — visualisasi permukaan memakai
   *feathered plane* bawaan template.
7. Build iOS belum dikonfigurasi (hanya Android yang disiapkan), meskipun ARKit terpasang.
8. APK ditandatangani debug keystore — untuk distribusi perlu keystore rilis.
9. Jika perangkat tidak mendukung ARCore, APK dengan *requirement = Required* tidak
   dapat dipasang dari Play Store/perangkat non-ARCore.
10. **Pengujian terbatas**: aplikasi terpasang dan berjalan di Samsung Galaxy A54
    (sesi AR aktif, UI template tampil), tetapi tabel 5 skenario pengujian dan
    checklist screenshot/video belum diisi penuh.

---

## 11. Penanganan Kesalahan yang Tersedia

| Kondisi | Perilaku aplikasi |
|---|---|
| Belum ada permukaan | Coaching prompt template: *Scan Surfaces* / *Tap to Place* |
| Izin kamera | Diminta otomatis oleh AR Foundation saat sesi dimulai |
| Tracking terganggu | Template menampilkan status melalui `ARDebugMenu`/coaching UI |
| Objek dihapus | Tombol `Delete` / `Remove Objects` pada menu opsi |
| Visualisasi permukaan | `Debug Plane Toggle` pada menu opsi |
| (Versi awal) Pesan status, toast, dan deteksi perangkat non-ARCore | Tersedia pada skrip `ARPlaneController`/`ARUIController`, **tidak aktif** di scene final |

---

## 12. Referensi

* Unity AR Foundation Manual: *Plane detection*, *Raycasts*, *Anchors*
* ARCore Unity requirements (min API level, arsitektur 64-bit)
* Skrip sumber ada di `Assets/SimpleARPlacement/Scripts/`
