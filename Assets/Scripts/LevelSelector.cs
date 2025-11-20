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

    AudioManager_Selection audioManager;
    private void Awake()
    {
        
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager_Selection>();
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        
        for(int i=0; i<buttons.Length; i++)
        {
            buttons[i].interactable = false;
        }
        for (int i = 0; i < unlockedLevel && i < buttons.Length; i++)
        {
            buttons[i].interactable = true;
        }
    }
    public void Openlvl(int levelId){
        String levelName = "";
          if(levelId == 1) {
                levelName = "Tutorial";
            }else{levelName = "Nivel " + (levelId-1);}

         if (audioManager != null && audioManager.sfx != null)
        {
            audioManager.PlaySFX(audioManager.sfx); 
            StartCoroutine(DelaySceneLoad(audioManager.sfx.length,levelName));
            }
        else{
            SceneManager.LoadScene(levelName);
        }
        

    }
private IEnumerator DelaySceneLoad(float soundDuration,String level)
    {
        yield return new WaitForSeconds(soundDuration);
        SceneManager.LoadScene(level);
    }

    public void onlne(){
         if (audioManager != null && audioManager.sfx != null)
        {
            audioManager.PlaySFX(audioManager.sfx); 
            StartCoroutine(DelaySceneLoad(audioManager.sfx.length,"online"));
            }
        else{
            SceneManager.LoadScene("online");
        }
        
    }
}