using BallCatch.Core;
using BallCatch.Gameplay;
using UnityEngine;

namespace BallCatch.Audio
{
    /// <summary>
    /// Spoken guidance for the exercise. "Make sure all joints are visible" repeats while the body is not tracked
    /// properly; "Bend left" / "Bend right" plays every time a ball is launched, following the ball's target side
    /// (target left = bend left, target right = bend right). Read-only: it only listens to the game and plays
    /// clips, and never touches movement, reps or resets.
    /// </summary>
    public class ExerciseVoiceGuidance : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ExerciseBallMovementBridge bridge;
        [SerializeField] private BallController ball;
        [SerializeField, Tooltip("Plays one-shots through the sound-effects mixer group.")]
        private AudioManager audioManager;

        [Header("Voice clips")]
        [SerializeField] private AudioClip jointsVisibleClip;
        [SerializeField] private AudioClip bendLeftClip;
        [SerializeField] private AudioClip bendRightClip;

        [Header("Timing")]
        [SerializeField, Min(0f), Tooltip("Tracking must stay bad this long before the voice speaks.")]
        private float notTrackedDelay = 1.5f;
        [SerializeField, Min(1f), Tooltip("Gap between repeats of the joints message while tracking stays bad.")]
        private float jointsRepeatSeconds = 8f;

        private float notTrackedTimer;
        private float nextJointsTime;
        private float voiceBusyUntil;
        private AudioClip pendingBendCue;
        private float pendingBendCueTime;
        private const float BendCueMaxAgeSeconds = 3f;

        private void Awake()
        {
            if (ball == null)
                ball = FindFirstObjectByType<BallController>();
        }

        private void OnEnable() => ball.Launched += OnBallLaunched;

        private void OnDisable() => ball.Launched -= OnBallLaunched;

        /// <summary>One cue per ball: the clip follows the side the ball is heading to.</summary>
        private void OnBallLaunched()
        {
            pendingBendCue = ball.TargetSide == TargetSide.Left ? bendLeftClip : bendRightClip;
            pendingBendCueTime = Time.unscaledTime;
        }

        private void Update()
        {
            bool playing = gameManager.State == GameState.Playing
                || gameManager.State == GameState.AttemptTransition;

            if (!playing || !bridge.IsConnected)
            {
                // Nothing is said in menus, pause or results; a stale cue is dropped.
                notTrackedTimer = 0f;
                pendingBendCue = null;
                return;
            }

            float now = Time.unscaledTime;
            bool tracked = bridge.TrackingActive && bridge.PoseReady;
            UpdateJointsGuidance(now, tracked);

            // A cue that could not be spoken in time belongs to a ball that is already gone: drop it.
            if (pendingBendCue != null && now - pendingBendCueTime > BendCueMaxAgeSeconds)
                pendingBendCue = null;

            // The joints message takes priority; the bend cue waits for a free voice and a tracked body.
            if (pendingBendCue != null && tracked && now >= voiceBusyUntil)
            {
                Speak(pendingBendCue, now);
                pendingBendCue = null;
            }
        }

        private void UpdateJointsGuidance(float now, bool tracked)
        {
            if (tracked)
            {
                notTrackedTimer = 0f;
                nextJointsTime = 0f;
                return;
            }

            notTrackedTimer += Time.unscaledDeltaTime;
            if (notTrackedTimer < notTrackedDelay || now < nextJointsTime)
                return;

            Speak(jointsVisibleClip, now);
            nextJointsTime = now + Mathf.Max(jointsRepeatSeconds, ClipLength(jointsVisibleClip) + 3f);
        }

        private void Speak(AudioClip clip, float now)
        {
            if (clip == null || audioManager == null)
                return;

            audioManager.PlaySfx(clip);
            voiceBusyUntil = now + clip.length;
        }

        private static float ClipLength(AudioClip clip) => clip != null ? clip.length : 0f;
    }
}
