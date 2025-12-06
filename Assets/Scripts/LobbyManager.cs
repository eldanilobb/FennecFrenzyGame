using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Serializable]
    public class PlayerData { public string id; public string name; public string status; }

    [Serializable]
    public class ServerResponse { public string @event; public List<PlayerData> data; }

    [Serializable]
    public class GamePayload 
    { 
        public string type; 
        public bool close; 
    }

    [Header("UI References")]
    public GameObject lobbyPanel;
    public Transform listContainer;
    public GameObject playerRowPrefab;
    public TextMeshProUGUI titleText;
    public Button cancelButton;

    [Header("Game Data")]
    public string myGameName = "Fennec Frenzy"; 
    public string gameSceneName = "NivelOnline"; 

    private GameServerConnection gameServer;
    private GameServerMatchmaking matchmaking;

    private bool amIReady = false;
    private bool isLobbyActive = false;
    private bool isStarting = false;
    private string currentMatchId = "";

    // Diccionario para saber quién está listo (Nombre -> Bool)
    private Dictionary<string, bool> remoteReadyStates = new Dictionary<string, bool>();
    
    // Diccionario traductor (ID -> Nombre) para los Pings
    private Dictionary<string, string> playerIdsToNames = new Dictionary<string, string>();

    void Start()
    {
        gameServer = GameServerConnection.Instance;
        if (gameServer == null) gameServer = FindFirstObjectByType<GameServerConnection>();

        matchmaking = FindFirstObjectByType<GameServerMatchmaking>();

        if (gameServer == null || matchmaking == null) return;

        // Limpieza inicial por si venimos de otra escena
        currentMatchId = ""; 
        amIReady = false;
        remoteReadyStates.Clear();
        playerIdsToNames.Clear();

        // Suscripciones
        gameServer.OnServerMessageReceived += HandleServerMessage;
        matchmaking.OnMatchAccepted += HandleMatchAccepted;
        
        matchmaking.OnPingReceived += HandlePingReceived;
        matchmaking.OnCustomReadyReceived += HandleGameData;
        matchmaking.OnPlayersReady += (msg) => OpenLobby();
        matchmaking.OnMatchStart += HandleMatchStart;

        if (cancelButton != null) cancelButton.onClick.AddListener(() => QuitLobby(true));
        
        if (lobbyPanel) lobbyPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (gameServer != null) gameServer.OnServerMessageReceived -= HandleServerMessage;
        
        if (matchmaking != null) 
        {
            matchmaking.OnMatchAccepted -= HandleMatchAccepted;
            matchmaking.OnPingReceived -= HandlePingReceived;
            matchmaking.OnCustomReadyReceived -= HandleGameData;
            matchmaking.OnMatchStart -= HandleMatchStart;
        }
    }

    // --- CONEXIÓN AL LOBBY ---

    private void HandleMatchAccepted(string id, string status)
    {
        if (this == null || gameObject == null) return;
        StartCoroutine(RutinaConexionInicial());
    }

    private IEnumerator RutinaConexionInicial()
    {
        // Espera de seguridad (igual que Godot)
        yield return new WaitForSeconds(0.5f);
        
        currentMatchId = matchmaking.GetCurrentMatchId();
        matchmaking.SendConnectMatch(currentMatchId);
    }

    public void OpenLobby()
    {
        if (isLobbyActive) return;
        StartCoroutine(RutinaAbrirLobby());
    }

    private IEnumerator RutinaAbrirLobby()
    {
        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) buscador.gameObject.SetActive(false);

        // Espera para sincronizar UI
        yield return new WaitForSeconds(0.8f);
        
        if (this == null) yield break;

        lobbyPanel.SetActive(true);
        ActivateLobbyLogic(currentMatchId);
    }

    private void ActivateLobbyLogic(string matchId)
    {
        isLobbyActive = true;
        isStarting = false;
        if(titleText) titleText.text = "Lobby";
        
        amIReady = false;
        remoteReadyStates.Clear();
        playerIdsToNames.Clear();

        UpdateLobbyUI(new List<PlayerData>()); 
        if (cancelButton) cancelButton.interactable = true;
        
        StartCoroutine(RutinaRefrescoInicial());
    }

    private IEnumerator RutinaRefrescoInicial()
    {
        yield return new WaitForSeconds(0.3f);
        RefreshPlayerList();
    }

    // --- SALIR DEL LOBBY ---

    public void QuitLobby(bool notifyRival = true)
    {
        isLobbyActive = false;
        isStarting = false;

        if (!string.IsNullOrEmpty(currentMatchId) && notifyRival)
        {
            matchmaking.SendLeaveMatch(currentMatchId);
        }

        lobbyPanel.SetActive(false);
        remoteReadyStates.Clear();
        playerIdsToNames.Clear();
        amIReady = false;
        currentMatchId = "";

        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) 
        { 
            buscador.gameObject.SetActive(true); 
            Invoke(nameof(RequestListLate), 1.0f);
        }
        else SceneManager.LoadScene("Online");
    }

    private void RequestListLate()
    {
        if (this == null) return;
        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) buscador.RequestPlayerList();
    }

    private void RefreshPlayerList() 
    { 
        if(gameServer != null && gameServer.isLoggedIn) 
            gameServer.SendWebSocketMessage("{\"event\": \"online-players\"}"); 
    }

    // --- MENSAJES DEL SERVIDOR ---

    private void HandleServerMessage(string jsonMessage)
    {
        if (this == null || !isLobbyActive) return; 

        if (jsonMessage.Contains("online-players"))
        {
            try { 
                ServerResponse r = JsonUtility.FromJson<ServerResponse>(jsonMessage); 
                if (r != null && r.data != null) UpdateLobbyUI(r.data); 
            } catch { }
        }
        
        if (jsonMessage.Contains("match-canceled") || jsonMessage.Contains("match-rejected")) QuitLobby(false);
        if (jsonMessage.Contains("close-match")) QuitLobby(false);
    }

    // --- PING / READY ---

    private void ToggleReady(PlayerRowUI rowUI)
    {
        amIReady = !amIReady; 
        rowUI.buttonText.text = amIReady ? "Listo" : "No listo";
        rowUI.readyButton.GetComponent<Image>().color = amIReady ? Color.green : Color.white;

        if (amIReady)
        {
            matchmaking.SendPingMatch(currentMatchId);
        }
        
        CheckIfAllReady();
    }

    private void HandlePingReceived(string fullJson)
    {
        string senderId = ExtractValue(fullJson, "playerId");
        
        // Si no soy yo, es el rival
        if (senderId != gameServer.playerName)
        {
            // Traducimos ID a Nombre
            if (playerIdsToNames.ContainsKey(senderId))
            {
                string rivalName = playerIdsToNames[senderId];
                
                if (remoteReadyStates.ContainsKey(rivalName))
                    remoteReadyStates[rivalName] = true;
                else
                    remoteReadyStates.Add(rivalName, true);

                RefreshReadyUI();
                CheckIfAllReady();
            }
            else
            {
                // Si no tenemos el nombre, refrescamos lista
                RefreshPlayerList();
            }
        }
    }

    private void HandleGameData(string jsonPayload)
    {
        try {
            GamePayload data = JsonUtility.FromJson<GamePayload>(jsonPayload);
            if (data != null && data.close == true) QuitLobby(false);
        } catch {}
    }

    // --- ACTUALIZACIÓN DE UI ---

    public void UpdateLobbyUI(List<PlayerData> remotePlayers)
    {
        if (listContainer == null) return;
        foreach (Transform child in listContainer) Destroy(child.gameObject);

        // Fila Local
        string myStatusText = amIReady ? "Listo" : "No listo";
        CreateRow(gameServer.playerName, myGameName, true, myStatusText);
        if (amIReady && listContainer.childCount > 0)
        {
             var row = listContainer.GetChild(listContainer.childCount - 1).GetComponent<PlayerRowUI>();
             if(row) row.readyButton.GetComponent<Image>().color = Color.green;
        }

        // Fila Rival
        string myOpponentId = matchmaking.GetCurrentOpponentId();
        foreach (var player in remotePlayers)
        {
            if (player.name == gameServer.playerName) continue;
            if (player.id != myOpponentId && matchmaking.GetCurrentMatchId() == "") continue; 
            
            // Guardamos ID -> Nombre para el Ping
            if (!playerIdsToNames.ContainsKey(player.id))
            {
                playerIdsToNames.Add(player.id, player.name);
            }

            bool isRemoteReady = remoteReadyStates.ContainsKey(player.name) && remoteReadyStates[player.name];
            string statusString = isRemoteReady ? "Listo" : "Esperando...";

            CreateRow(player.name, player.status, false, statusString);
            
            if (isRemoteReady)
            {
                var row = listContainer.GetChild(listContainer.childCount - 1).GetComponent<PlayerRowUI>();
                if (row) {
                    row.readyButton.GetComponent<Image>().color = Color.green;
                    row.buttonText.text = "Listo";
                }
            }
        }
        CheckIfAllReady();
    }

    private void CreateRow(string name, string info, bool isLocal, string statusBtn)
    {
        GameObject rowObj = Instantiate(playerRowPrefab, listContainer);
        PlayerRowUI rowUI = rowObj.GetComponent<PlayerRowUI>();
        if (rowUI != null)
        {
            rowUI.infoText.text = $"User: {name} | {info}";
            rowUI.buttonText.text = statusBtn;
            
            if (isLocal) {
                rowUI.readyButton.interactable = true;
                rowUI.readyButton.onClick.RemoveAllListeners();
                rowUI.readyButton.onClick.AddListener(() => ToggleReady(rowUI));
                rowUI.infoText.color = Color.yellow;
            } else rowUI.readyButton.interactable = false;
        }
    }

    private void RefreshReadyUI()
    {
        foreach (Transform child in listContainer)
        {
            var row = child.GetComponent<PlayerRowUI>();
            foreach(var kvp in remoteReadyStates) {
                if (row.infoText.text.Contains(kvp.Key) && kvp.Value == true) {
                    row.readyButton.GetComponent<Image>().color = Color.green;
                    row.buttonText.text = "Listo";
                }
            }
        }
    }

    private void CheckIfAllReady()
    {
        if (listContainer.childCount < 2) return;
        bool allReady = true;
        foreach (Transform child in listContainer) {
            var row = child.GetComponent<PlayerRowUI>();
            if (row.buttonText.text != "Listo") {
                allReady = false; 
                break;
            }
        }
        if (allReady) HandleMatchStart("Start local");
    }

    private void HandleMatchStart(string message) 
    { 
        if (this == null || isStarting) return;
        
        isStarting = true;
        isLobbyActive = false; 
        
        if(titleText) titleText.text = "INICIANDO...";
        StartCoroutine(LoadSceneDelayed());
    }

    private IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene(gameSceneName);
    }

    private string ExtractValue(string json, string key) {
        try {
            string pattern = $"\"{key}\""; 
            int keyIdx = json.IndexOf(pattern);
            if (keyIdx == -1) return "";
            int valStart = json.IndexOf("\"", keyIdx + pattern.Length + 1);
            int valEnd = json.IndexOf("\"", valStart + 1);
            return json.Substring(valStart + 1, valEnd - valStart - 1);
        } catch { return ""; }
    }
}