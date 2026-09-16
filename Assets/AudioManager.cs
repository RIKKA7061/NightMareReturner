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

    bool musicMuted = false;

    private void Start()
    {
        if (audio != null && audio.Length > 0 && musicSource != null)
        {
            musicSource.clip = audio[0];
            musicSource.Play();
        }

        if (!PlayerPrefs.HasKey("musicVolume"))
        {
            PlayerPrefs.SetFloat("musicVolume", 1);
        }
        Load();

        if (!PlayerPrefs.HasKey("musicVolume2"))
        {
            PlayerPrefs.SetFloat("musicVolume2", 1);
        }
        Load2();

        // 슬라이더 값이 같아서 이벤트가 안 불리는 경우에도 볼륨을 확실히 적용
        ApplyVolumes();
    }

    public void PlayerSFX(AudioClip clip)
    {
        if (clip == null || SFXSource == null) return;
        SFXSource.PlayOneShot(clip);
    }

    public void ChangeVolume()
    {
        if (BGMVolume == null) return;
        PlayerPrefs.SetFloat("musicVolume", BGMVolume.value);
        ApplyVolumes();
    }

    public void ChangeVolume2()
    {
        if (SFXVolume == null) return;
        PlayerPrefs.SetFloat("musicVolume2", SFXVolume.value);
        ApplyVolumes();
    }

    // ---- NR 설정 메뉴에서 호출 ----
    public void SetMusicVolume(float v)
    {
        PlayerPrefs.SetFloat("musicVolume", v);
        if (BGMVolume != null) BGMVolume.SetValueWithoutNotify(v);
        ApplyVolumes();
    }

    public void SetSfxVolume(float v)
    {
        PlayerPrefs.SetFloat("musicVolume2", v);
        if (SFXVolume != null) SFXVolume.SetValueWithoutNotify(v);
        ApplyVolumes();
    }

    /// <summary>보스전 등 별도 음악이 나올 때 씬 음악을 잠시 끕니다.</summary>
    public void SetMusicMuted(bool muted)
    {
        musicMuted = muted;
        ApplyVolumes();
    }

    void ApplyVolumes()
    {
        if (musicSource != null) musicSource.volume = musicMuted ? 0f : PlayerPrefs.GetFloat("musicVolume", 1f);
        if (SFXSource != null) SFXSource.volume = PlayerPrefs.GetFloat("musicVolume2", 1f);
    }

    private void Load()
    {
        if (BGMVolume != null) BGMVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("musicVolume"));
    }

    private void Load2()
    {
        if (SFXVolume != null) SFXVolume.SetValueWithoutNotify(PlayerPrefs.GetFloat("musicVolume2"));
    }
}
