using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance => instance;

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
    private EraMusic currentEraMusic;

    public bool IsSoundOn
    {
        get => isSoundOn;
        set => isSoundOn = value;
    }

    public bool IsMusicOn
    {
        get => isMusicOn;
        set
        {
            isMusicOn = value;
            if (musicSource != null)
            {
                if (isMusicOn && !musicSource.isPlaying && currentEraMusic != null)
                {
                    musicSource.clip = currentEraMusic.musicClip;
                    musicSource.Play();
                    Debug.Log("Resumed era music");
                }
                else if (!isMusicOn && musicSource.isPlaying)
                {
                    musicSource.Stop();
                    Debug.Log("Paused era music");
                }
            }
        }
    }

    public float MusicVolume => musicSource ? musicSource.volume : 1f;
    public float SoundVolume => effectsSource ? effectsSource.volume : 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize audio sources first
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.Stop();
            }
            
            if (effectsSource == null)
            {
                effectsSource = gameObject.AddComponent<AudioSource>();
                effectsSource.playOnAwake = false;
            }
            
            InitializeSounds();
            InitializeMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load settings
        LoadSettings();
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
        Debug.Log($"Attempting to play music for era: {eraName}");

        if (!musicDictionary.ContainsKey(eraName))
        {
            Debug.LogWarning($"No music found for era: {eraName}");
            return;
        }

        EraMusic newMusic = musicDictionary[eraName];
        Debug.Log($"Found music for era {eraName}: {(newMusic.musicClip != null ? "clip exists" : "no clip")}");
        currentEraMusic = newMusic;
        
        // Don't play if music is disabled
        if (!IsMusicOn)
        {
            Debug.Log("Music is disabled, not playing");
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
            return;
        }

        if (musicSource != null && musicSource.clip == newMusic.musicClip && musicSource.isPlaying)
        {
            Debug.Log("Same music already playing, skipping");
            return;
        }

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeMusic(newMusic));
    }

    private IEnumerator FadeMusic(EraMusic newMusic)
    {
        // Don't start fading if music is disabled
        if (!IsMusicOn)
        {
            yield break;
        }

        float fadeTime = 1f;
        float elapsed = 0;

        // Fade out current music
        if (musicSource != null && musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0, elapsed / fadeTime);
                yield return null;
            }
            musicSource.Stop();
        }

        // Only proceed if music is still enabled
        if (!IsMusicOn)
        {
            yield break;
        }

        // Change clip and start playing
        musicSource.clip = newMusic.musicClip;
        musicSource.Play();

        // Fade in new music
        elapsed = 0;
        while (elapsed < fadeTime && IsMusicOn)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, 1, elapsed / fadeTime);
            yield return null;
        }

        // If music was disabled during fade in, stop playing
        if (!IsMusicOn)
        {
            musicSource.Stop();
        }

        fadeCoroutine = null;
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
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

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = Mathf.Clamp01(volume);
        }
    }

    public void SetSoundVolume(float volume)
    {
        if (effectsSource != null)
        {
            effectsSource.volume = Mathf.Clamp01(volume);
        }
    }

    private void LoadEraMusic(string eraName, string resourcePath)
    {
        AudioClip musicClip = Resources.Load<AudioClip>(resourcePath);
        if (musicClip != null)
        {
            musicDictionary[eraName] = new EraMusic { musicClip = musicClip };
        }
        else
        {
            Debug.LogError($"Failed to load music for era {eraName} from path {resourcePath}");
        }
    }

    private void LoadSettings()
    {
        if (GameManager.Instance != null)
        {
            GameSettings settings = GameManager.Instance.GetSettings();
            IsMusicOn = settings.musicEnabled;
            IsSoundOn = settings.soundEnabled;
            SetMusicVolume(settings.musicVolume);
            SetSoundVolume(settings.soundVolume);
            Debug.Log($"Loaded sound settings: Music={IsMusicOn}, Sound={IsSoundOn}");
        }
    }
} 