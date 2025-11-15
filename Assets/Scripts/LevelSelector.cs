using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class LevelSelector : MonoBehaviour

{

    public Button[] lvlButtons;

    void Start()
    {
        int levelAt = PlayerPrefs.GetInt("LevelAt", 2);
        for (int i=0; i<lvlButtons.Length; i++)
        {
            if (i + 2 > levelAt)
            {
                lvlButtons[i].interactable = false;
            }
        }
    }

    public void Level_tuto()

    {

        SceneManager.LoadSceneAsync("tutorial");

    }

        public void Level_one()

    {

        SceneManager.LoadSceneAsync("Nivel 1");

    }
}