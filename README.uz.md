# 🍽️ CafeBot - Kafe boshqaruv uchun Telegram Bot

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue.svg)](https://www.postgresql.org/)
[![Telegram Bot API](https://img.shields.io/badge/Telegram%20Bot%20API-7.0+-blue.svg)](https://core.telegram.org/bots/api)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

CafeBot - bu buyurtmalarni qabul qilish, menyu boshqaruv, xodimlar boshqaruv va tahlil uchun kafe boshqaruv jarayonlarini avtomatlashtirish uchun zamonaviy Telegram bot.

## ✨ Imkoniyatlar

### 👥 Xodimlar boshqaruv
- Xodimlarni ro'yxatdan o'tkazish va autentifikatsiya
- Kirish huquqlarini taqsimlash (Admin/Ofitsiant rollari)
- Xodimlar faolligini kuzatish
- Xodimlar ish statistikasi

### 📋 Buyurtmalar boshqaruv
- Onlayn stol bron qilish
- Buyurtmalarni yaratish va boshqarish
- Buyurtma holatini kuzatish
- Buyurtma elementlarini boshqarish
- To'lovlarni qabul qilish

### 🍽️ Menyu boshqaruv
- Mahsulot kategoriyalari
- Menyu pozitsiyalarini qo'shish/tahrirlash/o'chirish
- Narx va tavsiflarni boshqarish
- Taomlar fotosuratlari
- Mahsulot mavjudligini boshqarish

### 🏠 Xonalar boshqaruv
- Xonalar va stollarni ro'yxatdan o'tkazish
- Sig'im boshqaruv
- Xona bron qilish
- Bandlikni kuzatish

### 📊 Tahlil va hisobotlar
- Savdo statistikasi
- Xodimlar bo'yicha tahlil
- Mashhur taomlar hisoboti
- Moliyaviy hisobotlar

## 🏗️ Arxitektura

Loyiha **Clean Architecture** tamoyillari asosida qurilgan va quyidagi qatlamlarga bo'lingan:

```
CafeBot/
├── Core/                    # Biznes logika va domen entitylar
│   ├── Entities/           # Domen modellar
│   ├── Enums/             # Sanashlar
│   └── Interfaces/        # Repository shartnomalar
├── Application/           # Ilova servislar
│   └── Services/         # Biznes logika servislar
├── Infrastructure/        # Tashqi bog'liqliklar
│   ├── Data/             # DB konfiguratsiya va kontekst
│   ├── Repositories/     # Repository realisatsiyalar
│   └── Migrations/       # DB migratsiyalar
├── TelegramBot/          # Telegram Bot interfeys
│   ├── Bot/             # Telegram bilan o'zaro ta'sir logikasi
│   ├── Handlers/        # Buyruq va callback handlerlar
│   ├── Keyboards/       # Bot klaviaturalar
│   └── States/          # Foydalanuvchi holatini boshqarish
└── Tests/               # Unit va integratsion testlar
```

## 🛠️ Texnologiyalar

- **Backend**: .NET 8.0, C# 12
- **Database**: PostgreSQL 15+
- **ORM**: Entity Framework Core 8.0
- **Bot Framework**: Telegram.Bot 19.0+
- **Testing**: xUnit, FluentAssertions, Moq
- **Logging**: Microsoft.Extensions.Logging
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection

## 📋 Oldindan talablar

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- Telegram Bot akkaunti (token olish uchun [@BotFather](https://t.me/botfather))
- [Docker](https://www.docker.com/) (ixtiyoriy, konteynerlashtirish uchun)

## 🔒 Xavfsizlik

### ⚠️ Muhim!

**Repositoryga hech qachon commit qilmang:**
- Haqiqiy Telegram bot tokenlar
- Ma'lumotlar bazasi parollar
- API kalitlar
- Shaxsiy kalitlar

### Gitdan chiqarilgan fayllar:
- `appsettings.json` (mahalliy sozlamalar mavjud)
- `.env` (muhit o'zgaruvchilari)
- Kengaytmalar bilan fayllar: `.key`, `.pem`, `.p12`, `.pfx`

### Muhit o'zgaruvchilaridan foydalanish:
```bash
# Namuna nusxalash
cp env.example .env

# Xavfsizlik ma'lumotlari bilan tahrirlash
nano .env
```

## 🚀 O'rnatish va ishga tushirish

### 1. Repositoryni klonlash

```bash
git clone https://github.com/yourusername/cafebot.git
cd cafebot
```

### 2. Ma'lumotlar bazasini sozlash

```bash
# PostgreSQL ma'lumotlar bazasini yaratish
createdb cafebot

# Foydalanuvchini yaratish (ixtiyoriy)
createuser cafebotuser --pwprompt --encrypted
# Parol: CafeBot2025!Strong

# Huquqlarni berish
GRANT ALL PRIVILEGES ON DATABASE cafebot TO cafebotuser;
```

### 3. Ilova konfiguratsiyasi

⚠️ **REPOSITORYGA HECH QACHON HAQIQIY TOKEN VA PAROLLARNI COMMIT QILMANG!**

#### Mahalliy ishlab chiqish uchun:
Loyihaning ildizida `.env` faylini yarating:

```bash
# Namuna nusxalash
cp env.example .env

# O'z ma'lumotlaringiz bilan tahrirlash
nano .env
```

`.env` mazmuni:
```env
# Telegramdagi @BotFather'dan olish
TELEGRAM_BOT_TOKEN=bu_yerdagi_bot_tokeningiz

# PostgreSQL sozlamalar
DB_HOST=localhost
DB_PORT=5432
DB_NAME=cafebot
DB_USER=cafebotuser
DB_PASSWORD=sizning_xavfsizlik_parolingiz
```

#### Production uchun:
Deployment platform (Railway, Render, va boshqalar) muhit o'zgaruvchilaridan foydalaning:

```env
TELEGRAM_BOT_TOKEN=sizning_bot_tokeningiz
ConnectionStrings__DefaultConnection=postgresql://user:pass@host:port/db
```

### 4. Ilovani ishga tushirish

#### Variant 1: Mahalliy ishga tushirish
```bash
# CafeBot.TelegramBot direktoriyasidan
cd CafeBot.TelegramBot
dotnet run
```

#### Variant 2: Makefile yordamida
```bash
# Tez sozlash
make setup

# Ishga tushirish
make run
```

#### Variant 3: Docker
```bash
# Qurish va ishga tushirish
docker-compose up --build

# Yoki Makefile yordamida
make docker-run
```

### 5. Ma'lumotlarni ishga tushirish

Birinchi marta ishga tushirishda ilova avtomatik ravishda DB strukturasini yaratadi va test ma'lumotlar bilan to'ldiradi.

## 🤖 Ishlatish

### Bot buyruqlari

| Buyruq | Tavsif |
|--------|---------|
| `/start` | Botni ishga tushirish va foydalanuvchini ro'yxatdan o'tkazish |
| `/help` | Buyruqlar yordami |
| `🔧 Admin paneli` | Administrativ funksiyalarga kirish |

### Admin paneli

#### Xodimlarni boshqarish
- Yangi xodimlarni qo'shish
- Xodimlar ro'yxatini ko'rish
- Rollarni va holatni boshqarish
- Ish statistikasi

#### Mahsulotlarni boshqarish
- Menyu pozitsiyalarini qo'shish/tahrirlash
- Kategoriyalarni boshqarish
- Narx va tavsiflarni o'rnatish
- Mahsulot mavjudligini boshqarish

#### Xonalarni boshqarish
- Yangi xonalarni ro'yxatdan o'tkazish
- Sig'imni sozlash
- Xona holatini boshqarish

#### Buyurtmalarni boshqarish
- Faol buyurtmalarni ko'rish
- Buyurtma holatini o'zgartirish
- Buyurtma elementlarini boshqarish
- To'lovlarni qayta ishlash

## 🧪 Testlash

### Unit testlarni ishga tushirish

```bash
# Loyihaning ildiz direktoriyasidan
dotnet test CafeBot.Tests

# Yoki Makefile yordamida
make test
```

### Kod qamrov bilan ishga tushirish

```bash
dotnet test CafeBot.Tests --collect:"XPlat Code Coverage"
```

### Testlar strukturas

```
Tests/
├── Services/           # Biznes logika testlar
├── Handlers/          # Telegram handlerlar testlar
├── Infrastructure/    # Repository testlar
└── Integration/       # Integratsion testlar
```

## 📊 Monitoring

### Loglar

Ilova barcha operatsiyalar bo'yicha batafsil log yozadi:
- Foydalanuvchi harakatlari haqida axborot xabarlar
- Xato va istisnolar
- SQL so'rovlari (ishlab chiqish rejimida)

### Metrikalar

Kelajakda qo'shish rejalashtirilgan:
- Faol buyurtmalar soni
- Bot javob vaqti
- Funksiyalardan foydalanish statistikasi

## 🔧 Ishlab chiqish

### Kod qoidalari

- Klass va metod nomlari uchun PascalCase dan foydalaning
- Parametrlar va mahalliy o'zgaruvchilar uchun camelCase
- Ochik API uchun batafsil XML izohlar
- Maksimal qator uzunligi: 120 ta belgidan iborat

### Yangi funksiyalar qo'shish

1. Issue tracker'da vazifa yaratish
2. Yangi funksiyalar uchun testlar yozish
3. Funksiyani amalga oshirish
4. Barcha testlar o'tishiga ishonch hosil qilish
5. Dokumentatsiyani yangilash

### Ma'lumotlar bazasi bilan ishlash

```bash
# Yangi migratsiya yaratish
dotnet ef migrations add MigrationName -p CafeBot.Infrastructure -s CafeBot.TelegramBot

# Migratsiyalarni qo'llash
dotnet ef database update -p CafeBot.Infrastructure -s CafeBot.TelegramBot
```

## 🐳 Docker

```bash
# Image qurish
docker build -t cafebot .

# PostgreSQL bilan ishga tushirish
docker-compose up
```

## 🤝 Contributing

1. Repositoryni fork qilish
2. Feature branch yaratish (`git checkout -b feature/AmazingFeature`)
3. O'zgarishlaringizni commit qilish (`git commit -m 'Add some AmazingFeature'`)
4. Branch'ga push qilish (`git push origin feature/AmazingFeature`)
5. Pull Request ochish

### PR talablari
- Barcha testlar o'tishi kerak
- Kod testlar bilan qamrab olishi kerak
- Loyihaning code style'iga rioya qilish
- Kerak bo'lganda dokumentatsiyani yangilash

## 📝 Litsenziya

Ushbu loyih MIT litsenziyasi ostida tarqatiladi. Batafsil ma'lumot uchun [LICENSE](LICENSE) faylini ko'ring.

## 👥 Mualliflar

- **Xush** - *Asosiy ishlab chiquvchi* - [GitHub](https://github.com/xush)

## 🙏 Minnatdorchilik

- Ajoyib platforma uchun Telegram Bot API
- Ajoyib vositalar uchun .NET community
- Loyihaning barcha contributorlariga

## 📞 Qo'llab-quvvatlash

Savollaringiz yoki muammolaringiz bo'lsa:
1. [Issues](https://github.com/yourusername/cafebot/issues) ni tekshiring
2. Muammoni batafsil tavsiflab yangi issue yarating
3. Ishlab chiquvchi bilan bog'laning

## 📚 Qo'shimcha dokumentatsiya

- [Contributing Guide](CONTRIBUTING.md) - Loyihaga qanday hissa qo'shish
- [Changelog](CHANGELOG.md) - O'zgarishlar tarixi
- [Makefile](Makefile) - Ishlab chiqish buyruqlari
- [Docker Setup](docker-compose.yml) - Docker konfiguratsiyasi

## 🏗️ Loyihaning arxitekturasi

```
CafeBot/
├── Core/                    # Biznes logika va domen entitylar
│   ├── Entities/           # Domen modellar
│   ├── Enums/             # Sanashlar
│   └── Interfaces/        # Repository shartnomalar
├── Application/           # Ilova servislar
│   └── Services/         # Biznes logika servislar
├── Infrastructure/        # Tashqi bog'liqliklar
│   ├── Data/             # DB konfiguratsiya va kontekst
│   ├── Repositories/     # Repository realisatsiyalar
│   └── Migrations/       # DB migratsiyalar
├── TelegramBot/          # Telegram Bot interfeys
│   ├── Bot/             # Telegram bilan o'zaro ta'sir logikasi
│   ├── Handlers/        # Buyruq va callback handlerlar
│   ├── Keyboards/       # Bot klaviaturalar
│   └── States/          # Foydalanuvchi holatini boshqarish
└── Tests/               # Unit va integratsion testlar
```

---

⭐ Agar loyiha sizga foydali bo'lsa, GitHub'da yulduzcha qo'ying!
