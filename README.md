# not defteri — .NET 8 + React 19 (JSON dosya tabanlı)

Google Keep benzeri, veritabanı olmadan çalışan bir not/todo uygulaması.
Notlar `backend/KeepApi/Data/notes.json` dosyasında saklanır.

## Klasör yapısı

```
keep-todo-app/
  backend/KeepApi/       .NET 8 minimal API
    Models/Note.cs
    Services/NoteStore.cs   JSON dosya okuma/yazma (thread-safe)
    Program.cs
    Data/notes.json         veri dosyası (otomatik oluşur/güncellenir)
  frontend/               React 19 + Vite
    src/
      App.jsx
      api.js
      components/
        Composer.jsx      hızlı not ekleme (useActionState)
        NoteCard.jsx       tekil not kartı
        ColorDots.jsx      renk seçici
      styles.css
```

## Çalıştırma

### 1) Backend

```bash
cd backend/KeepApi
dotnet restore
dotnet run
```

API `http://localhost:5080` üzerinde ayağa kalkar. İlk çalıştırmada
`Data/notes.json` yoksa otomatik oluşturulur (`[]` ile başlar).

### 2) Frontend

```bash
cd frontend
npm install
npm run dev
```

Uygulama `http://localhost:5173` üzerinde açılır. Backend CORS ayarı
sadece bu origin'e izin veriyor (`Program.cs` → `AllowFrontend` policy);
farklı bir portta çalıştırırsan `Program.cs` ve `src/api.js`'deki
URL'leri buna göre güncelle.

## API uçları

| Method | Yol                    | Açıklama            |
|--------|------------------------|----------------------|
| GET    | /api/notes             | tüm notları listeler |
| GET    | /api/notes/{id}        | tek not getirir      |
| POST   | /api/notes             | yeni not oluşturur   |
| PUT    | /api/notes/{id}        | notu günceller       |
| DELETE | /api/notes/{id}        | notu siler           |

Not şeması:

```json
{
  "id": "string",
  "title": "string",
  "content": "string",
  "color": "default | sage | sky | sand | blush | lilac",
  "pinned": false,
  "archived": false,
  "createdAt": "2026-07-18T00:00:00Z",
  "updatedAt": "2026-07-18T00:00:00Z"
}
```
