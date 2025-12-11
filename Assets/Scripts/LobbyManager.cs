using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    // --- CLASES PARA EL JSON ---
    [Serializable]
    public class PlayerData { public string id; public string name; public string status; }

    [Serializable]
    public class ServerResponse { public string @event; public List<PlayerData> data; }

    [Serializable]
    public class GamePayload { public string type; public bool close; }

    [Serializable]
    public class PingRoot { public string @event; public PingData data; }
    [Serializable]
    public class PingData { public string playerId; public string matchId; }


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
    
    private HashSet<string> readyPlayerIds = new HashSet<string>();
    private Dictionary<string, string> playerIdsToNames = new Dictionary<string, string>();

    void Start()
    {
        gameServer = GameServerConnection.Instance;
        if (gameServer == null) gameServer = FindFirstObjectByType<GameServerConnection>();

        matchmaking = FindFirstObjectByType<GameServerMatchmaking>();

        if (gameServer == null || matchmaking == null) return;

        currentMatchId = ""; 
        amIReady = false;
        readyPlayerIds.Clear();
        playerIdsToNames.Clear();

        gameServer.OnServerMessageReceived += HandleServerMessage;
        matchmaking.OnMatchAccepted += HandleMatchAccepted;
        matchmaking.OnPingReceived += HandlePingReceived;
        matchmaking.OnCustomReadyReceived += HandleGameData;
        matchmaking.OnMatchStart += HandleMatchStart;
        matchmaking.OnPlayersReady += HandlePlayersReady; 

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
            matchmaking.OnPlayersReady -= HandlePlayersReady;
        }
    }

    private void HandlePlayersReady(string msg)
    {
        if (this == null || gameObject == null) return;
        OpenLobby();
    }

    private void HandleMatchAccepted(string id, string status)
    {
        if (this == null || gameObject == null) return;
        StartCoroutine(RutinaConexionInicial());
    }

    private IEnumerator RutinaConexionInicial()
    {
        yield return new WaitForSeconds(0.5f);
        currentMatchId = matchmaking.GetCurrentMatchId();
        matchmaking.SendConnectMatch(currentMatchId);
    }

    public void OpenLobby()
    {
        if (isLobbyActive) return;
        if (this == null || !gameObject.activeInHierarchy) return;

        StartCoroutine(RutinaAbrirLobby());
    }

    private IEnumerator RutinaAbrirLobby()
    {
        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) buscador.gameObject.SetActive(false);

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
        readyPlayerIds.Clear();
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

    public void QuitLobby(bool notifyRival = true)
    {
        StartCoroutine(RutinaSalidaSegura(notifyRival));
    }

    private IEnumerator RutinaSalidaSegura(bool notify)
    {
        isLobbyActive = false;
        isStarting = false;

        if (!string.IsNullOrEmpty(currentMatchId) && notify)
        {
            matchmaking.SendLeaveMatch(currentMatchId);
        }

        yield return new WaitForSeconds(0.2f);

        if (gameServer != null) 
        {
            gameServer.ForceDisconnect(); 
        }

        lobbyPanel.SetActive(false);
        
        readyPlayerIds.Clear(); 
        
        playerIdsToNames.Clear();
        amIReady = false;
        currentMatchId = "";

        SceneManager.LoadScene("Online");
    }

    private void RefreshPlayerList() 
    { 
        if(gameServer != null && gameServer.isLoggedIn) 
            gameServer.SendWebSocketMessage("{\"event\": \"online-players\"}"); 
    }

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
        try 
        {
            PingRoot ping = JsonUtility.FromJson<PingRoot>(fullJson);
            
            if (ping != null && ping.data != null)
            {
                string senderId = ping.data.playerId;
                
                if (!string.IsNullOrEmpty(senderId) && senderId != gameServer.playerName)
                {
                    if (!readyPlayerIds.Contains(senderId))
                    {
                        readyPlayerIds.Add(senderId);
                    }
                    RefreshReadyUI();
                    CheckIfAllReady();
                }
            }
        }
        catch (Exception e) 
        {
            Debug.LogError("Error leyendo ping: " + e.Message);
        }
    }

    private void HandleGameData(string jsonPayload)
    {
        try {
            GamePayload data = JsonUtility.FromJson<GamePayload>(jsonPayload);
            if (data != null && data.close == true) QuitLobby(false);
        } catch {}
    }

   public void UpdateLobbyUI(List<PlayerData> remotePlayers)
    {
        if (listContainer == null) return;
        foreach (Transform child in listContainer) Destroy(child.gameObject);

        string myStatusText = amIReady ? "Listo" : "No listo";
        CreateRow(gameServer.playerName, myGameName, true, myStatusText);
        if (amIReady && listContainer.childCount > 0)
        {
             var row = listContainer.GetChild(listContainer.childCount - 1).GetComponent<PlayerRowUI>();
             if(row) row.readyButton.GetComponent<Image>().color = Color.green;
        }

        string myOpponentId = matchmaking.GetCurrentOpponentId();
        string currentMatch = matchmaking.GetCurrentMatchId();

        foreach (var player in remotePlayers)
        {
            if (player.name == gameServer.playerName) continue;

            if (player.id != myOpponentId) continue; 
            
            if (!playerIdsToNames.ContainsKey(player.id))
                playerIdsToNames.Add(player.id, player.name);

            bool isRemoteReady = readyPlayerIds.Contains(player.id);
            string statusString = isRemoteReady ? "Listo" : "Esperando...";

            CreateRow(player.name, player.status, false, statusString);
            
            var rowObj = listContainer.GetChild(listContainer.childCount - 1);
            rowObj.name = player.id; 

            if (isRemoteReady)
            {
                var row = rowObj.GetComponent<PlayerRowUI>();
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
            string rowId = child.name;
            if (readyPlayerIds.Contains(rowId))
            {
                var row = child.GetComponent<PlayerRowUI>();
                if (row) {
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
}