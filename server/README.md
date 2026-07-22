# Go Matchmaker Server

This directory contains a simple Go HTTP server for Unity integration.

## Endpoints

- `POST /api/login`
  - Request: `{ "name": "PlayerName" }`
  - Response: `{ "playerId": "...", "name": "...", "token": "..." }`

- `POST /api/join-queue`
  - Request: `{ "playerId": "...", "token": "..." }`
  - Response when waiting: `{ "status": "waiting", "message": "waiting for other players" }`
  - Response when matched: `{ "status": "matched", "matchId": "...", "players": ["...", "..."] }`

- `GET /api/match?playerId=...&token=...&matchId=...`
  - Response: `{ "matchId": "...", "players": ["...", "..."], "status": "found" }`

## Run server

Install Go 1.20+ and run:

```powershell
cd server
go run main.go
```

The server listens on `http://localhost:8080`.

## PostgreSQL support

The server stores players and matches in PostgreSQL. Set `DATABASE_URL` before running the server.

Example:

```powershell
$env:DATABASE_URL = "postgres://postgres:password@localhost:5432/devmoba?sslmode=disable"
cd server
go run main.go
```

If the database does not exist, create it manually and then run the server schema:

```sql
CREATE DATABASE devmoba;
```

The schema is also included in `server/schema.sql`.

## Unity integration

Use Unity's `UnityWebRequest` to call the login and matchmaking endpoints before starting a Photon Fusion session.

The sample Unity client script is in `Assets/Scripts/GoMatchmakerClient.cs`.
