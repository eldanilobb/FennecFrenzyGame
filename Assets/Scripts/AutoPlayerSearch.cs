using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

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

    [Header("Conexiones")]
    [SerializeField] private GameServerConnection gameServer;
    [SerializeField] private GameServerMatchmaking matchmaking;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    [SerializeField] private Button btnRefrescar;

    [Header("Lista de Rivales")]
    [SerializeField] private Transform botonesContainer;
    [SerializeField] private GameObject botonRivalPrefab;
    
    private List<PlayerData> onlinePlayers = new List<PlayerData>();

    void Start()
    {
        if(gameServer == null) gameServer = FindFirstObjectByType<GameServerConnection>();
        if(matchmaking == null) matchmaking = FindFirstObjectByType<GameServerMatchmaking>();

        SubscribeToEvents();
        InitializeUI();
        StartCoroutine(WaitAndRefreshList());
    }

    void OnDestroy()
    {
        if (gameServer != null) gameServer.OnServerMessageReceived -= HandleServerMessage;
    }

    private void SubscribeToEvents()
    {
        if (acceptButton) acceptButton.onClick.AddListener(AcceptMatch);
        if (rejectButton) rejectButton.onClick.AddListener(RejectMatch);
        if (btnRefrescar) btnRefrescar.onClick.AddListener(RequestPlayerList);

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
        if (!gameServer.isLoggedIn) return;
        statusText.text = "Buscando jugadores...";
        foreach (Transform child in botonesContainer) Destroy(child.gameObject);
        
        gameServer.SendWebSocketMessage("{\"event\": \"online-players\"}");
    }

    private void HandleServerMessage(string json)
    {
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
        catch (Exception e)
        {
            Debug.LogWarning("Error leyendo lista: " + e.Message);
        }
    }

    private void UpdateButtonsUI()
    {
        foreach (Transform child in botonesContainer) Destroy(child.gameObject);

        string miNombre = gameServer.playerName;
        int encontrados = 0;

        foreach (var player in onlinePlayers)
        {
            if (player.name == miNombre) continue; 

            encontrados++;
            GameObject boton = Instantiate(botonRivalPrefab, botonesContainer);
            TextMeshProUGUI texto = boton.GetComponentInChildren<TextMeshProUGUI>();
            texto.text = $"{player.name} ({player.status})"; 
            
            Button btn = boton.GetComponent<Button>();
            string targetId = player.id; 
            string targetName = player.name;

            if (player.status != "AVAILABLE") btn.interactable = false;

            btn.onClick.AddListener(() => DesafiarRival(targetId, targetName));
        }

        if (encontrados == 0) statusText.text = "Sin jugadores disponibles";
        else statusText.text = "Selecciona un rival:";
    }

    private void DesafiarRival(string id, string nombre)
    {
        if (!gameServer.isLoggedIn) return;
        matchmaking.SendMatchRequest(id);
        statusText.text = $"Esperando a {nombre}...";
    }

    private void OnMatchRequestReceived(string opponentId, string matchId)
    {
        UpdateUI("Te han desafiado!");
        UpdateOpponentName($"Rival: {opponentId}");
        ShowMatchButtons(true);
    }

    public void AcceptMatch()
    {
        Debug.Log("BOTÓN ACEPTAR PULSADO"); // <--- CONFIRMACIÓN VISUAL
        
        if (matchmaking == null)
        {
            Debug.LogError("ERROR: No tengo referencia al script Matchmaking.");
            return;
        }

        matchmaking.AcceptMatchRequest();
        UpdateUI("Confirmando con servidor...");
        
        if (acceptButton) acceptButton.interactable = false;
        if (rejectButton) rejectButton.interactable = false;
    }

    private void OnMatchAccepted(string matchId, string status)
    {
        UpdateUI("Partida iniciada!");
        // Nos desactivamos para dejar paso al LobbyManager
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

    private void UpdateUI(string status) { if (statusText) statusText.text = status; }
    private void UpdateOpponentName(string name) { if (opponentNameText) opponentNameText.text = name; }
    private void ShowMatchButtons(bool show) { 
        if(acceptButton) { acceptButton.gameObject.SetActive(show); acceptButton.interactable = true; }
        if(rejectButton) { rejectButton.gameObject.SetActive(show); rejectButton.interactable = true; }
    }
}