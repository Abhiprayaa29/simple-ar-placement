# Checklist Screenshot & Video — SimpleARPlacement

Dokumen ini daftar periksa bukti visual untuk laporan. Semua item **diambil di
perangkat Android fisik** (kecuali disebut editor).

## A. Screenshot (PNG)

Simpan ke `Docs/SimpleARPlacement/Screenshots/` dengan penamaan `01_nama.png`, dst.

| No | Kondisi | Konten yang harus terlihat | File | Status |
|---|---|---|---|---|
| 01 | Perangkat tidak mendukung ARCore / izin ditolak | Pesan kesalahan di layar | _(isi)_ | ☐ |
| 02 | Menunggu izin kamera | Dialog izin Android | _(isi)_ | ☐ |
| 03 | Belum ada permukaan | Teks “Arahkan kamera ke lantai atau meja…” | _(isi)_ | ☐ |
| 04 | Plane terdeteksi | Visualisasi grid permukaan muncul | _(isi)_ | ☐ |
| 05 | Sebelum ketuk | Indikator penempatan di titik raycast + instruksi | _(isi)_ | ☐ |
| 06 | Setelah ketuk | Objek kursi ditempatkan + toast konfirmasi | _(isi)_ | ☐ |
| 07 | Objek dipindah | Kursi pindah ke titik permukaan baru (tap kedua) | _(isi)_ | ☐ |
| 08 | Rotasi kiri/kanan | Orientasi kursi berubah 30° | _(isi)_ | ☐ |
| 09 | Skala − / + | Ukuran kursi mengecil/membesar | _(isi)_ | ☐ |
| 10 | Tombol Grid disembunyikan | Plane hilang, objek tetap ada | _(isi)_ | ☐ |
| 11 | Tombol Reset | Objek hilang, status kembali instruksi penempatan | _(isi)_ | ☐ |
| 12 | Skenario cahaya redup | Pesan status/tracking | _(isi)_ | ☐ |
| 13 | Editor: scene terbuka | Hierarchy + objek scene (opsional) | _(isi)_ | ☐ |
| 14 | Editor: menu Tools > SimpleARPlacement | Tiga menu terlihat (opsional) | _(isi)_ | ☐ |

Minimal **10 screenshot** wajib diambil; tandai kolom Status dengan ✓.

## B. Video (1–2 menit)

| No | Isi video | Durasi | Berkas | Status |
|---|---|---|---|---|
| V1 | Launch aplikasi → izin kamera → deteksi plane → penempatan kursi → rotasi → skala → reset | 60–120 dtk | _(isi)_ | ☐ |

Rekam layar perangkat (bukan layar monitor) dengan audio dari lingkungan.
Format: MP4 (H.264). Jangan menutupi area instruksi/teks status saat merekam.

## C. Perangkat uji

| Item | Nilai |
|---|---|
| Merek/tipe | _(isi)_ |
| Versi Android | _(isi)_ |
| Versi aplikasi/`versionName` | 1.0.0 (`com.simplearplacement.app`) |
| Tanggal pengujian | _(isi)_ |
| Pengujer | _(isi)_ |
