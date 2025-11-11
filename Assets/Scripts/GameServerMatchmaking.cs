using System;
using UnityEngine;

public class GameServerMatchmaking : MonoBehaviour
{
    private GameServerConnection gameServer;
    
    // Estados del matchmaking
    private string currentMatchId = "";
    private string currentOpponentId = "";
    private bool hasActiveMatchRequest = false;
    
    // Eventos
    public event Action<string> OnMatchRequestSent;
    public event Action<string, string> OnMatchRequestReceived;
    public event Action<string, string> OnMatchAccepted;
    public event Action<string> OnMatchRejected;
    public event Action<string> OnMatchCanceledBySender;
    public event Action OnMatchCanceled;
    public event Action<string> OnMatchRequestError;

    void Start()
    {
        gameServer = FindFirstObjectByType<GameServerConnection>();
    }

    public void SendMatchRequest(string targetPlayerId)
    {
        if (!gameServer.isLoggedIn)
        {
            Debug.LogWarning("No estás logueado");
            return;
        }

        SendEvent("send-match-request", $"\"playerId\":\"{targetPlayerId}\"");
        Debug.Log($"Solicitud enviada a: {targetPlayerId}");
    }

    public void CancelMatchRequest()
    {
        if (!hasActiveMatchRequest)
        {
            Debug.LogWarning("No tienes una solicitud activa");
            return;
        }

        SendEvent("cancel-match-request", "");
        Debug.Log("Solicitud cancelada");
    }

    public void AcceptMatchRequest()
    {
        SendEvent("accept-match", "");
        Debug.Log("Solicitud aceptada");
    }

    public void RejectMatchRequest()
    {
        SendEvent("reject-match", "");
        Debug.Log("Solicitud rechazada");
    }

    private void SendEvent(string eventName, string data)
    {
        string dataField = string.IsNullOrEmpty(data) ? "" : data;
        string json = $"{{\"event\":\"{eventName}\",\"data\":{{{dataField}}}}}";
        gameServer.SendWebSocketMessage(json);
    }

    public void ProcessMatchmakingEvent(string eventName, string message)
    {
        switch (eventName)
        {
            case "send-match-request":
                HandleSendMatchRequestResponse(message);
                break;
            case "match-request-received":
                HandleMatchRequestReceived(message);
                break;
            case "match-accepted":
                HandleMatchAccepted(message);
                break;
            case "match-rejected":
                HandleMatchRejected(message);
                break;
            case "match-canceled-by-sender":
                HandleMatchCanceledBySender(message);
                break;
            case "cancel-match-request":
                HandleResponse(message, "Solicitud cancelada", () => {
                    OnMatchCanceled?.Invoke();
                    hasActiveMatchRequest = false;
                });
                break;
            case "accept-match":
                HandleAcceptMatchResponse(message);
                break;
            case "reject-match":
                HandleResponse(message, "Solicitud rechazada", () => {
                    OnMatchRejected?.Invoke(currentOpponentId);
                    ResetMatchState();
                });
                break;
        }
    }

    private void HandleSendMatchRequestResponse(string message)
    {
        if (message.Contains("\"status\":\"OK\""))
        {
            hasActiveMatchRequest = true;
            currentMatchId = ExtractValue(message, "matchId");
            Debug.Log($"Solicitud enviada. Match ID: {currentMatchId}");
            OnMatchRequestSent?.Invoke(currentMatchId);
        }
        else
        {
            Debug.LogError($"Error enviando solicitud: {message}");
            OnMatchRequestError?.Invoke(message);
        }
    }

    private void HandleMatchRequestReceived(string message)
    {
        try
        {
            currentOpponentId = ExtractValue(message, "playerId");
            currentMatchId = ExtractValue(message, "matchId");
            hasActiveMatchRequest = true;

            Debug.Log($"Solicitud recibida de: {currentOpponentId}");
            OnMatchRequestReceived?.Invoke(currentOpponentId, currentMatchId);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error procesando solicitud: {ex.Message}");
        }
    }

    private void HandleMatchAccepted(string message)
    {
        try
        {
            currentMatchId = ExtractValue(message, "matchId");
            string matchStatus = ExtractValue(message, "matchStatus");

            Debug.Log($"Partida aceptada! Match ID: {currentMatchId}, Status: {matchStatus}");
            OnMatchAccepted?.Invoke(currentMatchId, matchStatus);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error procesando aceptación: {ex.Message}");
        }
    }

    private void HandleMatchRejected(string message)
    {
        try
        {
            string rejectingPlayerId = ExtractValue(message, "playerId");
            Debug.Log($"Solicitud rechazada por: {rejectingPlayerId}");
            OnMatchRejected?.Invoke(rejectingPlayerId);
            ResetMatchState();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error procesando rechazo: {ex.Message}");
        }
    }

    private void HandleMatchCanceledBySender(string message)
    {
        try
        {
            string cancelingPlayerId = ExtractValue(message, "playerId");
            Debug.Log($"Solicitud cancelada por: {cancelingPlayerId}");
            OnMatchCanceledBySender?.Invoke(cancelingPlayerId);
            ResetMatchState();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error procesando cancelación: {ex.Message}");
        }
    }

    private void HandleAcceptMatchResponse(string message)
    {
        if (message.Contains("\"status\":\"OK\""))
        {
            try
            {
                currentMatchId = ExtractValue(message, "matchId");
                Debug.Log($"Partida aceptada! Match ID: {currentMatchId}");
                OnMatchAccepted?.Invoke(currentMatchId, "WAITING_PLAYERS");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogError($"Error aceptando: {message}");
        }
    }

    private void HandleResponse(string message, string successMsg, Action onSuccess)
    {
        if (message.Contains("\"status\":\"OK\""))
        {
            Debug.Log(successMsg);
            onSuccess?.Invoke();
        }
        else
        {
            Debug.LogError($"Error: {message}");
        }
    }

    private string ExtractValue(string json, string key)
    {
        int startIndex = json.IndexOf($"\"{key}\":\"") + key.Length + 4;
        int endIndex = json.IndexOf("\"", startIndex);
        return json.Substring(startIndex, endIndex - startIndex);
    }

    private void ResetMatchState()
    {
        hasActiveMatchRequest = false;
        currentMatchId = "";
    }

    // Getters
    public string GetCurrentMatchId() => currentMatchId;
    public string GetCurrentOpponentId() => currentOpponentId;
    public bool HasActiveMatchRequest() => hasActiveMatchRequest;
}
