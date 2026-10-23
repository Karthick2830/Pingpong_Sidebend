using UnityEngine;

namespace BallCatch.Gameplay
{
    /// <summary>
    /// Visual-only ball feel: squash-and-stretch on impacts (applied to a child mesh so the collider is untouched),
    /// layered trails and sparks while in flight, and a directional spark spray on every impact.
    /// Trails and sparks are cleared whenever the ball is repositioned.
    /// </summary>
    [RequireComponent(typeof(BallController))]
    public class BallFeedback : MonoBehaviour
    {
        [SerializeField] private Transform visual;

        [Header("Trail Effects")]
        [SerializeField] private TrailRenderer[] trails;
        [SerializeField, Tooltip("Particle systems that emit behind the ball while it travels (sparks, twinkling sparkles).")]
        private ParticleSystem[] trailParticles;
        [SerializeField, Tooltip("Particle system used for the impact spray (emission rate should be 0).")]
        private ParticleSystem impactBurst;
        [SerializeField, Tooltip("Twinkling star particles added to every impact (emission rate should be 0).")]
        private ParticleSystem impactSparkle;

        [Header("Squash & Stretch")]
        [SerializeField, Min(0.01f)] private float squashDuration = 0.2f;
        [SerializeField, Range(0f, 0.9f)] private float squashStrength = 0.4f;

        [Header("Impact Spray")]
        [SerializeField, Min(0)] private int wallBurstCount = 14;
        [SerializeField, Min(0)] private int playerBurstCount = 22;
        [SerializeField, Min(0)] private int groundBurstCount = 10;
        [SerializeField] private Vector2 burstSpeedRange = new Vector2(2f, 6f);
        [SerializeField, Min(0), Tooltip("Sparkle stars emitted per impact.")]
        private int sparkleBurstCount = 8;

        private BallController ball;
        private float squashTimer;
        private Vector2 squashNormal = Vector2.up;

        private void Awake()
        {
            ball = GetComponent<BallController>();
        }

        private void OnEnable()
        {
            ball.Launched += HandleLaunched;
            ball.WallBounced += HandleWallBounce;
            ball.PlayerHit += HandlePlayerHit;
            ball.GroundHit += HandleGroundHit;
            ball.Parked += HandleParked;
        }

        private void OnDisable()
        {
            ball.Launched -= HandleLaunched;
            ball.WallBounced -= HandleWallBounce;
            ball.PlayerHit -= HandlePlayerHit;
            ball.GroundHit -= HandleGroundHit;
            ball.Parked -= HandleParked;
        }

        private void Update()
        {
            if (squashTimer <= 0f || visual == null)
                return;

            squashTimer -= Time.deltaTime;
            if (squashTimer <= 0f)
            {
                visual.localScale = Vector3.one;
                return;
            }

            // 0 -> 1 -> 0 over the duration: compress along the impact normal, bulge across it.
            float amount = Mathf.Sin((1f - squashTimer / squashDuration) * Mathf.PI) * squashStrength;
            float nx = Mathf.Abs(squashNormal.x);
            float ny = Mathf.Abs(squashNormal.y);
            visual.localScale = new Vector3(1f + amount * (0.5f * ny - nx), 1f + amount * (0.5f * nx - ny), 1f);
        }

        private void HandleLaunched()
        {
            SetTrailsEmitting(true);
            foreach (ParticleSystem particles in trailParticles)
                particles.Play();
        }

        private void HandleWallBounce()
        {
            // After the bounce the velocity points away from the wall, which is the impact normal.
            Vector2 normal = new Vector2(Mathf.Sign(ball.Velocity.x), 0f);
            StartSquash(normal);
            SprayAway(normal, wallBurstCount);
        }

        private void HandlePlayerHit()
        {
            StartSquash(Vector2.up);
            SprayAway(Vector2.up, playerBurstCount);
            StopSparks();
        }

        private void HandleGroundHit()
        {
            StartSquash(Vector2.up);
            SprayAway(Vector2.up, groundBurstCount);
            StopSparks();
        }

        private void HandleParked()
        {
            squashTimer = 0f;
            if (visual != null)
                visual.localScale = Vector3.one;

            SetTrailsEmitting(false);
            foreach (TrailRenderer trail in trails)
                trail.Clear();

            foreach (ParticleSystem particles in trailParticles)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (impactBurst != null)
                impactBurst.Clear(true);
            if (impactSparkle != null)
                impactSparkle.Clear(true);
        }

        private void SetTrailsEmitting(bool emitting)
        {
            foreach (TrailRenderer trail in trails)
                trail.emitting = emitting;
        }

        private void StopSparks()
        {
            foreach (ParticleSystem particles in trailParticles)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void StartSquash(Vector2 normal)
        {
            squashNormal = normal;
            squashTimer = squashDuration;
        }

        /// <summary>Sprays sparks in a cone around the impact normal.</summary>
        private void SprayAway(Vector2 normal, int count)
        {
            var emit = new ParticleSystem.EmitParams { position = transform.position };
            if (impactBurst != null)
                EmitCone(impactBurst, emit, normal, count);
            if (impactSparkle != null)
                EmitCone(impactSparkle, emit, normal, sparkleBurstCount);
        }

        private void EmitCone(ParticleSystem system, ParticleSystem.EmitParams emit, Vector2 normal, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = (normal + Random.insideUnitCircle * 0.9f).normalized;
                emit.velocity = direction * Random.Range(burstSpeedRange.x, burstSpeedRange.y);
                system.Emit(emit, 1);
            }
        }
    }
}
