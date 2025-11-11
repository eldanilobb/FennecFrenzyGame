using System;
using System.Collections;
using UnityEngine;
using System.Net.WebSockets;
using System.Threading;

public class GameServerConnection : MonoBehaviour
{
    // Configuración
    public string serverUrl = "ws://cross-game-ucn.martux.cl:4010";
    public string gameId = "B";
    public string playerName = "Fennec";
    
    // Estados
    private ClientWebSocket websocket;
    public bool isLoggedIn = false;
    private float lastActivityTime = 0f;
    private const float PING_INTERVAL = 30f;
    private CancellationTokenSource cancellationTokenSource;
    
    // Eventos del servidor
    private GameServerMatchmaking matchmakingSystem;

    void Start()
    {
        ConnectToServer();
    }

    void ConnectToServer()
    {
        cancellationTokenSource = new CancellationTokenSource();
        websocket = new ClientWebSocket();
        
        string fullUrl = $"{serverUrl}/?gameId={gameId}&playerName={playerName}";
        Debug.Log($"Conectando a: {fullUrl}");
        
        StartCoroutine(ConnectToServerCoroutine(fullUrl));
    }

    private IEnumerator ConnectToServerCoroutine(string fullUrl)
    {
        var connectTask = websocket.ConnectAsync(new Uri(fullUrl), cancellationTokenSource.Token);
        yield return new WaitUntil(() => connectTask.IsCompleted);

        try
        {
            if (connectTask.IsCompletedSuccessfully)
            {
                Debug.Log("Conectado al servidor!");
                StartCoroutine(ReceiveMessages());
                SendLoginRequest();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error conectando: {ex.Message}");
        }
    }

    private IEnumerator ReceiveMessages()
    {
        byte[] buffer = new byte[1024 * 4];

        while (websocket.State == WebSocketState.Open)
        {
            var receiveTask = websocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), 
                CancellationToken.None
            );

            yield return new WaitUntil(() => receiveTask.IsCompleted);
            ProcessReceivedMessage(receiveTask, buffer);
        }

        Debug.LogWarning("Conexión cerrada");
        isLoggedIn = false;
    }

    private void ProcessReceivedMessage(System.Threading.Tasks.Task<WebSocketReceiveResult> receiveTask, byte[] buffer)
    {
        try
        {
            if (receiveTask.IsCompletedSuccessfully)
            {
                var result = receiveTask.Result;
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = System.Text.Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Debug.Log($"Mensaje recibido: {message}");
                    ProcessServerMessage(message);
                    lastActivityTime = Time.time;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error recibiendo: {ex.Message}");
        }
    }

    private void ProcessServerMessage(string message)
    {
        if (message.Contains("\"event\":\"connected-to-server\""))
        {
            Debug.Log("Conectado al servidor, esperando login...");
        }
        else if (message.Contains("\"event\":\"login\""))
        {
            HandleLoginResponse(message);
        }
        else if (IsMatchmakingEvent(message))
        {
            HandleMatchmakingEvent(message);
        }
        else
        {
            Debug.LogWarning($"Evento no manejado: {message}");
        }
    }


    private void HandleLoginResponse(string message)
    {
        if (message.Contains("\"status\":\"OK\""))
        {
            isLoggedIn = true;
            Debug.Log("LOGIN EXITOSO!");
        }
        else
        {
            Debug.LogError($"Login fallido: {message}");
        }
    }

    private bool IsMatchmakingEvent(string message)
    {
        return message.Contains("\"event\":\"send-match-request\"") ||
               message.Contains("\"event\":\"match-request-received\"") ||
               message.Contains("\"event\":\"match-accepted\"") ||
               message.Contains("\"event\":\"match-rejected\"") ||
               message.Contains("\"event\":\"match-canceled-by-sender\"") ||
               message.Contains("\"event\":\"cancel-match-request\"") ||
               message.Contains("\"event\":\"accept-match\"") ||
               message.Contains("\"event\":\"reject-match\"");
    }

    private void HandleMatchmakingEvent(string message)
    {
        if (matchmakingSystem == null)
            matchmakingSystem = FindFirstObjectByType<GameServerMatchmaking>();

        string eventName = ExtractJsonValue(message, "event");
        matchmakingSystem?.ProcessMatchmakingEvent(eventName, message);
    }

    private string ExtractJsonValue(string json, string key)
    {
        int startIndex = json.IndexOf($"\"{key}\":\"") + key.Length + 4;
        int endIndex = json.IndexOf("\"", startIndex);
        return json.Substring(startIndex, endIndex - startIndex);
    }

    private void SendLoginRequest()
    {
        string loginJson = "{\"event\":\"login\",\"data\":{\"gameKey\":\"V832E2HO8X\"}}";
        Debug.Log("Enviando login con gameKey: V832E2HO8X");
        SendWebSocketMessage(loginJson);
    }

    public void SendWebSocketMessage(string message)
    {
        if (websocket == null || websocket.State != WebSocketState.Open)
        {
            Debug.LogWarning("WebSocket no está conectado");
            return;
        }

        try
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
            var sendTask = websocket.SendAsync(
                new ArraySegment<byte>(buffer),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None
            );

            while (!sendTask.IsCompleted) { }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error enviando: {ex.Message}");
        }
    }

    void Update()
    {
        if (isLoggedIn && Time.time - lastActivityTime > PING_INTERVAL)
            SendWebSocketMessage("{\"event\":\"ping\",\"data\":{}}");
    }

    void OnDestroy()
    {
        websocket?.Dispose();
        cancellationTokenSource?.Cancel();
    }
}
