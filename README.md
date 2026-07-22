# Piece — Interactive music streaming web application

Piece is a full-stack music streaming web application built with ASP.NET Core 8 Blazor Server. It combines a local music library with the Jamendo API for royalty-free streaming, real-time audio visualizers, an interactive world music map, playlist management, subscription plans, and a full admin panel.

---

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (local development) or a PostgreSQL connection string (Railway)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or VS Code with C# extension
- Git

---

### 1. Clone the Repository

```bash
git clone https://github.com/projectraya/piece.git
cd piece
```

---

### 2. Configure User Secrets (Local Development)

Piece uses .NET User Secrets to store API keys locally. Run the following commands in the project root:

```bash
dotnet user-secrets set "Jamendo:ClientId" "YOUR_JAMENDO_CLIENT_ID"
dotnet user-secrets set "LastFm:ApiKey" "YOUR_LASTFM_API_KEY"
```

To obtain API keys:
- **Jamendo**: Register at [developer.jamendo.com](https://developer.jamendo.com) and create an application
- **Last.fm**: Register at [last.fm/api](https://www.last.fm/api/account/create)

The database connection string is configured in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=PieceDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

---

### 3. Database Setup

The database is created automatically on first run using `EnsureCreatedAsync()`. No manual migrations are needed.

To seed the database with initial data (genres, tracks, countries, subscription plans, admin account), set the following in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "SeedDatabase": true
}
```

> Set `SeedDatabase` to `false` after the first run to avoid re-seeding on every startup.

---

### 4. Music Files

The local music library is included in the repository under `wwwroot/music/` and will be available automatically after cloning. No additional steps are needed.

---

### 5. Run the Application

```bash
dotnet run
```

Or press **F5** in Visual Studio. The application will be available at:

```
https://localhost:7289
```

---

## Test Accounts

### Admin Account

| Field    | Value           |
|----------|-----------------|
| Email    | admin1@piece.com |
| Password | *provided in a separate file*       |

The admin account is created automatically during seeding. It has access to:
- `/admin/dashboard` — Platform overview and statistics
- `/admin/users` — User management and role assignment
- `/admin/tracks` — Track management and uploads
- `/admin/playlists` — Playlist moderation
- `/admin/genres` — Genre management
- `/admin/logs` — Activity logs

---

### Regular User Account

Register a new account at `/Account/Register` using any email address.

> Email confirmation is disabled in development — you can log in immediately after registration.

---

## Deployed Version

The application is deployed and publicly accessible at:

```
https://piece-production.up.railway.app/
```

---

## Tech Stack

| Layer      | Technology                             |
|------------|----------------------------------------|
| Frontend   | Blazor Server (.NET 8), HTML5, CSS3    |
| Backend    | ASP.NET Core 8, Entity Framework Core |
| Database   | SQL Server (local), PostgreSQL (Railway)|
| Audio      | Web Audio API, Three.js, Canvas 2D    |
| Maps       | Leaflet.js                            |
| APIs       | Jamendo, MusicBrainz, Deezer, Last.fm |
| Deployment | Railway (CI/CD via GitHub)            |

---

## Environment Variables (Production / Railway)

| Variable            | Description                       |
|---------------------|-----------------------------------|
| `DATABASE_URL`      | PostgreSQL connection string      |
| `Jamendo__ClientId` | Jamendo API client ID             |
| `LastFm__ApiKey`    | Last.fm API key                   |
| `SeedDatabase`      | Set to `false` since it's already seeded |

---

## Project Structure

```
Piece/
├── Components/         # Blazor pages and shared components
│   ├── Pages/          # Main pages (Player, Map, Visualizer, etc.)
│   └── Account/        # Authentication pages
├── Data/               # DbContext, models, enums
├── DTOs/               # Data transfer objects
├── Middleware/         # BanCheck middleware
├── Services/           # Business logic services
├── wwwroot/            # Static files (CSS, JS, music, images)
│   ├── music/          # Local audio files
│   ├── images/         # Album art and uploads
│   └── js/             # visualizerManager.js, threeSphere.js
└── Program.cs          # App configuration and startup
```

---

## Running Tests

```bash
dotnet test
```

Unit tests are located in the `Piece.Tests` project and cover core services including playlist management, favorites, listening history, and security utilities.

---

## License

This project was developed as a diploma project. All music in the local library is original or royalty-free. Jamendo tracks are streamed under their respective Creative Commons licenses.

## Images
<img width="2869" height="1574" alt="image" src="https://github.com/user-attachments/assets/d5023207-9ca0-4752-b5bd-ea9863fce48e" />
<img width="2873" height="1567" alt="image" src="https://github.com/user-attachments/assets/e69fa4cb-df07-4d21-8b38-1f26be3a5aa2" />
<img width="2871" height="1564" alt="image" src="https://github.com/user-attachments/assets/7d0f6046-84ed-4190-8a63-531840417845" />
<img width="2875" height="1567" alt="image" src="https://github.com/user-attachments/assets/748dd795-6f37-4431-9fbc-43518ff92b65" />
<img width="2870" height="1562" alt="image" src="https://github.com/user-attachments/assets/d9fed139-f558-4a74-8c95-be14ead2170d" />




