# RatingApp — Mutual Rating MVP

A full-stack "mutual rating" app where users rate each other 1–10. When both users rate each other 7 or above, a match is created and a real-time chat opens.

**Stack:** ASP.NET Core 8 · Blazor WASM · PostgreSQL · EF Core · SignalR · JWT Auth

---

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for Postgres)

### 1. Start PostgreSQL

```bash
# From repo root
cp .env.example .env
docker compose -f docker/docker-compose.yml up -d
```

Optional: start pgAdmin too
```bash
docker compose -f docker/docker-compose.yml --profile tools up -d
# pgAdmin → http://localhost:5050  (admin@ratingapp.dev / admin)
```

### 2. Run Database Migrations

```bash
cd backend
dotnet ef database update \
  --project src/RatingApp.Infrastructure \
  --startup-project src/RatingApp.Api
```

> Migrations are also run automatically on startup in Development mode.

### 3. Start the Backend

```bash
cd backend/src/RatingApp.Api
dotnet run
```

API available at: `https://localhost:7100`
Swagger UI: `https://localhost:7100/swagger`

> On first run in Development, 20 seed users are created automatically.

### 4. Start the Frontend

```bash
cd frontend/RatingApp.Client
dotnet run
```

Frontend available at: `https://localhost:7200`

---

## Configuration

The backend reads configuration in this order (last wins):

1. `appsettings.json` (committed, placeholder values)
2. `appsettings.Development.json` (committed, local dev defaults)
3. Environment variables (use `__` as separator, e.g. `Jwt__Secret=...`)
4. `dotnet user-secrets` (recommended for local secret override)

**To set the JWT secret using user-secrets:**
```bash
cd backend/src/RatingApp.Api
dotnet user-secrets set "Jwt:Secret" "your-super-secret-key-at-least-32-chars"
```

---

## Seed Accounts

On startup in Development, 20 users are seeded:
- Mix of Men and Women
- Located near New York City (±1.5° lat/lon)
- Default preferences: opposite gender, 18–45, 50 miles

**Password for all seed users:** `Password123!`

To find a seeded email: check the database or use the Swagger `/api/auth/login` endpoint and try emails from your DB. You can also register a new account from the UI.

---

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/api/auth/register` | No | Register new user |
| POST | `/api/auth/login` | No | Login, get JWT |
| GET | `/api/me` | Yes | Get own profile |
| PUT | `/api/me` | Yes | Update profile |
| GET | `/api/me/rating-summary` | Yes | Get avg rating + count |
| GET | `/api/me/preferences` | Yes | Get match preferences |
| PUT | `/api/me/preferences` | Yes | Update preferences |
| GET | `/api/candidates?pageSize=10` | Yes | Get candidate feed |
| POST | `/api/ratings` | Yes | Submit a rating |
| GET | `/api/chats` | Yes | List user's chats |
| GET | `/api/chats/{id}/messages` | Yes | Get last 50 messages |
| POST | `/api/chats/{id}/messages` | Yes | Send a message |
| WS | `/hubs/chat` | Yes (JWT) | SignalR real-time hub |

### Sample Requests

**Register:**
```json
POST /api/auth/register
{
  "email": "alice@example.com",
  "password": "Password1!",
  "displayName": "Alice",
  "gender": 2,
  "birthdate": "1997-03-15",
  "latitude": 40.7128,
  "longitude": -74.0060
}
```

**Login:**
```json
POST /api/auth/login
{ "email": "alice@example.com", "password": "Password1!" }
```
Response: `{ "token": "eyJ...", "userId": "...", "displayName": "Alice", "email": "alice@example.com" }`

**Submit Rating:**
```json
POST /api/ratings
Authorization: Bearer {token}
{ "ratedUserId": "...", "score": 8 }
```
Response: `{ "matchCreated": true, "matchId": "..." }` or `{ "matchCreated": false, "matchId": null }`

**Send Message:**
```json
POST /api/chats/{chatId}/messages
Authorization: Bearer {token}
{ "content": "Hey, nice to meet you!" }
```

---

## Project Structure

```
RatingApp/
├── .env.example              # Environment variable template
├── README.md
├── docker/
│   └── docker-compose.yml    # Postgres (+ optional pgAdmin)
├── backend/
│   ├── RatingApp.sln
│   └── src/
│       ├── RatingApp.Domain/          # Entities, enums, interfaces
│       ├── RatingApp.Infrastructure/  # EF Core, migrations, security
│       ├── RatingApp.Application/     # Services, DTOs, validators
│       └── RatingApp.Api/             # Controllers, SignalR hub, Program.cs
└── frontend/
    └── RatingApp.Client/              # Blazor WASM
```

---

## How Matching Works

1. User A rates User B a score ≥ 7
2. System checks if User B has already rated User A ≥ 7
3. If yes → a **Match** is created (with normalized GUID ordering: smaller GUID = UserAId)
4. A **Chat** is automatically created for that match
5. Both users can now message each other in real-time via SignalR

Ratings are **upsertable** — re-rating the same person updates the score and rechecks the match condition.

---

## Real-Time Chat

The SignalR hub at `/hubs/chat` authenticates via JWT passed as `?access_token=` query parameter (required because WebSocket upgrades can't send custom headers).

On connect, the hub automatically adds the user to all their existing chat groups (`chat-{chatId}`). When a match happens, call `JoinChat(chatId)` from the client to join the new group.

---

## Assumptions

- Gender is binary (Man/Woman) for this MVP
- Location is entered as lat/lon manually — no geocoding integration
- Distance uses Haversine formula computed in C# after a bounding-box SQL prefilter; no PostGIS required
- JWT tokens expire after 24 hours (configurable via `Jwt:ExpiryHours`)
- Seeding only runs in Development environment and is idempotent
- No email verification, password reset, or account deletion in MVP
- Messages are not encrypted at rest (MVP scope)
