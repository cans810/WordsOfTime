using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [System.Serializable]
    public class EraMusic
    {
        public string eraName;
        public AudioClip musicClip;
        [Range(0f, 1f)]
        public float volume = 0.5f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [SerializeField] private EraMusic[] eraMusics;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeTime = 1.5f; // Time to fade between tracks

    private bool isMusicOn = true;
    private Dictionary<string, EraMusic> musicDictionary = new Dictionary<string, EraMusic>();
    private Coroutine fadeCoroutine;

    public bool IsMusicOn
    {
        get => isMusicOn;
        set
        {
            isMusicOn = value;
            if (!value)
            {
                StopMusic();
            }
            else if (musicSource.clip != null)
            {
                musicSource.Play();
            }
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeMusic()
    {
        musicDictionary.Clear();
        foreach (EraMusic music in eraMusics)
        {
            musicDictionary[music.eraName] = music;
        }

        // Set up AudioSource
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }

    public void PlayEraMusic(string eraName)
    {
        if (!musicDictionary.ContainsKey(eraName))
        {
            Debug.LogWarning($"No music found for era: {eraName}");
            return;
        }

        EraMusic newMusic = musicDictionary[eraName];
        
        // If it's the same music that's already playing, don't restart
        if (musicSource.clip == newMusic.musicClip)
            return;

        // Stop any existing fade
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        // Start fade between tracks
        fadeCoroutine = StartCoroutine(FadeMusic(newMusic));
    }

    private IEnumerator FadeMusic(EraMusic newMusic)
    {
        float startVolume = musicSource.volume;
        float timer = 0;

        // Fade out current music
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeTime);
            yield return null;
        }

        // Change to new music
        musicSource.clip = newMusic.musicClip;
        musicSource.pitch = newMusic.pitch;
        
        if (isMusicOn)
            musicSource.Play();

        // Fade in new music
        timer = 0;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, newMusic.volume, timer / fadeTime);
            yield return null;
        }

        musicSource.volume = newMusic.volume;
    }

    public void StopMusic()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        
        musicSource.Stop();
    }

    public void ToggleMusic()
    {
        IsMusicOn = !IsMusicOn;
    }
}