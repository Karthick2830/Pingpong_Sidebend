using UnityEngine;
using UnityEngine.Audio;

namespace BallCatch.Settings
{
    /// <summary>
    /// Owns the player's volume preferences. Slider values (0..1) are converted to decibels and written to the
    /// Audio Mixer's exposed parameters; individual AudioSource volumes are never touched. Values persist in PlayerPrefs.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        private const string MusicPrefsKey = "settings.musicVolume";
        private const string SfxPrefsKey = "settings.sfxVolume";
        private const float MinDecibels = -80f;

        [SerializeField] private AudioMixer mixer;

        [Header("Exposed Mixer Parameters")]
        [SerializeField] private string musicParameter = "MusicVolume";
        [SerializeField] private string sfxParameter = "SFXVolume";

        [Header("Defaults")]
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defaultSfxVolume = 0.8f;

        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        private void Awake()
        {
            MusicVolume = PlayerPrefs.GetFloat(MusicPrefsKey, defaultMusicVolume);
            SfxVolume = PlayerPrefs.GetFloat(SfxPrefsKey, defaultSfxVolume);
        }

        private void Start()
        {
            ApplyToMixer();
        }

        private void OnDisable()
        {
            Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
                Save();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            mixer.SetFloat(musicParameter, ToDecibels(MusicVolume));
            PlayerPrefs.SetFloat(MusicPrefsKey, MusicVolume);
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            mixer.SetFloat(sfxParameter, ToDecibels(SfxVolume));
            PlayerPrefs.SetFloat(SfxPrefsKey, SfxVolume);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }

        private void ApplyToMixer()
        {
            mixer.SetFloat(musicParameter, ToDecibels(MusicVolume));
            mixer.SetFloat(sfxParameter, ToDecibels(SfxVolume));
        }

        /// <summary>Logarithmic mapping of a linear 0..1 slider to decibels; zero maps safely to silence.</summary>
        public static float ToDecibels(float linear)
        {
            if (linear <= 0.0001f)
                return MinDecibels;

            return Mathf.Clamp(20f * Mathf.Log10(linear), MinDecibels, 0f);
        }
    }
}
