using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI textoMonedasMenu;

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
        
        SceneManager.LoadSceneAsync("LevelSelection");

    }

    public void QuitGame(){

        Application.Quit();

    }



}