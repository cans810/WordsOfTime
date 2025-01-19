using UnityEngine;
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

    [SerializeField] private Sound[] sounds;
    [SerializeField] private AudioSource effectsSource;
    
    // For background music (if needed)
    [SerializeField] private AudioSource musicSource;

    private bool isSoundOn = true;
    public bool IsSoundOn 
    { 
        get => isSoundOn;
        set 
        {
            isSoundOn = value;
            effectsSource.mute = !value;
        }
    }

    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSounds();
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

    public void StopAllSounds()
    {
        effectsSource.Stop();
    }

    public void ToggleSound()
    {
        IsSoundOn = !IsSoundOn;
    }
} 