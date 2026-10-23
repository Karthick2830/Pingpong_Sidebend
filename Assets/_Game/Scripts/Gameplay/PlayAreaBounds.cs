using UnityEngine;

namespace BallCatch.Gameplay
{
    public enum TargetSide
    {
        Right,
        Left
    }

    /// <summary>
    /// Single source of truth for play-area geometry. The inner play area spans X 0..Width and Y 0..Height
    /// (matching the specification's nominal target ranges); walls sit outside that rectangle.
    /// All other gameplay scripts query this component instead of hardcoding coordinates.
    /// </summary>
    public class PlayAreaBounds : MonoBehaviour
    {
        [Header("Play Area (inner size, world units)")]
        [SerializeField] private float width = 10f;
        [SerializeField] private float height = 10f;
        [SerializeField] private float wallThickness = 1f;
        [SerializeField] private float zPosition = 0f;

        [Header("Ball")]
        [SerializeField] private float ballRadius = 0.3f;
        [SerializeField, Tooltip("Gap between the top wall and the ball when it spawns.")]
        private float launchOffset = 0.1f;

        [Header("Player")]
        [SerializeField] private float playerHalfWidth = 0.9f;
        [SerializeField] private float playerHeightAboveGround = 0.6f;
        [SerializeField] private float playerHalfHeight = 0.2f;

        [Header("Target Zones (nominal X ranges)")]
        [SerializeField] private Vector2 leftTargetRange = new Vector2(0f, 2.5f);
        [SerializeField] private Vector2 rightTargetRange = new Vector2(8f, 10f);

        public float Width => width;
        public float Height => height;
        public float WallThickness => wallThickness;
        public float ZPosition => zPosition;
        public float BallRadius => ballRadius;
        public float PlayerHalfWidth => playerHalfWidth;

        public float LeftX => 0f;
        public float RightX => width;
        public float GroundY => 0f;
        public float TopY => height;
        public float CenterX => width * 0.5f;

        /// <summary>Ball spawn point: top-center, just below the top wall.</summary>
        public Vector3 TopCenterLaunchPoint =>
            new Vector3(CenterX, TopY - ballRadius - launchOffset, zPosition);

        /// <summary>Player start position: bottom-center at the configured height.</summary>
        public Vector3 PlayerStartPosition => new Vector3(CenterX, PlayerY, zPosition);

        public float PlayerY => GroundY + playerHeightAboveGround;
        /// <summary>Ball-center Y when the ball rests on top of the player; trajectories are aimed here.</summary>
        public float CatchY => PlayerY + playerHalfHeight + ballRadius;
        public float PlayerMinX => LeftX + playerHalfWidth;
        public float PlayerMaxX => RightX - playerHalfWidth;

        /// <summary>Ball-center X when touching the given wall's inner face.</summary>
        public float WallContactX(TargetSide wallSide) =>
            wallSide == TargetSide.Left ? LeftX + ballRadius : RightX - ballRadius;

        /// <summary>Valid ball-center X range for a target zone, inset by the ball radius.</summary>
        public Vector2 GetTargetRange(TargetSide side)
        {
            Vector2 nominal = side == TargetSide.Left ? leftTargetRange : rightTargetRange;
            float min = Mathf.Max(nominal.x, LeftX + ballRadius);
            float max = Mathf.Min(nominal.y, RightX - ballRadius);
            return new Vector2(min, Mathf.Max(min, max));
        }

        private void OnValidate()
        {
            width = Mathf.Max(1f, width);
            height = Mathf.Max(1f, height);
            wallThickness = Mathf.Max(0.1f, wallThickness);
            ballRadius = Mathf.Max(0.05f, ballRadius);
            playerHalfWidth = Mathf.Max(0.1f, playerHalfWidth);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Vector3 center = new Vector3(CenterX, height * 0.5f, zPosition);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(TopCenterLaunchPoint, ballRadius);

            Gizmos.color = Color.cyan;
            DrawZone(GetTargetRange(TargetSide.Left));
            DrawZone(GetTargetRange(TargetSide.Right));
        }

        private void DrawZone(Vector2 range)
        {
            Vector3 a = new Vector3(range.x, GroundY + ballRadius, zPosition);
            Vector3 b = new Vector3(range.y, GroundY + ballRadius, zPosition);
            Gizmos.DrawLine(a, b);
        }
    }
}
