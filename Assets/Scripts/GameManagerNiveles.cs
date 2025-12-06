using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManagerNiveles : MonoBehaviour 
{
    public enum TipoDeTopo { Normal, Especial }

    [Header("Referencias de Fennecs")]
    [SerializeField] protected List<movimientosNiveles> fennecs; // protected para que el hijo lo vea

    [Header("Configuración de Nivel")] 
    [SerializeField] protected float tiempoInicio = 60f;
    [SerializeField] protected int maxFennecsActivos = 3;
    [SerializeField] protected float minIntervaloSpawn = 0.8f; 
    [SerializeField] protected float maxIntervaloSpawn = 1.5f; 
    [Range(0, 100)] [SerializeField] protected float chanceTopoEspecial = 15f; 

    [Header("Valores de Puntuación")]
    [SerializeField] protected int puntosPorTopoNormal = 1;
    [SerializeField] protected int puntosPorTopoEspecial = 5;
    [SerializeField] protected float tiempoExtraPorEspecial = 3f;

    [Header("UI Objects")]
    [SerializeField] protected TextMeshProUGUI textoPuntaje;
    [SerializeField] protected TextMeshProUGUI textoTiempo;
    
    [Header("Pantalla Final UI")]
    [SerializeField] protected GameObject panelFinPartida;
    [SerializeField] protected TextMeshProUGUI textoPuntajeFinal;
    [SerializeField] protected TextMeshProUGUI textoMonedasGanadas;
    
    [Header("Pausa UI")]
    public GameObject menuPausa;
    public GameObject canvasPowerUps;
    public GameObject botonPuntosDobles;

    [Header("Sistema de Vidas Visual")]
    [SerializeField] protected int vidas = 3;
    protected int vidasActuales;
    [SerializeField] protected List<Image> corazonesUI;
    [SerializeField] protected Sprite spriteCorazonLleno; 
    [SerializeField] protected Sprite spriteCorazonVacio;

    // --- Variables ---
    protected float tiempoRestante;
    protected int puntaje; 
    protected bool puntosDoblesActivo = false;
    protected float timerPuntosDobles = 0f;
    protected bool jugando = false;
    protected float timerSpawn = 0f; 
    protected int monedasGanadasEnNivel; 

    public static bool juegopausado = false; 

    protected AudioManager_Tutorial audioManager; 

    protected virtual void Awake(){ // virtual para poder extenderlo si hace falta
        GameObject audioObj = GameObject.FindGameObjectWithTag("Audio");
        if(audioObj != null) audioManager = audioObj.GetComponent<AudioManager_Tutorial>();
    }

    protected virtual void Start() { // virtual
        Time.timeScale = 1;
        juegopausado = false;

        vidasActuales = vidas;
        ActualizarVidasUI();
        
        if (panelFinPartida != null) panelFinPartida.SetActive(false);
        if (menuPausa != null) menuPausa.SetActive(false);
        
        empezarPartida();
    }

    public virtual void empezarPartida() { // virtual
        for(int i = 0; i < fennecs.Count; i++) {
            fennecs[i].esconder();
        }

        tiempoRestante = tiempoInicio;
        puntaje = 0;
        actualizarTextoPuntaje();
        actualizarTextoTiempo(); 
        jugando = true;
        
        StartCoroutine(SpawnLoop());
    }

    protected void ActualizarVidasUI() 
    {
        if (spriteCorazonLleno == null) return;

        for (int i = 0; i < corazonesUI.Count; i++) {
            if (i < vidasActuales) {
                corazonesUI[i].sprite = spriteCorazonLleno;
            } else {
                corazonesUI[i].sprite = spriteCorazonVacio;
            }
        }
    }

    // Corrutina protegida para que el hijo la use o la sobreescriba si quiere
    protected virtual IEnumerator SpawnLoop() {
        while (jugando) {
            // Modificación para soportar el online (donde el tiempo no se detiene)
            if (juegopausado) {
                // Si es offline, esperamos. Si es online, esto se ignorará en el override o se manejará distinto
                yield return null; 
                continue; 
            }

            float esperaSpawn = UnityEngine.Random.Range(minIntervaloSpawn, maxIntervaloSpawn);
            yield return new WaitForSeconds(esperaSpawn);

            if (!jugando || (juegopausado && Time.timeScale == 0)) continue;

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
                    
                    // Al ser el padre, pasamos 'this'. El hijo también pasará 'this' y funcionará por herencia.
                    fennecs[indice].ActivarFennec(this, indice, tipo);
                }
            }
        }
    }

    protected virtual void Update() {
        if (juegopausado || !jugando) return;

        if (puntosDoblesActivo) {
            timerPuntosDobles -= Time.deltaTime;
            if (timerPuntosDobles <= 0) puntosDoblesActivo = false;
        }

        tiempoRestante -= Time.deltaTime;
        actualizarTextoTiempo(); 
        
        if (tiempoRestante <= 0) {
            tiempoRestante = 0;
            actualizarTextoTiempo(); 
            gameOver();
        }
    }

    public void ActivarPuntosDobles(float duracion) {
        puntosDoblesActivo = true;
        timerPuntosDobles = duracion;
    }

    public virtual void TopoGolpeado(int indiceFennec, TipoDeTopo tipo) {
        if (!jugando) return;

        int puntos = (tipo == TipoDeTopo.Especial) ? puntosPorTopoEspecial : puntosPorTopoNormal;

        if (tipo == TipoDeTopo.Especial) {
            AgregarTiempo(tiempoExtraPorEspecial);
        }

        if (puntosDoblesActivo) puntos *= 2;

        puntaje += puntos;
        actualizarTextoPuntaje();
    }

    public virtual void Perdido(int indiceFennec, TipoDeTopo tipo) { 
        if (!jugando) return;

        vidasActuales--;
        int indiceCorazon = Mathf.Clamp(vidasActuales, 0, corazonesUI.Count - 1);

        if (corazonesUI.Count > indiceCorazon && spriteCorazonVacio != null) {
            corazonesUI[indiceCorazon].sprite = spriteCorazonVacio;
        }

        if (vidasActuales <= 0) {
            vidasActuales = 0;
            gameOver();
        }
    }

    public void AgregarTiempo(float segundos) {
        tiempoRestante += segundos;
        if (tiempoRestante > tiempoInicio * 1.5f) tiempoRestante = tiempoInicio * 1.5f;
        actualizarTextoTiempo();
    }

    public virtual void gameOver() { 
        jugando = false;
        StopAllCoroutines(); 
        for(int i = 0; i < fennecs.Count; i++) fennecs[i].DetenerJuego();
        
        monedasGanadasEnNivel = calcularMonedas(puntaje);
        guardarMonedas(monedasGanadasEnNivel);
        mostrarGameOver();
    }

    protected int calcularMonedas(int puntos) {
        int monedasBase = Mathf.FloorToInt(puntos / 10f);
        if (puntos >= 100) monedasBase += 20; 
        if (puntos >= 200) monedasBase += 30;
        return monedasBase;
    }

    protected void guardarMonedas(int monedas) {
        int monedasTotales = PlayerPrefs.GetInt("MonedasTotales", 0);
        monedasTotales += monedas;
        PlayerPrefs.SetInt("MonedasTotales", monedasTotales);
        PlayerPrefs.Save();
    }

    protected void mostrarGameOver(){
        if (panelFinPartida != null){
            panelFinPartida.SetActive(true);
            textoPuntajeFinal.text = "Puntaje final: " + puntaje; 
            if (textoMonedasGanadas != null) textoMonedasGanadas.text = "Monedas ganadas: " + monedasGanadasEnNivel;
        }
    }

    public virtual void IrAlMenu() {
        if (audioManager != null && audioManager.sfx_button != null) {
            audioManager.PlaySFX(audioManager.sfx_button); 
            StartCoroutine(DelaySceneLoad(audioManager.sfx_button.length,"LevelSelection"));
        } else {
            Time.timeScale = 1;
            juegopausado = false;
            SceneManager.LoadScene("LevelSelection"); 
        }    
    }

    public virtual void pausar() {
        if (!jugando) return;
        Time.timeScale = 0;
        juegopausado = true;
        if(canvasPowerUps) canvasPowerUps.SetActive(false);
        if(botonPuntosDobles) botonPuntosDobles.SetActive(false);
        menuPausa.SetActive(true);
    }

    public virtual void Reanudar() {
        Time.timeScale = 1;
        juegopausado = false;
        if(canvasPowerUps) canvasPowerUps.SetActive(true);
        if(botonPuntosDobles) botonPuntosDobles.SetActive(true);
        menuPausa.SetActive(false);
    }

    protected void actualizarTextoPuntaje() { if(textoPuntaje) textoPuntaje.text = "Puntaje " + puntaje; }
    
    protected void actualizarTextoTiempo() { 
        if(textoTiempo) {
            int minutos = (int)tiempoRestante / 60;
            int segundos = (int)tiempoRestante % 60;
            textoTiempo.text = minutos.ToString("00") + ":" + segundos.ToString("00");
        }
    }

    protected void UnlockNewLevel(){
        if(SceneManager.GetActiveScene().buildIndex >= PlayerPrefs.GetInt("ReachedIndex")){
            PlayerPrefs.SetInt("ReachedIndex", SceneManager.GetActiveScene().buildIndex + 1);
            PlayerPrefs.SetInt("UnlockedLevel", PlayerPrefs.GetInt("UnlockedLevel", 1) + 1);
            PlayerPrefs.Save();
        }
    }

    protected IEnumerator DelaySceneLoad(float soundDuration,String level) {
        yield return new WaitForSeconds(soundDuration);

        Time.timeScale = 1;
        juegopausado = false;

        SceneManager.LoadScene(level);
    }
}