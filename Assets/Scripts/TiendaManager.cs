using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TiendaManager : MonoBehaviour
{
    AudioManager_Selection audioManager;
    private void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager_Selection>();

    }
    public void goTienda(){
    if (audioManager != null && audioManager.sfx != null)
    {
        audioManager.PlaySFX(audioManager.sfx); 

        StartCoroutine(DelaySceneLoad(audioManager.sfx.length));
    }
    else
    {
        SceneManager.LoadScene("Tienda");
    }
}
    private IEnumerator DelaySceneLoad(float soundDuration)
    {
        yield return new WaitForSeconds(soundDuration);
        SceneManager.LoadScene("Tienda");
    }
}
