using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SFXUIManager : MonoBehaviour
{
    public Slider sfxSlider;
    public Slider musicSlider;

    void Awake()
    {
        sfxSlider.value = SFXManager.Instance.SFXVolume;
        musicSlider.value = SFXManager.Instance.MusicVolume;

        sfxSlider.onValueChanged.AddListener(OnSFXVolumeUpdated);
        musicSlider.onValueChanged.AddListener(OnMusicVolumeUpdated);
    }

    void OnSFXVolumeUpdated(float value)
    {
        SFXManager.Instance.SetSFXVolume(value);
    }

    void OnMusicVolumeUpdated(float value)
    {
        SFXManager.Instance.SetMusicVolume(value);
    }
}
