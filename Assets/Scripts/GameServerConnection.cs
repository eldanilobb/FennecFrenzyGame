using System;
using System.Collections;
using UnityEngine;
using System.Net.WebSockets;
using System.Threading;

public class GameServerConnection : MonoBehaviour
{
    public string serverUrl = "ws://cross-game-ucn.martux.cl:4010";
    public string gameId = "B";
    public string playerName = "Fennec"; 
    
    private ClientWebSocket websocket;
    public bool isLoggedIn = false;
    private CancellationTokenSource cancellationTokenSource;
    
    [Header("Referencias Obligatorias")]
    [SerializeField] private GameServerMatchmaking matchmakingSystem;

    public event Action<string> OnServerMessageReceived; 

    void Start() 
    { 
        if (playerName == "Fennec")
        {
            playerName = "Fennec_" + UnityEngine.Random.Range(100, 999);
        }
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

        if (connectTask.IsCompletedSuccessfully)
        {
            Debug.Log("Conectado. Enviando Login...");
            StartCoroutine(ReceiveMessages());
            SendWebSocketMessage("{\"event\":\"login\",\"data\":{\"gameKey\":\"V832E2HO8X\"}}");
        }
        else
        {
            Debug.LogError($"Error: {connectTask.Exception}");
        }
    }

    private IEnumerator ReceiveMessages()
    {
        byte[] buffer = new byte[1024 * 4];
        while (websocket.State == WebSocketState.Open)
        {
            var receiveTask = websocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            yield return new WaitUntil(() => receiveTask.IsCompleted);
            
            if (receiveTask.Result.MessageType == WebSocketMessageType.Text)
            {
                string msg = System.Text.Encoding.UTF8.GetString(buffer, 0, receiveTask.Result.Count);
                ProcessReceivedMessage(msg);
            }
        }
    }

    private void ProcessReceivedMessage(string message)
    {
        Debug.Log($"Recibido:{message}"); 

        OnServerMessageReceived?.Invoke(message);

        if (message.Contains("connected-to-server")) { }
        else if (message.Contains("\"event\":\"login\"") || message.Contains("event\":\"login")) HandleLoginResponse(message);
        else if (message.Contains("online-players") || message.Contains("player-connected")) { }
        
        else if (message.Contains("match") || message.Contains("connect") || message.Contains("ping") || message.Contains("ready"))
        {
            string eventName = QuickExtractEvent(message);
            if (!string.IsNullOrEmpty(eventName))
            {
                HandleMatchmakingEvent(eventName, message);
            }
        }
    }

    private string QuickExtractEvent(string json)
    {
        try {
            if (json.Contains("\"event\"")) {
                string[] parts = json.Split(new string[] { "\"event\"" }, StringSplitOptions.None);
                if (parts.Length > 1) {
                    int firstQuote = parts[1].IndexOf("\"");
                    int secondQuote = parts[1].IndexOf("\"", firstQuote + 1);
                    if (firstQuote != -1 && secondQuote != -1)
                        return parts[1].Substring(firstQuote + 1, secondQuote - firstQuote - 1);
                }
            }
            return "";
        } catch { return ""; }
    }

    private void HandleMatchmakingEvent(string eventName, string message)
    {
        if (matchmakingSystem != null)
        {
            matchmakingSystem.ProcessMatchmakingEvent(eventName, message);
        }
        else
        {
            Debug.LogError("GameServerConnection NO TIENE REFERENCIA a GameServerMatchmaking");
        }
    }

    private void HandleLoginResponse(string message)
    {
        if (message.Contains("OK")) { isLoggedIn = true; Debug.Log("Login OK"); }
    }

    public void SendWebSocketMessage(string message)
    {
        if (websocket == null || websocket.State != WebSocketState.Open) {
            Debug.LogError("No conectado al enviar.");
            return;
        }
        Debug.Log($"Enviando: {message}");
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
        websocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    void OnDestroy() { websocket?.Dispose(); cancellationTokenSource?.Cancel(); }
}