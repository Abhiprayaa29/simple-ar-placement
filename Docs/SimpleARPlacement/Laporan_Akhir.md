# Laporan Akhir — SimpleARPlacement

Tanggal laporan: **3 Oktober 2026**
Lingkungan pengembangan: Windows 10/11, Unity Editor **6000.3.24f1**, proyek
`D:\Unity\RVA 1`.

> Status: **pembuatan, konfigurasi, dan build APK selesai dan terverifikasi lewat
> Unity batchmode. Pengujian pada perangkat fisik BELUM dilakukan** (tidak ada
> perangkat Android yang tersedia pada sesi ini). Kolom hasil uji pada
> `Panduan_Pengujian.md` dan `Checklist_Screenshot_dan_Video.md` sengaja dibiarkan
> kosong sampai diisi dari pengujian nyata.

---

## 1. Instalasi yang Dilakukan

| Komponen | Status | Bukti |
|---|---|---|
| Android Build Support (SDK + NDK + platform tools) | Terpasang | `Logs\hub-android-install.log` → *All Tasks Completed Successfully*; SDK punya platforms 34–37.0, build-tools 36.0.0 |
| Android NDK r27c | Terpasang | `Editor\Data\PlaybackEngines\AndroidPlayer\NDK` (27.2.12479018) |
| OpenJDK 17.0.18 | Dipulihkan (manual) | Diunduh dari URL resmi Unity, diekstrak ke `Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK`; verifikasi `java -version` / `javac -version` = 17.0.18 |
| Path tool (JDK/SDK/NDK) di proyek | Dikonfigurasi otomatis oleh tool | Log build: `jdkRootPath/sdRootPath/ndkRootPath` terisi |

## 2. Apa yang Dibuat

| Item | Lokasi |
|---|---|
| 3 skrip runtime | `Assets\SimpleARPlacement\Scripts\{ARPlacementManager, ARPlaneController, ARUIController}.cs` |
| Tool editor (scene/prefab/material + validasi + build) | `Assets\SimpleARPlacement\Editor\SimpleARPlacementTool.cs` |
| Scene utama | `Assets\Scenes\SimpleARPlacement.unity` (urutan 1 di Build Settings; `SampleScene` dinonaktifkan) |
| Prefab | `SimpleARPlaneVisualizer.prefab`, `SimpleChair.prefab` (kursi dari primitive) |
| Material & tekstur | 4 material URP + tekstur grid prosedural |
| Dokumentasi | `README.md`, `Docs\SimpleARPlacement\{Panduan_Pengujian, Checklist_Screenshot_dan_Video, Laporan_Akhir}.md` |

Fitur yang diimplementasikan: izin kamera Android, session AR + penanganan kondisi
gagal, deteksi permukaan horizontal-only, visualisasi grid, raycast penempatan +
indikator, penempatan/ pemindahan objek via ARAnchor (anti duplikasi), tombol
rotasi 30°, skala 0.4×–3×, reset, toggle grid, toast, teks status/instruksi,
penanganan tap di atas UI, toast saat raycast gagal.

## 3. Hasil Verifikasi (batchmode, dapat diulang)

| No | Perintah / metode | Hasil | Log |
|---|---|---|---|
| 1 | `CreateSceneAndPrefabsBatch` | Berhasil — scene, prefab kursi, prefab plane, material dibuat | `Logs\setup_scene.log` |
| 2 | `ValidateSetupBatch` | **Validasi lulus**: scene OK, prefab/material OK, AR Foundation + ARCore terpasang, loader ARCore aktif, Min SDK = AndroidApiLevel26, ARM64 aktif | `Logs\validate.log` |
| 3 | `BuildApkBatch` (`-buildTarget Android`) | **Build Succeeded** — `Hasil build: Succeeded, ukuran 1034539034 byte, waktu 00:11:12, error 0` | `Logs\build_apk.log` |

Verifikasi tambahan pada artefak:

* `Assets\Scenes\SimpleARPlacement.unity` (116.896 byte) memuat komponen ketiga
  skrip; referensi `placeablePrefab` menunjuk ke `SimpleChair.prefab`;
  `m_PlanePrefab` pada ARPlaneManager menunjuk ke `SimpleARPlaneVisualizer.prefab`;
  `m_DetectionMode = 1` (Horizontal); seluruh referensi UI (status, instruksi,
  toast, 5 tombol) tersambung.
* Log build: **0** `error CS`, **0** `warning CS`, tidak ada Exception.

## 4. APK Hasil Build

| Properti | Nilai |
|---|---|
| Berkas | `Builds\SimpleARPlacement.apk` |
| Ukuran | 39.447.059 byte (~37,6 MB) |
| SHA-256 | `BF2EDAEA02860B80E34302F04CB6469C0EEFA477D51EBA0C12A33585240541A5` |
| Package | `com.simplearplacement.app` |
| versionName / versionCode | 1.0.0 / 1 |
| minSdk / targetSdk / compileSdk | 26 / 36 / 36 |
| ABI | `arm64-v8a` (IL2CPP) |
| Izin | `android.permission.CAMERA`, `android.permission.INTERNET` |
| Graphics | OpenGL ES 3.0 (`glEsVersion 0x30000`) |
| ARCore | `uses-feature android.hardware.camera.ar required=true`, `meta-data com.google.ar.core = required` |
| Tanda tangan | Debug keystore bawaan (untuk pengujian manual) |

Perintah pemasangan:

```powershell
adb install -r "D:\Unity\RVA 1\Builds\SimpleARPlacement.apk"
```

## 4.5 Iterasi: scene diganti menjadi salinan template Mobile AR

Permintaan saat pengujian: **gunakan UI bawaan template Mobile AR** (dianggap sudah
lengkap). Tindakan:

| Perubahan | Detail |
|---|---|
| Scene utama | `Assets/Scenes/SimpleARPlacement.unity` dibuat ulang sebagai **salinan persis** `SampleScene.unity` (template) oleh tool (`CreateSceneFromTemplate`) |
| UI | Coaching prompt (*Scan Surfaces*, *Tap to Place*, *Move/Rotate/Scale*), object menu, `Cancel`, tombol `⋯` (Options), `Delete`/`Remove Objects`, `Debug Plane Toggle`, slider, onboarding `GoalManager` |
| Input UI | `XRUIInputModule` (milik XRI) menggantikan `InputSystemUIInputModule` buatan sendiri |
| Objek | Prefab objek template (`MobileARTemplateAssets/Prefabs`) menggantikan kursi buatan sendiri |
| Interaksi | Tap untuk menempatkan, drag 1 jari untuk memindah, cubit/putar 2 jari untuk skala & rotasi (gestur `TouchscreenGestureInputController`) |
| Skrip sendiri | `ARPlacementManager`, `ARPlaneController`, `ARUIController` tetap ada di repo, **tidak dipakai scene final** (belum dihapus agar riwayat implementasi terjaga) |
| Build ulang | `BuildApkBatch` ke-3: **Succeeded**, `error 0`, durasi 00:04:55 |

Verifikasi di perangkat (Samsung Galaxy A54, Android 16):

* APK terpasang ulang (`adb install -r`, `lastUpdateTime 2026-10-03 15:52:42`).
* Aplikasi menjadi activity terdepan, proses berjalan, **tidak ada exception Unity** di logcat.
* Screenshot `Screenshots/05_template_ui.png` menunjukkan coaching prompt
  *Tap to Place*, object menu (cube/pyramid/torus/wedge), tombol `Cancel` dan `⋯`.
* Screenshot sebelumnya: `01_status_device.png` (versi UI sendiri, status
  `AR: SessionTracking | Plane: 0`), `02_layout_fix.png` (perbaikan label tombol
  & baris tombol yang tadinya kepanjangan).

Temuan perbaikan yang dilakukan pada versi UI sendiri sebelum beralih ke template:

1. Glyph panah `◀▶` tidak tersedia di font bawaan TMP → tampil kotak; diganti label `Kiri`/`Kanan`.
2. Baris tombol selebar ±1082 px melebihi lebar kanvas (±978 px) sehingga tombol
   `Grid` terpotong → lebar minimum diperkecil + `flexibleWidth = 1`.
3. Tombol tidak bereaksi di perangkat (belum selesai didiagnosis) → atas permintaan,
   seluruh UI diganti dengan UI template yang memakai `XRUIInputModule`.

---

## 5. Batasan & Pekerjaan Berikutnya

1. **Pengujian terbatas** — jalankan 5 skenario di
   `Panduan_Pengujian.md`, isi tabel hasil, ambil ≥10 screenshot + video 1–2 menit
   sesuai `Checklist_Screenshot_dan_Video.md`.
2. Belum ada bayangan pantulan objek ke permukaan nyata (AR shadow receiver).
3. UI template mengizinkan **multi-objek** (berbeda dengan ketentuan awal anti-duplikasi).
4. Build iOS belum dikonfigurasi (ARKit terpasang, belum disiapkan build-nya).
5. Untuk distribusi perlu keystore rilis (saat ini debug).
6. Teks UI template berbahasa Inggris; skrip buatan sendiri tidak aktif di scene final.

## 6. Cara Mengulang Semua dari Nol

```powershell
# 1) (Opsional) buat ulang scene/prefab/material
& "D:\Unity\6000.3.24f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Unity\RVA 1" `
  -executeMethod SimpleARPlacement.EditorTools.SimpleARPlacementTool.CreateSceneAndPrefabsBatch `
  -logFile "D:\Unity\RVA 1\Logs\setup_scene.log"

# 2) validasi konfigurasi
& "D:\Unity\6000.3.24f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Unity\RVA 1" `
  -executeMethod SimpleARPlacement.EditorTools.SimpleARPlacementTool.ValidateSetupBatch `
  -logFile "D:\Unity\RVA 1\Logs\validate.log"

# 3) build APK
& "D:\Unity\6000.3.24f1\Editor\Unity.exe" -batchmode -nographics -projectPath "D:\Unity\RVA 1" `
  -buildTarget Android `
  -executeMethod SimpleARPlacement.EditorTools.SimpleARPlacementTool.BuildApkBatch `
  -logFile "D:\Unity\RVA 1\Logs\build_apk.log"
```

Atau lewat menu editor: `Tools > SimpleARPlacement > 1/2/3`.
