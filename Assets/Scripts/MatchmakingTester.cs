using UnityEngine;
using UnityEngine.UI;

public class MatchmakingTester : MonoBehaviour
{
    public GameServerMatchmaking matchmaking;
    public InputField opponentIdInput;
    
    void Update()
    {
        // Presiona 'R' para enviar solicitud (usa un ID de prueba)
        if (Input.GetKeyDown(KeyCode.R))
        {
            string testOpponentId = "test-opponent-id";
            if (opponentIdInput != null && !string.IsNullOrEmpty(opponentIdInput.text))
            {
                testOpponentId = opponentIdInput.text;
            }
            matchmaking.SendMatchRequest(testOpponentId);
        }

        // Presiona 'A' para aceptar solicitud
        if (Input.GetKeyDown(KeyCode.A))
        {
            matchmaking.AcceptMatchRequest();
        }

        // Presiona 'J' para rechazar solicitud
        if (Input.GetKeyDown(KeyCode.J))
        {
            matchmaking.RejectMatchRequest();
        }

        // Presiona 'C' para cancelar solicitud
        if (Input.GetKeyDown(KeyCode.C))
        {
            matchmaking.CancelMatchRequest();
        }
    }

    void Start()
    {
        // Suscribirse a eventos
        if (matchmaking != null)
        {
            matchmaking.OnMatchRequestSent += (matchId) => Debug.Log($"Match enviada: {matchId}");
            matchmaking.OnMatchRequestReceived += (playerId, matchId) => Debug.Log($"Solicitud recibida de {playerId}");
            matchmaking.OnMatchAccepted += (matchId, status) => Debug.Log($"Partida aceptada: {matchId} - {status}");
            matchmaking.OnMatchRejected += (playerId) => Debug.Log($"Solicitud rechazada por {playerId}");
        }
    }
}

