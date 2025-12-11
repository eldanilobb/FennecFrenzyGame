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
        if (textoTiempo != null) textoTiempo.gameObject.SetActive(false);
        multiplicadorGlobal = 1.0f; 
        gameServer = GameServerConnection.Instance;
        matchmaking = FindFirstObjectByType<GameServerMatchmaking>();
        vidasActuales = vidas;
        ActualizarVidasUI();

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

            if (signal != null)
            {
                if (signal.type == "attack")
                {
                    iniciarCastigoVelocidad();
                }
                else if (signal.close == true || signal.type == "defeat")
                {
                    Debug.Log("El rival abandonó la partida.");
                    gameOver(true); 
                }
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
        
        multiplicadorGlobal = 2.2f; 
        
        yield return new WaitForSeconds(duracionCastigoLocal);

        multiplicadorGlobal = 1.0f;
        
        if (avisoAtaqueUI != null) avisoAtaqueUI.SetActive(false);
        
        Debug.Log("Castigo finalizado.");
    }

    public override void pausar() { if (jugando) { juegopausado = true; menuPausa.SetActive(true); } }
    public override void Reanudar() { juegopausado = false; menuPausa.SetActive(false); }
    
    public override void gameOver(bool esVictoria)
    {   
        if(menuPausa != null) menuPausa.SetActive(false);
        base.gameOver(esVictoria);
        if (matchmaking != null && !string.IsNullOrEmpty(currentMatchId))
            matchmaking.SendFinishMatch(currentMatchId, gameServer.playerName);
    }

    public override void IrAlMenu()
    {
        StartCoroutine(RutinaSalidaCompleta());
    }

    private IEnumerator RutinaSalidaCompleta()
    {
        // Sonido de botón (si existe)
        if (audioManager != null && audioManager.sfx_button != null) {
            audioManager.PlaySFX(audioManager.sfx_button); 
        }

        if (!string.IsNullOrEmpty(currentMatchId))
        {
            string closePayload = $@"{{
                ""event"": ""send-game-data"", 
                ""data"": {{ 
                    ""matchId"": ""{currentMatchId}"", 
                    ""payload"": {{ 
                        ""type"": ""defeat"", 
                        ""close"": true 
                    }} 
                }} 
            }}";
            gameServer.SendWebSocketMessage(closePayload);
        }

        // Esperamos un momento para que el mensaje salga
        yield return new WaitForSeconds(0.1f);

        // 2. ENVIAR QUIT-MATCH AL SERVIDOR
        if (!string.IsNullOrEmpty(currentMatchId))
        {
            matchmaking.SendLeaveMatch(currentMatchId);
        }

        // 3. OPCIÓN NUCLEAR: CORTAR CONEXIÓN
        // Desconectamos forzosamente para que el servidor borre el estado "IN_MATCH"
        if (gameServer != null)
        {
            gameServer.ForceDisconnect();
        }

        // 4. LIMPIEZA LOCAL
        Time.timeScale = 1; 
        currentMatchId = "";

        // Esperamos un poco para asegurar que la desconexión ocurra
        yield return new WaitForSeconds(0.2f);

        // 5. CARGAR MENÚ
        SceneManager.LoadScene("Online");
    }

    protected override IEnumerator SpawnLoop() 
    {
        while (jugando) 
        {
            float esperaSpawn = UnityEngine.Random.Range(minIntervaloSpawn, maxIntervaloSpawn);
            
            yield return new WaitForSeconds(esperaSpawn / multiplicadorGlobal);

            if (!jugando) continue;

            int fennecsActivosCount = 0;
            foreach (var fennec in fennecs) {
                if (fennec.EstaOcupado()) fennecsActivosCount++;
            }

            if (fennecsActivosCount < maxFennecsActivos) {
                List<int> fennecsDisponibles = new List<int>();
                for (int i = 0; i < fennecs.Count; i++) {
                    if (!fennecs[i].EstaOcupado()) fennecsDisponibles.Add(i);
                }

                if (fennecsDisponibles.Count > 0) {
                    int indice = fennecsDisponibles[UnityEngine.Random.Range(0, fennecsDisponibles.Count)];
                    
                    TipoDeTopo tipo = TipoDeTopo.Normal;
                    if (UnityEngine.Random.Range(0f, 100f) <= chanceTopoEspecial) tipo = TipoDeTopo.Especial;
                    
                    fennecs[indice].ActivarFennec(this, indice, tipo);
                }
            }
        }
    }
}

[System.Serializable]
public class GameSignal
{
    public string type; 
    public bool close;
}