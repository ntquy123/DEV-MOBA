package main

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"
)

func TestLoginAndMatchmaking(t *testing.T) {
	store = NewServerStore(nil)

	loginBody := loginRequest{Name: "TestPlayer"}
	bodyData, _ := json.Marshal(loginBody)
	req := httptest.NewRequest(http.MethodPost, "/api/login", bytes.NewReader(bodyData))
	w := httptest.NewRecorder()

	handleLogin(w, req)
	if w.Result().StatusCode != http.StatusOK {
		t.Fatalf("expected 200 OK, got %d", w.Result().StatusCode)
	}

	var loginResp loginResponse
	if err := json.NewDecoder(w.Body).Decode(&loginResp); err != nil {
		t.Fatal(err)
	}
	if loginResp.PlayerID == "" || loginResp.Token == "" {
		t.Fatalf("expected valid playerId and token, got %+v", loginResp)
	}

	joinBody := joinQueueRequest{PlayerID: loginResp.PlayerID, Token: loginResp.Token}
	bodyData, _ = json.Marshal(joinBody)
	req = httptest.NewRequest(http.MethodPost, "/api/join-queue", bytes.NewReader(bodyData))
	w = httptest.NewRecorder()

	handleJoinQueue(w, req)
	if w.Result().StatusCode != http.StatusOK {
		t.Fatalf("expected 200 OK on first join, got %d", w.Result().StatusCode)
	}

	var queueResp queueResponse
	if err := json.NewDecoder(w.Body).Decode(&queueResp); err != nil {
		t.Fatal(err)
	}
	if queueResp.Status != "waiting" {
		t.Fatalf("expected waiting status, got %s", queueResp.Status)
	}

	secondLoginBody := loginRequest{Name: "TestPlayer2"}
	bodyData, _ = json.Marshal(secondLoginBody)
	req = httptest.NewRequest(http.MethodPost, "/api/login", bytes.NewReader(bodyData))
	w = httptest.NewRecorder()
	handleLogin(w, req)
	var secondLoginResp loginResponse
	if err := json.NewDecoder(w.Body).Decode(&secondLoginResp); err != nil {
		t.Fatal(err)
	}

	joinBody = joinQueueRequest{PlayerID: secondLoginResp.PlayerID, Token: secondLoginResp.Token}
	bodyData, _ = json.Marshal(joinBody)
	req = httptest.NewRequest(http.MethodPost, "/api/join-queue", bytes.NewReader(bodyData))
	w = httptest.NewRecorder()
	handleJoinQueue(w, req)
	if w.Result().StatusCode != http.StatusOK {
		t.Fatalf("expected 200 OK on second join, got %d", w.Result().StatusCode)
	}

	if err := json.NewDecoder(w.Body).Decode(&queueResp); err != nil {
		t.Fatal(err)
	}
	if queueResp.Status != "matched" || queueResp.MatchID == "" {
		t.Fatalf("expected matched status, got %+v", queueResp)
	}

	matchReq := httptest.NewRequest(http.MethodGet, "/api/match?playerId="+loginResp.PlayerID+"&token="+loginResp.Token+"&matchId="+queueResp.MatchID, nil)
	w = httptest.NewRecorder()
	handleGetMatch(w, matchReq)
	if w.Result().StatusCode != http.StatusOK {
		t.Fatalf("expected 200 OK on get match, got %d", w.Result().StatusCode)
	}

	var matchResp matchResponse
	if err := json.NewDecoder(w.Body).Decode(&matchResp); err != nil {
		t.Fatal(err)
	}
	if matchResp.MatchID != queueResp.MatchID {
		t.Fatalf("expected matchId %s, got %s", queueResp.MatchID, matchResp.MatchID)
	}
	if len(matchResp.Players) != 2 {
		t.Fatalf("expected 2 players in match, got %d", len(matchResp.Players))
	}
}
