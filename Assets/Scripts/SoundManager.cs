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
            if (!value)
            {
                StopMusic();
            }
            else
            {
                if (currentEraMusic != null && musicSource != null)
                {
                    musicSource.clip = currentEraMusic.musicClip;
                    musicSource.Play();
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
            
            // Don't start music automatically
            if (musicSource != null)
            {
                musicSource.playOnAwake = false;
                musicSource.Stop();
            }
            
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
        currentEraMusic = newMusic;
        
        // Don't play if music is disabled
        if (!IsMusicOn)
        {
            if (musicSource != null && musicSource.isPlaying)
            {
                musicSource.Stop();
            }
            return;
        }

        if (musicSource != null && musicSource.clip == newMusic.musicClip && musicSource.isPlaying)
            return;

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
} 