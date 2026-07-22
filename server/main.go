package main

import (
	"context"
	"crypto/rand"
	"encoding/hex"
	"encoding/json"
	"log"
	"net/http"
	"os"
	"sync"
	"time"

	"github.com/jackc/pgx/v5/pgxpool"
)

type Player struct {
	ID    string `json:"playerId"`
	Name  string `json:"name"`
	Token string `json:"token"`
}

type Match struct {
	ID        string   `json:"matchId"`
	PlayerIDs []string `json:"players"`
	CreatedAt int64    `json:"createdAt"`
}

type loginRequest struct {
	Name string `json:"name"`
}

type loginResponse struct {
	PlayerID string `json:"playerId"`
	Name     string `json:"name"`
	Token    string `json:"token"`
}

type joinQueueRequest struct {
	PlayerID string `json:"playerId"`
	Token    string `json:"token"`
}

type queueResponse struct {
	Status  string   `json:"status"`
	Message string   `json:"message,omitempty"`
	MatchID string   `json:"matchId,omitempty"`
	Players []string `json:"players,omitempty"`
}

type matchResponse struct {
	MatchID string   `json:"matchId"`
	Players []string `json:"players"`
	Status  string   `json:"status"`
}

var store *ServerStore

func main() {
	ctx := context.Background()
	dbURL := os.Getenv("DATABASE_URL")
	if dbURL == "" {
		dbURL = "postgres://postgres:password@localhost:5432/devmoba?sslmode=disable"
		log.Printf("DATABASE_URL not set, defaulting to %s", dbURL)
	}

	db, err := initDB(ctx, dbURL)
	if err != nil {
		log.Fatalf("failed to initialize database: %v", err)
	}
	defer db.Close()

	store = NewServerStore(db)

	mux := http.NewServeMux()
	mux.HandleFunc("/api/health", handleHealth)
	mux.HandleFunc("/api/login", handleLogin)
	mux.HandleFunc("/api/join-queue", handleJoinQueue)
	mux.HandleFunc("/api/match", handleGetMatch)

	server := &http.Server{
		Addr:         ":8080",
		Handler:      loggingMiddleware(mux),
		ReadTimeout:  10 * time.Second,
		WriteTimeout: 10 * time.Second,
		IdleTimeout:  30 * time.Second,
	}

	log.Printf("Go matchmaker server listening on %s", server.Addr)
	if err := server.ListenAndServe(); err != nil && err != http.ErrServerClosed {
		log.Fatalf("server failed: %v", err)
	}
}

func initDB(ctx context.Context, dbURL string) (*pgxpool.Pool, error) {
	pool, err := pgxpool.New(ctx, dbURL)
	if err != nil {
		return nil, err
	}

	schema := `
CREATE TABLE IF NOT EXISTS players (
	id text PRIMARY KEY,
	name text NOT NULL,
	token text NOT NULL UNIQUE,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS matches (
	id text PRIMARY KEY,
	player_ids text[] NOT NULL,
	created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_matches_player_ids ON matches USING GIN (player_ids);
`

	if _, err := pool.Exec(ctx, schema); err != nil {
		pool.Close()
		return nil, err
	}
	return pool, nil
}

func handleHealth(w http.ResponseWriter, _ *http.Request) {
	respondJSON(w, http.StatusOK, map[string]string{"status": "ok"})
}

func handleLogin(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req loginRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "invalid request body", http.StatusBadRequest)
		return
	}

	name := sanitizeName(req.Name)
	if name == "" {
		http.Error(w, "name must not be empty", http.StatusBadRequest)
		return
	}

	player := store.CreatePlayer(name)
	respondJSON(w, http.StatusOK, loginResponse{
		PlayerID: player.ID,
		Name:     player.Name,
		Token:    player.Token,
	})
}

func handleJoinQueue(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodPost {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	var req joinQueueRequest
	if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
		http.Error(w, "invalid request body", http.StatusBadRequest)
		return
	}

	player, ok := store.ValidatePlayer(req.PlayerID, req.Token)
	if !ok {
		http.Error(w, "invalid player or token", http.StatusUnauthorized)
		return
	}

	match, status := store.JoinQueue(player.ID)
	if status == "matched" {
		respondJSON(w, http.StatusOK, queueResponse{
			Status:  status,
			MatchID: match.ID,
			Players: match.PlayerIDs,
			Message: "match found",
		})
		return
	}

	respondJSON(w, http.StatusOK, queueResponse{
		Status:  status,
		Message: "waiting for other players",
	})
}

func handleGetMatch(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		http.Error(w, "method not allowed", http.StatusMethodNotAllowed)
		return
	}

	playerID := r.URL.Query().Get("playerId")
	token := r.URL.Query().Get("token")
	matchID := r.URL.Query().Get("matchId")

	if playerID == "" || token == "" || matchID == "" {
		http.Error(w, "playerId, token and matchId are required", http.StatusBadRequest)
		return
	}

	_, ok := store.ValidatePlayer(playerID, token)
	if !ok {
		http.Error(w, "invalid player or token", http.StatusUnauthorized)
		return
	}

	match, exists := store.GetMatch(matchID)
	if !exists {
		http.Error(w, "match not found", http.StatusNotFound)
		return
	}

	respondJSON(w, http.StatusOK, matchResponse{
		MatchID: match.ID,
		Players: match.PlayerIDs,
		Status:  "found",
	})
}

func loggingMiddleware(next http.Handler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		log.Printf("%s %s", r.Method, r.URL.Path)
		next.ServeHTTP(w, r)
	})
}

func respondJSON(w http.ResponseWriter, status int, payload any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(payload)
}

func sanitizeName(value string) string {
	return string([]rune(value))
}

func generateID(prefix string) string {
	b := make([]byte, 12)
	_, err := rand.Read(b)
	if err != nil {
		log.Printf("failed to generate id: %v", err)
		return prefix + "-unknown"
	}
	return prefix + "-" + hex.EncodeToString(b)
}

type ServerStore struct {
	mu      sync.Mutex
	db      *pgxpool.Pool
	players map[string]*Player
	matches map[string]*Match
	queue   []string
}

func NewServerStore(db *pgxpool.Pool) *ServerStore {
	return &ServerStore{
		db:      db,
		players: make(map[string]*Player),
		matches: make(map[string]*Match),
		queue:   make([]string, 0),
	}
}

func (s *ServerStore) CreatePlayer(name string) *Player {
	s.mu.Lock()
	defer s.mu.Unlock()

	id := generateID("player")
	token := generateID("token")
	player := &Player{ID: id, Name: name, Token: token}

	if s.db != nil {
		if err := s.insertPlayer(context.Background(), player); err != nil {
			log.Printf("failed to insert player into db: %v", err)
		}
		return player
	}

	s.players[id] = player
	return player
}

func (s *ServerStore) ValidatePlayer(playerID, token string) (*Player, bool) {
	if s.db != nil {
		return s.validatePlayerDB(context.Background(), playerID, token)
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	player, ok := s.players[playerID]
	if !ok || player.Token != token {
		return nil, false
	}
	return player, true
}

func (s *ServerStore) JoinQueue(playerID string) (*Match, string) {
	s.mu.Lock()
	defer s.mu.Unlock()

	for _, p := range s.queue {
		if p == playerID {
			return nil, "waiting"
		}
	}

	if !s.playerExists(playerID) {
		return nil, "invalid-player"
	}

	s.queue = append(s.queue, playerID)
	if len(s.queue) >= 2 {
		players := s.queue[:2]
		s.queue = s.queue[2:]
		match := &Match{
			ID:        generateID("match"),
			PlayerIDs: players,
			CreatedAt: time.Now().Unix(),
		}

		if s.db != nil {
			if err := s.insertMatch(context.Background(), match); err != nil {
			    log.Printf("failed to insert match into db: %v", err)
			}
		} else {
			s.matches[match.ID] = match
		}

		return match, "matched"
	}

	return nil, "waiting"
}

func (s *ServerStore) GetMatch(matchID string) (*Match, bool) {
	if s.db != nil {
		return s.getMatchDB(context.Background(), matchID)
	}

	s.mu.Lock()
	defer s.mu.Unlock()

	match, ok := s.matches[matchID]
	return match, ok
}

func (s *ServerStore) playerExists(playerID string) bool {
	if s.db != nil {
		player, ok := s.getPlayerDB(context.Background(), playerID)
		return ok && player != nil
	}

	_, ok := s.players[playerID]
	return ok
}

func (s *ServerStore) insertPlayer(ctx context.Context, player *Player) error {
	_, err := s.db.Exec(ctx,
		`INSERT INTO players (id, name, token, created_at) VALUES ($1, $2, $3, now())`,
		player.ID, player.Name, player.Token,
	)
	return err
}

func (s *ServerStore) validatePlayerDB(ctx context.Context, playerID, token string) (*Player, bool) {
	player, ok := s.getPlayerDB(ctx, playerID)
	if !ok || player.Token != token {
		return nil, false
	}
	return player, true
}

func (s *ServerStore) getPlayerDB(ctx context.Context, playerID string) (*Player, bool) {
	row := s.db.QueryRow(ctx, `SELECT id, name, token FROM players WHERE id = $1`, playerID)
	var player Player
	if err := row.Scan(&player.ID, &player.Name, &player.Token); err != nil {
		return nil, false
	}
	return &player, true
}

func (s *ServerStore) insertMatch(ctx context.Context, match *Match) error {
	_, err := s.db.Exec(ctx,
		`INSERT INTO matches (id, player_ids, created_at) VALUES ($1, $2, to_timestamp($3))`,
		match.ID, match.PlayerIDs, match.CreatedAt,
	)
	return err
}

func (s *ServerStore) getMatchDB(ctx context.Context, matchID string) (*Match, bool) {
	row := s.db.QueryRow(ctx,
		`SELECT id, player_ids, created_at FROM matches WHERE id = $1`,
		matchID,
	)
	var match Match
	var createdAt time.Time
	if err := row.Scan(&match.ID, &match.PlayerIDs, &createdAt); err != nil {
		return nil, false
	}
	match.CreatedAt = createdAt.Unix()
	return &match, true
}
