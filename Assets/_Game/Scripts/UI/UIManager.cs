using BallCatch.Audio;
using BallCatch.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace BallCatch.UI
{
    /// <summary>
    /// Shows exactly one panel for the current game state, keeps the score display in sync, and handles the
    /// Escape shortcut. Button logic lives on the individual panel scripts.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Panels (one visible at a time)")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject confirmPanel;

        [Header("HUD")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Button pauseButton;

        [Header("Audio")]
        [SerializeField] private AudioManager audioManager;

        private void OnEnable()
        {
            gameManager.StateChanged += ShowPanelFor;
            gameManager.ScoreChanged += UpdateScore;
            pauseButton.onClick.AddListener(gameManager.PauseGame);

            foreach (Button button in GetComponentsInChildren<Button>(true))
                button.onClick.AddListener(audioManager.PlayUiClick);
        }

        private void OnDisable()
        {
            gameManager.StateChanged -= ShowPanelFor;
            gameManager.ScoreChanged -= UpdateScore;
            pauseButton.onClick.RemoveListener(gameManager.PauseGame);

            foreach (Button button in GetComponentsInChildren<Button>(true))
                button.onClick.RemoveListener(audioManager.PlayUiClick);
        }

        private void Start()
        {
            ShowPanelFor(gameManager.State);
            UpdateScore(gameManager.Score);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
                return;

            switch (gameManager.State)
            {
                case GameState.Playing:
                case GameState.AttemptTransition:
                    gameManager.PauseGame();
                    break;
                case GameState.Paused:
                    gameManager.ResumeGame();
                    break;
                case GameState.Settings:
                    gameManager.CloseSettings();
                    break;
                case GameState.ConfirmReturnToMenu:
                    gameManager.CancelReturnToMainMenu();
                    break;
            }
        }

        private void ShowPanelFor(GameState state)
        {
            mainMenuPanel.SetActive(state == GameState.MainMenu);
            hudPanel.SetActive(state == GameState.Playing || state == GameState.AttemptTransition);
            pausePanel.SetActive(state == GameState.Paused);
            settingsPanel.SetActive(state == GameState.Settings);
            confirmPanel.SetActive(state == GameState.ConfirmReturnToMenu);
        }

        private void UpdateScore(int score)
        {
            scoreText.text = $"SCORE  {score}";
        }
    }
}
