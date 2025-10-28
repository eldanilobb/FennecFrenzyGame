using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour {
    [SerializeField] private List<movimientos> fennecs;

    [Header("Ui Objects")]
    [SerializeField] private GameObject botonJugar;

    [Header("Configuración de Dificultad")]
    [SerializeField] private int maxFennecsActivos = 3;
    [SerializeField] private float intervaloSpawn = 1f;
    [SerializeField] private TextMeshProUGUI textoPuntaje;
    [SerializeField] private TextMeshProUGUI textoTiempo;

    private float tiempoInicio = 60f;
    private float tiempoRestante;
    private HashSet<movimientos> fennecsActuales = new HashSet<movimientos>();
    private int puntaje;
    private bool jugando = false;
    private float timerSpawn = 0f;

    public void empezarPartida() {
        botonJugar.SetActive(false);

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

    public void GameOver(int tipo) {
        jugando = false;
        botonJugar.SetActive(true);

        for(int i = 0; i < fennecs.Count; i++) {
            fennecs[i].DetenerJuego();
        }
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
        if(jugando) {
            tiempoRestante -= Time.deltaTime;
            timerSpawn -= Time.deltaTime;
            actualizarTextoTiempo(); 
            if(tiempoRestante <= 0) {
                tiempoRestante = 0;
                actualizarTextoTiempo(); 
                GameOver(0);
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
}
