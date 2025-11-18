using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("---- Audio Source ----")]
    [SerializeField] AudioSource musicsource;
    [SerializeField] AudioSource sfxsource;

    [Header("---- Audio Clip ----")]
    public AudioClip backgroundMusic;
    public AudioClip sfx;
    
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
