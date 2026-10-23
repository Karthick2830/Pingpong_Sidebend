using UnityEngine;

namespace BallCatch.Gameplay
{
    /// <summary>
    /// Keeps the whole play frame visible on any screen aspect ratio. The camera never zooms in past the base
    /// size, widens when the screen is too narrow for the play area, and always leaves a strip above the top
    /// wall for the HUD.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private PlayAreaBounds playArea;
        [SerializeField, Min(1f), Tooltip("Orthographic size used whenever the screen is wide enough.")]
        private float baseOrthographicSize = 6.6f;
        [SerializeField, Min(0f), Tooltip("Visible world units left and right of the frame.")]
        private float sideMargin = 0.75f;
        [SerializeField, Min(0f), Tooltip("World units between the frame's top edge and the top of the screen (HUD strip).")]
        private float topMargin = 1.83f;
        [SerializeField, Min(0f), Tooltip("Thickness of the visible frame drawn outside the play area.")]
        private float frameThickness = 0.25f;

        private Camera cam;
        private float appliedAspect;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (playArea == null)
                playArea = FindFirstObjectByType<PlayAreaBounds>();
            Apply();
        }

        private void LateUpdate()
        {
            if (!Mathf.Approximately(cam.aspect, appliedAspect))
                Apply();
        }

        private void Apply()
        {
            appliedAspect = cam.aspect;

            float halfFrameWidth = playArea.Width * 0.5f + frameThickness;
            float widthFitSize = (halfFrameWidth + sideMargin) / cam.aspect;
            float size = Mathf.Max(baseOrthographicSize, widthFitSize);

            float frameTop = playArea.TopY + frameThickness;
            cam.orthographicSize = size;
            cam.transform.position = new Vector3(playArea.CenterX, frameTop + topMargin - size, cam.transform.position.z);
        }
    }
}
