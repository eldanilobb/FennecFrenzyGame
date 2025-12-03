using System;
using UnityEngine;

public class GameServerMatchmaking : MonoBehaviour
{
    private GameServerConnection gameServer;
    private string currentMatchId = "";
    private string currentOpponentId = "";
    private bool hasActiveMatchRequest = false;

    public event Action<string> OnMatchRequestSent;
    public event Action<string, string> OnMatchRequestReceived;
    public event Action<string, string> OnMatchAccepted;
    public event Action<string> OnMatchRejected;
    public event Action<string> OnMatchCanceledBySender;
    public event Action OnMatchCanceled;
    public event Action<string> OnMatchRequestError;
    
    public event Action<string> OnConnectMatchSuccess;
    public event Action<string> OnConnectMatchError;
    public event Action OnPingOK;
    public event Action<string> OnMatchStart;
    public event Action<string> OnPlayersReady;
    public event Action<string> OnCustomReadyReceived; 

    void Start() 
    { 
        gameServer = FindFirstObjectByType<GameServerConnection>(); 
    }

    public void SendMatchRequest(string targetPlayerId)
    {
        if (!gameServer.isLoggedIn) return;
        if (hasActiveMatchRequest) return;
    
        currentOpponentId = targetPlayerId; 

        SendEvent("send-match-request", $"\"playerId\":\"{targetPlayerId}\"");
    }

    public void CancelMatchRequest()
    {
        hasActiveMatchRequest = false;
        SendEvent("cancel-match-request", "");
    }

    public void AcceptMatchRequest() => SendEvent("accept-match", "");
    public void RejectMatchRequest() => SendEvent("reject-match", "");
    
    public void SendConnectMatch(string matchId)
    {
        if (string.IsNullOrEmpty(matchId)) return;
        SendEvent("connect-match", $"\"matchId\":\"{matchId}\"");
    }

    public void SendPingMatch(string matchId) 
    { 
        if (!string.IsNullOrEmpty(matchId)) 
            SendEvent("ping-match", $"\"matchId\":\"{matchId}\""); 
    }

    public void SendLeaveMatch(string matchId)
    {
        if (string.IsNullOrEmpty(matchId)) return;
        SendEvent("quit-match", $"\"matchId\":\"{matchId}\"");
        
        currentMatchId = "";
        currentOpponentId = "";
        hasActiveMatchRequest = false;
    }

    public void SendFinishMatch(string matchId, string winnerId)
    {
        if (string.IsNullOrEmpty(matchId)) return;
        SendEvent("finish-game", $"\"matchId\":\"{matchId}\",\"winner\":\"{winnerId}\"");
    }

    private void SendEvent(string eventName, string data)
    {
        string d = string.IsNullOrEmpty(data) ? "" : data;
        string json = $"{{\"event\":\"{eventName}\",\"data\":{{{d}}}}}";
        gameServer.SendWebSocketMessage(json);
    }

    public void ProcessMatchmakingEvent(string eventName, string message)
    {
        switch (eventName)
        {
            case "send-match-request":
                if (message.Contains("OK"))
                {
                    hasActiveMatchRequest = true;
                    currentMatchId = ExtractValue(message, "matchId");
                    OnMatchRequestSent?.Invoke(currentMatchId);
                }
                else OnMatchRequestError?.Invoke(message);
                break;

            case "match-request-received":
                currentOpponentId = ExtractValue(message, "playerId");
                currentMatchId = ExtractValue(message, "matchId");
                hasActiveMatchRequest = true;
                OnMatchRequestReceived?.Invoke(currentOpponentId, currentMatchId);
                break;

            case "match-accepted":
                currentMatchId = ExtractValue(message, "matchId");
                if (!string.IsNullOrEmpty(currentMatchId))
                {
                    OnMatchAccepted?.Invoke(currentMatchId, "CONNECTED");
                }
                break;

            case "match-rejected":
                string rejectId = ExtractValue(message, "playerId");
                OnMatchRejected?.Invoke(rejectId);
                ResetMatchState();
                break;

            case "match-canceled-by-sender":
                string cancelId = ExtractValue(message, "playerId");
                OnMatchCanceledBySender?.Invoke(cancelId);
                ResetMatchState();
                break;

            case "cancel-match-request":
                if (message.Contains("OK"))
                {
                    hasActiveMatchRequest = false;
                    OnMatchCanceled?.Invoke();
                }
                break;
            
            case "connect-match":
                if (message.Contains("OK")) OnConnectMatchSuccess?.Invoke(currentMatchId);
                else 
                {
                     string errorMsg = ExtractValue(message, "msg");
                     if(string.IsNullOrEmpty(errorMsg)) errorMsg = "Error";
                     OnConnectMatchError?.Invoke(errorMsg);
                }
                break;

            case "ping-match":
                if (message.Contains("OK")) OnPingOK?.Invoke();
                break;

            case "match-start":
                OnMatchStart?.Invoke(message);
                break;

            case "accept-match":
                if (message.Contains("OK") || message.Contains("ok"))
                {
                    string possibleId = ExtractValue(message, "matchId");
                    if (!string.IsNullOrEmpty(possibleId))
                    {
                        currentMatchId = possibleId;
                        OnMatchAccepted?.Invoke(currentMatchId, "CONNECTED");
                    }
                }
                break;
            
            case "receive-game-data":
                string internalPayload = ExtractJsonPayload(message); 
                
                if (!string.IsNullOrEmpty(internalPayload))
                {
                    OnCustomReadyReceived?.Invoke(internalPayload);
                }
                break;
                
            case "players-ready":
                break; 
                
            case "reject-match":
                break;
        }
    }

    private string ExtractValue(string json, string key)
    {
        try {
            string pattern = $"\"{key}\""; 
            int keyIdx = json.IndexOf(pattern);
            if (keyIdx == -1) return "";
            
            int valStart = json.IndexOf("\"", keyIdx + pattern.Length + 1);
            if (valStart == -1) return "";
            
            int valEnd = json.IndexOf("\"", valStart + 1);
            if (valEnd == -1) return "";
            
            return json.Substring(valStart + 1, valEnd - valStart - 1);
        } catch { return ""; }
    }

    private string ExtractJsonPayload(string fullJson)
    {
        try 
        {
            string key = "\"payload\":";
            int startIdx = fullJson.IndexOf(key);
            if (startIdx == -1) return "";

            startIdx += key.Length;
            int openBrace = fullJson.IndexOf('{', startIdx);
            if (openBrace == -1) return "";

            int balance = 0;
            for (int i = openBrace; i < fullJson.Length; i++)
            {
                if (fullJson[i] == '{') balance++;
                else if (fullJson[i] == '}')
                {
                    balance--;
                    if (balance == 0)
                    {
                        return fullJson.Substring(openBrace, i - openBrace + 1);
                    }
                }
            }
        }
        catch { }
        return "";
    }

    private void ResetMatchState()
    {
        hasActiveMatchRequest = false;
        currentMatchId = "";
    }

    public string GetCurrentMatchId() => currentMatchId;
    public string GetCurrentOpponentId() => currentOpponentId;
}