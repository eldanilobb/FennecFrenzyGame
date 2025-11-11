using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchmakingUI : MonoBehaviour
{
    [SerializeField] private GameServerConnection gameServer;
    [SerializeField] private GameServerMatchmaking matchmaking;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject matchmakingPanel;
    [SerializeField] private InputField opponentIdInput;
    [SerializeField] private Button sendRequestButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private TextMeshProUGUI opponentNameText;
    
    private string currentOpponentId = "";
    private string currentMatchId = "";
    private bool matchAccepted = false;

    void Start()
    {
        gameServer = FindFirstObjectByType<GameServerConnection>();
        matchmaking = FindFirstObjectByType<GameServerMatchmaking>();
        
        // Suscribirse a eventos
        matchmaking.OnMatchRequestSent += OnRequestSent;
        matchmaking.OnMatchRequestReceived += OnRequestReceived;
        matchmaking.OnMatchAccepted += OnMatchAccepted;
        matchmaking.OnMatchRejected += OnMatchRejected;
        
        // Botones
        sendRequestButton.onClick.AddListener(SendMatchRequest);
        acceptButton.onClick.AddListener(AcceptMatch);
        rejectButton.onClick.AddListener(RejectMatch);
        
        // Inicialmente esconder botones
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
    }

    public void SendMatchRequest()
    {
        string targetId = opponentIdInput.text.Trim();
        
        if (string.IsNullOrEmpty(targetId))
        {
            statusText.text = "Ingresa el ID del oponente";
            return;
        }
        
        if (!gameServer.isLoggedIn)
        {
            statusText.text = "No estás conectado";
            return;
        }
        
        matchmaking.SendMatchRequest(targetId);
        statusText.text = "Enviando solicitud...";
        sendRequestButton.interactable = false;
        opponentIdInput.interactable = false;
    }

    void OnRequestSent(string matchId)
    {
        statusText.text = "Solicitud enviada. Esperando respuesta...";
        currentMatchId = matchId;
    }

    void OnRequestReceived(string opponentId, string matchId)
    {
        currentOpponentId = opponentId;
        currentMatchId = matchId;
        statusText.text = $"Solicitud recibida de {opponentId}";
        opponentNameText.text = $"Oponente: {opponentId}";
        acceptButton.gameObject.SetActive(true);
        rejectButton.gameObject.SetActive(true);
    }

    public void AcceptMatch()
    {
        matchmaking.AcceptMatchRequest();
        statusText.text = "Aceptando partida...";
        acceptButton.interactable = false;
        rejectButton.interactable = false;
    }

    void OnMatchAccepted(string matchId, string status)
    {
        matchAccepted = true;
        statusText.text = $"¡Partida Aceptada! ID: {matchId}";
        
        // Después de 2 segundos, cargar el juego
        Invoke("StartGame", 2f);
    }

    public void RejectMatch()
    {
        matchmaking.RejectMatchRequest();
        statusText.text = "Solicitud rechazada";
        acceptButton.gameObject.SetActive(false);
        rejectButton.gameObject.SetActive(false);
        ResetUI();
    }

    void OnMatchRejected(string opponentId)
    {
        statusText.text = "El oponente rechazó la solicitud";
        ResetUI();
    }

    void ResetUI()
    {
        sendRequestButton.interactable = true;
        opponentIdInput.interactable = true;
        opponentIdInput.text = "";
    }

    void StartGame()
    {
        // Pasar el matchId y opponentId al GameManager
        GameManager gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            gm.IniciarPartidaMultijugador(currentMatchId, currentOpponentId);
        }
        
        matchmakingPanel.SetActive(false);
    }

    public bool IsMatchAccepted() => matchAccepted;
}
