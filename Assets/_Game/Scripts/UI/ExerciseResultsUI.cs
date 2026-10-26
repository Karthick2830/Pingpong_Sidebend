using BallCatch.Core;
using BallCatch.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BallCatch.UI
{
    /// <summary>
    /// Results panel shown when every exercise set is done: sets completed, ball score, Retry and Main Menu.
    /// The panel is only active in the ExerciseComplete state, so values are refreshed when it is enabled.
    /// </summary>
    public class ExerciseResultsUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private ExerciseBallMovementBridge bridge;

        [SerializeField] private Text setsText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        private void OnEnable()
        {
            setsText.text = $"SETS COMPLETED   {bridge.CompletedSets}/{bridge.TotalSets}";
            scoreText.text = $"BALL SCORE   {gameManager.Score}";

            retryButton.onClick.AddListener(gameManager.Restart);
            mainMenuButton.onClick.AddListener(gameManager.ReturnToMainMenuFromResults);
        }

        private void OnDisable()
        {
            retryButton.onClick.RemoveListener(gameManager.Restart);
            mainMenuButton.onClick.RemoveListener(gameManager.ReturnToMainMenuFromResults);
        }
    }
}
