using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    [SerializeField] private List<movimientos> fennecs;

    [Header("Ui Objects")]

    [Header("Configuración de Dificultad")]
    [SerializeField] private int maxFennecsActivos = 3;
    [SerializeField] private float intervaloSpawn = 1f;
    [SerializeField] private TextMeshProUGUI textoPuntaje;
    [SerializeField] private TextMeshProUGUI textoTiempo;

    [Header("Pantalla Final UI")]
    [SerializeField] private GameObject panelFinPartida;
    [SerializeField] private TextMeshProUGUI textoPuntajeFinal;
    [SerializeField] private TextMeshProUGUI textoMonedasGanadas;

    [Header("Variables")]
    private float tiempoInicio = 60f;
    private float tiempoRestante;
    private HashSet<movimientos> fennecsActuales = new HashSet<movimientos>();
    public int puntaje;
    private bool jugando = false;
    private float timerSpawn = 0f;
    private int puntajeFinal = 0;
    public GameObject menuPausa;
    public static bool juegopausado = false;
    private int monedas; 

    public void empezarPartida() {

        for(int i = 0; i < fennecs.Count; i++) {
            fennecs[i].esconder();
            fennecs[i].actualizarIndice(i);
        }

        fennecsActuales.Clear();
        tiempoRestante = tiempoInicio;
        puntaje = 0;
        actualizarTextoPuntaje();
        actualizarTextoTiempo(); 
        jugando = true;
        timerSpawn = 0f;
    }

    public void gameOver(int tipo) {

        jugando = false;

        for(int i = 0; i < fennecs.Count; i++) {
            fennecs[i].DetenerJuego();
        }
        puntajeFinal = puntaje;
        monedas = calcularMonedas(puntajeFinal);
        guardarMonedas(monedas);
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
            textoPuntajeFinal.text = "Puntaje final: " + puntajeFinal;
            
            if (textoMonedasGanadas != null) {
            textoMonedasGanadas.text = "Monedas ganadas: " + monedas;
            }
        }
    }

    public void IrAlMenu() {
        SceneManager.LoadScene("MainMenu"); 
    }

    public void pausar(){

        if(!jugando){return;}

        Time.timeScale = 0;
        juegopausado = true;
        menuPausa.SetActive(true);
    }

    public void reanudar() {

        if(!jugando){return;}
        Time.timeScale = 1;
        juegopausado = false;
        menuPausa.SetActive(false);
    }

    public void aumentarPuntaje(int indiceFennec) {

        puntaje += 1;
        actualizarTextoPuntaje();
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

    public void Perdido(int indiceFennec) {
        fennecsActuales.Remove(fennecs[indiceFennec]);
    }

    void Update() {

        if (juegopausado){return;}
        if(jugando) {
            tiempoRestante -= Time.deltaTime;
            timerSpawn -= Time.deltaTime;
            actualizarTextoTiempo(); 
            if(tiempoRestante <= 0) {
                tiempoRestante = 0;
                actualizarTextoTiempo(); 
                gameOver(0);
            }

            // Solo intenta activar un nuevo fennec cada intervaloSpawn segundos
            if(fennecsActuales.Count < maxFennecsActivos && timerSpawn <= 0f) {
                int indice = Random.Range(0, fennecs.Count);
                if(!fennecsActuales.Contains(fennecs[indice])) {
                    fennecsActuales.Add(fennecs[indice]);
                    fennecs[indice].Activate(0);
                    timerSpawn = intervaloSpawn; // reinicia el contador
                }
            }
        }
    }

    // Inicia la partida automáticamente al cargar la escena (se activa cuando la escena 'a' se abre desde el MainMenu).
    void Start() {
        
        Time.timeScale = 1;
        juegopausado = false;
        if (panelFinPartida != null){
        panelFinPartida.SetActive(false);
        }
        empezarPartida();
    }
}
