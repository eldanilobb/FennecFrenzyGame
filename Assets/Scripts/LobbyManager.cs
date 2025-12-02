using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Serializable]
    public class PlayerData { public string id; public string name; public string status; }

    [Serializable]
    public class ServerResponse { public string @event; public List<PlayerData> data; }

    [Serializable]
    public class CustomReadyPayload 
    { 
        public string type; 
        public string playerId; 
        public bool isReady; 
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
    public string gameSceneName = "OnlineMatch"; 

    private GameServerConnection gameServer;
    private GameServerMatchmaking matchmaking;

    private bool amIReady = false;
    private bool isLobbyActive = false;
    private string currentMatchId = "";

    private Dictionary<string, bool> remoteReadyStates = new Dictionary<string, bool>();

    void Start()
    {
        gameServer = GameServerConnection.Instance;
        if (gameServer == null) gameServer = FindFirstObjectByType<GameServerConnection>();

        matchmaking = FindFirstObjectByType<GameServerMatchmaking>();

        if (gameServer == null || matchmaking == null) return;

        gameServer.OnServerMessageReceived += HandleServerMessage;
        matchmaking.OnMatchAccepted += HandleMatchAccepted;
        matchmaking.OnCustomReadyReceived += HandleCustomReady;

        if (cancelButton != null) cancelButton.onClick.AddListener(() => QuitLobby(true));
        lobbyPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (gameServer != null) gameServer.OnServerMessageReceived -= HandleServerMessage;
        
        if (matchmaking != null) 
        {
            matchmaking.OnMatchAccepted -= HandleMatchAccepted;
            matchmaking.OnCustomReadyReceived -= HandleCustomReady;
        }
    }

    private void HandleMatchAccepted(string id, string status)
    {
        if (this == null || gameObject == null) return;
        OpenLobby();
    }

    public void OpenLobby()
    {
        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) buscador.gameObject.SetActive(false);

        lobbyPanel.SetActive(true);
        currentMatchId = matchmaking.GetCurrentMatchId();
        
        remoteReadyStates.Clear();
        amIReady = false;

        matchmaking.SendConnectMatch(currentMatchId);
        ActivateLobbyLogic(currentMatchId);
    }

    private async void ActivateLobbyLogic(string matchId)
    {
        if (isLobbyActive) return;

        isLobbyActive = true;
        if(titleText) titleText.text = "Lobby";

        UpdateLobbyUI(new List<PlayerData>()); 
        
        if (cancelButton) cancelButton.interactable = true;
        
        await Task.Delay(500);
        if (this == null) return; 

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

    public void QuitLobby(bool notifyRival = true)
    {
        isLobbyActive = false;

        if (!string.IsNullOrEmpty(currentMatchId))
        {
            if (notifyRival)
            {
                string closePayload = $@"{{
                    ""event"": ""send-game-data"", 
                    ""data"": {{ 
                        ""matchId"": ""{currentMatchId}"", 
                        ""payload"": {{ 
                            ""type"": ""lobby-close"", 
                            ""playerId"": ""{gameServer.playerName}"", 
                            ""close"": true 
                        }} 
                    }} 
                }}";
                gameServer.SendWebSocketMessage(closePayload);
            }

            matchmaking.SendFinishMatch(currentMatchId, gameServer.playerName);
            matchmaking.SendLeaveMatch(currentMatchId);
        }

        lobbyPanel.SetActive(false);
        remoteReadyStates.Clear();
        amIReady = false;
        currentMatchId = "";

        var buscador = FindFirstObjectByType<AutoPlayerSearch>();
        if (buscador) 
        { 
            buscador.gameObject.SetActive(true); 
            Invoke(nameof(RequestListLate), 1.0f);
        }
        else
        {
             SceneManager.LoadScene("Online");
        }
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

    private void HandleServerMessage(string jsonMessage)
    {
        if (this == null) return;
        
        if (!isLobbyActive && lobbyPanel.activeInHierarchy) isLobbyActive = true;
        if (!isLobbyActive) return; 

        if (jsonMessage.Contains("online-players"))
        {
            try 
            { 
                ServerResponse r = JsonUtility.FromJson<ServerResponse>(jsonMessage); 
                if (r != null && r.data != null) UpdateLobbyUI(r.data); 
            } 
            catch { }
        }
        
        if (jsonMessage.Contains("match-canceled") || jsonMessage.Contains("match-rejected")) QuitLobby(false);
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
        foreach (var player in remotePlayers)
        {
            if (player.name == gameServer.playerName) continue;
            if (player.id != myOpponentId) continue; 
            
            bool isRemoteReady = remoteReadyStates.ContainsKey(player.name) && remoteReadyStates[player.name];
            string statusString = isRemoteReady ? "Listo" : "Esperando...";

            CreateRow(player.name, player.status, false, statusString);

            if (isRemoteReady && listContainer.childCount > 0)
            {
                var row = listContainer.GetChild(listContainer.childCount - 1).GetComponent<PlayerRowUI>();
                if (row)
                {
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
        amIReady = !amIReady; 
        rowUI.buttonText.text = amIReady ? "Listo" : "No listo";
        rowUI.readyButton.GetComponent<Image>().color = amIReady ? Color.green : Color.white;

        string payload = $@"{{
            ""event"": ""send-game-data"", 
            ""data"": {{ 
                ""matchId"": ""{currentMatchId}"", 
                ""payload"": {{ 
                    ""type"": ""custom-ready"", 
                    ""playerId"": ""{gameServer.playerName}"", 
                    ""isReady"": {amIReady.ToString().ToLower()} 
                }} 
            }} 
        }}";
        gameServer.SendWebSocketMessage(payload);
        CheckIfAllReady();
    }

    public void HandleCustomReady(string jsonPayload)
    {
        if (this == null) return;
        
        try 
        {
            CustomReadyPayload readyData = JsonUtility.FromJson<CustomReadyPayload>(jsonPayload);
            if (readyData == null) return;

            if (readyData.close == true || readyData.type == "lobby-close")
            {
                QuitLobby(false); 
                return;
            }

            string remotePlayerId = readyData.playerId;
            bool remoteIsReady = readyData.isReady;

            if (remoteReadyStates.ContainsKey(remotePlayerId))
                remoteReadyStates[remotePlayerId] = remoteIsReady;
            else
                remoteReadyStates.Add(remotePlayerId, remoteIsReady);

            foreach (Transform child in listContainer)
            {
                var row = child.GetComponent<PlayerRowUI>();
                if (row != null && row.infoText.text.Contains(remotePlayerId)) 
                {
                     row.buttonText.text = remoteIsReady ? "Listo" : "Esperando...";
                     row.readyButton.GetComponent<Image>().color = remoteIsReady ? Color.green : Color.white;
                }
            }
            CheckIfAllReady();
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
        }
    }

    private void CheckIfAllReady()
    {
        if (listContainer.childCount < 2) return;
        bool allReady = true;
        foreach (Transform child in listContainer)
        {
            var row = child.GetComponent<PlayerRowUI>();
            if (row.buttonText.text != "Listo")
            {
                allReady = false; 
                break;
            }
        }

        if (allReady)
        {
            HandleMatchStart("Start local");
        }
    }

    private void HandleMatchStart(string message) 
    { 
        if (this == null) return;
        isLobbyActive = false; 
        if(titleText) titleText.text = "INICIANDO...";
        StartCoroutine(LoadSceneDelayed());
    }

    private System.Collections.IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSeconds(1.0f);
        SceneManager.LoadScene(gameSceneName);
    }
}