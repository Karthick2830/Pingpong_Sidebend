using BallCatch.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BallCatch.UI
{
    /// <summary>
    /// Shows exercise progress next to the existing ball score: one horizontal stats row in the top bar and a
    /// guidance banner (next side + validator feedback). Read-only: it never changes game or exercise state.
    /// </summary>
    public class ExerciseProgressHUD : MonoBehaviour
    {
        private const string Highlight = "#FFD54F";

        [SerializeField] private ExerciseBallMovementBridge bridge;
        [SerializeField, Tooltip("Single-line stats row: set, reps, left, right.")]
        private Text progressText;
        [SerializeField, Tooltip("Guidance banner text: next direction and validator feedback.")]
        private Text feedbackText;
        [SerializeField, Tooltip("Small tracking view (camera + skeleton) shown in the top-left corner.")]
        private RawImage previewImage;

        private void Update()
        {
            if (previewImage != null && previewImage.texture != bridge.PreviewTexture)
            {
                previewImage.texture = bridge.PreviewTexture;
                previewImage.enabled = bridge.PreviewTexture != null;
            }

            if (!bridge.IsConnected)
            {
                progressText.text = string.Empty;
                if (feedbackText != null)
                    feedbackText.text = bridge.Feedback;
                return;
            }

            if (bridge.ExerciseCompleted)
            {
                progressText.text = $"SETS  <color={Highlight}>{bridge.TotalSets}/{bridge.TotalSets}</color>";
                if (feedbackText != null)
                    feedbackText.text = "EXERCISE COMPLETE";
                return;
            }

            progressText.text =
                $"SET <color={Highlight}>{bridge.CurrentSet}/{bridge.TotalSets}</color>     " +
                $"REPS <color={Highlight}>{bridge.CurrentReps}/{bridge.RepsPerSet}</color>     " +
                $"LEFT <color={Highlight}>{bridge.LeftReps}/{bridge.RepsPerSide}</color>     " +
                $"RIGHT <color={Highlight}>{bridge.RightReps}/{bridge.RepsPerSide}</color>";

            if (feedbackText != null)
            {
                string feedback = bridge.Feedback;
                string guidance = $"BEND <color={Highlight}>{DirectionName(bridge.ExpectedDirection)}</color>";
                feedbackText.text = string.IsNullOrEmpty(feedback) ? guidance : $"{guidance}   |   {feedback}";
            }
        }

        private static string DirectionName(int direction)
        {
            if (direction < 0) return "LEFT";
            if (direction > 0) return "RIGHT";
            return "EITHER SIDE";
        }
    }
}
