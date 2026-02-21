# RatingApp

A full-stack mutual-rating app: users rate each other 1–10. When both rate each other ≥ 7, a match is created and a real-time chat opens.

**Stack:** ASP.NET Core 8 · Blazor WASM · PostgreSQL · EF Core · SignalR · JWT Auth

[![CI](https://github.com/anthonydagostino/rating-app/actions/workflows/ci.yml/badge.svg)](https://github.com/anthonydagostino/rating-app/actions/workflows/ci.yml)

---

## Download & Run (Latest Release)

> **Easiest path — no code required.**

Go to the [Releases page](https://github.com/anthonydagostino/rating-app/releases/latest) and choose your method:

### Running with Docker Compose (Recommended)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop).

```bash
# 1. Copy the env template and set your secrets
cp .env.example .env
#    Edit .env — at minimum change JWT_SECRET to a random 32+ char string

# 2. Pull and start everything (app + PostgreSQL)
docker compose -f docker/docker-compose.yml up -d

# 3. Open the app
open http://localhost:8080
```

To stop:
```bash
docker compose -f docker/docker-compose.yml down
```

To upgrade to a new release:
```bash
docker compose -f docker/docker-compose.yml pull
docker compose -f docker/docker-compose.yml up -d
```

### Standalone Binary

Requires [PostgreSQL 14+](https://www.postgresql.org/download/) already running.

1. Download the archive for your platform from the [latest release](https://github.com/anthonydagostino/rating-app/releases/latest):
   - **Windows** — `rating-app-win-x64.zip`
   - **Linux** — `rating-app-linux-x64.tar.gz`
   - **macOS** — `rating-app-osx-x64.tar.gz`

2. Extract and configure:

```bash
# Linux / macOS
tar -xzf rating-app-linux-x64.tar.gz
cd rating-app-linux-x64
```

3. Set environment variables (or edit `appsettings.json`):

```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=ratingapp_db;Username=postgres;Password=yourpassword"
export Jwt__Secret="your-random-secret-at-least-32-chars-long"
export ASPNETCORE_URLS="http://localhost:8080"
```

4. Run:

```bash
# Linux / macOS
chmod +x RatingApp.Api
./RatingApp.Api

# Windows
RatingApp.Api.exe
```

5. Open `http://localhost:8080`

> The app runs migrations automatically on startup — no manual `dotnet ef` commands needed.

---

## Development Setup

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)

### 1. Clone and start PostgreSQL

```bash
git clone https://github.com/anthonydagostino/rating-app.git
cd rating-app
cp .env.example .env
docker compose -f docker/docker-compose.yml up postgres -d
```

### 2. Start the backend

```bash
cd backend/src/RatingApp.Api
dotnet run
# API: http://localhost:5212   Swagger: http://localhost:5212/swagger
```

On first run in Development, 20 seed users are created automatically.

### 3. Start the frontend

```bash
cd frontend/RatingApp.Client
dotnet run
# App: http://localhost:5173
```

---

## Branch Strategy

| Branch | Purpose |
|--------|---------|
| `main` | Stable, production-ready. Protected — merge via PR only. |
| `dev` | Active development. CI runs on every push. |

**Workflow:**
1. Work on `dev` (or a feature branch off `dev`)
2. Open a PR from `dev` → `main`
3. CI must pass before merging

---

## Releases & Versioning

Releases follow [Semantic Versioning](https://semver.org): `vMAJOR.MINOR.PATCH`

To cut a release (maintainers only):

```bash
git checkout main
git pull
git tag v1.2.3
git push origin v1.2.3
```

This triggers the release pipeline which automatically:
1. Runs all 49 tests (31 unit + 18 integration)
2. Builds self-contained binaries for Windows, Linux, and macOS
3. Builds and pushes a Docker image to `ghcr.io/anthonydagostino/rating-app`
4. Creates a GitHub Release with the binaries and auto-generated changelog

---

## Configuration

The backend reads configuration in priority order (last wins):

| Source | Notes |
|--------|-------|
| `appsettings.json` | Committed, placeholder values |
| `appsettings.{Environment}.json` | Per-environment overrides |
| Environment variables | Use `__` as separator, e.g. `Jwt__Secret` |
| `dotnet user-secrets` | Recommended for local dev secrets |

**Key settings:**

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__Secret` | HMAC-SHA256 signing key (min 32 chars) |
| `Jwt__Issuer` | JWT issuer claim (default: `RatingApp`) |
| `Jwt__Audience` | JWT audience claim (default: `RatingApp.Client`) |
| `Jwt__ExpiryHours` | Token lifetime in hours (default: `24`) |

---

## Running Tests

```bash
cd backend

# All tests
dotnet test RatingApp.sln -c Release

# Unit tests only (31 tests — no DB or network needed)
dotnet test tests/RatingApp.Application.Tests -c Release

# Integration tests only (18 tests — uses in-memory DB)
dotnet test tests/RatingApp.Api.IntegrationTests -c Release
```

---

## API Reference

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Register |
| POST | `/api/auth/login` | No | Login, returns JWT |
| GET | `/api/me` | Yes | Get own profile |
| PUT | `/api/me` | Yes | Update profile |
| GET | `/api/me/rating-summary` | Yes | Avg rating + count |
| GET | `/api/me/preferences` | Yes | Match preferences |
| PUT | `/api/me/preferences` | Yes | Update preferences |
| GET | `/api/candidates?pageSize=10` | Yes | Candidate feed |
| POST | `/api/ratings` | Yes | Submit a rating |
| GET | `/api/chats` | Yes | List chats |
| GET | `/api/chats/{id}/messages` | Yes | Last 50 messages |
| POST | `/api/chats/{id}/messages` | Yes | Send message |
| WS | `/hubs/chat?access_token=...` | Yes | SignalR real-time chat |
| GET | `/health` | No | Health check |

---

## Project Structure

```
RatingApp/
├── .github/
│   └── workflows/
│       ├── ci.yml          # Tests on push to main/dev and PRs
│       └── release.yml     # Binaries + Docker + GitHub Release on vX.Y.Z tag
├── docker/
│   ├── Dockerfile          # Multi-stage build (frontend + backend combined)
│   └── docker-compose.yml  # App + PostgreSQL + optional pgAdmin
├── backend/
│   ├── RatingApp.sln
│   ├── src/
│   │   ├── RatingApp.Domain/          # Entities, enums, interfaces
│   │   ├── RatingApp.Infrastructure/  # EF Core, migrations, security
│   │   ├── RatingApp.Application/     # Services, DTOs, validators
│   │   └── RatingApp.Api/             # Controllers, SignalR hub, Program.cs
│   └── tests/
│       ├── RatingApp.Application.Tests/     # 31 unit tests
│       └── RatingApp.Api.IntegrationTests/  # 18 integration tests
└── frontend/
    └── RatingApp.Client/   # Blazor WASM
```

---

## How Matching Works

1. User A rates User B **≥ 7**
2. The system checks if User B already rated User A **≥ 7**
3. If yes → a **Match** is created and a **Chat** opens automatically
4. Both users can message each other in real-time via SignalR

Ratings are **upsertable** — re-rating someone updates the score and re-checks the match condition.

---

## Seed Accounts (Development only)

On first run in Development, 20 users are seeded near New York City with a mix of genders.

**Password for all seed users:** `Password123!`

---

## Road Map

- [ ] Mobile app (MAUI Blazor Hybrid / React Native)
- [ ] Email verification & password reset
- [ ] Photo moderation
- [ ] Push notifications
- [ ] Subscription / paywall