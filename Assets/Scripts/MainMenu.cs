using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour {
    AudioManager audioManager;

    [SerializeField] private TextMeshProUGUI textoMonedasMenu;

    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();

    }
    private void OnEnable() {
        MostrarMonedasAcumuladas();
    }

    private void MostrarMonedasAcumuladas() {

        int monedasTotales = PlayerPrefs.GetInt("MonedasTotales", 0);
        
        if (textoMonedasMenu != null) {
            textoMonedasMenu.text = "Monedas: " + monedasTotales;
        }
    }


  public void PlayGame(){
        if (audioManager != null && audioManager.sfx != null)
        {
            audioManager.PlaySFX(audioManager.sfx); 

            StartCoroutine(DelaySceneLoad(audioManager.sfx.length));
        }
        else
        {
            SceneManager.LoadScene("LevelSelection");
        }
    }

    private IEnumerator DelaySceneLoad(float soundDuration)
    {
        yield return new WaitForSeconds(soundDuration);
        SceneManager.LoadScene("LevelSelection");
    }


    public void QuitGame(){

        Application.Quit();

    }



}