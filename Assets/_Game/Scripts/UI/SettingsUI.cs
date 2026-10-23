using BallCatch.Core;
using BallCatch.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace BallCatch.UI
{
    /// <summary>Settings panel: Back navigation and the Music / SFX volume sliders.</summary>
    public class SettingsUI : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private SettingsManager settingsManager;
        [SerializeField] private Button backButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private void OnEnable()
        {
            backButton.onClick.AddListener(gameManager.CloseSettings);
            musicSlider.onValueChanged.AddListener(settingsManager.SetMusicVolume);
            sfxSlider.onValueChanged.AddListener(settingsManager.SetSfxVolume);
        }

        private void Start()
        {
            // Show the stored preferences without re-applying them.
            musicSlider.SetValueWithoutNotify(settingsManager.MusicVolume);
            sfxSlider.SetValueWithoutNotify(settingsManager.SfxVolume);
        }

        private void OnDisable()
        {
            backButton.onClick.RemoveListener(gameManager.CloseSettings);
            musicSlider.onValueChanged.RemoveListener(settingsManager.SetMusicVolume);
            sfxSlider.onValueChanged.RemoveListener(settingsManager.SetSfxVolume);
        }
    }
}
