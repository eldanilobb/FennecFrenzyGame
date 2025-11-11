using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoPlayerSearch : MonoBehaviour
{
    [System.Serializable]
    public class Rival
    {
        public string nombre;
        public string id;
    }

    [SerializeField] private GameServerConnection gameServer;
    [SerializeField] private GameServerMatchmaking matchmaking;
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    
    [Header("Rivales")]
    [SerializeField] private Rival[] rivales = new Rival[]
    {
        new Rival { nombre = "Rival A", id = "A" },
        new Rival { nombre = "Rival C", id = "C" },
        new Rival { nombre = "Rival D", id = "D" },
        new Rival { nombre = "Rival E", id = "E" },
    };
    
    [Header("Botones de Rivales")]
    [SerializeField] private Transform botonesContainer;
    [SerializeField] private GameObject botonRivalPrefab;
    
    private string currentOpponentId = "";

    void Start()
    {
        FindAndCacheComponents();
        SubscribeToEvents();
        InitializeUI();
        CrearBotonesRivales();
    }

    private void FindAndCacheComponents()
    {
        gameServer = gameServer ?? FindFirstObjectByType<GameServerConnection>();
        matchmaking = matchmaking ?? FindFirstObjectByType<GameServerMatchmaking>();
    }

    private void SubscribeToEvents()
    {
        if (acceptButton) acceptButton.onClick.AddListener(AcceptMatch);
        if (rejectButton) rejectButton.onClick.AddListener(RejectMatch);
        
        if (matchmaking)
        {
            matchmaking.OnMatchRequestReceived += OnMatchRequestReceived;
            matchmaking.OnMatchAccepted += OnMatchAccepted;
            matchmaking.OnMatchRejected += OnMatchRejected;
        }
    }

    private void InitializeUI()
    {
        SetButtonActive(acceptButton, false);
        SetButtonActive(rejectButton, false);
    }

    private void CrearBotonesRivales()
    {
        foreach (var rival in rivales)
        {
            GameObject boton = Instantiate(botonRivalPrefab, botonesContainer);
            
            TextMeshProUGUI textoBoton = boton.GetComponentInChildren<TextMeshProUGUI>();
            textoBoton.text = $"Desafiar a {rival.nombre}";
            
            Button btnComponent = boton.GetComponent<Button>();
            btnComponent.onClick.AddListener(() => DesafiarRival(rival));
            
            Debug.Log($"Boton creado para: {rival.nombre}");
        }
    }

    private void DesafiarRival(Rival rival)
    {
        if (!gameServer.isLoggedIn)
        {
            statusText.text = "No estas conectado";
            return;
        }

        Debug.Log($"Desafiando a {rival.nombre} (ID: {rival.id})");
        matchmaking.SendMatchRequest(rival.id);
        statusText.text = $"Esperando que {rival.nombre} acepte...";
    }

    private void OnMatchRequestReceived(string opponentId, string matchId)
    {
        currentOpponentId = opponentId;
        UpdateUI("Solicitud recibida");
        UpdateOpponentName($"Oponente: {opponentId}");
        ShowMatchButtons(true);
    }

    public void AcceptMatch()
    {
        if (matchmaking == null) return;
        
        matchmaking.AcceptMatchRequest();
        UpdateUI("Aceptando partida...");
        SetButtonInteractable(acceptButton, false);
        SetButtonInteractable(rejectButton, false);
    }

    private void OnMatchAccepted(string matchId, string status)
    {
        UpdateUI("Partida iniciada!");
        Debug.Log($"Match aceptada: {matchId}");
        Invoke("LoadGameScene", 1.5f);
    }

    public void RejectMatch()
    {
        if (matchmaking == null) return;
        
        matchmaking.RejectMatchRequest();
        UpdateUI("Solicitud rechazada");
        ResetSearch();
    }

    private void OnMatchRejected(string opponentId)
    {
        UpdateUI("El oponente rechazo");
        ResetSearch();
    }

    private void ResetSearch()
    {
        ShowMatchButtons(false);
    }

    private void UpdateUI(string status)
    {
        if (statusText) statusText.text = status;
    }

    private void UpdateOpponentName(string name)
    {
        if (opponentNameText) opponentNameText.text = name;
    }

    private void ShowMatchButtons(bool show)
    {
        SetButtonActive(acceptButton, show);
        SetButtonActive(rejectButton, show);
    }

    private void SetButtonActive(Button button, bool active)
    {
        if (button) button.gameObject.SetActive(active);
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button) button.interactable = interactable;
    }

    private void LoadGameScene()
    {
        Debug.Log("Cargando escena del juego...");
    }
}
