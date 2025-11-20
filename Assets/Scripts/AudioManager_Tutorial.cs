using UnityEngine;

public class AudioManager_Tutorial : MonoBehaviour
{
    [Header("---- Audio Source ----")]
    [SerializeField] AudioSource musicsource;
    [SerializeField] AudioSource sfxsource;

    [Header("---- Audio Clip ----")]
    public AudioClip backgroundMusic;
    public AudioClip sfx;
    public AudioClip sfx_button;
    
    private void Start()
    {
        musicsource.clip = backgroundMusic;
        musicsource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        sfxsource.PlayOneShot(clip);
    }

}
