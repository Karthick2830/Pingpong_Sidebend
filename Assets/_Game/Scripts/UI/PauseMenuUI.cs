using BallCatch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BallCatch.UI
{
    /// <summary>Buttons for the pause menu and the "return to main menu" confirmation dialog.</summary>
    public class PauseMenuUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;

        [Header("Pause Menu")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Confirmation Dialog")]
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmCancelButton;

        private void OnEnable()
        {
            resumeButton.onClick.AddListener(gameManager.ResumeGame);
            restartButton.onClick.AddListener(gameManager.Restart);
            settingsButton.onClick.AddListener(gameManager.OpenSettings);
            mainMenuButton.onClick.AddListener(gameManager.RequestReturnToMainMenu);
            confirmYesButton.onClick.AddListener(gameManager.ConfirmReturnToMainMenu);
            confirmCancelButton.onClick.AddListener(gameManager.CancelReturnToMainMenu);
        }

        private void OnDisable()
        {
            resumeButton.onClick.RemoveListener(gameManager.ResumeGame);
            restartButton.onClick.RemoveListener(gameManager.Restart);
            settingsButton.onClick.RemoveListener(gameManager.OpenSettings);
            mainMenuButton.onClick.RemoveListener(gameManager.RequestReturnToMainMenu);
            confirmYesButton.onClick.RemoveListener(gameManager.ConfirmReturnToMainMenu);
            confirmCancelButton.onClick.RemoveListener(gameManager.CancelReturnToMainMenu);
        }
    }
}
