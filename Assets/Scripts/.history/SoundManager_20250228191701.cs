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
        public float volume = 0.22f;
        [Range(0.1f, 3f)]
        public float pitch = 1f;
    }

    [SerializeField] private Sound[] sounds;
    [SerializeField] private EraMusic[] eraMusics;
    [SerializeField] private AudioSource effectsSource;
    [SerializeField] private AudioSource musicSource;

    private bool isSoundOn = true;
    private bool isMusicOn = true;
    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
    private Dictionary<string, EraMusic> musicDictionary = new Dictionary<string, EraMusic>();
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
                }
                else if (!isMusicOn && musicSource.isPlaying)
                {
                    musicSource.Stop();
                }
            }
        }
    }

    public float MusicVolume => musicSource ? musicSource.volume : 0.22f;
    public float SoundVolume => effectsSource ? effectsSource.volume : 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
                musicSource.volume = 0f;
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
        musicSource.volume = 0f;  // Ensure volume is 0
        musicSource.Stop();  // Make sure it's stopped initially
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
        currentEraMusic = newMusic;
        
        if (!IsMusicOn)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
            return;
        }

        if (musicSource != null)
        {
            musicSource.clip = newMusic.musicClip;
            musicSource.volume = 0.22f;
            musicSource.Play();
        }
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

    public void PlayMusicWithDelay()
    {
        if (currentEraMusic != null && musicSource != null && IsMusicOn)
        {
            // Check if music is already playing
            if (!musicSource.isPlaying)
            {
                // Only set clip and play if not already playing
                musicSource.clip = currentEraMusic.musicClip;
                musicSource.Play();
            }
            // Always ensure volume is set correctly
            musicSource.volume = 0.25f;
        }
    }

    public bool IsMusicPlaying()
    {
        return musicSource != null && musicSource.isPlaying;
    }
} 