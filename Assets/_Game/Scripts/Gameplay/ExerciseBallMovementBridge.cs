using BallCatch.Core;
using ExerciseGame.Core.Exercise;
using ExerciseGame.Core.Pose;
using ExerciseGame.Exercises.Debug;
using ExerciseGame.Exercises.SideBend;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BallCatch.Gameplay
{
    /// <summary>
    /// Connects the existing exercise system (SideBendValidator, ExerciseManager, PoseDataProvider) to the ball
    /// game. Loads the ExerciseTracking scene additively (once), reuses the objects found there, and each frame
    /// maps the validator's already-validated LEFT/RIGHT direction to the player's movement command.
    /// It never reads landmarks, never validates, and never resets the player or ball: the existing per-turn
    /// reset in GameManager is untouched. It only clears its own command when the ball is parked.
    /// </summary>
    public class ExerciseBallMovementBridge : MonoBehaviour
    {
        [Header("Game")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlayerController player;
        [SerializeField] private BallController ball;

        [Header("Exercise Tracking Scene")]
        [SerializeField] private string trackingSceneName = "ExerciseTracking";
        [SerializeField, Tooltip("Tracking preview render-texture size (shown in the HUD corner).")]
        private Vector2Int previewSize = new Vector2Int(640, 360);
        [SerializeField, Min(0.01f), Tooltip("Skeleton joint size in the preview.")]
        private float skeletonLandmarkRadius = 0.4f;
        [SerializeField, Min(0.01f), Tooltip("Skeleton bone width in the preview.")]
        private float skeletonConnectionWidth = 0.1f;

        [Header("Exercise Target")]
        [SerializeField, Min(1)] private int repsPerSet = 10;
        [SerializeField, Min(1), Tooltip("Total sets; 0 keeps the value configured in the tracking scene.")]
        private int totalSets = 0;

        [Header("Safety")]
        [SerializeField, Min(0.1f), Tooltip("Stop moving if no new pose frame arrives for this many seconds (real time).")]
        private float stalePoseSeconds = 0.5f;

        private SideBendValidator validator;
        private ExerciseManager exerciseManager;
        private ExerciseValidationTester tester;
        private PoseDataProvider poseProvider;

        private bool loading;
        private bool connected;
        private bool exerciseStarted;
        private bool wasGameplayActive;
        private bool suppressUntilNeutral;
        private int lastFrameCount = -1;
        private float lastFrameRealtime;
        private int observedSet = 1;

        public bool IsConnected => connected;
        public RenderTexture PreviewTexture { get; private set; }
        /// <summary>True while a person is tracked and pose frames are arriving.</summary>
        public bool TrackingActive { get; private set; }
        public int CurrentSet => exerciseManager != null ? exerciseManager.CurrentSet : 0;
        public int TotalSets => exerciseManager != null ? exerciseManager.TargetSets : 0;
        public int CurrentReps => exerciseManager != null ? exerciseManager.CurrentRep : 0;
        public int RepsPerSet => exerciseManager != null ? exerciseManager.TargetRepsPerSet : repsPerSet;
        public int LeftReps { get; private set; }
        public int RightReps { get; private set; }
        public int RepsPerSide => RepsPerSet / 2;
        public bool ExerciseCompleted => exerciseManager != null && exerciseManager.IsExerciseCompleted;
        /// <summary>-1 LEFT, +1 RIGHT, 0 any/none.</summary>
        public int ExpectedDirection => validator != null ? validator.ExpectedDirection : 0;
        public string Feedback
        {
            get
            {
                if (!connected)
                    return loading ? "Loading exercise tracking..." : "Exercise tracking unavailable.";
                return tester != null ? tester.LastResult.feedback : string.Empty;
            }
        }

        private void Awake()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
            if (player == null)
                player = FindFirstObjectByType<PlayerController>();
            if (ball == null)
                ball = FindFirstObjectByType<BallController>();
        }

        private void OnEnable()
        {
            // Parked fires inside every turn reset (and restart): clear the command and wait for a fresh movement.
            ball.Parked += OnBallParked;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            ball.Parked -= OnBallParked;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Unsubscribe();
            player.SetExternalMoveDirection(0f);
        }

        private void Start()
        {
            Scene tracking = SceneManager.GetSceneByName(trackingSceneName);
            if (tracking.isLoaded)
            {
                Connect(tracking);
                return;
            }

            loading = true;
            SceneManager.LoadSceneAsync(trackingSceneName, LoadSceneMode.Additive);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (connected || scene.name != trackingSceneName)
                return;
            Connect(scene);
        }

        private void Connect(Scene scene)
        {
            loading = false;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent(out SideBendValidator foundValidator)) validator = foundValidator;
                if (root.TryGetComponent(out ExerciseManager foundManager)) exerciseManager = foundManager;
                if (root.TryGetComponent(out ExerciseValidationTester foundTester)) tester = foundTester;
                if (root.TryGetComponent(out PoseDataProvider foundProvider)) poseProvider = foundProvider;

                // The game owns the camera, lighting, audio listener and event system.
                if (root.TryGetComponent(out Camera _) || root.TryGetComponent(out Light _)
                    || root.TryGetComponent(out UnityEngine.EventSystems.EventSystem _))
                    root.SetActive(false);
                else if (root.TryGetComponent(out Canvas canvas))
                    ConfigurePreview(canvas);

                // Debug overlays (OnGUI) must not draw over the game.
                if (root.TryGetComponent(out ExerciseGame.Core.UI.ExerciseUIDataDebug uiDebug))
                    uiDebug.enabled = false;
            }

            if (tester != null)
            {
                tester.ShowDebugPanel = false;
                if (tester.TryGetComponent(out PoseVisibilityDebugger visibilityDebugger))
                    visibilityDebugger.enabled = false;
            }

            if (validator == null || exerciseManager == null || tester == null || poseProvider == null)
            {
                Debug.LogError("[ExerciseBallMovementBridge] ExerciseTracking scene is missing the validator, " +
                               "manager, tester or pose provider; exercise movement is disabled.");
                return;
            }

            exerciseManager.SetExerciseTarget(repsPerSet, totalSets > 0 ? totalSets : exerciseManager.TargetSets);
            validator.RepConfirmed += OnRepConfirmed;

            // ExerciseManager auto-starts in its own scene; hold it until real gameplay begins.
            exerciseManager.PauseExercise();
            exerciseStarted = false;
            connected = true;
        }

        /// <summary>
        /// Renders the tracking canvas (webcam image + skeleton) with its own camera into a small texture
        /// that the HUD shows in a corner, instead of letting it overlay the whole game.
        /// The camera sits far from the play area so the game camera never sees the canvas.
        /// </summary>
        private void ConfigurePreview(Canvas canvas)
        {
            // Only the camera view is wanted: hide the tracking scene's header, footer and labels.
            foreach (string name in new[] { "Header", "Footer" })
            {
                Transform part = canvas.transform.Find("Container Panel/" + name);
                if (part != null)
                    part.gameObject.SetActive(false);
            }
            foreach (TMPro.TMP_Text label in canvas.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (label.transform.parent != null && label.transform.parent.name == "Body")
                    label.gameObject.SetActive(false);
            }

            var skeleton = canvas.GetComponentInChildren<Mediapipe.Unity.MultiPoseLandmarkListWithMaskAnnotation>(true);
            if (skeleton != null)
            {
                skeleton.SetLandmarkRadius(skeletonLandmarkRadius);
                skeleton.SetConnectionWidth(skeletonConnectionWidth);
            }

            PreviewTexture = new RenderTexture(previewSize.x, previewSize.y, 16);

            var cameraObject = new GameObject("TrackingPreviewCamera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.position = new Vector3(0f, -5000f, 0f);
            Camera previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = 5f;
            previewCamera.nearClipPlane = 0.1f;
            previewCamera.farClipPlane = 50f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.black;
            previewCamera.depth = -10f;
            previewCamera.targetTexture = PreviewTexture;

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = previewCamera;
            canvas.planeDistance = 10f;
        }

        private void OnDestroy()
        {
            if (PreviewTexture != null)
            {
                PreviewTexture.Release();
                Destroy(PreviewTexture);
            }
        }

        private void Unsubscribe()
        {
            if (validator != null)
                validator.RepConfirmed -= OnRepConfirmed;
        }

        private void OnBallParked()
        {
            player.SetExternalMoveDirection(0f);
            suppressUntilNeutral = true;
        }

        private void OnRepConfirmed(int direction)
        {
            if (exerciseManager == null || !exerciseManager.IsRunning || exerciseManager.IsPaused
                || exerciseManager.IsExerciseCompleted)
                return;

            if (direction < 0) LeftReps++;
            else if (direction > 0) RightReps++;
        }

        private void Update()
        {
            if (!connected)
                return;

            bool poseFresh = IsPoseFresh();
            TrackingActive = poseFresh;

            bool active = gameManager.State == GameState.Playing
                || gameManager.State == GameState.AttemptTransition;

            UpdateExerciseLifecycle(active);
            SyncSetCounters();

            int direction = 0;
            if (active && exerciseStarted && poseFresh)
            {
                direction = validator.ValidatedMoveDirection;
                if (direction == 0)
                    suppressUntilNeutral = false;
                else if (suppressUntilNeutral)
                    direction = 0;
            }

            player.SetExternalMoveDirection(direction);
        }

        private void UpdateExerciseLifecycle(bool active)
        {
            if (active && !wasGameplayActive)
            {
                if (!exerciseStarted)
                {
                    exerciseManager.StartExercise();
                    exerciseStarted = true;
                    LeftReps = 0;
                    RightReps = 0;
                    observedSet = exerciseManager.CurrentSet;
                }
                else
                {
                    exerciseManager.ResumeExercise();
                }
            }
            else if (!active && wasGameplayActive)
            {
                if (gameManager.State == GameState.MainMenu)
                {
                    exerciseManager.ResetExercise();
                    exerciseStarted = false;
                    LeftReps = 0;
                    RightReps = 0;
                }
                else
                {
                    exerciseManager.PauseExercise();
                }
                suppressUntilNeutral = true;
            }

            wasGameplayActive = active;
        }

        private void SyncSetCounters()
        {
            // Per-side counters belong to a set; start fresh when the manager moves to the next set.
            if (exerciseManager.CurrentSet != observedSet)
            {
                observedSet = exerciseManager.CurrentSet;
                LeftReps = 0;
                RightReps = 0;
            }
        }

        private bool IsPoseFresh()
        {
            if (!poseProvider.HasPose)
                return false;

            // Real time, so this stays valid when Time.timeScale is 0.
            if (poseProvider.FrameCount != lastFrameCount)
            {
                lastFrameCount = poseProvider.FrameCount;
                lastFrameRealtime = Time.unscaledTime;
                return true;
            }

            return Time.unscaledTime - lastFrameRealtime <= stalePoseSeconds;
        }
    }
}
