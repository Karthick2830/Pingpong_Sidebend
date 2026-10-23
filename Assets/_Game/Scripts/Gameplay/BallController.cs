using System;
using UnityEngine;

namespace BallCatch.Gameplay
{
    public enum BallPhase
    {
        Parked,
        ToWall,
        ToTarget,
        Resolved
    }

    /// <summary>
    /// Drives the ball with Rigidbody velocity only (gravity off). Each attempt picks a wall-hit point and a
    /// target point up front, launches toward the opposite wall, and on the single intended wall contact flips
    /// the horizontal direction and redirects toward the target at the same speed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private PlayAreaBounds playArea;

        [Header("Speed")]
        [SerializeField, Min(0.1f)] private float launchSpeed = 9f;
        [SerializeField] private bool varySpeed;
        [SerializeField, Min(0f), Tooltip("Max +/- deviation from launch speed when Vary Speed is on.")]
        private float speedVariation = 1f;

        [Header("Trajectory Variation")]
        [SerializeField, Tooltip("Allowed world-Y range for the wall impact (ball center).")]
        private Vector2 wallHitHeightRange = new Vector2(3.5f, 8f);
        [SerializeField, Min(0f), Tooltip("Keeps the wall impact away from the top wall and the catch height.")]
        private float wallHitMargin = 0.5f;

        [Header("Catch Presentation (visual only, after the catch is scored)")]
        [SerializeField, Min(0f)] private float catchBounceSpeed = 4f;
        [SerializeField, Min(0f), Tooltip("Sideways kick when the ball hits the edge of the player.")]
        private float catchDeflectSpeed = 3f;

        private Rigidbody body;
        private TargetSide targetSide;
        private Vector3 wallPoint;
        private Vector3 targetPoint;
        private float currentSpeed;

        public event Action Parked;
        public event Action Launched;
        public event Action WallBounced;
        /// <summary>Raised once per attempt when the ball touches the player before the ground.</summary>
        public event Action PlayerHit;
        /// <summary>Raised once per attempt when the ball reaches the ground uncaught.</summary>
        public event Action GroundHit;

        public BallPhase Phase { get; private set; } = BallPhase.Parked;
        public TargetSide TargetSide => targetSide;
        /// <summary>The wall the ball must hit: the side opposite the target.</summary>
        public TargetSide IntendedWall => targetSide == TargetSide.Right ? TargetSide.Left : TargetSide.Right;
        public int WallBounceCount { get; private set; }
        /// <summary>Wall contacts that were ignored because the one intended bounce was already used or the wall was wrong.</summary>
        public int IgnoredWallContacts { get; private set; }
        public Vector3 WallPoint => wallPoint;
        public Vector3 TargetPoint => targetPoint;
        public float CurrentSpeed => currentSpeed;
        public Vector3 Velocity => body.linearVelocity;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (playArea == null)
                playArea = FindFirstObjectByType<PlayAreaBounds>();
            body.useGravity = false;
            Park();
        }

        private bool IsInFlight => Phase == BallPhase.ToWall || Phase == BallPhase.ToTarget;

        /// <summary>Places the ball at top-center and clears all attempt state.</summary>
        public void Park()
        {
            Phase = BallPhase.Parked;
            WallBounceCount = 0;
            IgnoredWallContacts = 0;
            currentSpeed = 0f;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            Vector3 start = playArea.TopCenterLaunchPoint;
            body.position = start;
            transform.position = start;
            Parked?.Invoke();
        }

        /// <summary>Starts a new attempt toward <paramref name="target"/> with a fresh randomized trajectory.</summary>
        public void Launch(TargetSide target)
        {
            Park();
            targetSide = target;

            currentSpeed = launchSpeed;
            if (varySpeed)
                currentSpeed += UnityEngine.Random.Range(-speedVariation, speedVariation);
            currentSpeed = Mathf.Max(0.1f, currentSpeed);

            Vector2 targetRange = playArea.GetTargetRange(target);
            targetPoint = new Vector3(UnityEngine.Random.Range(targetRange.x, targetRange.y),
                playArea.CatchY, playArea.ZPosition);

            float minY = playArea.CatchY + wallHitMargin;
            float maxY = playArea.TopCenterLaunchPoint.y - wallHitMargin;
            float wallY = Mathf.Clamp(UnityEngine.Random.Range(wallHitHeightRange.x, wallHitHeightRange.y),
                minY, Mathf.Max(minY, maxY));
            wallPoint = new Vector3(playArea.WallContactX(IntendedWall), wallY, playArea.ZPosition);

            Phase = BallPhase.ToWall;
            body.linearVelocity = (wallPoint - transform.position).normalized * currentSpeed;
            Launched?.Invoke();
        }

        /// <summary>Stops the ball in place; later contacts are ignored until the next launch.</summary>
        private void Resolve()
        {
            Phase = BallPhase.Resolved;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        /// <summary>Presentation only: the catch is already decided, the ball just bounces off the paddle.</summary>
        private void HopOffPlayer(PlayerController hitPlayer)
        {
            float offset = (body.position.x - hitPlayer.transform.position.x) / playArea.PlayerHalfWidth;
            body.useGravity = true;
            body.linearVelocity = new Vector3(Mathf.Clamp(offset, -1f, 1f) * catchDeflectSpeed, catchBounceSpeed, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out GroundSurface _))
                return;

            if (Phase == BallPhase.Resolved)
            {
                // A caught ball that hops off the player must still stop at the ground.
                RestOnGround();
                return;
            }

            if (!IsInFlight)
                return;

            Resolve();
            RestOnGround();
            GroundHit?.Invoke();
        }

        /// <summary>Keeps only the vertical motion after a non-bounce wall touch, restoring the constant speed.</summary>
        private void SlideAlongWall()
        {
            float direction = body.linearVelocity.y < 0f ? -1f : 1f;
            body.linearVelocity = new Vector3(0f, direction * currentSpeed, 0f);
        }

        /// <summary>Stops the ball and seats it on top of the ground so it never sinks into it.</summary>
        private void RestOnGround()
        {
            Vector3 position = body.position;
            position.y = playArea.GroundY + playArea.BallRadius;
            body.position = position;
            transform.position = position;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsInFlight)
                return;

            if (collision.collider.TryGetComponent(out PlayerController hitPlayer))
            {
                Resolve();
                HopOffPlayer(hitPlayer);
                PlayerHit?.Invoke();
                return;
            }

            if (!collision.collider.TryGetComponent(out WallSurface wall))
                return;

            if (Phase != BallPhase.ToWall || wall.Side != IntendedWall)
            {
                // Never a second bounce: just slide down the wall at the attempt's constant speed.
                IgnoredWallContacts++;
                SlideAlongWall();
                return;
            }

            // The one intended bounce: flip the horizontal direction and head for the target at constant speed.
            WallBounceCount++;
            Phase = BallPhase.ToTarget;
            Vector3 contact = body.position;
            Vector3 toTarget = targetPoint - contact;
            body.linearVelocity = toTarget.normalized * currentSpeed;
            WallBounced?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (Phase == BallPhase.Parked || playArea == null)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(playArea.TopCenterLaunchPoint, wallPoint);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(wallPoint, targetPoint);
        }
    }
}
