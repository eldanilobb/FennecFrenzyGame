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
        for(int i = 0; i < buttons.Length; i++)
        {
            if(i + 1 > unlockedLevel)
            {
                buttons[i].interactable = false;
            }
        }
    }
    public void Openlvl(int levelId){
        String levelName = "";
        if(levelId == 1)
        {
             levelName = "Tutorial";
        }else{levelName = "Nivel " + levelId;}
        
        SceneManager.LoadScene(levelName);

    }
}