using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManagerNiveles : MonoBehaviour 
{
    // NUEVO: Definimos los tipos de Fennec
    public enum TipoDeTopo { Normal, Especial }

    [Header("Referencias de Fennecs")]
    [SerializeField] private List<movimientosNiveles> fennecs;

    [Header("Configuración de Nivel (Plantilla)")] // Renombrado para claridad
    [Tooltip("Duración total del nivel en segundos")]
    [SerializeField] private float tiempoInicio = 60f; // Usamos tu nombre original
    
    [Tooltip("Número máximo de fennecs visibles al mismo tiempo")]
    [SerializeField] private int maxFennecsActivos = 3;
    
    [Tooltip("Tiempo MÍNIMO de espera antes de intentar spawnear un nuevo fennec")]
    [SerializeField] private float minIntervaloSpawn = 0.8f; // Intervalo entre intentos
    
    [Tooltip("Tiempo MÁXIMO de espera antes de intentar spawnear un nuevo fennec")]
    [SerializeField] private float maxIntervaloSpawn = 1.5f; // Para dar más variabilidad
    
    [Tooltip("Probabilidad (de 0 a 100) de que salga un topo especial")]
    [Range(0, 100)]
    [SerializeField] private float chanceTopoEspecial = 15f; // 15% de probabilidad para Nivel 1

    [Header("Valores de Puntuación y Bonificación")]
    [SerializeField] private int puntosPorTopoNormal = 1;
    [SerializeField] private int puntosPorTopoEspecial = 5;
    [SerializeField] private float tiempoExtraPorEspecial = 3f; // Segundos extra por topo especial

    [Header("UI Objects")]
    [SerializeField] private TextMeshProUGUI textoPuntaje;
    [SerializeField] private TextMeshProUGUI textoTiempo;
    
    [Header("Pantalla Final UI")]
    [SerializeField] private GameObject panelFinPartida;
    [SerializeField] private TextMeshProUGUI textoPuntajeFinal;
    [SerializeField] private TextMeshProUGUI textoMonedasGanadas;
    
    [Header("Pausa UI")]
    public GameObject menuPausa;
    public GameObject canvasPowerUps;

    // --- Variables Internas del Juego ---
    private float tiempoRestante;
    private int puntaje; // Eliminado 'public' para mantenerlo gestionado internamente

    private bool puntosDoblesActivo = false;

    public GameObject botonPuntosDobles;
    private float timerPuntosDobles = 0f;

    private bool jugando = false;
    private float timerSpawn = 0f; // Tiempo para el próximo intento de spawn
    private int monedasGanadasEnNivel; // Renombrado de 'monedas' para claridad

    public static bool juegopausado = false; // Estática para que los topos la lean

    AudioManager_Tutorial audioManager; 
    private void Awake(){
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager_Tutorial>();

    }
    void Start() {
        Time.timeScale = 1;
        juegopausado = false;
        if (panelFinPartida != null){
            panelFinPartida.SetActive(false);
        }
        if (menuPausa != null) {
            menuPausa.SetActive(false);
        }
        empezarPartida();
    }

    public void ActivarPuntosDobles(float duracion) {
        puntosDoblesActivo = true;
        timerPuntosDobles = duracion;
    }
    public void aumentarPuntaje(int indiceFennec) {
        puntaje += puntosDoblesActivo ? 2 : 1;
        actualizarTextoPuntaje();
    }
    public void empezarPartida() {
        // Asignar índices a los fennecs (una sola vez al inicio)
        for(int i = 0; i < fennecs.Count; i++) {
            fennecs[i].esconder();
            // Ya no se actualiza el índice desde aquí, cada Fennec tendrá un ID interno si es necesario.
            // Para la lógica de este GameManager, el índice en la lista 'fennecs' es suficiente.
        }

        // fennecsActuales.Clear(); // Ya no necesitamos este HashSet, lo gestionamos con EstaOcupado()
        tiempoRestante = tiempoInicio;
        puntaje = 0;
        actualizarTextoPuntaje();
        actualizarTextoTiempo(); 
        jugando = true;
        timerSpawn = 0f;

        // NUEVO: Iniciar la corutina de spawn en lugar de hacerlo en Update
        StartCoroutine(SpawnLoop());
    }

    private IEnumerator SpawnLoop() {
        while (jugando) {
            if (juegopausado) {
                yield return null; // Espera un frame si está pausado
                continue; // Vuelve a revisar
            }

            // Espera un tiempo aleatorio antes del próximo intento de spawn
            float esperaSpawn = UnityEngine.Random.Range(minIntervaloSpawn, maxIntervaloSpawn);
            yield return new WaitForSeconds(esperaSpawn);

            // Si el juego ha terminado o está pausado mientras esperábamos, salir
            if (!jugando || juegopausado) continue;

            // Contar cuántos fennecs están activos actualmente
            int fennecsActivosCount = 0;
            foreach (var fennec in fennecs) {
                if (fennec.EstaOcupado()) {
                    fennecsActivosCount++;
                }
            }

            // Solo si hay menos del máximo permitido y hay topos disponibles
            if (fennecsActivosCount < maxFennecsActivos) {
                List<int> fennecsDisponibles = new List<int>();
                for (int i = 0; i < fennecs.Count; i++) {
                    if (!fennecs[i].EstaOcupado()) {
                        fennecsDisponibles.Add(i);
                    }
                }

                if (fennecsDisponibles.Count > 0) {
                    int indiceASpawnear = fennecsDisponibles[UnityEngine.Random.Range(0, fennecsDisponibles.Count)];
                    
                    // Decide si será un topo especial
                    TipoDeTopo tipoASpawnear = TipoDeTopo.Normal;
                    if (UnityEngine.Random.Range(0f, 100f) <= chanceTopoEspecial) {
                        tipoASpawnear = TipoDeTopo.Especial;
                    }
                    
                    fennecs[indiceASpawnear].ActivarFennec(this, indiceASpawnear, tipoASpawnear);
                }
            }
        }
    }


    void Update() {
        if (juegopausado || !jugando) return;

        if (puntosDoblesActivo) {
            timerPuntosDobles -= Time.deltaTime;
            if (timerPuntosDobles <= 0) {
                puntosDoblesActivo = false;
            }
        }

        tiempoRestante -= Time.deltaTime;
        actualizarTextoTiempo(); 
        if (tiempoRestante <= 0) {
            tiempoRestante = 0;
            actualizarTextoTiempo(); 
            gameOver();
        }
    }

    public void AgregarTiempo(float segundos) {
        tiempoRestante += segundos;

        if (tiempoRestante > tiempoInicio)
            tiempoRestante = tiempoInicio;

        actualizarTextoTiempo();
    }

    // NUEVO: La función TopoGolpeado ahora recibe el índice y el tipo
    public void TopoGolpeado(int indiceFennec, TipoDeTopo tipoDeTopoGolpeado) {
        if (!jugando) return;

        int puntos = 0;

        if (tipoDeTopoGolpeado == TipoDeTopo.Normal) {
            puntos = puntosPorTopoNormal;
        }
        else if (tipoDeTopoGolpeado == TipoDeTopo.Especial) {
            puntos = puntosPorTopoEspecial;

            // Bonificación de tiempo por topo especial
            tiempoRestante += tiempoExtraPorEspecial;

            if (tiempoRestante > tiempoInicio * 1.5f) {
                tiempoRestante = tiempoInicio * 1.5f;
            }
        }

        // APLICAR PUNTOS DOBLES AQUÍ
        if (puntosDoblesActivo)
            puntos *= 2;

        puntaje += puntos;

        actualizarTextoPuntaje();
    }


    // NUEVO: La función Perdido ahora recibe el índice y el tipo
    public void Perdido(int indiceFennec, TipoDeTopo tipoDeTopoPerdido) {
        // En tu lógica actual, no penalizas por topos perdidos, pero aquí podrías hacerlo
        // Por ejemplo: si tipoDeTopoPerdido == TipoDeTopo.Especial, quitar puntos
        // fennecsActuales.Remove(fennecs[indiceFennec]); // Ya no necesario
    }


    public void gameOver() { // Eliminado el parámetro 'int tipo'
        UnlockNewLevel();
        jugando = false;

        StopAllCoroutines(); // Detener el SpawnLoop y el TimerTick

        for(int i = 0; i < fennecs.Count; i++) {
            fennecs[i].DetenerJuego();
        }
        
        monedasGanadasEnNivel = calcularMonedas(puntaje);
        guardarMonedas(monedasGanadasEnNivel);
        mostrarGameOver();
    }

    private int calcularMonedas(int puntos) {
        int monedasBase = Mathf.FloorToInt(puntos / 10f);
        if (puntos >= 100) {
            monedasBase += 20; 
        }
        if (puntos >= 200) {
            monedasBase += 30;
        }
        return monedasBase;
    }

    private void guardarMonedas(int monedas) {
        int monedasTotales = PlayerPrefs.GetInt("MonedasTotales", 0);
        monedasTotales += monedas;
        PlayerPrefs.SetInt("MonedasTotales", monedasTotales);
        PlayerPrefs.Save();
    }

    public int ObtenerMonedasTotales() {
        return PlayerPrefs.GetInt("MonedasTotales", 0);
    }

    private void mostrarGameOver(){
        if (panelFinPartida != null){
            panelFinPartida.SetActive(true);
            textoPuntajeFinal.text = "Puntaje final: " + puntaje; // Usar 'puntaje' directamente
            if (textoMonedasGanadas != null) {
                textoMonedasGanadas.text = "Monedas ganadas: " + monedasGanadasEnNivel;
            }
        }
    }

    public void IrAlMenu() {
        
        if (audioManager != null && audioManager.sfx_button != null)
        {
            audioManager.PlaySFX(audioManager.sfx_button); 

            StartCoroutine(DelaySceneLoad(audioManager.sfx_button.length,"LevelSelection"));
        }
        else
        {
            SceneManager.LoadScene("LevelSelection"); 
        }    }

    public void pausar() {
        if (!jugando) return;

        Time.timeScale = 0;
        juegopausado = true;

        // Oculta los power-ups
        canvasPowerUps.SetActive(false);
        botonPuntosDobles.SetActive(false);

        // Muestra el menú de pausa
        menuPausa.SetActive(true);
    }


    public void Reanudar() {
        Time.timeScale = 1;
        juegopausado = false;

        canvasPowerUps.SetActive(true);
        botonPuntosDobles.SetActive(true);
        menuPausa.SetActive(false);
    }



    private void actualizarTextoPuntaje() {
        if(textoPuntaje != null) {
            textoPuntaje.text = "Puntaje " + puntaje;
        }
    }

    private void actualizarTextoTiempo() {
        if(textoTiempo != null) {
            int minutos = (int)tiempoRestante / 60;
            int segundos = (int)tiempoRestante % 60;
            textoTiempo.text = minutos.ToString("00") + ":" + segundos.ToString("00");
        }
    }

        void UnlockNewLevel(){
        if(SceneManager.GetActiveScene().buildIndex >= PlayerPrefs.GetInt("ReachedIndex")){
            PlayerPrefs.SetInt("ReachedIndex", SceneManager.GetActiveScene().buildIndex + 1);
            PlayerPrefs.SetInt("UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 1) + 1);
            PlayerPrefs.Save();
        }
    }

    private IEnumerator DelaySceneLoad(float soundDuration,String level)
    {
        yield return new WaitForSeconds(soundDuration);
        SceneManager.LoadScene(level);
    }
    // Eliminamos fennecsActuales.Remove, ya no lo usamos
    // public void Perdido(int indiceFennec) {
    //     fennecsActuales.Remove(fennecs[indiceFennec]);
    // }
}