# Keep Todo — .NET 10 + React 19 + Oracle

Google Keep benzeri, çok kullanıcılı, LLM destekli bir not/todo uygulaması.
Notlar Oracle'da (EF Core) saklanır, kimlik doğrulama ASP.NET Core Identity + JWT
(ve Google/Microsoft/GitHub ile sosyal giriş) ile yapılır, ayarlar DB-backed bir
`AppSettings` tablosundan (appsettings.json değil) okunur ve her gün Quartz.NET
ile o günün notlarını özetleyen bir LLM job'ı çalışır.

## Klasör yapısı

```
keep-todo-app/
  backend/
    KeepApi/                  ASP.NET Core Web API (giriş projesi)
      Controllers/            NotesController, AuthController, DailySummaryController
      Jobs/DailySummaryJob.cs Quartz job — günlük özet notu üretir/günceller
      Services/               NoteService, DailySummaryService, AttachmentSummaryService, FileSignatureValidator
      Middleware/              ExceptionMiddleware, LoggingMiddleware
      Program.cs
    KeepApi.Data/              EF Core DbContext, Entity'ler, Configuration'lar, Migrations, Seeder'lar
    KeepApi.Application/       Interface'ler ve Request/Response/DTO modelleri
    KeepApi.Infrastructure/    JWT, external OAuth, SMTP, LLM client'ları (Gemini/OpenAI/Ollama/Groq)
    KeepApi.Common/            Ortak modeller (ApiResponse, BaseEntity) ve AppSettings şifreleme
    KeepApi.Tests/             xUnit/NUnit testleri
    Dockerfile
  frontend/                    React 19 + Vite 6
    src/
      App.jsx
      api.js                  backend istekleri (base URL: http://localhost:5080)
      i18n.js, locales/tr, locales/en   react-i18next ile TR/EN
      components/
        Composer.jsx          hızlı not ekleme
        NoteCard.jsx           not kartı (checklist, resim, hatırlatma)
        SearchPanel.jsx        arama
        Trash.jsx / TrashCard.jsx  çöp kutusu
        Login.jsx / Register.jsx / AuthLayout.jsx
        SocialLoginButtons.jsx / OAuthCallBack.jsx  Google/Microsoft/GitHub girişi
        LanguageSwitcher.jsx    TR/EN geçişi
        useReminders.jsx        hatırlatma bildirimleri
      styles.css
    Dockerfile, nginx.conf
  docker-compose.yml            oracle-db + redis + backend + frontend
  .env.example                  ORACLE_PASSWORD
```

## Mimari notları

- **Ayarlar DB'de tutulur.** `appsettings.json` yalnızca Serilog ayarlarını ve
  bootstrap için gereken Oracle bağlantı bilgisini içerir; geri kalan her şey
  (`Llm:*`, `Jwt:*`, `ExternalProviders:*`, `Smtp:*`, `Redis:ConnectionString`,
  `Cors:AllowedOrigin` ...) `APP_SETTINGS` tablosundan `DbSettingsConfigurationSource`
  ile okunur. Hassas olanlar (`IsEncrypted = true`) Data Protection API ile
  şifrelenir/çözülür. İlk çalıştırmada `AppSettingsSeeder` eksik anahtarları
  otomatik ekler — **varsayılan değerlerle**, bu yüzden ilk kurulumdan sonra
  gerçek API anahtarlarını/şifreleri DB üzerinden güncellemek gerekir.
- **LLM sağlayıcı seçilebilir.** `Llm:Provider` ayarına göre (`gemini` | `openai`
  | `ollama` | `groq`) `Program.cs` içinde ilgili `ILlmClient` implementasyonu
  DI'a kaydedilir. `DailySummaryService` ve `AttachmentSummaryService` sadece
  `ILlmClient` arayüzüne bağımlı olduğu için sağlayıcı değişimi tek bir yeri etkiler.
- **Günlük özet job'ı** (`DailySummaryJob`, Quartz.NET, non-clustered) her gün
  `0 57 16 * * ?` cron ifadesiyle (Türkiye saati) çalışır, kullanıcının o günkü
  notlarını LLM ile özetler ve `IsDailySummary=true` olan tek notu günceller;
  geçmiş özetler ayrıca `DailySummaryHistory`/`JobHistory` tablolarına yazılır.
  Dil, kullanıcının `PreferredLanguage` alanına göre belirlenir.
- **Attachment özetleme:** `/api/notes/attachments/summarize` ile yüklenen
  görsel/PDF/metin dosyası (maks. 8MB) LLM ile özetlenir; dosya sunucuda
  saklanmaz, sadece özet metni döner. Yüklenen dosyanın gerçek baytları,
  bildirilen `Content-Type` ile `FileSignatureValidator` üzerinden karşılaştırılır.
- **Redis**, DB'den okunan ayarları önbelleğe alıp Oracle'a gitmeden (restart
  gerekmeden) değişiklikleri yakalamak için ara katman olarak kullanılır.

## Çalıştırma

### Seçenek A — Docker Compose (önerilen)

```bash
cp .env.example .env      # gerekirse ORACLE_PASSWORD'ü değiştirin
docker compose up --build
```

- Backend: `http://localhost:5080` (Swagger: `/swagger`, sadece Development'ta açık)
- Frontend: `http://localhost:5173`
- Oracle: `localhost:1521/XEPDB1` (kullanıcı `SYSTEM`)
- Redis: `localhost:6379`

Backend container'ı ayağa kalkarken migration'ları otomatik uygular
(`context.Database.MigrateAsync()`), ardından `AppSettings` ve kullanıcı
seed'lerini çalıştırır.

### Seçenek B — Yerelde (Docker'sız)

```bash
# 1) Oracle XE ve Redis'i kendiniz ayağa kaldırın (veya sadece bu ikisi için
#    docker compose up oracle-db redis kullanın)

# 2) Backend
cd backend/KeepApi
dotnet restore
dotnet run
```

API `http://localhost:5080` üzerinde ayağa kalkar. Oracle bağlantı dizesi
`OracleConnectionStringResolver` üzerinden (env var veya Windows Credential
Manager'daki `KeepApi:OracleConnection` girdisi) çözülür — `appsettings.json`
içindeki `ConnectionStrings:OracleConnection` boş bırakılmıştır, doğrudan
doldurmayın.

```bash
# 3) Frontend
cd frontend
npm install
npm run dev
```

Uygulama `http://localhost:5173` üzerinde açılır. Backend CORS ayarı
(`Cors:AllowedOrigin`, DB'den okunur) sadece bu origin'e izin verir.
Sosyal giriş butonlarının çalışması için `frontend/.env` içine
`VITE_GOOGLE_CLIENT_ID`, `VITE_MICROSOFT_CLIENT_ID`, `VITE_GITHUB_CLIENT_ID`
tanımlanmalı (bkz. `OAuthConfig.jsx`); backend tarafındaki karşılık gelen
`ClientId`/`ClientSecret` çiftleri `ExternalProviders:*` DB ayarlarında tutulur.

### Testler

```bash
cd backend
dotnet test
```

## API uçları (özet)

| Method | Yol                              | Auth        | Açıklama |
|--------|-----------------------------------|-------------|----------|
| POST   | /api/auth/login                   | -           | Giriş, JWT döner |
| POST   | /api/auth/external/{provider}     | -           | Google/Microsoft/GitHub ile giriş |
| POST   | /api/auth/register                | -           | Kayıt |
| POST   | /api/auth/verify-email            | -           | E-posta doğrulama |
| POST   | /api/auth/forgot-password         | -           | Şifre sıfırlama kodu gönderir |
| POST   | /api/auth/reset-password          | -           | Kodla şifre sıfırlar |
| GET    | /api/auth/me                      | JWT         | Oturum sahibi kullanıcı bilgisi |
| GET    | /api/notes                        | JWT         | Aktif + arşivlenmiş notlar |
| GET    | /api/notes/getall                 | JWT (Admin) | Silinenler dahil tüm notlar |
| GET    | /api/notes/{id}                   | JWT         | Tek not |
| GET    | /api/notes/search                 | JWT         | Arama ekranı için tüm görünür kayıtlar |
| GET    | /api/notes/trash                  | JWT         | Çöp kutusu |
| POST   | /api/notes                        | JWT         | Not oluşturur |
| PUT    | /api/notes/{id}                   | JWT         | Notu günceller |
| PUT    | /api/notes/{id}/restore           | JWT         | Çöpten geri alır |
| DELETE | /api/notes/{id}                   | JWT         | Siler (soft delete → çöp) |
| DELETE | /api/notes/{id}/permanent         | JWT         | Kalıcı siler |
| POST   | /api/notes/attachments/summarize  | JWT         | Dosyayı LLM ile özetler |
| POST   | /api/dailysummary/me/run          | JWT         | Kendi günlük özetini senkron üretir |
| GET    | /api/dailysummary/me/history      | JWT         | Kendi özet geçmişi |
| POST   | /api/dailysummary/run-all         | JWT (Admin) | Job'ı tüm kullanıcılar için tetikler (async) |

## Bilinen eksikler / dikkat edilmesi gerekenler

- **Kritik — kırık uç:** Frontend (`src/api.js` → `updateLanguage()`)
  `PUT /api/auth/me/language` çağırıyor ve `IAuthService.UpdateLanguageAsync` /
  `AuthService.UpdateLanguageAsync` da tam olarak implemente edilmiş durumda,
  ama `AuthController`'da bu route'a karşılık gelen bir action **yok**.
  `LanguageSwitcher` bileşeni şu an sadece frontend state'ini (i18next) değiştiriyor;
  oturum açık bir kullanıcıda backend'e yazma isteği 404 ile başarısız olur ve
  günlük özet job'ı kullanıcının gerçek tercihini hiç görmez. Eksik olan
  `AuthController` action'ı eklenmeli, örn.:
  ```csharp
  [HttpPut("me/language")]
  [Authorize]
  public async Task<IActionResult> UpdateLanguage([FromBody] UpdateLanguageRequest request)
  {
      var userId = GetCurrentUserId();
      if (userId is null) return Unauthorized();
      await _authService.UpdateLanguageAsync(userId.Value, request.Language);
      return Ok(ApiResponse<object>.Ok(new { }, "Dil güncellendi."));
  }
  ```
- **Güvenlik — repoya gerçek görünen bir sır sızmış olabilir:** `KeepApi.Data/Seed/AppSettingsSeeder.cs`
  içindeki diğer tüm secret seed değerleri `***************` ile maskelenmişken,
  `Smtp:Password` alanı gerçek bir Gmail Uygulama Şifresi formatında
  (`"palf nwuh qctu mnyz"`) düz metin olarak duruyor, `Smtp:User`/`Smtp:From` de
  gerçek bir e-posta adresi. Bu değerler ilk çalıştırmada DB'ye (şifreli olarak)
  yazılıyor olsa da, kaynak kodda düz metin halde commit'lenmiş durumda. Eğer bu
  gerçek bir kimlik bilgisiyse, uygulama şifresini Google hesap ayarlarından
  **hemen iptal edip yeniden oluşturmanızı**, seed dosyasındaki değeri placeholder
  ile değiştirmenizi ve gerekiyorsa git geçmişini temizlemenizi öneririm.
- **Debug/test uçları prod'da açık:** `NotesController.oracleconnectiontest`
  (`[AllowAnonymous]`, Oracle bağlantısını doğrudan dener) ve `AuthController.Test`
  (`GET /api/auth/test`, `[AllowAnonymous]`, kasıtlı olarak `Exception` fırlatır)
  kimlik doğrulaması olmadan erişilebilir durumda. Muhtemelen geliştirme sırasında
  eklenmiş debug uçları — production'a gitmeden kaldırılmalı ya da en azından
  `[Authorize(Roles = "Admin")]` ile korunmalı.
- **`node_modules` git'e commit'lenmiş** (`frontend/node_modules`, ~2300 dosya).
  `.gitignore` bunu kapsamıyor; repo boyutunu şişiriyor ve `npm install`'u
  anlamsız kılıyor. `frontend/.gitignore` içine `node_modules/` eklenip
  `git rm -r --cached frontend/node_modules` ile temizlenmesi önerilir.
- **Quartz cron zaman dilimi:** `Program.cs` içinde `TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")`
  kullanılıyor; bu ID Windows ICU zaman dilimi adıdır. Linux container'da (ki
  Dockerfile `mcr.microsoft.com/dotnet/aspnet:10.0` — Linux tabanlı) bu ID'nin
  bulunamama riski var; bulunamazsa uygulama açılışta `TimeZoneNotFoundException`
  ile çöker. Bulunuyorsa (ICU verisiyle) sorun yok, ama garantilemek için IANA
  karşılığı `"Europe/Istanbul"` kullanmak ya da ikisini de deneyen bir fallback
  yazmak daha güvenli olur.
- **Frontend base URL sabit kodlanmış:** `src/api.js` içindeki `NOTES_BASE_URL`
  ve `AUTH_BASE_URL` doğrudan `http://localhost:5080` olarak yazılmış (env
  değişkeni yok). Docker Compose'daki port eşlemesiyle (5080:8080) yerelde
  çalışıyor, ama başka bir host/port'a taşındığında (örn. gerçek bir sunucuya
  deploy) elle değiştirmek gerekir; bir `VITE_API_BASE_URL` env değişkenine
  taşımak daha esnek olur.

## Not şeması (Oracle — `Notes` tablosu, özet)

```json
{
  "id": "32 karakterlik guid (N formatı)",
  "title": "string?",
  "content": "string?",
  "color": "default | sage | sky | sand | blush | lilac",
  "pinned": false,
  "pinnedAt": "datetime?",
  "archived": false,
  "archievedAt": "datetime?",
  "isDailySummary": false,
  "checklist": false,
  "imageAdded": false,
  "imageUrl": "string?",
  "reminderAt": "datetime?",
  "userId": "guid",
  "isDeleted": false
}
```

`Pinned/Archived/IsDailySummary/Checklist/ImageAdded/IsDeleted` Oracle'da
`NUMBER(1)` olarak tutulur; `Oracle.EntityFrameworkCore`'un yerleşik bool
mapping'i kullanılır (elle `HasConversion<int>()` **eklenmemeli** — bu, projede
uzun süre debug edilmiş bir `InvalidCastException`'a yol açmıştı).