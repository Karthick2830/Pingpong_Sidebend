using BallCatch.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BallCatch.UI
{
    /// <summary>
    /// Shows exercise progress next to the existing ball score: stat chips in the top bar, a framed live tracking
    /// preview, and a bottom status bar (tracking state, next side, validator feedback).
    /// Read-only: it never changes game or exercise state.
    /// </summary>
    public class ExerciseProgressHUD : MonoBehaviour
    {
        private const string Highlight = "#FFD54F";
        private static readonly Color ActiveColor = new Color(0.25f, 0.9f, 0.35f);
        private static readonly Color LostColor = new Color(0.95f, 0.3f, 0.3f);

        [SerializeField] private ExerciseBallMovementBridge bridge;

        [Header("Top bar chips")]
        [SerializeField] private Text setText;
        [SerializeField] private Text repsText;
        [SerializeField] private Text leftText;
        [SerializeField] private Text rightText;

        [Header("Live tracking preview")]
        [SerializeField] private RawImage previewImage;
        [SerializeField] private Image previewDot;

        [Header("Bottom status bar")]
        [SerializeField] private Image trackingDot;
        [SerializeField] private Text trackingStatusText;
        [SerializeField] private Text directionText;
        [SerializeField] private Text feedbackText;

        private void Update()
        {
            if (previewImage != null && previewImage.texture != bridge.PreviewTexture)
            {
                previewImage.texture = bridge.PreviewTexture;
                previewImage.enabled = bridge.PreviewTexture != null;
            }

            bool tracking = bridge.IsConnected && bridge.TrackingActive;
            Color dotColor = tracking ? ActiveColor : LostColor;
            if (previewDot != null) previewDot.color = dotColor;
            if (trackingDot != null) trackingDot.color = dotColor;
            if (trackingStatusText != null)
                trackingStatusText.text = tracking ? "TRACKING ACTIVE" : "NO TRACKING";

            if (!bridge.IsConnected)
            {
                SetStats(string.Empty, string.Empty, string.Empty, string.Empty);
                directionText.text = string.Empty;
                feedbackText.text = bridge.Feedback;
                return;
            }

            if (bridge.ExerciseCompleted)
            {
                SetStats($"SET <color={Highlight}>{bridge.TotalSets}/{bridge.TotalSets}</color>",
                    $"REPS <color={Highlight}>{bridge.RepsPerSet}/{bridge.RepsPerSet}</color>",
                    $"LEFT <color={Highlight}>{bridge.RepsPerSide}/{bridge.RepsPerSide}</color>",
                    $"RIGHT <color={Highlight}>{bridge.RepsPerSide}/{bridge.RepsPerSide}</color>");
                directionText.text = "DONE";
                feedbackText.text = "Exercise complete!";
                return;
            }

            SetStats($"SET <color={Highlight}>{bridge.CurrentSet}/{bridge.TotalSets}</color>",
                $"REPS <color={Highlight}>{bridge.CurrentReps}/{bridge.RepsPerSet}</color>",
                $"LEFT <color={Highlight}>{bridge.LeftReps}/{bridge.RepsPerSide}</color>",
                $"RIGHT <color={Highlight}>{bridge.RightReps}/{bridge.RepsPerSide}</color>");

            directionText.text = $"BEND <color={Highlight}>{DirectionName(bridge.ExpectedDirection)}</color>";
            feedbackText.text = bridge.Feedback;
        }

        private void SetStats(string set, string reps, string left, string right)
        {
            setText.text = set;
            repsText.text = reps;
            leftText.text = left;
            rightText.text = right;
        }

        private static string DirectionName(int direction)
        {
            if (direction < 0) return "LEFT";
            if (direction > 0) return "RIGHT";
            return "ANY SIDE";
        }
    }
}
