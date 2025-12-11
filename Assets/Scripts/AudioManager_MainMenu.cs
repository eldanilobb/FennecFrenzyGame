using UnityEngine;
using UnityEngine.UI;
public class AudioManager : MonoBehaviour
{
    [Header("---- Audio Source ----")]
    [SerializeField] AudioSource musicsource;
    [SerializeField] AudioSource sfxsource;
    [SerializeField] Slider volumeSlider;
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
    public void ChangeVolume()
    {
        AudioListener.volume = volumeSlider.value;
        Save();
    }

    private void Load()
    {
        volumeSlider.value = PlayerPrefs.GetFloat("musicVolume");
    }

    private void Save()
    {
        PlayerPrefs.SetFloat("musicVolume", volumeSlider.value);
    }

}
