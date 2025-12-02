using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class AutoPlayerSearch : MonoBehaviour
{
    [Serializable]
    public class PlayerData
    {
        public string id;
        public string name;
        public string status;
    }

    [Serializable]
    public class ServerResponse
    {
        public string @event;
        public string status;
        public string msg;
        public List<PlayerData> data;
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private Button btnRefrescar;
    [SerializeField] private Button btnSalir;

    [Header("Lista de Rivales")]
    [SerializeField] private Transform botonesContainer;
    [SerializeField] private GameObject botonRivalPrefab;
    
    private GameServerConnection gameServer;
    private GameServerMatchmaking matchmaking;
    
    private List<PlayerData> onlinePlayers = new List<PlayerData>();

    void Start()
    {
        gameServer = GameServerConnection.Instance;
        if(gameServer == null) gameServer = FindFirstObjectByType<GameServerConnection>();

        matchmaking = FindFirstObjectByType<GameServerMatchmaking>();

        InitializeUI();
        SubscribeToEvents();
        StartCoroutine(WaitAndRefreshList());
    }

    void OnDestroy()
    {
        if (gameServer != null) gameServer.OnServerMessageReceived -= HandleServerMessage;
        
        if (matchmaking != null)
        {
            matchmaking.OnMatchRequestReceived -= OnMatchRequestReceived;
            matchmaking.OnMatchAccepted -= OnMatchAccepted;
            matchmaking.OnMatchRejected -= OnMatchRejected;
        }
    }

    private void SubscribeToEvents()
    {
        if (acceptButton) acceptButton.onClick.AddListener(AcceptMatch);
        if (rejectButton) rejectButton.onClick.AddListener(RejectMatch);
        if (btnRefrescar) btnRefrescar.onClick.AddListener(RequestPlayerList);
        if (btnSalir) btnSalir.onClick.AddListener(SalirAlMenu);

        if (gameServer != null)
            gameServer.OnServerMessageReceived += HandleServerMessage;
        
        if (matchmaking)
        {
            matchmaking.OnMatchRequestReceived += OnMatchRequestReceived;
            matchmaking.OnMatchAccepted += OnMatchAccepted;
            matchmaking.OnMatchRejected += OnMatchRejected;
        }
    }

    private IEnumerator WaitAndRefreshList()
    {
        yield return new WaitUntil(() => gameServer != null && gameServer.isLoggedIn);
        RequestPlayerList();
    }

    public void RequestPlayerList()
    {
        if (gameServer == null || !gameServer.isLoggedIn) return;
        if (statusText) statusText.text = "Buscando jugadores...";
        
        if (botonesContainer)
            foreach (Transform child in botonesContainer) Destroy(child.gameObject);
        
        gameServer.SendWebSocketMessage("{\"event\": \"online-players\"}");
    }

    private void HandleServerMessage(string json)
    {
        if (this == null) return;

        if (json.Contains("\"event\":\"online-players\"")) 
        {
            ParseAndBuildList(json);
        }
        else if (json.Contains("player-connected") || json.Contains("player-disconnected"))
        {
            RequestPlayerList();
        }
    }

    private void ParseAndBuildList(string json)
    {
        try
        {
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);

            if (response != null && response.data != null)
            {
                onlinePlayers = response.data;
                UpdateButtonsUI();
            }
        }
        catch (Exception) { }
    }

    private void UpdateButtonsUI()
    {
        if (botonesContainer == null) return;

        foreach (Transform child in botonesContainer) Destroy(child.gameObject);

        string miNombre = gameServer.playerName.Trim();
        int encontrados = 0;

        foreach (var player in onlinePlayers)
        {
            string nombreRemoto = player.name.Trim();
            
            if (string.Equals(nombreRemoto, miNombre, StringComparison.OrdinalIgnoreCase)) 
            {
                continue; 
            }

            encontrados++;
            GameObject boton = Instantiate(botonRivalPrefab, botonesContainer);
            TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>();
            texto.text = $"{nombreRemoto} ({player.status})"; 
            
            Button btn = boton.GetComponent<Button>();
            string targetId = player.id; 
            string targetName = player.name;

            if (player.status != "AVAILABLE") btn.interactable = false;

            btn.onClick.AddListener(() => DesafiarRival(targetId, targetName));
        }

        if (statusText)
        {
            if (encontrados == 0) statusText.text = "Sin jugadores disponibles";
            else statusText.text = "Selecciona un rival:";
        }
    }

    private void DesafiarRival(string id, string nombre)
    {
        if (!gameServer.isLoggedIn) return;
        matchmaking.SendMatchRequest(id);
        if(statusText) statusText.text = $"Esperando a {nombre}...";
    }

    private void OnMatchRequestReceived(string opponentId, string matchId)
    {
        if (this == null) return;
        UpdateUI("Te han desafiado!");
        UpdateOpponentName($"Rival: {opponentId}");
        ShowMatchButtons(true);
    }

    public void AcceptMatch()
    {
        if (matchmaking == null) return;

        matchmaking.AcceptMatchRequest();
        UpdateUI("Confirmando con servidor...");
        
        if (acceptButton) acceptButton.interactable = false;
        if (rejectButton) rejectButton.interactable = false;
    }

    private void OnMatchAccepted(string matchId, string status)
    {
        if (this == null) return;
        UpdateUI("Partida iniciada!");
        this.gameObject.SetActive(false); 
    }

    public void RejectMatch()
    {
        matchmaking.RejectMatchRequest();
        UpdateUI("Rechazaste la solicitud");
        ResetSearch();
    }

    private void OnMatchRejected(string opponentId)
    {
        if (this == null) return;
        UpdateUI($"Oponente rechazo");
        ResetSearch();
    }

    private void ResetSearch()
    {
        ShowMatchButtons(false);
        RequestPlayerList(); 
    }
    
    private void InitializeUI()
    {
        ShowMatchButtons(false);
        UpdateUI("Conectando...");
    }

    public void SalirAlMenu()
    {
        if (GameServerConnection.Instance != null)
        {
            GameServerConnection.Instance.ForceDisconnect();
        }
        SceneManager.LoadScene("LevelSelection");
    }

    private void UpdateUI(string status) { if (statusText) statusText.text = status; }
    private void UpdateOpponentName(string name) { if (opponentNameText) opponentNameText.text = name; }
    private void ShowMatchButtons(bool show) { 
        if(acceptButton) { acceptButton.gameObject.SetActive(show); acceptButton.interactable = true; }
        if(rejectButton) { rejectButton.gameObject.SetActive(show); rejectButton.interactable = true; }
    }
}