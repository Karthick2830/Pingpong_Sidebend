using BallCatch.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BallCatch.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button exitButton;

        private void OnEnable()
        {
            playButton.onClick.AddListener(gameManager.OpenHowToPlay);
            settingsButton.onClick.AddListener(gameManager.OpenSettings);
            exitButton.onClick.AddListener(Quit);
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(gameManager.OpenHowToPlay);
            settingsButton.onClick.RemoveListener(gameManager.OpenSettings);
            exitButton.onClick.RemoveListener(Quit);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
