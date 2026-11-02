using BallCatch.Audio;
using BallCatch.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace BallCatch.UI
{
    /// <summary>
    /// Shows exercise progress next to the existing ball score: stat chips in the top bar, a framed live tracking
    /// preview, and a bottom status bar (tracking state, next side, validator feedback). When a rep, side or set
    /// count goes up, the matching chip pops and a short sound plays.
    /// Read-only: it never changes game or exercise state.
    /// </summary>
    public class ExerciseProgressHUD : MonoBehaviour
    {
        private const string Highlight = "#FFD54F";
        private static readonly Color ActiveColor = new Color(0.25f, 0.9f, 0.35f);
        private static readonly Color LostColor = new Color(0.95f, 0.3f, 0.3f);

        private const int SetChip = 0, RepsChip = 1, LeftChip = 2, RightChip = 3;

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

        [Header("Count-up feedback")]
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private AudioClip repClip;
        [SerializeField] private AudioClip setClip;
        [SerializeField, Min(0.05f)] private float punchSeconds = 0.4f;
        [SerializeField, Min(0f)] private float punchScale = 0.45f;

        private struct Punch
        {
            public Text text;
            public Image border;
            public Color borderBase;
            public Vector3 baseScale;
            public float remaining;
        }

        private readonly Punch[] punches = new Punch[4];
        private int prevSet, prevReps, prevLeft, prevRight;
        private float lastRepSoundTime = -10f;

        private void Awake()
        {
            InitPunch(SetChip, setText);
            InitPunch(RepsChip, repsText);
            InitPunch(LeftChip, leftText);
            InitPunch(RightChip, rightText);
        }

        private void InitPunch(int index, Text text)
        {
            punches[index].text = text;
            punches[index].baseScale = text.transform.localScale;
            // text -> Fill -> chip root (the bordered frame)
            Transform root = text.transform.parent != null ? text.transform.parent.parent : null;
            Image border = root != null ? root.GetComponent<Image>() : null;
            punches[index].border = border;
            punches[index].borderBase = border != null ? border.color : Color.white;
        }

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

            UpdatePunches();

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

            DetectCountUps();

            SetStats($"SET <color={Highlight}>{bridge.CurrentSet}/{bridge.TotalSets}</color>",
                $"REPS <color={Highlight}>{bridge.CurrentReps}/{bridge.RepsPerSet}</color>",
                $"LEFT <color={Highlight}>{bridge.LeftReps}/{bridge.RepsPerSide}</color>",
                $"RIGHT <color={Highlight}>{bridge.RightReps}/{bridge.RepsPerSide}</color>");

            directionText.text = $"BEND <color={Highlight}>{DirectionName(bridge.ExpectedDirection)}</color>";
            feedbackText.text = bridge.Feedback;
        }

        /// <summary>Pops the chip(s) whose count went up. Decreases (resets, new set, restart) do nothing.</summary>
        private void DetectCountUps()
        {
            int set = bridge.CurrentSet, reps = bridge.CurrentReps;
            int left = bridge.LeftReps, right = bridge.RightReps;

            // prevSet >= 1 skips the 0 -> 1 jump when an exercise starts.
            bool setUp = prevSet >= 1 && set > prevSet;
            if (setUp)
            {
                Trigger(SetChip);
                Trigger(RepsChip);
                Play(setClip);
            }
            else
            {
                bool repUp = reps > prevReps, leftUp = left > prevLeft, rightUp = right > prevRight;
                if (repUp) Trigger(RepsChip);
                if (leftUp) Trigger(LeftChip);
                if (rightUp) Trigger(RightChip);

                // Left/right and total reps can change a frame apart; one sound per rep.
                if ((repUp || leftUp || rightUp) && Time.unscaledTime - lastRepSoundTime > 0.25f)
                {
                    lastRepSoundTime = Time.unscaledTime;
                    Play(repClip);
                }
            }

            prevSet = set;
            prevReps = reps;
            prevLeft = left;
            prevRight = right;
        }

        private void Play(AudioClip clip)
        {
            if (audioManager != null && clip != null)
                audioManager.PlaySfx(clip);
        }

        private void Trigger(int index) => punches[index].remaining = punchSeconds;

        private void UpdatePunches()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = 0; i < punches.Length; i++)
            {
                Punch p = punches[i];
                if (p.text == null)
                    continue;

                if (p.remaining > 0f)
                    p.remaining = Mathf.Max(0f, p.remaining - dt);

                // 0 -> 1 -> 0 over the punch: grow, then settle back.
                float k = p.remaining > 0f ? Mathf.Sin((1f - p.remaining / punchSeconds) * Mathf.PI) : 0f;
                p.text.transform.localScale = p.baseScale * (1f + punchScale * k);
                if (p.border != null)
                    p.border.color = Color.Lerp(p.borderBase, Color.white, k);

                punches[i] = p;
            }
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
