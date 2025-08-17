using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : Singleton<SFXManager>
{
    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource sfxSource;

    [SerializeField]
    private AudioSource sfxSource2;

    [SerializeField]
    private AudioSource sfxSource3;

    [SerializeField]
    private AudioSource durationSFXSource;

    [SerializeField]
    private AudioSource musicSource;

    [Header("Audio Clips")]
    [SerializeField]
    private AudioClip[] audioClips;

    [SerializeField]
    private AudioClip backgroundMusic;

    [Header("Volume Settings")]
    [SerializeField]
    private float sfxMaxVolume = 1f;

    [SerializeField]
    private float musicMaxVolume = 1f;

    private float sfxVolume = 1f;
    private float musicVolume = 1f;

    public float SFXVolume => sfxVolume / sfxMaxVolume;
    public float MusicVolume => musicVolume / musicMaxVolume;

    private Dictionary<string, AudioClip> clipDictionary = new Dictionary<string, AudioClip>();

    public System.Action<float> OnSFXVolumeChanged;
    public System.Action<float> OnMusicVolumeChanged;

    void Start()
    {
        // Set initial volume
        sfxVolume = sfxMaxVolume;
        musicVolume = musicMaxVolume;

        OnSFXVolumeChanged?.Invoke(sfxVolume);
        OnMusicVolumeChanged?.Invoke(musicVolume);

        // Create audio sources if they don't exist
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }

        if (sfxSource2 == null)
        {
            sfxSource2 = gameObject.AddComponent<AudioSource>();
            sfxSource2.playOnAwake = false;
        }

        if (sfxSource3 == null)
        {
            sfxSource3 = gameObject.AddComponent<AudioSource>();
            sfxSource3.playOnAwake = false;
        }

        if (durationSFXSource == null)
        {
            durationSFXSource = gameObject.AddComponent<AudioSource>();
            durationSFXSource.playOnAwake = false;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        // Build dictionary from audio clips
        BuildClipDictionary();

        // Play background music
        musicSource.clip = backgroundMusic;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void BuildClipDictionary()
    {
        clipDictionary.Clear();
        foreach (AudioClip clip in audioClips)
        {
            if (clip != null)
            {
                clipDictionary[clip.name] = clip;
            }
        }
    }

    public void PlaySFX(string clipName, int channel = 0)
    {
        if (clipDictionary.ContainsKey(clipName))
        {
            switch (channel)
            {
                case 0:
                    sfxSource.PlayOneShot(clipDictionary[clipName], sfxVolume);
                    break;
                case 1:
                    sfxSource2.PlayOneShot(clipDictionary[clipName], sfxVolume);
                    break;
                case 2:
                    sfxSource3.PlayOneShot(clipDictionary[clipName], sfxVolume);
                    break;
            }
        }
        else
        {
            Debug.LogWarning($"SFX clip '{clipName}' not found!");
        }
    }

    public void PlaySFXForDuration(string clipName, float duration)
    {
        if (clipDictionary.ContainsKey(clipName))
        {
            durationSFXSource.clip = clipDictionary[clipName];
            durationSFXSource.Play();
            StartCoroutine(StopSFXAfterDuration(duration));
        }
    }

    private IEnumerator StopSFXAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        durationSFXSource.Stop();
    }

    public void PauseMusic()
    {
        musicSource?.Pause();
    }

    public void ResumeMusic()
    {
        musicSource?.UnPause();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume * musicMaxVolume;
        musicSource.volume = musicVolume;
        OnMusicVolumeChanged?.Invoke(musicVolume);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume * sfxMaxVolume;
        sfxSource.volume = sfxVolume;
        sfxSource2.volume = sfxVolume;
        sfxSource3.volume = sfxVolume;
        durationSFXSource.volume = sfxVolume;
        OnSFXVolumeChanged?.Invoke(sfxVolume);
    }
}
