using BallCatch.Core;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace BallCatch.UI
{
    /// <summary>
    /// "How to play" panel shown after Play is pressed: reference video, instructions, a skip button and a
    /// countdown that starts the session automatically. The panel is only active in the HowToPlay state, so
    /// everything starts in OnEnable and stops in OnDisable. Timing uses unscaled time.
    /// </summary>
    public class HowToPlayUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoImage;
        [SerializeField] private Text countdownText;
        [SerializeField] private Button skipButton;
        [SerializeField, Min(1f), Tooltip("Seconds before the game starts on its own.")]
        private float durationSeconds = 10f;

        private RenderTexture videoTexture;
        private float remaining;
        private bool finished;

        private void OnEnable()
        {
            remaining = durationSeconds;
            finished = false;
            UpdateCountdown();

            skipButton.onClick.AddListener(StartGame);

            if (videoTexture == null)
                videoTexture = new RenderTexture(1280, 720, 0);
            videoPlayer.targetTexture = videoTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoImage.texture = videoTexture;

            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.Prepare();
        }

        private void OnDisable()
        {
            skipButton.onClick.RemoveListener(StartGame);
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }

        private static void OnVideoPrepared(VideoPlayer source) => source.Play();

        private static void OnVideoError(VideoPlayer source, string message) =>
            Debug.LogWarning($"[HowToPlayUI] Reference video failed: {message}");

        private void OnDestroy()
        {
            if (videoTexture != null)
            {
                videoTexture.Release();
                Destroy(videoTexture);
            }
        }

        private void Update()
        {
            if (finished)
                return;

            remaining -= Time.unscaledDeltaTime;
            UpdateCountdown();
            if (remaining <= 0f)
                StartGame();
        }

        private void UpdateCountdown()
        {
            countdownText.text = $"Starting in {Mathf.Max(0, Mathf.CeilToInt(remaining))}s";
        }

        private void StartGame()
        {
            if (finished)
                return;

            finished = true;
            gameManager.StartNewSession();
        }
    }
}
