# Panduan Pengujian — SimpleARPlacement

Dokumen ini berisi **rancangan pengujian eksperimental** untuk 5 skenario pada
perangkat Android yang mendukung ARCore.

Aturan pelaporan:

* **Jangan mengisi kolom hasil sebelum benar-benar diuji pada perangkat fisik.**
* Hasil yang sudah diamati ditulis pada kolom *Hasil Observasi*; perkiraan teknis
  ditulis terpisah pada kolom *Analisis Teknis* dan wajib diberi label “(analisis)”.
* Kategori yang dipakai:
  * **Stabilitas tracking:** Baik / Sedang / Buruk
  * **Perilaku objek:** Stabil / Bergoyang / Tidak menempel / Hilang
  * **Penyebab & solusi:** berdasarkan observasi nyata, atau analisis teknis yang
    dinyatakan secara jelas.

## Prasyarat pengujian

| Item | Nilai |
|---|---|
| Versi APK / build | _(isi: tanggal/hash build)_ |
| Perangkat | _(merek & tipe, Android versi, dukungan ARCore)_ |
| Kondisi ruangan | _(suhu cahaya, jenis permukaan)_ |
| Aplikasi pendukung | adb, screencast opsional |

---

## Skenario 1 — Permukaan bertekstur

| Aspek | Detail |
|---|---|
| **Skenario** | Lantai atau meja dengan pola, detail, dan tekstur yang jelas (keramik bermotif, karpet, kayu berpola) |
| **Hal yang diamati** | Stabilitas tracking, akurasi posisi objek, kemudahan deteksi plane |
| **Langkah** | 1) Buka aplikasi, beri izin kamera. 2) Arahkan ke permukaan bertekstur, gerakkan perlahan. 3) Catat waktu sampai grid muncul. 4) Ketuk untuk menempatkan kursi. 5) Amati 30 detik, lalu gerakkan kamera mendekat & menjauh. |

| No | Metrik | Target | Hasil Observasi | Analisis Teknis |
|---|---|---|---|---|
| 1.1 | Waktu deteksi plane pertama | < 5 dtk | _(isi setelah uji)_ | _(analisis)_ |
| 1.2 | Stabilitas tracking | Baik | _(isi)_ | _(analisis)_ |
| 1.3 | Perilaku objek setelah ditempatkan | Stabil | _(isi)_ | _(analisis)_ |
| 1.4 | Akurasi posisi (objek tepat di titik ketukan) | Tepat | _(isi)_ | _(analisis)_ |
| 1.5 | Jumlah plane terdeteksi vs permukaan nyata | Sesuai | _(isi)_ | _(analisis)_ |

**Kemungkinan penyebab masalah & solusi**

| Gejala | Penyebab umum | Solusi/mitigasi |
|---|---|---|
| Plane lambat muncul | Pergerakan kamera terlalu cepat / permukaan kurang fitur | Gerakkan perlahan, ubah sudut |
| Objek meleset dari titik ketukan | Kalibrasi sentuh/pergeseran UI | Pastikan skala UI sesuai resolusi, uji ulang |
| Objek bergoyang | Tracking kualitas sedang | Perbanyak fitur visual, kurangi gerakan tangan |

---

## Skenario 2 — Permukaan polos

| Aspek | Detail |
|---|---|
| **Skenario** | Permukaan dengan sedikit tekstur/pola (meja putih polos, lantai putih rata) |
| **Hal yang diamati** | Waktu deteksi, kestabilan plane, kemungkinan objek bergoyang |
| **Langkah** | Sama seperti skenario 1, pada permukaan polos. |

| No | Metrik | Target | Hasil Observasi | Analisis Teknis |
|---|---|---|---|---|
| 2.1 | Waktu deteksi plane pertama | _(catat, diperkirakan lebih lama)_ | _(isi)_ | _(analisis)_ |
| 2.2 | Stabilitas tracking | Baik/Sedang | _(isi)_ | _(analisis)_ |
| 2.3 | Perilaku objek | Stabil/Bergoyang | _(isi)_ | _(analisis)_ |
| 2.4 | Plane bertahan setelah berpindah ruang pandang | Ya | _(isi)_ | _(analisis)_ |

**Catatan teknis (analisis, bukan hasil uji):** ARCore membutuhkan fitur visual untuk
estimasi pose; permukaan polos menurunkan kualitas estimasi sehingga deteksi bisa lebih
lama dan tracking lebih mudah berubah menjadi *Limited*.

---

## Skenario 3 — Cahaya terang

| Aspek | Detail |
|---|---|
| **Skenario** | Lingkungan dengan pencahayaan cukup terang (siang hari dekat jendela / lampu terang) |
| **Hal yang diamati** | Stabilitas tracking, visibilitas kamera, akurasi objek |
| **Langkah** | Uji di area terang; pastikan tidak ada flare/over-exposure ke kamera. |

| No | Metrik | Target | Hasil Observasi | Analisis Teknis |
|---|---|---|---|---|
| 3.1 | Stabilitas tracking | Baik | _(isi)_ | _(analisis)_ |
| 3.2 | Keterbacaan teks UI | Terbaca | _(isi)_ | _(analisis)_ |
| 3.3 | Visibilitas grid plane vs kamera terang | Terlihat | _(isi)_ | _(analisis)_ |
| 3.4 | Akurasi posisi objek | Tepat | _(isi)_ | _(analisis)_ |

---

## Skenario 4 — Cahaya redup

| Aspek | Detail |
|---|---|
| **Skenario** | Pencahayaan rendah (malam, lampu redup) |
| **Hal yang diamati** | Penurunan tracking, kegagalan deteksi plane, objek tidak stabil |
| **Langkah** | Uji pada malam hari dengan satu lampu redup; catat pesan status AR. |

| No | Metrik | Target | Hasil Observasi | Analisis Teknis |
|---|---|---|---|---|
| 4.1 | Berhasil deteksi plane? | Ya/Tidak | _(isi)_ | _(analisis) ARCore melaporkan `InsufficientLight` bila cahaya kurang; UI menampilkan alasan tracking terganggu_ |
| 4.2 | Stabilitas tracking | Sedang/Buruk | _(isi)_ | _(analisis)_ |
| 4.3 | Perilaku objek | Stabil/Bergoyang/Tidak menempel | _(isi)_ | _(analisis)_ |
| 4.4 | Pesan yang tampil pada status | _(catat persis)_ | _(isi)_ | _(analisis)_ |

**Mitigasi yang tersedia di aplikasi:** pesan “Tracking terganggu (&lt;alasan&gt;)”
muncul otomatis dari `ARSession.notTrackingReason` dan instruksi meminta pengguna
bergerak ke permukaan bertekstur.

---

## Skenario 5 — Gerakan kamera cepat

| Aspek | Detail |
|---|---|
| **Skenario** | Kamera digerakkan lebih cepat dari gerakan normal (sapuan cepat kiri–kanan, putaran cepat) |
| **Hal yang diamati** | Kehilangan tracking, pergeseran objek, waktu pemulihan tracking |
| **Langkah** | 1) Tempatkan objek. 2) Sapu kamera cepat 5 detik. 3) Hentikan di tempat semula. 4) Catat apakah objek berpindah dan berapa lama pulih. |

| No | Metrik | Target | Hasil Observasi | Analisis Teknis |
|---|---|---|---|---|
| 5.1 | Tracking hilang saat sapuan cepat? | — | _(isi)_ | _(analisis)_ |
| 5.2 | Objek bergeser dari posisi semula | Minimal | _(isi)_ | _(analisis)_ objek menempel ARAnchor, seharusnya mengikuti koreksi pose |
| 5.3 | Waktu pemulihan tracking | < 2 dtk | _(isi)_ | _(analisis)_ |
| 5.4 | Setelah pulih, apakah teks instruksi kembali normal | Ya | _(isi)_ | _(analisis)_ |

---

## Ringkasan hasil (diisi setelah seluruh skenario diuji)

| No | Skenario | Stabilitas tracking | Perilaku objek | Penyebab & solusi |
|---|---|---|---|---|
| 1 | Permukaan bertekstur | _(isi)_ | _(isi)_ | _(isi)_ |
| 2 | Permukaan polos | _(isi)_ | _(isi)_ | _(isi)_ |
| 3 | Cahaya terang | _(isi)_ | _(isi)_ | _(isi)_ |
| 4 | Cahaya redup | _(isi)_ | _(isi)_ | _(isi)_ |
| 5 | Gerakan kamera cepat | _(isi)_ | _(isi)_ | _(isi)_ |

> Tabel di atas sengaja dibiarkan kosong sampai pengujian fisik benar-benar
> dilakukan, agar tidak ada data uji yang dikarang.
