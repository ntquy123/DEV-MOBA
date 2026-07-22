using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[AddComponentMenu("DEV MOBA/Go Matchmaker Client")]
public class GoMatchmakerClient : MonoBehaviour
{
    [Header("Matchmaker Server")]
    [Tooltip("Local Go server base URL, e.g. http://localhost:8080")]
    public string serverBaseUrl = "http://localhost:8080";

    [Tooltip("Player name sent to the Go matchmaking server")]
    public string playerName = "Player";

    [Header("Match Status")]
    public string playerId;
    public string token;
    public string matchId;
    public string matchStatus;

    public void StartMatchmaking()
    {
        StartCoroutine(LoginAndJoinQueue());
    }

    private IEnumerator LoginAndJoinQueue()
    {
        matchStatus = "Logging in...";
        var loginRequest = new LoginRequest { name = playerName };
        var loginJson = JsonUtility.ToJson(loginRequest);

        using (var request = new UnityWebRequest(serverBaseUrl + "/api/login", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(loginJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                matchStatus = "Login failed: " + request.error;
                Debug.LogError(matchStatus);
                yield break;
            }

            var loginResponse = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);
            playerId = loginResponse.playerId;
            token = loginResponse.token;
            Debug.Log($"Logged in as {loginResponse.name} (playerId={playerId})");
        }

        matchStatus = "Joining queue...";
        var joinRequest = new JoinQueueRequest { playerId = playerId, token = token };
        var joinJson = JsonUtility.ToJson(joinRequest);

        using (var request = new UnityWebRequest(serverBaseUrl + "/api/join-queue", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(joinJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                matchStatus = "Join queue failed: " + request.error;
                Debug.LogError(matchStatus);
                yield break;
            }

            var queueResponse = JsonUtility.FromJson<QueueResponse>(request.downloadHandler.text);
            matchStatus = queueResponse.status;
            if (queueResponse.status == "matched")
            {
                matchId = queueResponse.matchId;
                matchStatus = "Matched: " + matchId;
                Debug.Log(matchStatus);
                Debug.Log("Players in match: " + string.Join(", ", queueResponse.players));
            }
            else
            {
                Debug.Log(matchStatus + ": waiting for another player...");
            }
        }
    }

    [System.Serializable]
    private class LoginRequest
    {
        public string name;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public string playerId;
        public string name;
        public string token;
    }

    [System.Serializable]
    private class JoinQueueRequest
    {
        public string playerId;
        public string token;
    }

    [System.Serializable]
    private class QueueResponse
    {
        public string status;
        public string message;
        public string matchId;
        public string[] players;
    }
}
