// GameManager.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // Necesario para los textos de UI

public class GameManager : MonoBehaviour
{
    [Header("Parámetros del Juego")]
    public float gameDuration = 60f;
    public float minSpawnTime = 0.5f;
    public float maxSpawnTime = 1.5f;

    [Header("Referencias (Arrastrar en el Inspector)")]
    // IMPORTANTE: Esta lista ahora busca tu script "movimientos"
    public List<movimientos> moles; 
    
    [Header("UI (Interfaz)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    // --- Variables Internas ---
    private int score = 0;
    private float timer;
    private bool gameIsRunning = false;

    void Start()
    {
        score = 0;
        scoreText.text = "Score: 0";
        timer = gameDuration;
        timerText.text = "Time: " + timer.ToString("F0");
        
        StartGame();
    }

    public void StartGame()
    {
        gameIsRunning = true;
        StartCoroutine(GameLoop());
        StartCoroutine(TimerTick());
    }

    private IEnumerator GameLoop()
    {
        while (gameIsRunning)
        {
            // 1. Espera un tiempo aleatorio
            float waitTime = Random.Range(minSpawnTime, maxSpawnTime);
            yield return new WaitForSeconds(waitTime);
            
            // 2. Elige un "cocinero" (topo) al azar de la lista
            int index = Random.Range(0, moles.Count);
            
            // 3. ¡Le da la orden de salir!
            // Llama a la función Salir() de TU script 'movimientos.cs'
            moles[index].Salir(this); 
        }
    }

    private IEnumerator TimerTick()
    {
        while (timer > 0)
        {
            yield return new WaitForSeconds(1f);
            timer--;
            timerText.text = "Time: " + timer.ToString("F0");
            
            if (timer <= 0)
            {
                EndGame();
            }
        }
    }

    // El topo (movimientos.cs) llamará a esto cuando sea golpeado
    public void AddScore(int points)
    {
        if (!gameIsRunning) return;
        score += points;
        scoreText.text = "Score: " + score;
    }

    private void EndGame()
    {
        gameIsRunning = false;
        StopAllCoroutines();
        Debug.Log("¡Juego Terminado! Puntuación Final: " + score);
        // Aquí pones la pantalla de Game Over
    }
}
