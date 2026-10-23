using BallCatch.Core;
using BallCatch.Gameplay;
using UnityEngine;
using UnityEngine.Audio;

namespace BallCatch.Audio
{
    /// <summary>
    /// Plays music and sound effects through the Audio Mixer groups. Clip slots may be left empty; an empty slot
    /// simply plays nothing. Volume is controlled only by the mixer (see SettingsManager).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private BallController ball;

        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Mixer Routing")]
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("Music")]
        [SerializeField] private AudioClip musicClip;

        [Header("Sound Effects")]
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip wallBounceClip;
        [SerializeField] private AudioClip catchClip;
        [SerializeField] private AudioClip missClip;

        private void Awake()
        {
            musicSource.outputAudioMixerGroup = musicGroup;
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            sfxSource.outputAudioMixerGroup = sfxGroup;
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        private void OnEnable()
        {
            ball.WallBounced += HandleWallBounce;
            gameManager.AttemptResolved += HandleAttemptResolved;
        }

        private void OnDisable()
        {
            ball.WallBounced -= HandleWallBounce;
            gameManager.AttemptResolved -= HandleAttemptResolved;
        }

        private void Start()
        {
            PlayMusic();
        }

        public void PlayMusic()
        {
            if (musicClip == null || musicSource.isPlaying)
                return;

            musicSource.clip = musicClip;
            musicSource.Play();
        }

        public void StopMusic()
        {
            musicSource.Stop();
        }

        public void PlayUiClick() => PlaySfx(uiClickClip);

        public void PlaySfx(AudioClip clip)
        {
            if (clip != null)
                sfxSource.PlayOneShot(clip);
        }

        private void HandleWallBounce() => PlaySfx(wallBounceClip);

        private void HandleAttemptResolved(AttemptResult result)
        {
            PlaySfx(result == AttemptResult.Catch ? catchClip : missClip);
        }
    }
}
