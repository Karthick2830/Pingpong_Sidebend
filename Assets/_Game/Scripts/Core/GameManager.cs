using System;
using System.Collections;
using BallCatch.Gameplay;
using UnityEngine;

namespace BallCatch.Core
{
    /// <summary>
    /// Owns the game state, score and target side, and coordinates attempts. Each attempt resolves exactly once:
    /// the first catch or miss moves the state out of Playing, so duplicate collision callbacks are ignored.
    /// Pausing freezes the simulation with Time.timeScale = 0; every transition goes through a guarded method.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private BallController ball;
        [SerializeField] private PlayerController player;

        [Header("Attempt Flow")]
        [SerializeField, Min(0f), Tooltip("Pause between an attempt resolving and the next launch (scaled time).")]
        private float attemptResetDelay = 0.9f;
        [SerializeField, Tooltip("Skip the main menu and start a session when the scene loads (leave off for the real game).")]
        private bool startSessionOnPlay;

        private Coroutine pendingTransition;
        private GameState stateBeforePause = GameState.Playing;
        private GameState stateBeforeSettings = GameState.MainMenu;

        public GameState State { get; private set; } = GameState.MainMenu;
        public int Score { get; private set; }
        public TargetSide CurrentTarget { get; private set; } = TargetSide.Right;

        public event Action<GameState> StateChanged;
        public event Action<int> ScoreChanged;
        public event Action<AttemptResult> AttemptResolved;

        private void Awake()
        {
            if (ball == null)
                ball = FindFirstObjectByType<BallController>();
            if (player == null)
                player = FindFirstObjectByType<PlayerController>();
        }

        private void OnEnable()
        {
            ball.PlayerHit += HandleCatch;
            ball.GroundHit += HandleMiss;
        }

        private void OnDisable()
        {
            ball.PlayerHit -= HandleCatch;
            ball.GroundHit -= HandleMiss;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            // Apply the initial state so listeners (UI, player input) start in sync.
            ApplyState(State);
            if (startSessionOnPlay)
                StartNewSession();
        }

        /// <summary>Begins a completely fresh session: score 0, target RIGHT, ball and player reset.</summary>
        public void StartNewSession()
        {
            CancelPendingTransition();

            Score = 0;
            ScoreChanged?.Invoke(Score);
            CurrentTarget = TargetSide.Right;

            player.ResetToStart();
            ball.Park();
            SetState(GameState.Playing);
            ball.Launch(CurrentTarget);
        }

        public void Restart() => StartNewSession();

        public void PauseGame()
        {
            if (State != GameState.Playing && State != GameState.AttemptTransition)
                return;

            stateBeforePause = State;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused)
                return;

            SetState(stateBeforePause);
        }

        public void OpenSettings()
        {
            if (State != GameState.Paused && State != GameState.MainMenu)
                return;

            stateBeforeSettings = State;
            SetState(GameState.Settings);
        }

        public void CloseSettings()
        {
            if (State != GameState.Settings)
                return;

            SetState(stateBeforeSettings);
        }

        public void RequestReturnToMainMenu()
        {
            if (State == GameState.Paused)
                SetState(GameState.ConfirmReturnToMenu);
        }

        public void CancelReturnToMainMenu()
        {
            if (State == GameState.ConfirmReturnToMenu)
                SetState(GameState.Paused);
        }

        /// <summary>Ends the current session and clears all active gameplay state.</summary>
        public void ConfirmReturnToMainMenu()
        {
            if (State != GameState.ConfirmReturnToMenu)
                return;

            CancelPendingTransition();
            Score = 0;
            ScoreChanged?.Invoke(Score);
            CurrentTarget = TargetSide.Right;
            player.ResetToStart();
            ball.Park();
            SetState(GameState.MainMenu);
        }

        private void HandleCatch() => ResolveAttempt(AttemptResult.Catch);

        private void HandleMiss() => ResolveAttempt(AttemptResult.Miss);

        private void ResolveAttempt(AttemptResult result)
        {
            if (State != GameState.Playing)
                return;

            SetState(GameState.AttemptTransition);

            if (result == AttemptResult.Catch)
            {
                Score++;
                ScoreChanged?.Invoke(Score);
            }

            CurrentTarget = CurrentTarget == TargetSide.Right ? TargetSide.Left : TargetSide.Right;
            AttemptResolved?.Invoke(result);
            pendingTransition = StartCoroutine(StartNextAttempt());
        }

        private IEnumerator StartNextAttempt()
        {
            yield return new WaitForSeconds(attemptResetDelay);

            pendingTransition = null;
            player.ResetToStart();
            ball.Park();
            SetState(GameState.Playing);
            ball.Launch(CurrentTarget);
        }

        private void CancelPendingTransition()
        {
            if (pendingTransition == null)
                return;

            StopCoroutine(pendingTransition);
            pendingTransition = null;
        }

        private void SetState(GameState newState)
        {
            if (State == newState)
                return;

            State = newState;
            ApplyState(newState);
            StateChanged?.Invoke(State);
        }

        /// <summary>Applies the side effects every state needs: simulation freeze and player input gating.</summary>
        private void ApplyState(GameState state)
        {
            bool frozen = state == GameState.Paused || state == GameState.Settings
                || state == GameState.ConfirmReturnToMenu;
            Time.timeScale = frozen ? 0f : 1f;
            player.SetInputEnabled(state == GameState.Playing || state == GameState.AttemptTransition);
        }
    }
}
