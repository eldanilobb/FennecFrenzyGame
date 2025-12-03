using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerOnline : GameManagerNiveles 
{
    private GameServerConnection gameServer;
    private GameServerMatchmaking matchmaking;
    private string currentMatchId = "";

    protected override void Start() 
    {
        gameServer = GameServerConnection.Instance;
        matchmaking = FindFirstObjectByType<GameServerMatchmaking>();

        if (gameServer == null || matchmaking == null)
        {
            Debug.LogError("Error Online: Falta conexión. Volviendo a base...");
            base.Start(); 
            return;
        }

        currentMatchId = matchmaking.GetCurrentMatchId();
        
        matchmaking.OnCustomReadyReceived += RecibirEventoDeRival;

        Time.timeScale = 1;
        juegopausado = false;
        
        if(canvasPowerUps) canvasPowerUps.SetActive(false);
        if(botonPuntosDobles) botonPuntosDobles.SetActive(false);
        if(panelFinPartida) panelFinPartida.SetActive(false);
        if(menuPausa) menuPausa.SetActive(false);

        empezarPartida();
    }

    void OnDestroy()
    {
        if (matchmaking != null) matchmaking.OnCustomReadyReceived -= RecibirEventoDeRival;
    }

    protected override void Update()
    {
        if (!jugando) return;

        tiempoRestante -= Time.deltaTime;
        actualizarTextoTiempo(); 
        
        if (tiempoRestante <= 0) {
            tiempoRestante = 0;
            actualizarTextoTiempo(); 
            gameOver();
        }
        
    }

    public override void TopoGolpeado(int indiceFennec, TipoDeTopo tipo)
    {
        base.TopoGolpeado(indiceFennec, tipo);
    }

    public override void pausar()
    {
        if (!jugando) return;
        
        juegopausado = true; 
        menuPausa.SetActive(true);
    }

    public override void Reanudar()
    {
        juegopausado = false;
        menuPausa.SetActive(false);
    }

    public override void IrAlMenu()
    {
        if (matchmaking != null && !string.IsNullOrEmpty(currentMatchId))
        {
            string closePayload = $@"{{
                ""event"": ""send-game-data"", 
                ""data"": {{ 
                    ""matchId"": ""{currentMatchId}"", 
                    ""payload"": {{ 
                        ""type"": ""game-close"", 
                        ""close"": true 
                    }} 
                }} 
            }}";
            
            if (gameServer != null) gameServer.SendWebSocketMessage(closePayload);

            matchmaking.SendFinishMatch(currentMatchId, gameServer.playerName);
            
            matchmaking.SendLeaveMatch(currentMatchId);
        }
        if (audioManager != null && audioManager.sfx_button != null) {
            audioManager.PlaySFX(audioManager.sfx_button); 
            StartCoroutine(DelaySceneLoad(audioManager.sfx_button.length, "Online"));
        } else {
            SceneManager.LoadScene("Online"); 
        }   
    }

    public override void gameOver()
    {
        base.gameOver();

        if (matchmaking != null && !string.IsNullOrEmpty(currentMatchId))
        {
            matchmaking.SendFinishMatch(currentMatchId, gameServer.playerName);
        }
    }

    public void EnviarAtaque(string tipo, int cantidad)
    {
        if (string.IsNullOrEmpty(currentMatchId)) return;
        Debug.Log($"Enviando ataque al rival: {tipo}");
    }

    private void RecibirEventoDeRival(string jsonPayload)
    {
        Debug.Log($"Recibido del rival: {jsonPayload}");
    }
}