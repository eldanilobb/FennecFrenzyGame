using System;
using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;

//AudioManager_Selection audioManager;


public class LevelSelector : MonoBehaviour
{
    public Button[] buttons;

    private void Awake()
    {
        int unlockedLevel = PlayerPrefs.GetInt("unlockedLevel", 1);
        int corrected = unlockedLevel;
        if (buttons != null)
        {
            corrected = Mathf.Clamp(unlockedLevel, 1, buttons.Length);
        }
        else
        {
            Debug.LogWarning("LevelSelector: 'buttons' is null in the inspector.");
        }

        if (corrected != unlockedLevel)
        {
            PlayerPrefs.SetInt("unlockedLevel", corrected);
            PlayerPrefs.Save();
            Debug.Log($"LevelSelector: corrected unlockedLevel from {unlockedLevel} to {corrected}");
        }

        for(int i=0;i<buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }
        for(int i = 0; i < corrected; i++)
        {
            buttons[i].interactable = true;
        }
    }
    public void Openlvl(int levelId){
        String levelName = "";
        if(levelId == 0)
        {
             levelName = "Tutorial";
        }else{levelName = "Nivel " + levelId;}
        
        SceneManager.LoadScene(levelName);

    }
}