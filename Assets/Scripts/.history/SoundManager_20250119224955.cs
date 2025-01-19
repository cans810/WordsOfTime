using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

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

    [SerializeField] private Sound[] sounds;
    [SerializeField] private EraMusic[] eraMusics;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeTime = 1.5f;

    private bool isSoundOn = true;
    private bool isMusicOn = true;
    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
    private Dictionary<string, EraMusic> musicDictionary = new Dictionary<string, EraMusic>();
    private Coroutine fadeCoroutine;

    public bool IsSoundOn 
    { 
        get => isSoundOn;
        set 
        {
            isSoundOn = value;
            effectsSource.mute = !value;
        }
    }

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
            InitializeSounds();
            InitializeMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSounds()
    {
        soundDictionary.Clear();
        foreach (Sound sound in sounds)
        {
            soundDictionary[sound.name] = sound;
        }
    }

    private void InitializeMusic()
    {
        musicDictionary.Clear();
        foreach (EraMusic music in eraMusics)
        {
            musicDictionary[music.eraName] = music;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }
        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }

    public void PlaySound(string soundName)
    {
        if (!isSoundOn || !soundDictionary.ContainsKey(soundName))
        {
            return;
        }

        Sound sound = soundDictionary[soundName];
        effectsSource.pitch = sound.pitch;
        effectsSource.PlayOneShot(sound.clip, sound.volume);
    }

    public void PlayEraMusic(string eraName)
    {
        if (!musicDictionary.ContainsKey(eraName))
        {
            Debug.LogWarning($"No music found for era: {eraName}");
            return;
        }

        EraMusic newMusic = musicDictionary[eraName];
        
        if (musicSource.clip == newMusic.musicClip)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeMusic(newMusic));
    }

    private IEnumerator FadeMusic(EraMusic newMusic)
    {
        float startVolume = musicSource.volume;
        float timer = 0;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, timer / fadeTime);
            yield return null;
        }

        musicSource.clip = newMusic.musicClip;
        musicSource.pitch = newMusic.pitch;
        
        if (isMusicOn)
            musicSource.Play();

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

    public void StopAllSounds()
    {
        effectsSource.Stop();
    }

    public void ToggleSound()
    {
        IsSoundOn = !IsSoundOn;
    }

    public void ToggleMusic()
    {
        IsMusicOn = !IsMusicOn;
    }
} 