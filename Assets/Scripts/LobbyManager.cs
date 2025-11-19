using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

public class LobbyManager : MonoBehaviour
{
    [Serializable]
    public class PlayerData { public string id; public string name; public string status; }

    [Serializable]
    public class ServerResponse { public string @event; public List<PlayerData> data; }

    [Header("Referencias Obligatorias")]
    [SerializeField] public GameServerConnection gameServer;
    [SerializeField] public GameServerMatchmaking matchmaking;

    [Header("UI References")]
    public GameObject lobbyPanel;
    public Transform listContainer;
    public GameObject playerRowPrefab;
    public TextMeshProUGUI titleText;
    public Button cancelButton;

    [Header("Game Data")]
    public string myGameName = "Fennec Frenzy"; 

    private bool isLobbyActive = false;
    private string currentMatchId = "";

    void Start()
    {
        if (gameServer == null || matchmaking == null)
        {
            Debug.LogError("ERROR FATAL: Faltan referencias en LobbyManager.");
            return;
        }

        gameServer.OnServerMessageReceived += HandleServerMessage;
        matchmaking.OnMatchAccepted += (id, status) => OpenLobby();
        matchmaking.OnMatchStart += HandleMatchStart;

        if (cancelButton != null) cancelButton.onClick.AddListener(QuitLobby);
        lobbyPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (gameServer != null) gameServer.OnServerMessageReceived -= HandleServerMessage;
        if (matchmaking != null) matchmaking.OnMatchStart -= HandleMatchStart;
    }

    public void OpenLobby()
    {
        Debug.Log("OpenLobby llamado. Iniciando secuencia de entrada...");
        
        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) buscador.gameObject.SetActive(false);

        lobbyPanel.SetActive(true);
        currentMatchId = matchmaking.GetCurrentMatchId();
        
        // --- CAMBIO IMPORTANTE DE ORDEN ---
        
        // 1. PRIMERO: Avisar al servidor que entramos
        matchmaking.SendConnectMatch(currentMatchId);

        // 2. LUEGO: Activar la UI y pedir la lista (con un pequeño retraso)
        ActivateLobbyLogic(currentMatchId);
    }

    private async void ActivateLobbyLogic(string matchId)
    {
        if (isLobbyActive) return;

        isLobbyActive = true;
        Debug.Log($"Lobby Activado. MatchID: {matchId}");

        if(titleText) titleText.text = "Lobby";

        // Dibujar solo al jugador local al principio
        UpdateLobbyUI(new List<PlayerData>()); 
        
        if (cancelButton) cancelButton.interactable = true;
        
        // Esperar 0.5 segundos para asegurar que el servidor nos registró en la sala
        await Task.Delay(500);

        // AHORA SÍ pedimos la lista
        RefreshPlayerList();
        
        StartPingLoop();
    }

    private async void StartPingLoop()
    {
        while (isLobbyActive && this != null)
        {
            matchmaking.SendPingMatch(currentMatchId);
            RefreshPlayerList();
            await Task.Delay(3000);
        }
    }

    public void QuitLobby()
    {
        isLobbyActive = false;
        matchmaking.CancelMatchRequest();
        lobbyPanel.SetActive(false);
        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) { buscador.gameObject.SetActive(true); buscador.RequestPlayerList(); }
    }

    private void RefreshPlayerList() 
    { 
        if(gameServer.isLoggedIn) 
        {
            // Debug.Log("📤 Pidiendo lista actualizada...");
            gameServer.SendWebSocketMessage("{\"event\": \"online-players\"}"); 
        }
    }

    private void HandleServerMessage(string jsonMessage)
    {
        // --- SILENCIADOR DE ADVERTENCIAS ---
        // Si el lobby está "apagado" pero el panel está visible, nos auto-activamos
        if (!isLobbyActive && lobbyPanel.activeInHierarchy) isLobbyActive = true;
        
        if (!isLobbyActive) return; // Si sigue apagado, ignoramos silenciosamente

        if (jsonMessage.Contains("online-players"))
        {
            try 
            { 
                ServerResponse r = JsonUtility.FromJson<ServerResponse>(jsonMessage); 
                if (r != null && r.data != null) 
                {
                    UpdateLobbyUI(r.data); 
                }
            } 
            catch { }
        }
        
        if (jsonMessage.Contains("match-canceled") || jsonMessage.Contains("match-rejected")) QuitLobby();
        if (jsonMessage.Contains("players-ready") || jsonMessage.Contains("\"type\":\"ready\"")) RefreshPlayerList();
    }

    public void UpdateLobbyUI(List<PlayerData> remotePlayers)
    {
        foreach (Transform child in listContainer) Destroy(child.gameObject);

        CreateRow(gameServer.playerName, myGameName, true, "No listo");

        foreach (var player in remotePlayers)
        {
            if (player.name == gameServer.playerName) continue;
            
            // Sin filtros, dibujamos todo lo que llegue
            CreateRow(player.name, player.status, false, "Esperando...");
        }
    }

    private void CreateRow(string name, string info, bool isLocal, string statusBtn)
    {
        GameObject rowObj = Instantiate(playerRowPrefab, listContainer);
        PlayerRowUI rowUI = rowObj.GetComponent<PlayerRowUI>();
        if (rowUI != null)
        {
            rowUI.infoText.text = $"User: {name} | {info}";
            rowUI.buttonText.text = statusBtn;
            if (isLocal)
            {
                rowUI.readyButton.interactable = true;
                rowUI.readyButton.onClick.RemoveAllListeners();
                rowUI.readyButton.onClick.AddListener(() => ToggleReady(rowUI));
                rowUI.infoText.color = Color.yellow;
            }
            else rowUI.readyButton.interactable = false;
        }
    }

    private void ToggleReady(PlayerRowUI rowUI)
    {
        bool isNowReady = rowUI.buttonText.text == "No listo";
        rowUI.buttonText.text = isNowReady ? "Listo" : "No listo";
        string payload = $@"{{""event"": ""players-ready"", ""data"": {{ ""matchId"": ""{currentMatchId}"", ""ready"": {isNowReady.ToString().ToLower()} }} }}";
        gameServer.SendWebSocketMessage(payload);
    }

    private void HandleMatchStart(string message) 
    { 
        isLobbyActive = false; 
        if(titleText) titleText.text = "INICIANDO..."; 
    }
}
