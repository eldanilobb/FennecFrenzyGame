using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManagerOnline : GameManagerNiveles 
{
    private GameServerConnection gameServer;
    private GameServerMatchmaking matchmaking;
    private string currentMatchId = "";

    [Header("Configuración Online")]
    public float duracionCastigoLocal = 5.0f; 
    public static float multiplicadorGlobal = 1.0f;

    [Header("Feedback Visual")]
    public GameObject avisoAtaqueUI; 

    protected override void Start() 
    {
        multiplicadorGlobal = 1.0f; 
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

        if (avisoAtaqueUI != null) avisoAtaqueUI.SetActive(false);

        empezarPartida();
    }

    void OnDestroy()
    {
        if (matchmaking != null) matchmaking.OnCustomReadyReceived -= RecibirEventoDeRival;
        multiplicadorGlobal = 1.0f; 
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
        if (!jugando) return;

        int puntos = (tipo == TipoDeTopo.Especial) ? puntosPorTopoEspecial : puntosPorTopoNormal;

        if (tipo == TipoDeTopo.Especial) 
        { 
            EnviarAtaque(); 
        } 

        if (puntosDoblesActivo) puntos *= 2;

        puntaje += puntos;
        actualizarTextoPuntaje();
    }

    public void EnviarAtaque()
    {
        if (string.IsNullOrEmpty(currentMatchId)) return;

        GameSignal signal = new GameSignal();
        signal.type = "attack"; 

        string jsonPayload = JsonUtility.ToJson(signal);

        string finalMessage = $@"{{
            ""event"": ""send-game-data"", 
            ""data"": {{ 
                ""matchId"": ""{currentMatchId}"", 
                ""payload"": {jsonPayload} 
            }} 
        }}";

        gameServer.SendWebSocketMessage(finalMessage);
    }

    private void RecibirEventoDeRival(string jsonPayload)
    {
        try 
        {
            GameSignal signal = JsonUtility.FromJson<GameSignal>(jsonPayload);

            if (signal != null && signal.type == "attack")
            {
                iniciarCastigoVelocidad();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Ignorando mensaje desconocido: " + e.Message);
        }
    }

    public void iniciarCastigoVelocidad()
    {
        StopCoroutine("RutinaCastigoVelocidad"); 
        StartCoroutine("RutinaCastigoVelocidad");
    }

    private IEnumerator RutinaCastigoVelocidad()
    {
        if (avisoAtaqueUI != null) avisoAtaqueUI.SetActive(true);
        
        multiplicadorGlobal = 3.0f; 
        
        yield return new WaitForSeconds(duracionCastigoLocal);

        multiplicadorGlobal = 1.0f;
        
        if (avisoAtaqueUI != null) avisoAtaqueUI.SetActive(false);
        
        Debug.Log("Castigo finalizado.");
    }

    public override void pausar() { if (jugando) { juegopausado = true; menuPausa.SetActive(true); } }
    public override void Reanudar() { juegopausado = false; menuPausa.SetActive(false); }
    
    public override void gameOver()
    {
        base.gameOver();
        if (matchmaking != null && !string.IsNullOrEmpty(currentMatchId))
            matchmaking.SendFinishMatch(currentMatchId, gameServer.playerName);
    }

    public override void IrAlMenu()
    {
        if (matchmaking != null && !string.IsNullOrEmpty(currentMatchId))
        {
            string closePayload = $@"{{
                ""event"": ""send-game-data"", 
                ""data"": {{ ""matchId"": ""{currentMatchId}"", ""payload"": {{ ""type"": ""game-close"", ""close"": true }} }} 
            }}";
            gameServer.SendWebSocketMessage(closePayload);
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
}

[System.Serializable]
public class GameSignal
{
    public string type; 
}