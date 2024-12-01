using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("----------- Audio Source -----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("------------ Audio Clip ------------")]
    public new AudioClip[] audio;

    [Header("------------ Setting UI ------------")]
    public Slider BGMVolume;
    public Slider SFXVolume;

    private void Start()
    {
        musicSource.clip = audio[0];
        musicSource.Play();

        if (!PlayerPrefs.HasKey("musicVolume"))
        {
            PlayerPrefs.SetFloat("musicVolume", 1);
            Load();
        }
        else
        {
            Load();
        }

        if (!PlayerPrefs.HasKey("musicVolume2"))
        {
            PlayerPrefs.SetFloat("musicVolume2", 1);
            Load2();
        }
        else
        {
            Load2();
        }
    }

    public void PlayerSFX(AudioClip clip)
    {
        SFXSource.PlayOneShot(clip);
    }

    public void ChangeVolume()
    {
        musicSource.volume = BGMVolume.value;
        Save();
    }

    public void ChangeVolume2()
    {
        SFXSource.volume = SFXVolume.value;
        Save2();
    }

    private void Load()
    {
        BGMVolume.value = PlayerPrefs.GetFloat("musicVolume");
    }

    private void Load2()
    {
        SFXVolume.value = PlayerPrefs.GetFloat("musicVolume2");
    }

    private void Save()
    {
        PlayerPrefs.SetFloat("musicVolume", BGMVolume.value);
    }

    private void Save2()
    {
        PlayerPrefs.SetFloat("musicVolume2", SFXVolume.value);
    }
}
