# Pokémon Adventure RPG — Beta 1.1

Game console C# untuk proyek belajar dan kerja sama dengan teman. Pemain memilih starter, menjelajah, bertarung, menangkap Pokémon, dan menantang Gym pertama.

## Menjalankan game

Butuh **.NET SDK 10** untuk menjalankan proyek dari source:

```powershell
dotnet run
```

Audio memakai fitur bawaan **Windows**, tanpa NAudio atau paket audio tambahan. Pada sistem selain Windows, game tetap berjalan tanpa suara.

## Yang ditambahkan pada Beta 1.1

### Audio dan pengaturan suara

- Memakai 13 file WAV di folder `Audio`; otomatis disalin saat build dan publish.
- Efek saat bertemu Pokémon liar, menyerang, mengeluarkan Pokémon, menangkap, healing, menemukan/menerima item, naik level, evolusi, kabur, menyimpan, dan menang.
- Musik battle berulang dengan volume lebih rendah dan tetap berjalan saat efek serangan atau efek Pokémon berbunyi. Musik berhenti saat battle selesai.
- Suara exclamation di luar battle lebih pendek dan lembut. Menu langsung merespons, termasuk saat efek belum selesai. Pilihan battle, input nama, input salah, dan Enter untuk lanjut tidak memicu suara menu.
- Tombol **Sound ON/OFF** pada menu judul dan menu utama. Pengaturan pemain ikut disimpan melalui Save Game.
- File audio hilang/rusak atau pemutar gagal tidak menghentikan game.

### Pokémon dan evolusi

Jumlah spesies naik dari **5 menjadi 20**, dengan **17 move**:

- Bulbasaur, Ivysaur, Venusaur.
- Charmander, Charmeleon, Charizard.
- Squirtle, Wartortle, Blastoise.
- Pidgey dan Rattata.
- Caterpie, Metapod, Butterfree.
- Weedle, Kakuna, Beedrill.
- Pikachu, Geodude, dan Onix.

Evolusi otomatis setelah naik level:

| Pokémon | Evolusi pertama | Evolusi terakhir |
|---|---|---|
| Bulbasaur | Ivysaur, level 16 | Venusaur, level 32 |
| Charmander | Charmeleon, level 16 | Charizard, level 36 |
| Squirtle | Wartortle, level 16 | Blastoise, level 36 |
| Caterpie | Metapod, level 7 | Butterfree, level 10 |
| Weedle | Kakuna, level 7 | Beedrill, level 10 |

- Tambahan tipe Electric, Bug, Rock, dan Ground beserta efektivitas serangannya.
- Move tambahan: Razor Leaf, Flamethrower, Bubble Beam, Bug Bite, Pin Missile, Thunder Shock, Thunderbolt, Rock Throw, Mud Slap, dan Dig.
- Batas level 100 dan maksimum empat move. Jika penuh, move baru menggantikan move tertua.
- Evolusi ikut memperbarui Pokédex.
- Starter dari save lama yang sudah melewati syarat evolusi akan berevolusi ketika naik level berikutnya.

### Area, Gym, dan item

- **Route 1:** Pidgey dan Rattata, level 2–5.
- Kemenangan melawan Pokémon liar di Route 1 memberi bonus 15 EXP.
- **Pewter City Gym:** Brock memakai Geodude level 6 dan Onix level 7.
- Charmander belajar Mud Slap saat naik ke level 6 agar punya jurus Ground melawan Brock. Bulbasaur dan Squirtle mendapat jurus Grass atau Water di level 7.
- Menang atas Brock memberi Boulder Badge, 5 Poké Ball, dan 2 Potion. Hadiah hanya bisa diterima sekali.
- **Route 2** terbuka setelah mendapatkan Boulder Badge: Caterpie, Weedle, Pidgey, dan Pikachu, level 5–9.
- Setiap eksplorasi memiliki peluang 65% battle, 25% menemukan item, dan 10% tidak menemukan apa pun.
- Item temuan berupa 1–2 Poké Ball atau Potion.
- Pokémon milik trainer tidak bisa ditangkap; pemain tidak bisa kabur dari Gym.

### Party dan storage

- Deposit Pokémon dari party ke storage.
- Withdraw Pokémon dari storage ke party.
- Exchange Pokémon party dengan Pokémon storage, termasuk saat party penuh.
- Mengubah urutan party dengan menukar dua slot.
- Maksimum enam Pokémon di party; minimal satu harus tetap berada di party.
- HP, EXP, level, dan move tidak direset saat dipindahkan.
- Pokémon sehat pertama dalam urutan party menjadi pemimpin battle.
- Pokémon hasil tangkapan masuk storage otomatis jika party penuh.

### Perbaikan battle dan tampilan

- Layar dibersihkan sebelum setiap pesan serangan, termasuk serangan meleset.
- Enter untuk melanjutkan setiap pesan serangan agar mudah dibaca.
- Quick Attack mendahului jurus biasa. Jika prioritas sama, Speed menentukan giliran. Speed sama ditentukan acak.
- Growl menurunkan Attack lawan tanpa mengurangi HP. Efek bisa bertumpuk sampai empat kali dan hilang saat Pokémon yang terkena diganti atau battle selesai.
- Membatalkan pergantian Pokémon atau memilih Pokémon yang sama/pingsan tidak menghabiskan giliran.
- Potion tidak terpakai jika HP sudah penuh.
- EXP diberikan kepada Pokémon aktif yang mengalahkan lawan, termasuk setelah pergantian Pokémon.
- Kekebalan tipe menyebabkan nol damage.

### Save dan keamanan progres

- Save Beta 1.0 yang valid tetap dapat dimuat. Field baru mendapat nilai awal otomatis.
- Save ditulis ke file sementara sebelum mengganti `save.json`.
- Mulai penyimpanan kedua, save valid sebelumnya disimpan sebagai `save.json.bak`.
- Jika save utama rusak atau hilang, game mencoba memuat cadangan. Save kembali untuk memulihkan file utama.
- Save rusak tidak menimpa cadangan yang valid.
- Data Pokémon, party, item, dan Pokédex diperiksa saat load.
- Gagal menulis save menampilkan pesan dan mempertahankan progres dalam memori.
- Kembali ke judul dengan progres belum disimpan memberi pilihan simpan, buang perubahan, atau tetap bermain.
- Menutup paksa terminal tidak menyimpan otomatis.
- `.gitignore` mengecualikan hasil build, save pribadi, dan file konfigurasi rahasia lokal.

## Menu utama

1. Explore: pilih Route 1 atau Route 2.
2. Party: lihat Pokémon, ubah urutan, atau buka storage.
3. Pokédex.
4. Bag.
5. Pokémon Center: menyembuhkan seluruh party.
6. Save Game.
7. Return to Title.
8. Pewter City Gym.
9. Pokémon Storage.
10. Sound ON/OFF.

Save berada di folder tempat game dijalankan. Game memakai satu slot save dan satu cadangan. Save dari New Game akan mengganti slot tersebut; save valid sebelumnya menjadi cadangan.

## Fitur dasar dari Beta 1.0

- Membuat trainer dan memilih satu dari tiga starter.
- Battle bergiliran, akurasi, critical hit, EXP, dan pembelajaran move.
- Poké Ball, Potion, healing, Pokédex, Bag, serta save/load JSON.

## Batasan Beta 1.1

- Baru satu Gym dan dua route; cerita sampai Champion belum tersedia.
- Geodude dan Onix muncul sebagai lawan Gym, belum sebagai encounter liar.
- Pokémon memakai satu tipe. Stats dan beberapa move disederhanakan untuk proyek belajar.
- Status seperti tidur dan racun, PP, serta dual type belum tersedia.
- Pidgey, Rattata, Pikachu, Geodude, dan Onix belum memiliki evolusi dalam beta ini.
- Admin console belum termasuk dalam versi ini.

## Struktur proyek

- `Program.cs`: titik mulai program.
- `Data`: daftar spesies, move, encounter, dan tim Brock.
- `Models`: data pemain dan Pokémon.
- `Game`: menu, input, serta tampilan console.
- `Systems`: battle, audio, eksplorasi, party/storage, dan save.
- `Audio`: 13 aset WAV.
- `Tests`: pemeriksaan otomatis tanpa paket pengujian tambahan.

## Pemeriksaan untuk pengembangan

```powershell
dotnet build
dotnet run --project Tests/PokemonAdventure.Checks.csproj
```

Pemeriksaan meliputi evolusi, batas party, perpindahan Pokémon, battle, hadiah Gym, save lama, pemulihan cadangan, input menu, dan validasi WAV. Data uji memakai folder sementara agar tidak mengubah save pemain.

Untuk menguji penerimaan permintaan suara oleh Windows (akan mengeluarkan suara):

```powershell
dotnet run --project Tests/PokemonAdventure.Checks.csproj -- --audio
```

Hasil pemeriksaan audio menunjukkan apakah Windows menerima permintaan playback; keluaran speaker tetap perlu didengarkan langsung.
